"""Conservative printed-reference recognition, not generic candy recognition."""
from pathlib import Path
import cv2
import numpy as np


class PrintedReferenceVerifier:
    def __init__(self, path):
        image=cv2.imread(str(path))
        if image is None: raise ValueError(f'Reference unreadable: {path}')
        self.reference=cv2.resize(image,(500,round(image.shape[0]*500/image.shape[1])))
        self.sift=cv2.SIFT_create(nfeatures=1200)
        self.matcher=cv2.BFMatcher(cv2.NORM_L2)
        self.variants=[]
        for mirrored in (False,True):
            image=cv2.flip(self.reference,1) if mirrored else self.reference
            gray=cv2.cvtColor(image,cv2.COLOR_BGR2GRAY)
            points,features=self.sift.detectAndCompute(gray,None)
            self.variants.append((image,gray,points,features,mirrored))

    def verify(self, frame):
        original_h,original_w=frame.shape[:2]
        scale=min(1.,640/original_w)
        view=cv2.resize(frame,(round(original_w*scale),round(original_h*scale)))
        gray=cv2.cvtColor(view,cv2.COLOR_BGR2GRAY)
        points,features=self.sift.detectAndCompute(gray,None)
        if features is None or len(points)<16: return None
        best=None
        for reference,refgray,refpoints,reff,mirrored in self.variants:
            pairs=self.matcher.knnMatch(reff,features,k=2)
            good=[a for pair in pairs if len(pair)==2 for a,b in [pair] if a.distance<.70*b.distance]
            if len(good)<20: continue
            src=np.float32([refpoints[m.queryIdx].pt for m in good])
            dst=np.float32([points[m.trainIdx].pt for m in good])
            matrix,inliers=cv2.findHomography(src,dst,cv2.RANSAC,3)
            if matrix is None or inliers is None: continue
            selected=inliers.ravel().astype(bool); count=int(selected.sum()); ratio=count/len(good)
            if count<16 or ratio<.55: continue
            # Swirl aliases or one small texture patch must not prove a whole card.
            spread=np.ptp(src[selected],axis=0)
            if spread[0]<180 or spread[1]<180: continue
            h,w=refgray.shape
            corners=cv2.perspectiveTransform(np.float32([[[0,0],[w,0],[w,h],[0,h]]]),matrix)[0]
            if not np.isfinite(corners).all() or not cv2.isContourConvex(corners): continue
            area=abs(cv2.contourArea(corners))
            edges=np.linalg.norm(corners-np.roll(corners,1,axis=0),axis=1)
            if area<1200 or area>view.shape[0]*view.shape[1]*1.15 or edges.min()<25 or edges.max()/edges.min()>5: continue
            if np.any(corners < -20) or np.any(corners[:,0]>view.shape[1]+20) or np.any(corners[:,1]>view.shape[0]+20): continue
            aligned=cv2.warpPerspective(view,np.linalg.inv(matrix),(w,h))
            # Verify the actual pink/white swirl after alignment, beyond keypoints.
            pink=(reference[:,:,2].astype(float)-reference[:,:,1]>35)&(reference[:,:,2]>130)
            observed=(aligned[:,:,2].astype(float)-aligned[:,:,1]>25)&(aligned[:,:,2]>95)
            dice=2*np.sum(pink&observed)/max(1,np.sum(pink)+np.sum(observed))
            roi=np.s_[10:min(440,h),35:w-35]
            a=cv2.GaussianBlur(refgray,(5,5),0)[roi].astype(float).ravel()
            b=cv2.GaussianBlur(cv2.cvtColor(aligned,cv2.COLOR_BGR2GRAY),(5,5),0)[roi].astype(float).ravel()
            correlation=float(np.corrcoef(a,b)[0,1]) if b.std()>1 else 0
            if not np.isfinite(correlation) or correlation<.60 or dice<.58: continue
            # Heuristic verifier score: not a calibrated real-world probability.
            score=float(min(.99,.72+.16*correlation+.08*dice))
            lo=np.maximum(corners.min(0)/scale,0); hi=np.minimum(corners.max(0)/scale,[original_w,original_h])
            result={'bbox':[int(lo[0]),int(lo[1]),int(hi[0]-lo[0]),int(hi[1]-lo[1])],'score':score,'inliers':count,'pattern_correlation':correlation,'pink_overlap':float(dice),'mirrored':mirrored}
            if best is None or result['score']>best['score']: best=result
        return best
