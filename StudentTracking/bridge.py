"""DigiPhant camera -> local Unity UDP bridge. Run with --help for setup options."""
import argparse
from contextlib import ExitStack
import errno
import itertools
import json
import math
from pathlib import Path
import socket
import time
import uuid

FEATURES = ('LeftHandHeight', 'RightHandHeight', 'LeftFootLift', 'RightFootLift', 'Lean', 'ArmSpread')
MODEL_URL = 'https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_full/float16/latest/pose_landmarker_full.task'


def extract_features(landmarks, upper_body_only=False):
    """Body-relative image measurements; Unity subtracts the calibrated neutral pose."""
    def xy(i):
        p = landmarks[i]
        return p.x, p.y
    def quality(indices):
        scores = [v for i in indices for v in (landmarks[i].visibility, landmarks[i].presence)]
        return min(max(0., min(1., v)) if math.isfinite(v) else 0. for v in scores)
    if upper_body_only:
        ls, rs = xy(11), xy(12)
        shoulder = ((ls[0] + rs[0]) / 2, (ls[1] + rs[1]) / 2)
        scale = max(math.dist(ls, rs), .03)
        left = (shoulder[1] - xy(15)[1]) / scale
        right = (shoulder[1] - xy(16)[1]) / scale
        # Shoulder tilt replaces hip-relative lean; leg channels use hand heights.
        raw = [left, right, left, right, (ls[1] - rs[1]) / scale,
               abs(xy(15)[0] - xy(16)[0]) / (2 * scale)]
        groups = [[11, 12, 15], [11, 12, 16], [11, 12, 15],
                  [11, 12, 16], [11, 12], [11, 12, 15, 16]]
        return shoulder, [max(-3, min(3, v)) for v in raw], [quality(g) for g in groups]
    ls, rs, lh, rh = (xy(i) for i in (11, 12, 23, 24))
    shoulder = ((ls[0] + rs[0]) / 2, (ls[1] + rs[1]) / 2)
    hip = ((lh[0] + rh[0]) / 2, (lh[1] + rh[1]) / 2)
    scale = max(math.dist(shoulder, hip), .03)
    base = [11, 12, 23, 24]
    groups = [base + [15], base + [16], base + [27], base + [28], base, base + [15, 16]]
    raw = [(shoulder[1] - xy(15)[1]) / scale,
           (shoulder[1] - xy(16)[1]) / scale,
           (hip[1] - xy(27)[1]) / scale,
           (hip[1] - xy(28)[1]) / scale,
           (shoulder[0] - hip[0]) / scale,
           abs(xy(15)[0] - xy(16)[0]) / (2 * scale)]
    return hip, [max(-3, min(3, v)) for v in raw], [quality(g) for g in groups]


def hand_depth(image, world):
    """Anatomical L/R wrist depth; positive is toward camera, in shoulder widths."""
    values, confidence = [0., 0.], [0., 0.]
    if len(world) < 33:
        return values, confidence
    width = math.dist((world[11].x, world[11].y, world[11].z),
                      (world[12].x, world[12].y, world[12].z))
    if not math.isfinite(width) or width < .01:
        return values, confidence
    for side, (shoulder, wrist) in enumerate(((11, 15), (12, 16))):
        value = (world[shoulder].z - world[wrist].z) / width
        quality = min(min(image[i].visibility, image[i].presence) for i in (11, 12, wrist))
        if math.isfinite(value) and math.isfinite(quality):
            values[side] = max(-3., min(3., value))
            confidence[side] = max(0., min(1., quality))
    return values, confidence


def palm_facing(world):
    """Raw palm-normal z; Unity calibrates its sign separately for each hand."""
    if len(world) < 18:
        return None
    a = [getattr(world[5], axis) - getattr(world[0], axis) for axis in ('x', 'y', 'z')]
    b = [getattr(world[17], axis) - getattr(world[0], axis) for axis in ('x', 'y', 'z')]
    n = (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
    length = math.sqrt(sum(v*v for v in n))
    return n[2] / length if math.isfinite(length) and length > 1e-8 else None


def palm_scale(hand, pose, aspect=1.):
    """Apparent palm size / shoulder span; perspective proxy, not metric depth."""
    if len(hand) < 18 or len(pose) < 13:
        return 0.
    def distance(a, b):
        return math.hypot((a.x-b.x)*aspect, a.y-b.y)
    width = distance(pose[11], pose[12])
    size = (distance(hand[0], hand[9]) + distance(hand[5], hand[17])) / 2
    return size / width if math.isfinite(size) and math.isfinite(width) and width > .03 else 0.


def match_hands(poses, hands, worlds, max_distance=.45, margin=.12, aspect=1.):
    """Match image wrists against ALL bodies; reject close rivals and duplicates.

    Keys are (pose index, anatomical side). No handedness label or performer
    assignment is used, so unassigned bystanders still compete for their hands.
    """
    wrists = []
    for person, pose in enumerate(poses):
        width = math.dist((pose[11].x, pose[11].y), (pose[12].x, pose[12].y))
        if not math.isfinite(width) or width < .03:
            continue
        for side, index in enumerate((15, 16)):
            quality = min(pose[index].visibility, pose[index].presence)
            if quality >= .5 and all(math.isfinite(v) for v in (pose[index].x, pose[index].y)):
                wrists.append(((person, side), pose[index], width, quality))
    proposals = {}
    for hand, world in zip(hands, worlds):
        facing = palm_facing(world)
        if not hand or facing is None:
            continue
        distances = sorted((math.dist((hand[0].x, hand[0].y), (wrist.x, wrist.y))/width,
                            key, quality) for key, wrist, width, quality in wrists)
        if not distances or not math.isfinite(distances[0][0]):
            continue
        distance, key, quality = distances[0]
        if distance > max_distance or (len(distances) > 1 and distances[1][0]-distance < margin):
            continue
        scale = palm_scale(hand, poses[key[0]], aspect)
        proposals.setdefault(key, []).append((facing, min(1., quality), scale))
    return {key: values[0] for key, values in proposals.items() if len(values) == 1}


class PerformerTracker:
    """Small-group nearest-position assignment with ambiguity rejection.

    This is not biometric identity. Crossings/long occlusions may need manual reset.
    """
    def __init__(self, count, max_distance=.20):
        if count not in (1, 2, 3, 4):
            raise ValueError("Choose 1, 2, 3, or 4 people")
        self.count = count
        self.max_distance = max_distance
        self.positions = None
        self.epoch = uuid.uuid4().hex

    def reset(self):
        self.positions = None
        self.epoch = uuid.uuid4().hex

    def assign(self, centers):
        if self.positions is None:
            if len(centers) < self.count:
                return {}
            order = sorted(range(len(centers)), key=lambda i: centers[i][0])[:self.count]
            self.positions = [centers[i] for i in order]
            return {slot + 1: index for slot, index in enumerate(order)}
        candidates = []
        for assignment in itertools.product(range(-1, len(centers)), repeat=self.count):
            used = [i for i in assignment if i >= 0]
            if len(set(used)) != len(used):
                continue
            distances = [math.dist(self.positions[s], centers[i]) for s, i in enumerate(assignment) if i >= 0]
            if any(d > self.max_distance for d in distances):
                continue
            candidates.append((-len(used), sum(distances), assignment))
        candidates.sort()
        best = candidates[0]
        # Keep only slots on which every near-optimal assignment agrees. Ambiguity
        # between other people should not interrupt a clearly tracked driver.
        # This remains nearest-position tracking, not biometric identity.
        plausible = [candidate[2] for candidate in candidates
                     if candidate[0] == best[0] and candidate[1] - best[1] < .015]
        result = {s + 1: i for s, i in enumerate(best[2])
                  if i >= 0 and all(assignment[s] == i for assignment in plausible)}
        for slot, index in result.items():
            self.positions[slot - 1] = centers[index]
        return result


def requested_count(data):
    """Validate Unity's local count request without changing tracking on bad input."""
    try:
        value = json.loads(data)
        if isinstance(value, dict) and value.get('version') == 1:
            count = value.get('performerCount')
            if type(count) is int and 1 <= count <= 4:
                return count
    except (ValueError, UnicodeError):
        pass
    return None


def encode_preview(frame, cv2):
    """Fit one low-latency JPEG into a local UDP datagram, preserving aspect ratio."""
    height, width = frame.shape[:2]
    scale = min(320 / width, 240 / height)
    preview = cv2.resize(frame, (max(1, round(width * scale)), max(1, round(height * scale))))
    # Stay below macOS's commonly configured 9216-byte UDP send limit.
    # JPEG size depends on scene detail, so reduce quality and then dimensions.
    for _ in range(4):
        for quality in (65, 45, 25):
            ok, encoded = cv2.imencode('.jpg', preview, [cv2.IMWRITE_JPEG_QUALITY, quality])
            if ok and len(encoded) <= 8000:
                return encoded.tobytes()
        h, w = preview.shape[:2]
        preview = cv2.resize(preview, (max(1, w // 2), max(1, h // 2)))
    return None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--no-mirror-preview', action='store_true', help='Show raw camera orientation; tracking coordinates are unchanged')
    parser.add_argument('--people', type=int, choices=(1, 2, 3, 4), default=3)
    parser.add_argument('--no-window', action='store_true', help='Preview is displayed inside Unity instead')
    parser.add_argument('--upper-body-only', action='store_true', help='Use shoulders and hands; feet and hips may be out of frame')
    parser.add_argument('--camera', type=int, default=0)
    parser.add_argument('--port', type=int, default=5055)
    parser.add_argument('--model', type=Path, default=Path(__file__).parent / 'pose_landmarker_full.task')
    parser.add_argument('--hand-model', type=Path, default=Path(__file__).parent / 'hand_landmarker.task')
    parser.add_argument('--download-model', action='store_true', help='Download the official model if missing')
    args = parser.parse_args()
    if not 1024 <= args.port <= 65533:
        parser.error('--port must be between 1024 and 65533')
    if not args.model.exists():
        if not args.download_model:
            parser.error('Model missing. Run again with --download-model (internet required).')
        import urllib.request
        args.model.parent.mkdir(parents=True, exist_ok=True)
        temporary = args.model.with_suffix('.download')
        print('Downloading the official MediaPipe pose model...')
        urllib.request.urlretrieve(MODEL_URL, temporary)
        temporary.replace(args.model)
    import cv2
    import mediapipe as mp
    from mediapipe.tasks import python
    from mediapipe.tasks.python import vision

    options = vision.PoseLandmarkerOptions(
        base_options=python.BaseOptions(model_asset_path=str(args.model)),
        running_mode=vision.RunningMode.VIDEO, num_poses=4,
        min_pose_detection_confidence=.5, min_pose_presence_confidence=.5,
        min_tracking_confidence=.5)
    tracker = PerformerTracker(args.people)
    camera = cv2.VideoCapture(args.camera)
    if not camera.isOpened():
        raise SystemExit('Camera could not open. Check permissions or try --camera 1.')
    sender = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        sender.bind(('127.0.0.1', args.port + 1))
        sender.setblocking(False)
    except OSError:
        camera.release()
        sender.close()
        raise SystemExit('Another camera bridge is already running. Close its preview with Q first.')
    edges = [(11, 12), (11, 23), (12, 24), (23, 24), (11, 13), (13, 15),
             (12, 14), (14, 16), (23, 25), (25, 27), (24, 26), (26, 28),
             (27, 29), (29, 31), (27, 31), (28, 30), (30, 32), (28, 32),
             (0, 7), (0, 8), (7, 11), (8, 12)]
    colors = [(100, 230, 100), (255, 180, 80), (100, 160, 255), (220, 100, 230)]
    previous_timestamp = -1
    last_preview = 0.0
    print('Stand side by side, all visible. Follow P labels; in mirrored preview P1 starts on the right. Tracking roles do not change.')
    print('R: reset identities (then recalibrate in Unity). Q: quit.')
    try:
        with ExitStack() as stack:
            detector = stack.enter_context(vision.PoseLandmarker.create_from_options(options))
            hand_detector = None
            if args.hand_model.exists():
                hand_options = vision.HandLandmarkerOptions(
                    base_options=python.BaseOptions(model_asset_path=str(args.hand_model)),
                    running_mode=vision.RunningMode.VIDEO, num_hands=8,
                    min_hand_detection_confidence=.5, min_hand_presence_confidence=.5,
                    min_tracking_confidence=.5)
                hand_detector = stack.enter_context(vision.HandLandmarker.create_from_options(hand_options))
            else:
                print('Hand model missing: hand navigation unavailable; legacy pose controls still work. '
                      'Place hand_landmarker.task beside bridge.py or specify --hand-model.', flush=True)
            while True:
                for _ in range(32):
                    try:
                        data, address = sender.recvfrom(1024)
                    except BlockingIOError:
                        break
                    if address == ('127.0.0.1', args.port):
                        try:
                            command = json.loads(data)
                            if isinstance(command, dict) and command.get('version') == 1:
                                upper = command.get('upperBodyOnly')
                                if type(upper) is bool and upper != args.upper_body_only:
                                    args.upper_body_only = upper
                                    tracker.reset()
                                    print('Movement mode changed. Set neutral pose in Unity.')
                                if command.get('reset') is True:
                                    tracker.reset()
                        except (ValueError, UnicodeError):
                            pass
                    count = requested_count(data) if address == ('127.0.0.1', args.port) else None
                    if count is not None and count != args.people:
                        args.people = count
                        tracker = PerformerTracker(count)
                        print(f'Unity selected {count} people. Reassigning; set neutral pose in Unity.')
                ok, frame = camera.read()
                if not ok:
                    raise RuntimeError('Camera stopped delivering frames.')
                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                timestamp = max(previous_timestamp + 1, time.monotonic_ns() // 1_000_000)
                previous_timestamp = timestamp
                result = detector.detect_for_video(mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb), timestamp)
                hand_matches = {}
                if hand_detector is not None:
                    hand_result = hand_detector.detect_for_video(
                        mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb), timestamp)
                    hand_matches = match_hands(result.pose_landmarks,
                                               hand_result.hand_landmarks, hand_result.hand_world_landmarks,
                                               aspect=frame.shape[1]/frame.shape[0])
                observations = []
                for person, landmarks in enumerate(result.pose_landmarks):
                    center, values, confidence = extract_features(landmarks, args.upper_body_only)
                    if confidence[4] >= .5:
                        world = result.pose_world_landmarks[person] if person < len(result.pose_world_landmarks) else []
                        depths, depth_confidence = hand_depth(landmarks, world)
                        palm = [hand_matches.get((person, side), (0., 0., 0.)) for side in (0, 1)]
                        extras = dict(handDepth=depths, handDepthConfidence=depth_confidence,
                                      palmFacing=[v[0] for v in palm], palmConfidence=[v[1] for v in palm],
                                      handScale=[v[2] for v in palm],
                                      handScaleConfidence=[v[1] if v[2] > 0 else 0. for v in palm])
                        observations.append((center, values, confidence, landmarks, extras))
                assignments = tracker.assign([o[0] for o in observations])
                packet = {'version': 1, 'trackingEpoch': tracker.epoch, 'performerCount': args.people, 'upperBodyOnly': args.upper_body_only, 'people': [dict(slot=slot, values=observations[i][1], confidence=observations[i][2], **observations[i][4])
                                                 for slot, i in assignments.items()]}
                sender.sendto(json.dumps(packet, allow_nan=False).encode(), ('127.0.0.1', args.port))
                # Inference and identity assignment use raw coordinates; mirror display only.
                if not args.no_mirror_preview:
                    frame = cv2.flip(frame, 1)
                h, w = frame.shape[:2]
                # Draw every detection, even while waiting for the full group or
                # rejecting an ambiguous identity. Only assigned bodies drive Unity.
                slots = {id(observations[i][3]): slot for slot, i in assignments.items()}
                thickness = max(2, round(max(w / 320, h / 240) * 2))
                radius = max(3, round(thickness * 1.5))
                for landmarks in result.pose_landmarks:
                    slot = slots.get(id(landmarks))
                    color = colors[slot - 1] if slot is not None else (180, 180, 180)
                    points = {}
                    for index, landmark in enumerate(landmarks):
                        if (min(landmark.visibility, landmark.presence) >= .5
                                and math.isfinite(landmark.x) and math.isfinite(landmark.y)
                                and 0 <= landmark.x <= 1 and 0 <= landmark.y <= 1):
                            points[index] = ((min(w - 1, int(landmark.x * w)) if args.no_mirror_preview else w - 1 - min(w - 1, int(landmark.x * w))),
                                             min(h - 1, int(landmark.y * h)))
                    for a, b in edges:
                        if a in points and b in points:
                            cv2.line(frame, points[a], points[b], (20, 20, 20), thickness + 2, cv2.LINE_AA)
                            cv2.line(frame, points[a], points[b], color, thickness, cv2.LINE_AA)
                    for point in points.values():
                        cv2.circle(frame, point, radius + 1, (20, 20, 20), -1, cv2.LINE_AA)
                        cv2.circle(frame, point, radius, color, -1, cv2.LINE_AA)
                    anchor = points.get(11, next(iter(points.values()), None))
                    if anchor is not None:
                        label = f'P{slot}' if slot is not None else 'Unassigned'
                        font_scale = max(.6, thickness * .3)
                        cv2.putText(frame, label, anchor, cv2.FONT_HERSHEY_SIMPLEX, font_scale,
                                    (20, 20, 20), thickness + 2, cv2.LINE_AA)
                        cv2.putText(frame, label, anchor, cv2.FONT_HERSHEY_SIMPLEX, font_scale,
                                    color, thickness, cv2.LINE_AA)
                message = f'{len(assignments)}/{args.people} assigned' if args.no_window else f'{len(assignments)}/{args.people} assigned | R: reassign | Q: quit'
                if tracker.positions is None:
                    message = f'Waiting for {args.people} visible people' if args.no_window else f'Stand side by side: waiting for {args.people} visible people | Q: quit'
                cv2.putText(frame, message, (12, 26), cv2.FONT_HERSHEY_SIMPLEX, .55, (255, 255, 255), 2)
                now = time.monotonic()
                if now - last_preview >= .1:
                    jpeg = encode_preview(frame, cv2)
                    if jpeg is not None:
                        try:
                            sender.sendto(jpeg, ('127.0.0.1', args.port + 2))
                        except OSError as error:
                            # A dropped preview must not stop performer tracking.
                            if error.errno not in (errno.EMSGSIZE, errno.EAGAIN, errno.EWOULDBLOCK, errno.ENOBUFS):
                                raise
                    last_preview = now
                if not args.no_window:
                    cv2.imshow('DigiPhant tracking (' + ('unmirrored' if args.no_mirror_preview else 'mirrored') + ')', frame)
                key = (cv2.waitKey(1) & 0xff) if not args.no_window else -1
                if key == ord('q'):
                    break
                if key == ord('r'):
                    tracker.reset()
    finally:
        sender.sendto(b'{"version":1,"people":[]}', ('127.0.0.1', args.port))
        sender.close()
        camera.release()
        cv2.destroyAllWindows()


if __name__ == '__main__':
    main()
