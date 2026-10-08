"""Make deterministic SYNTHETIC detection data from one printed lollipop image."""
import argparse
import hashlib
import json
from pathlib import Path
import cv2
import numpy as np

ROOT = Path(__file__).resolve().parent
SIZE = 512
SOURCE_BOX = np.float32([[88,18],[1055,18],[1055,1372],[88,1372]])


def sample(source, rng, negative):
    base = rng.integers(30, 225, 3).astype(np.float32)
    yy, xx = np.mgrid[:SIZE,:SIZE]
    background = np.clip(base + (xx[...,None]/SIZE-.5)*rng.uniform(-75,75) + rng.normal(0, 8, (SIZE,SIZE,1)), 0,255).astype(np.uint8)
    for _ in range(int(rng.integers(3,9))):
        center = tuple(int(n) for n in rng.integers(0,SIZE,2))
        color = tuple(int(n) for n in rng.integers(0,255,3))
        cv2.circle(background, center, int(rng.integers(15,80)), color, -1)
    if negative:
        # Pink round and patterned objects without the source swirl/stick target.
        center = tuple(int(n) for n in rng.integers(90,422,2))
        radius = int(rng.integers(25,95))
        cv2.circle(background,center,radius,(145,65,245),-1)
        if rng.random() < .6:
            for offset in range(-radius,radius,12):
                cv2.line(background,(center[0]-radius,center[1]+offset),(center[0]+radius,center[1]+offset),(220,170,250),3)
        return background, None
    h,w = source.shape[:2]
    angle = rng.uniform(-155,155)*np.pi/180
    scale = rng.uniform(.14,.27)
    corners = np.float32([[0,0],[w,0],[w,h],[0,h]])
    rotation = np.array([[np.cos(angle),-np.sin(angle)],[np.sin(angle),np.cos(angle)]])
    dest = (corners-[w/2,h/2]) @ rotation.T * scale
    dest += rng.normal(0,9,(4,2))
    span = dest.max(0)-dest.min(0)
    if max(span)>450: dest *= 450/max(span)
    dest -= dest.min(0)
    span = dest.max(0)
    dest += [rng.uniform(12,SIZE-span[0]-12),rng.uniform(12,SIZE-span[1]-12)]
    matrix = cv2.getPerspectiveTransform(corners,dest.astype(np.float32))
    warped = cv2.warpPerspective(source,matrix,(SIZE,SIZE))
    mask = cv2.warpPerspective(np.full((h,w),255,np.uint8),matrix,(SIZE,SIZE))
    background[mask>0] = warped[mask>0]
    points = cv2.perspectiveTransform(SOURCE_BOX[None,:,:],matrix)[0]
    lo = np.maximum(points.min(0),0); hi = np.minimum(points.max(0),SIZE)
    bbox = [float(lo[0]),float(lo[1]),float(hi[0]-lo[0]),float(hi[1]-lo[1])]
    return background,bbox


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source',type=Path,default=ROOT.parents[1]/'lollipop.png')
    parser.add_argument('--output',type=Path,default=ROOT/'data')
    args=parser.parse_args()
    source=cv2.imread(str(args.source))
    if source is None: raise SystemExit('Source image unreadable')
    if source.shape[:2] != (1372,1147): raise SystemExit('Manual source bbox expects 1147x1372 image; inspect annotation before changing source')
    args.output.mkdir(parents=True,exist_ok=True)
    previews=[]
    counts={}
    for split,n,seed in [('train',160,401),('val',40,902),('test',40,1403)]:
        rng=np.random.default_rng(seed)
        folder=args.output/split; (folder/'images').mkdir(parents=True,exist_ok=True)
        annotations=[]; images=[]; negative_count=0
        for i in range(n):
            negative=i%5==0; negative_count+=int(negative)
            image,bbox=sample(source,rng,negative)
            image=np.clip(image.astype(np.float32)*rng.uniform(.72,1.18)+rng.normal(0,rng.uniform(0,4),image.shape),0,255).astype(np.uint8)
            if rng.random()<.4: image=cv2.GaussianBlur(image,(3,3),rng.uniform(.3,1.4))
            name=f'{split}_{i:04d}.jpg'; cv2.imwrite(str(folder/'images'/name),image,[cv2.IMWRITE_JPEG_QUALITY,92])
            images.append({'id':i+1,'file_name':name,'height':SIZE,'width':SIZE})
            if bbox is not None:
                annotations.append({'id':len(annotations)+1,'image_id':i+1,'category_id':1,'bbox':bbox,'area':bbox[2]*bbox[3],'iscrowd':0})
            if i<8:
                preview=image.copy()
                if bbox:
                    x,y,w,h=map(int,bbox); cv2.rectangle(preview,(x,y),(x+w,y+h),(0,255,0),2)
                cv2.putText(preview,f'{split} {"negative" if negative else "lollipop"}',(12,26),cv2.FONT_HERSHEY_SIMPLEX,.7,(0,0,0),3)
                cv2.putText(preview,f'{split} {"negative" if negative else "lollipop"}',(12,26),cv2.FONT_HERSHEY_SIMPLEX,.7,(255,255,255),1)
                previews.append(cv2.resize(preview,(256,256)))
        coco={'images':images,'annotations':annotations,'categories':[{'id':0,'name':'background'},{'id':1,'name':'lollipop'}]}
        (folder/'labels.json').write_text(json.dumps(coco,indent=2)+'\n')
        counts[split]={'images':n,'positive':n-negative_count,'negative':negative_count,'seed':seed}
    sheet=np.concatenate([np.concatenate(previews[i:i+8],axis=1) for i in range(0,24,8)],axis=0)
    cv2.imwrite(str(args.output/'contact_sheet.jpg'),sheet)
    manifest={'kind':'synthetic printed-image card dataset','source':str(args.source.resolve()),'source_sha256':hashlib.sha256(args.source.read_bytes()).hexdigest(),'source_bbox_xywh':[88,18,967,1354],'splits':counts,'limitation':'All splits derive from one image. Synthetic test scores do not measure real-world generalization.'}
    (args.output/'provenance.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps(manifest,indent=2))


if __name__=='__main__': main()
