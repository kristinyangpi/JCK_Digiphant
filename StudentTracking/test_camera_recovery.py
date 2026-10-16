import unittest
from bridge import CameraFrameRecovery


class CameraRecoveryTests(unittest.TestCase):
    def test_warmup_is_bounded(self):
        recovery = CameraFrameRecovery()
        self.assertFalse(recovery.update(False, 10.))
        self.assertFalse(recovery.update(False, 12.99))
        with self.assertRaises(RuntimeError):
            recovery.update(False, 13.)

    def test_single_failed_read_recovers_and_resets_deadline(self):
        recovery = CameraFrameRecovery()
        self.assertTrue(recovery.update(True, 0.))
        self.assertFalse(recovery.update(False, 1.))
        self.assertTrue(recovery.update(True, 1.2))
        self.assertFalse(recovery.update(False, 5.))
        self.assertFalse(recovery.update(False, 5.99))
        with self.assertRaises(RuntimeError):
            recovery.update(False, 6.)
