import unittest
from types import SimpleNamespace
from bridge import flap_features
class FlapFeaturesTest(unittest.TestCase):
    def pose(self):
        p=[SimpleNamespace(x=.5,y=.5,visibility=1.,presence=1.) for _ in range(33)]
        p[11].x=.6;p[12].x=.4;p[15].x=.85;p[16].x=.15
        return p
    def test_sideways_and_synchronous_heights(self):
        p=self.pose();p[15].y=p[16].y=.35
        l,r,side,q=flap_features(p)
        self.assertAlmostEqual(l,.75);self.assertAlmostEqual(r,.75);self.assertEqual(side,1);self.assertEqual(q,1)
    def test_hanging_and_crossed_arms_rejected(self):
        p=self.pose();p[15].x=.6;p[16].x=.4
        self.assertEqual(flap_features(p)[2],0)
        p[15].x=.1;p[16].x=.9
        self.assertEqual(flap_features(p)[2],0)
    def test_translation_scale_and_mirror(self):
        p=self.pose();a=flap_features(p)
        for x in p:x.x=x.x*.7+.1;x.y=x.y*.7+.15
        self.assertEqual(flap_features(p)[2],a[2])
        for x in p:x.x=1-x.x
        self.assertEqual(flap_features(p)[2],1)
    def test_occlusion_confidence(self):
        p=self.pose();p[16].visibility=.1
        self.assertEqual(flap_features(p)[3],.1)
if __name__=='__main__':unittest.main()
