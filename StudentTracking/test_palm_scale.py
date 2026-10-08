import unittest
from types import SimpleNamespace as P
from bridge import palm_scale
class PalmScaleTests(unittest.TestCase):
    def geometry(self):
        pose=[P(x=0.,y=0.) for _ in range(33)]
        pose[11]=P(x=.3,y=.4); pose[12]=P(x=.7,y=.4)
        hand=[P(x=.2,y=.6) for _ in range(21)]
        hand[9]=P(x=.2,y=.5);hand[5]=P(x=.17,y=.52);hand[17]=P(x=.23,y=.52)
        return hand,pose
    def test_nearer_larger_hand_increases_proxy(self):
        hand,pose=self.geometry();base=palm_scale(hand,pose,4/3)
        near=[P(x=.2+(v.x-.2)*1.5,y=.6+(v.y-.6)*1.5) for v in hand]
        self.assertAlmostEqual(palm_scale(near,pose,4/3)/base,1.5)
    def test_translation_does_not_steer(self):
        hand,pose=self.geometry();moved=[P(x=v.x+.12,y=v.y-.07) for v in hand]
        self.assertAlmostEqual(palm_scale(hand,pose),palm_scale(moved,pose))
    def test_aspect_correction(self):
        hand,pose=self.geometry()
        compressed_hand=[P(x=v.x/2,y=v.y) for v in hand]
        compressed_pose=[P(x=v.x/2,y=v.y) for v in pose]
        self.assertAlmostEqual(palm_scale(hand,pose),palm_scale(compressed_hand,compressed_pose,2))
    def test_invalid_landmarks(self):
        self.assertEqual(palm_scale([],[]),0)
if __name__=='__main__':unittest.main()
