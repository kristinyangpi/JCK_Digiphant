"""Mouth gesture adapted from celenashen04-boop/JCK_digiphant face_bridge.py.
Only associates unambiguous pose noses with faces; no independent camera or UDP socket.
"""
import math

def mouth_open(face, aspect=1.):
    if len(face) < 153:
        return None
    def gap(a,b):return math.hypot((a.x-b.x)*aspect,a.y-b.y)
    length=gap(face[10],face[152])
    value=gap(face[13],face[14])/length/.12 if length>1e-5 else float('nan')
    return max(0.,min(3.,value)) if math.isfinite(value) else None

def head_pitch(face, aspect=1.):
    """Approximate mesh pitch in degrees: positive down, negative up."""
    if len(face) < 153 or not math.isfinite(aspect) or aspect <= 0:
        return None
    top, chin = face[10], face[152]
    coordinates = (top.x, top.y, getattr(top, 'z', float('nan')),
                   chin.x, chin.y, getattr(chin, 'z', float('nan')))
    if not all(math.isfinite(v) for v in coordinates):
        return None
    length = math.hypot((chin.x-top.x)*aspect, chin.y-top.y)
    if length < 1e-5:
        return None
    # A downward nod rotates the chin away from the camera relative to the
    # forehead (the nose points down). MediaPipe depth increases away.
    return math.degrees(math.atan2((chin.z-top.z)*aspect, length))

def _match_faces(poses,faces,aspect=1.,zoned=False):
    proposals={}
    for face in faces:
        if len(face) < 153:continue
        nose=face[1]
        if zoned and not 1/3+.02 < nose.x < 2/3-.02:continue
        distances=[]
        for i,pose in enumerate(poses):
            p=pose[0]
            if min(p.visibility,p.presence)<.5:continue
            distance=math.hypot((p.x-nose.x)*aspect,p.y-nose.y)
            if math.isfinite(distance):distances.append((distance,i))
        distances.sort()
        if not distances or distances[0][0]>.10:continue
        if len(distances)>1 and distances[1][0]-distances[0][0]<.02:continue
        proposals.setdefault(distances[0][1],[]).append(face)
    return {i:values[0] for i,values in proposals.items() if len(values)==1}


def match_mouths(poses,faces,aspect=1.,zoned=False):
    matches = _match_faces(poses, faces, aspect, zoned)
    return {i:(value,1.) for i,face in matches.items()
            if (value := mouth_open(face,aspect)) is not None}

def match_heads(poses,faces,aspect=1.,zoned=False):
    matches = _match_faces(poses, faces, aspect, zoned)
    return {i:(value,1.) for i,face in matches.items()
            if (value := head_pitch(face,aspect)) is not None}


def central_face_inputs(faces):
    """P2 head/mouth can work with a close-up face and no visible shoulders.

    Exactly one central face is required; boundary and multiple faces are rejected.
    """
    central=[face for face in faces if len(face)>152 and
             math.isfinite(face[1].x) and 1/3+.02<face[1].x<2/3-.02]
    if len(central)!=1:
        return None
    return central[0]
