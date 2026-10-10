import itertools
import unittest
from types import SimpleNamespace

from bridge import (FixedZoneTracker, PerformerTracker, ZONE_MARGIN,
                    create_tracker, draw_zones, shoulder_center)


class ZoneTests(unittest.TestCase):
    def test_runtime_selects_zones_only_for_three_people(self):
        self.assertIsInstance(create_tracker(3), FixedZoneTracker)
        for count in (1, 2, 4):
            self.assertIs(type(create_tracker(count)), PerformerTracker)

    def test_missing_driver_does_not_block_or_steal_other_roles(self):
        tracker = create_tracker(3)
        self.assertEqual(tracker.assign([(.5, .5), (.2, .5)]), {2: 0, 3: 1})
        self.assertEqual(tracker.assign([(.2, .5)]), {3: 0})
        self.assertEqual(tracker.assign([(.5, .5)]), {2: 0})

    def test_all_arrival_orders_and_detection_orders_keep_roles(self):
        positions = {1: (.8, .5), 2: (.5, .5), 3: (.2, .5)}
        for arrival in itertools.permutations(positions):
            tracker = create_tracker(3)
            for length in (1, 2, 3):
                for order in itertools.permutations(arrival[:length]):
                    self.assertEqual(tracker.assign([positions[s] for s in order]),
                                     {s: i for i, s in enumerate(order)})

    def test_crossing_clears_old_output_and_does_not_follow_person(self):
        tracker = create_tracker(3)
        self.assertEqual(tracker.assign([(.8, .5), (.2, .5)]), {1: 0, 3: 1})
        # The former driver enters the boundary gap; P1 disappears immediately.
        self.assertEqual(tracker.assign([(2/3, .5), (.2, .5)]), {3: 1})
        # Roles belong to areas; crossing into center means P2, never P1.
        self.assertEqual(tracker.assign([(.5, .5), (.2, .5)]), {2: 0, 3: 1})
        self.assertEqual(tracker.assign([]), {})

    def test_same_zone_ambiguity_stops_only_that_zone(self):
        tracker = create_tracker(3)
        tracker.assign([(.8, .5), (.5, .5), (.2, .5)])
        self.assertEqual(tracker.assign([(.76, .5), (.86, .5), (.5, .5), (.2, .5)]),
                         {2: 2, 3: 3})
        self.assertEqual(tracker.assign([(.8, .5), (.48, .5), (.54, .5)]), {1: 0})

    def test_boundary_gap_including_edges(self):
        tracker = create_tracker(3)
        for boundary in (1/3, 2/3):
            for offset in (-ZONE_MARGIN, -.01, 0, .01, ZONE_MARGIN):
                self.assertEqual(tracker.assign([(boundary + offset, .5)]), {})
        for x, slot in ((1/3-ZONE_MARGIN-.001, 3), (1/3+ZONE_MARGIN+.001, 2),
                        (2/3-ZONE_MARGIN-.001, 2), (2/3+ZONE_MARGIN+.001, 1)):
            self.assertEqual(tracker.assign([(x, .5)]), {slot: 0})

    def test_epoch_stable_across_arrivals_ambiguity_and_crossing(self):
        tracker = create_tracker(3)
        epoch = tracker.epoch
        for centers in ([], [(.8, .5)], [(.8, .5), (.9, .5)], [(2/3, .5)], [(.5, .5)]):
            tracker.assign(centers)
            self.assertEqual(tracker.epoch, epoch)
        tracker.reset()
        self.assertNotEqual(tracker.epoch, epoch)
        self.assertEqual(tracker.assign([(.5, .5)]), {2: 0})

    def test_shoulders_control_zone_independent_of_hips_and_hands(self):
        points = [SimpleNamespace(x=.2, y=.9, presence=0., visibility=0.) for _ in range(33)]
        points[11] = SimpleNamespace(x=.7, y=.4, presence=1., visibility=1.)
        points[12] = SimpleNamespace(x=.9, y=.4, presence=1., visibility=1.)
        center = shoulder_center(points)
        self.assertAlmostEqual(center[0], .8)
        self.assertEqual(create_tracker(3).assign([center]), {1: 0})
        points[11].visibility = .1
        self.assertEqual(create_tracker(3).assign([shoulder_center(points)]), {})

    def test_invalid_centers_do_not_drive_any_slot(self):
        tracker = create_tracker(3)
        for center in ((float('nan'), .5), (.8, float('inf')), (1.1, .5), (-.1, .5)):
            self.assertEqual(tracker.assign([center]), {})

    def test_preview_labels_reverse_with_raw_view_and_lines_stay_at_thirds(self):
        class Drawing:
            FONT_HERSHEY_SIMPLEX = LINE_AA = 0
            def __init__(self):
                self.labels, self.lines = [], []
            def getTextSize(self, *args):
                return ((60, 12), 3)
            def line(self, frame, start, end, *args):
                self.lines.append((start, end))
            def rectangle(self, *args):
                pass
            def putText(self, frame, label, anchor, *args):
                self.labels.append((label, anchor[0]))
        for mirrored, expected in ((True, ['P1 DRIVER', 'P2 EAT', 'P3 LOLLI/FLAP']),
                                   (False, ['P3 LOLLI/FLAP', 'P2 EAT', 'P1 DRIVER'])):
            drawing = Drawing()
            draw_zones(SimpleNamespace(shape=(480, 640, 3)), drawing, mirrored)
            self.assertEqual([label for label, _ in drawing.labels], expected)
            self.assertEqual([x for _, x in drawing.labels], sorted(x for _, x in drawing.labels))
            self.assertEqual(set(drawing.lines), {((213, 0), (213, 479)), ((427, 0), (427, 479))})


if __name__ == '__main__':
    unittest.main()
