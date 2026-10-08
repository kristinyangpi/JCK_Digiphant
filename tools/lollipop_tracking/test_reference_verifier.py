"""Offline printed-reference checks; no camera or performer images."""
from pathlib import Path
import sys
import unittest
import cv2
import numpy as np

sys.path.insert(0,str(Path(__file__).parent))
from reference_verifier import PrintedReferenceVerifier

class ReferenceChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.image=cv2.imread(str(Path(__file__).resolve().parents[2]/'lollipop.png'))
        cls.verifier=PrintedReferenceVerifier(Path(__file__).resolve().parents[2]/'lollipop.png')
    def test_source_mirrored_blurred(self):
        for image in (self.image,cv2.flip(self.image,1),cv2.GaussianBlur(self.image,(7,7),1.4)):
            self.assertIsNotNone(self.verifier.verify(image))
    def test_scaled_perspective_card(self):
        h,w=self.image.shape[:2]
        matrix=cv2.getPerspectiveTransform(np.float32([[0,0],[w,0],[w,h],[0,h]]),np.float32([[120,30],[430,75],[400,480],[95,430]]))
        card=cv2.warpPerspective(self.image,matrix,(640,512),borderValue=(90,150,50))
        self.assertIsNotNone(self.verifier.verify(card))
    def test_personlike_pink_shapes_and_noise(self):
        image=np.full((512,640,3),190,np.uint8)
        cv2.rectangle(image,(190,210),(430,510),(170,70,245),-1)
        cv2.circle(image,(310,150),90,(130,180,220),-1)
        for x in (280,340): cv2.circle(image,(x,135),8,(20,20,20),-1)
        cv2.ellipse(image,(310,180),(25,12),0,0,180,(20,20,20),4)
        self.assertIsNone(self.verifier.verify(image))
        self.assertIsNone(self.verifier.verify(np.random.default_rng(92).integers(0,256,(512,640,3),dtype=np.uint8)))
        cv2.circle(image,(310,150),100,(170,70,245),-1)
        for radius in range(10,95,12): cv2.circle(image,(310,150),radius,(235,210,240),5)
        self.assertIsNone(self.verifier.verify(image))

if __name__=='__main__': unittest.main()
