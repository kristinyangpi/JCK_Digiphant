import unittest
from types import SimpleNamespace as P
from mouth_tracking import mouth_open, match_mouths, head_pitch, match_heads
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
class HeadTests(MouthTests):
    def face(self,x=.5,gap=.024):
        f=super().face(x,gap)
        for point in f: point.z=0.
        return f
    def test_pitch_sign_and_scale(self):
        f=self.face();f[152].z=.1
        self.assertAlmostEqual(head_pitch(f),26.565051177)
        f[152].z=-.1
        self.assertLess(head_pitch(f),0)
        small=[P(x=.5+(v.x-.5)*.5,y=.5+(v.y-.5)*.5,z=v.z*.5) for v in f]
        self.assertAlmostEqual(head_pitch(f),head_pitch(small))
    def test_head_zone_and_ambiguity(self):
        self.assertEqual(set(match_heads([self.pose(.8),self.pose(.5),self.pose(.2)],
                                        [self.face(.8),self.face(.5),self.face(.2)],zoned=True)),{1})
        self.assertEqual(match_heads([self.pose()],[self.face(),self.face()],zoned=True),{})
        self.assertEqual(match_heads([self.pose(.49),self.pose(.51)],[self.face()],zoned=True),{})
        self.assertEqual(match_heads([self.pose(1/3)],[self.face(1/3)],zoned=True),{})
    def test_invalid_head_values(self):
        for bad in (float('nan'),float('inf'),-float('inf')):
            f=self.face();f[152].z=bad
            self.assertIsNone(head_pitch(f))
            self.assertEqual(match_heads([self.pose()],[f],zoned=True),{})
        f=self.face();f[152]=f[10]
        self.assertIsNone(head_pitch(f))
        self.assertIsNone(head_pitch([]))
        self.assertIsNone(head_pitch(self.face(),float('nan')))
    def test_ambiguous_invalid_head_does_not_select_other_face(self):
        invalid=self.face();invalid[152].z=float('nan')
        self.assertEqual(match_heads([self.pose()],[self.face(),invalid],zoned=True),{})

if __name__=='__main__':unittest.main()
