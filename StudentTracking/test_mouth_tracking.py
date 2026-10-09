import unittest
from types import SimpleNamespace as P
from mouth_tracking import mouth_open, match_mouths
class MouthTests(unittest.TestCase):
    def face(self,x=.5,gap=.024):
        f=[P(x=x,y=.5) for _ in range(468)]
        f[10]=P(x=x,y=.4);f[152]=P(x=x,y=.6)
        f[13]=P(x=x,y=.5);f[14]=P(x=x,y=.5+gap)
        return f
    def pose(self,x=.5):return [P(x=x,y=.5,visibility=1.,presence=1.) for _ in range(33)]
    def test_open_close(self):
        self.assertAlmostEqual(mouth_open(self.face()),1)
        self.assertEqual(mouth_open(self.face(gap=0)),0)
    def test_only_center_face_drives_zoned_mouth(self):
        poses=[self.pose(.8),self.pose(.5),self.pose(.2)]
        self.assertEqual(set(match_mouths(poses,[self.face(.8),self.face(.5),self.face(.2)],zoned=True)),{1})
    def test_ambiguous_faces_or_bodies_rejected(self):
        self.assertEqual(match_mouths([self.pose()],[self.face(),self.face()]),{})
        self.assertEqual(match_mouths([self.pose(.49),self.pose(.51)],[self.face()]),{})
    def test_boundary_and_far_faces_rejected(self):
        self.assertEqual(match_mouths([self.pose(1/3)],[self.face(1/3)],zoned=True),{})
        self.assertEqual(match_mouths([self.pose(.8)],[self.face(.5)],zoned=True),{})
    def test_ratio_independent_of_scale(self):
        f=self.face();small=[P(x=.5+(v.x-.5)*.5,y=.5+(v.y-.5)*.5) for v in f]
        self.assertAlmostEqual(mouth_open(f),mouth_open(small))
if __name__=='__main__':unittest.main()
