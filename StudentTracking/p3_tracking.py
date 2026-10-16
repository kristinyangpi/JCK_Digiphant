"""P3-only card recognition and shoulder-relative flapping channels."""
import math
import json


def flap_features(landmarks):
    if len(landmarks) < 17:
        return [0., 0., 0., 0.]
    ls, rs, lw, rw = [landmarks[i] for i in (11, 12, 15, 16)]
    if any(not all(math.isfinite(v) for v in (p.x, p.y, p.visibility, p.presence))
           for p in (ls, rs, lw, rw)):
        return [0., 0., 0., 0.]
    scale = max(math.dist((ls.x, ls.y), (rs.x, rs.y)), .03)
    center_y = (ls.y + rs.y) / 2
    outward = ((lw.x-ls.x)*(ls.x-rs.x) > 0 and (rw.x-rs.x)*(rs.x-ls.x) > 0)
    sideways = outward and abs(lw.x-ls.x)/scale > .35 and abs(rw.x-rs.x)/scale > .35
    quality = max(0., min(1., min(min(p.visibility, p.presence) for p in (ls, rs, lw, rw))))
    return [(center_y-lw.y)/scale, (center_y-rw.y)/scale, float(sideways), quality]


def p3_crop(frame, enabled, inset=.02):
    """Only raw-left (mirrored-right) zone pixels, excluding the boundary gap."""
    if not enabled or frame is None or len(frame.shape) != 3 or frame.shape[2] != 3:
        return None
    h, w = frame.shape[:2]
    right = math.floor(w * (1/3-inset) + 1e-9)
    if h < 2 or right < 2:
        return None
    return frame[:, :right]


class P3CardChannel:
    """No positive results are reused; invalid ownership clears immediately."""
    def __init__(self, verifier, sender, port):
        self.verifier, self.sender = verifier, sender
        self.address = ('127.0.0.1', port + 3)
        self.last_check = -math.inf
        self.positive = False
        self.status = "Show the lollipop image in P3 (preview right)"

    def emit(self, result=None):
        score = float(result.get('score', 0.)) if result else 0.
        detected = bool(result) and math.isfinite(score) and score > 0.
        packet = dict(version=1, label='lollipop', detected=detected,
                      score=score if detected else 0., status=self.status)
        self.sender.sendto(json.dumps(packet, allow_nan=False).encode(), self.address)
        self.positive = detected
        return packet

    def update(self, frame, assignments, people, now):
        self.status = ("Select 3 people for P3 lollipop detection" if people != 3 else
                       "P3 not tracked: stand in preview right with shoulders visible" if 3 not in assignments else
                       "Show the pink swirl image larger inside P3 (preview right)")
        crop = p3_crop(frame, people == 3 and 3 in assignments)
        if crop is None:
            if self.positive or now-self.last_check >= .1:
                self.last_check = now
                return self.emit()
            return None
        if now-self.last_check < .1:
            return None
        self.last_check = now
        result = self.verifier.verify(crop)
        if result:
            self.status = "Lollipop image matched; hold still"
        return self.emit(result)

    def close(self):
        self.status = "Camera interrupted or stopped: no lollipop detection"
        return self.emit()

