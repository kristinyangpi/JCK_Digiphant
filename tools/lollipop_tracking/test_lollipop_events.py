import importlib.util
from pathlib import Path
from types import SimpleNamespace as S
import unittest
import socket
import json

spec=importlib.util.spec_from_file_location('combined_bridge',Path(__file__).with_name('bridge.py'))
bridge=importlib.util.module_from_spec(spec); spec.loader.exec_module(bridge)

class Events(unittest.TestCase):
    def test_only_lollipop_finite_scores(self):
        result=S(detections=[S(categories=[S(category_name='other',score=.99),S(category_name='lollipop',score=.8),S(category_name='lollipop',score=float('nan'))])])
        self.assertEqual(bridge.lollipop_packet(result),{'version':1,'label':'lollipop','detected':True,'score':.8})
    def test_empty_is_absence_heartbeat(self):
        self.assertEqual(bridge.lollipop_packet(S(detections=[])),{'version':1,'label':'lollipop','detected':False,'score':0.})
    def test_udp_packet_roundtrip(self):
        with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as receiver, socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as sender:
            receiver.bind(('127.0.0.1',0)); receiver.settimeout(1)
            event=bridge.lollipop_packet(S(detections=[]))
            sender.sendto(json.dumps(event,allow_nan=False).encode(),receiver.getsockname())
            self.assertEqual(json.loads(receiver.recvfrom(1024)[0]),event)

if __name__=='__main__': unittest.main()
