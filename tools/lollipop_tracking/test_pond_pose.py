import unittest
from types import SimpleNamespace
from bridge import pond_pose_features
class PondPoseTests(unittest.TestCase):
    def pose(self):
        p=[SimpleNamespace(x=.5,y=.5,visibility=.95,presence=.95) for _ in range(33)]
        for i,xy in {11:(.6,.5),12:(.4,.5),13:(.8,.5),15:(1.,.5),14:(.5,.75),16:(.77,.55)}.items():
            p[i].x,p[i].y=xy
        return p
    def test_reference_and_mirror(self):
        p=self.pose();self.assertGreaterEqual(pond_pose_features(p)[0],.8)
        for q in p:q.x=1-q.x
        self.assertGreaterEqual(pond_pose_features(p)[0],.8)
    def test_ordinary_arms_and_flap_rejected(self):
        p=self.pose();p[16].x=.1;self.assertEqual(pond_pose_features(p)[0],0)
        p=self.pose();p[15].y=.1;self.assertEqual(pond_pose_features(p)[0],0)
        p=self.pose();p[13].y=.9;self.assertEqual(pond_pose_features(p)[0],0)
    def test_occlusion_and_invalid(self):
        p=self.pose();p[13].visibility=.2;self.assertLess(pond_pose_features(p)[1],.75)
        p=self.pose();p[15].x=float('nan');self.assertEqual(pond_pose_features(p),[0,0])
if __name__=='__main__':unittest.main()
