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

def match_mouths(poses,faces,aspect=1.,zoned=False):
    proposals={}
    for face in faces:
        value=mouth_open(face,aspect)
        if value is None:continue
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
        proposals.setdefault(distances[0][1],[]).append((value,1.))
    return {i:values[0] for i,values in proposals.items() if len(values)==1}
