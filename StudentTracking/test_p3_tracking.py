import unittest
from types import SimpleNamespace
import json
import numpy as np
from p3_tracking import P3CardChannel, flap_features
from bridge import create_tracker

class P3Tests(unittest.TestCase):
    def setUp(self):
        self.frames=[]; self.packets=[]
        self.result={'score':.93}
        verifier=SimpleNamespace(verify=lambda frame: self.verify(frame))
        sender=SimpleNamespace(sendto=lambda data,address:self.packets.append((json.loads(data),address)))
        self.channel=P3CardChannel(verifier,sender,5055)
        self.frame=np.zeros((100,300,3),np.uint8)
        self.frame[:,:94]=17; self.frame[:,94:]=255
    def verify(self,frame):
        self.frames.append(frame.copy()); return self.result
    def test_only_raw_left_third_inset_reaches_verifier(self):
        self.channel.update(self.frame,{3:0},3,1.)
        self.assertEqual(self.frames[0].shape,(100,94,3))
        self.assertTrue((self.frames[0]==17).all())
        self.assertEqual(self.packets[-1][1],('127.0.0.1',5058))
    def test_absent_ambiguous_and_other_modes_never_verify(self):
        tracker=create_tracker(3)
        for centers in ([],[(.5,.5)],[(.15,.5),(.25,.5)],[(1/3,.5)]):
            self.channel.update(self.frame,tracker.assign(centers),3,1.)
        for count in (1,2,4): self.channel.update(self.frame,{3:0},count,2.)
        self.assertEqual(self.frames,[])
        self.assertTrue(all(not p['detected'] for p,a in self.packets))
    def test_loss_negative_bypasses_throttle(self):
        self.channel.update(self.frame,{3:0},3,1.)
        packet=self.channel.update(self.frame,{},3,1.01)
        self.assertEqual(packet,dict(version=1,label='lollipop',detected=False,score=0.))
        self.assertEqual(len(self.frames),1)
    def test_invalid_frame_clears_positive_without_reusing(self):
        for invalid in (None,np.zeros((0,300,3)),np.zeros((50,50))):
            self.channel.last_check=-100
            self.channel.update(self.frame,{3:0},3,1.)
            self.assertFalse(self.channel.update(invalid,{3:0},3,1.01)['detected'])
    def test_no_match_and_cleanup_negative(self):
        self.channel.update(self.frame,{3:0},3,1.)
        self.result=None
        self.assertFalse(self.channel.update(self.frame,{3:0},3,1.2)['detected'])
        self.assertFalse(self.channel.close()['detected'])
    def test_throttle(self):
        self.channel.update(self.frame,{3:0},3,1.)
        self.assertIsNone(self.channel.update(self.frame,{3:0},3,1.05))
        self.assertEqual(len(self.frames),1)
    def test_flap_geometry_quality_mirror(self):
        points=[SimpleNamespace(x=.5,y=.5,visibility=1.,presence=1.) for _ in range(33)]
        for i,x,y in ((11,.4,.4),(12,.6,.4),(15,.2,.2),(16,.8,.6)):
            points[i].x=x; points[i].y=y
        values=flap_features(points)
        np.testing.assert_allclose(values,[1,-1,1,1])
        for p in points:p.x=1-p.x
        np.testing.assert_allclose(flap_features(points),values)
        points[15].visibility=.2
        self.assertEqual(flap_features(points)[3],.2)
        points[15].x=float('nan')
        self.assertEqual(flap_features(points),[0,0,0,0])
    def test_forward_or_crossed_hands_not_sideways(self):
        points=[SimpleNamespace(x=.5,y=.5,visibility=1.,presence=1.) for _ in range(33)]
        points[11].x=.4; points[12].x=.6
        self.assertEqual(flap_features(points)[2],0)
        points[15].x=.9; points[16].x=.1
        self.assertEqual(flap_features(points)[2],0)
if __name__=='__main__':unittest.main()
