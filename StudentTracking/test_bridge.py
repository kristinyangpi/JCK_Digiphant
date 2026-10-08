import unittest
from types import SimpleNamespace
from bridge import PerformerTracker, extract_features, requested_count, hand_depth, palm_facing, match_hands


class TrackingTests(unittest.TestCase):
    def test_confidence_is_finite_and_clamped(self):
        for score, expected in [(1.000001, 1.), (-.001, 0.), (float('nan'), 0.), (float('inf'), 0.)]:
            points = [SimpleNamespace(x=.5, y=.5, visibility=score, presence=score) for _ in range(33)]
            for upper in (False, True):
                _, _, confidence = extract_features(points, upper)
                self.assertEqual(confidence, [expected] * 6)

    def test_roles_survive_detection_reordering_and_dropout(self):
        t = PerformerTracker(3)
        self.assertEqual(t.assign([(.8, .5), (.2, .5)]), {})
        self.assertEqual(t.assign([(.8, .5), (.2, .5), (.5, .5)]), {1: 1, 2: 2, 3: 0})
        self.assertEqual(t.assign([(.51, .5), (.81, .5), (.21, .5)]), {1: 2, 2: 0, 3: 1})
        self.assertEqual(t.assign([(.52, .5), (.22, .5)]), {1: 1, 2: 0})
        self.assertEqual(t.assign([(.82, .5)]), {3: 0})

    def test_ambiguous_slots_are_rejected_but_stable_slot_survives(self):
        t = PerformerTracker(3)
        t.assign([(.2, .5), (.4, .5), (.8, .5)])
        self.assertEqual(t.assign([(.3, .5), (.8, .5)]), {3: 1})
        self.assertEqual(t.positions, [(.2, .5), (.4, .5), (.8, .5)])
        t.reset()
        self.assertIsNone(t.positions)

    def test_driver_survives_other_people_ambiguity(self):
        t = PerformerTracker(3)
        t.assign([(.2, .5), (.5, .5), (.8, .5)])
        self.assertEqual(t.assign([(.65, .5), (.21, .5)]), {1: 1})
        self.assertEqual(t.positions, [(.21, .5), (.5, .5), (.8, .5)])
        self.assertEqual(t.assign([(.81, .5), (.22, .5), (.51, .5)]),
                         {1: 1, 2: 2, 3: 0})

    def test_all_near_optimal_assignments_are_considered(self):
        t = PerformerTracker(3)
        # One observation is equally close to all three prior positions.
        t.assign([(.2, .5), (.4, .5), (.3, .5 + 3 ** .5 / 10)])
        self.assertEqual(t.assign([(.3, .5 + 3 ** .5 / 30)]), {})

    def test_missing_slots_do_not_update_or_claim_distant_observations(self):
        t = PerformerTracker(3)
        t.assign([(.2, .5), (.5, .5), (.8, .5)])
        self.assertEqual(t.assign([(.81, .5)]), {3: 0})
        self.assertEqual(t.positions, [(.2, .5), (.5, .5), (.81, .5)])
        self.assertEqual(t.assign([(.2, .9)]), {})
        self.assertEqual(t.assign([]), {})
        self.assertEqual(t.positions, [(.2, .5), (.5, .5), (.81, .5)])

    def test_one_and_two_people_with_extra_bystanders(self):
        for count in (1, 2):
            t = PerformerTracker(count)
            assigned = t.assign([(.8, .5), (.2, .5), (.5, .5)])
            self.assertEqual(len(assigned), count)
            self.assertEqual(assigned[1], 1)
            self.assertEqual(t.assign([(.21, .5)]), {1: 0})

    def test_epoch_changes_on_reset_not_assignment(self):
        t = PerformerTracker(1)
        epoch = t.epoch
        t.assign([(.2, .5)])
        self.assertEqual(t.epoch, epoch)
        t.reset()
        self.assertNotEqual(t.epoch, epoch)
        self.assertNotEqual(PerformerTracker(1).epoch, t.epoch)

    @staticmethod
    def pose(offset=0.):
        points = [SimpleNamespace(x=.5+offset, y=.5, z=0., presence=1., visibility=1.) for _ in range(33)]
        for i, x in ((11, .4), (12, .6), (15, .3), (16, .7)):
            points[i].x = x + offset
        return points

    @staticmethod
    def hand(x=.3, flip=False):
        image = [SimpleNamespace(x=x, y=.5, z=0.) for _ in range(21)]
        world = [SimpleNamespace(x=0., y=0., z=0.) for _ in range(21)]
        world[5].x = .1
        world[17].y = -.1 if flip else .1
        return image, world

    def test_depth_is_cameraward_and_body_relative(self):
        pose, world = self.pose(), self.pose()
        world[15].z = -.1
        world[16].z = .1
        depth, confidence = hand_depth(pose, world)
        self.assertAlmostEqual(depth[0], .5)
        self.assertAlmostEqual(depth[1], -.5)
        self.assertEqual(confidence, [1., 1.])
        pose[15].visibility = .1
        self.assertEqual(hand_depth(pose, world)[1], [.1, 1.])
        self.assertEqual(hand_depth(pose, [])[1], [0., 0.])

    def test_palm_facing_flips_and_degenerate_is_rejected(self):
        self.assertAlmostEqual(palm_facing(self.hand()[1]), 1.)
        self.assertAlmostEqual(palm_facing(self.hand(flip=True)[1]), -1.)
        self.assertIsNone(palm_facing([]))

    def test_hand_matches_anatomical_side_and_other_person(self):
        hands = [self.hand(.7), self.hand(.3), self.hand(1.3)]
        matched = match_hands([self.pose(), self.pose(1.)], [h[0] for h in hands], [h[1] for h in hands])
        self.assertEqual(set(matched), {(0, 0), (0, 1), (1, 0)})
        # A bystander's wrist wins even if Unity has not assigned that body.
        h, w = self.hand(.38)
        self.assertEqual(set(match_hands([self.pose(), self.pose(.08)], [h], [w])), {(1, 0)})

    def test_ambiguous_and_duplicate_hands_are_omitted(self):
        h, w = self.hand(.34)
        self.assertEqual(match_hands([self.pose(), self.pose(.08)], [h], [w]), {})
        h, w = self.hand()
        self.assertEqual(match_hands([self.pose()], [h, h], [w, w]), {})
        distant, w = self.hand(2.)
        self.assertEqual(match_hands([self.pose()], [distant], [w]), {})

    def test_count_request_validation(self):
        for count in (1, 2, 3, 4):
            self.assertEqual(requested_count('{"version":1,"performerCount":%d}' % count), count)
        for data in ('null', '[]', 'bad', '{"version":1,"performerCount":0}',
                     '{"version":1,"performerCount":5}', '{"version":1,"performerCount":true}',
                     '{"version":1,"performerCount":"2"}', '{"version":2,"performerCount":2}'):
            self.assertIsNone(requested_count(data))

    def test_four_people(self):
        t = PerformerTracker(4)
        self.assertEqual(len(t.assign([(.1, .5), (.35, .5), (.6, .5), (.85, .5)])), 4)
        self.assertEqual(t.assign([]), {})

    def test_upper_body_works_with_no_visible_lower_body(self):
        points = [SimpleNamespace(x=.5, y=.5, presence=0., visibility=0.) for _ in range(33)]
        for i, xy in {11:(.4,.3), 12:(.6,.3), 15:(.3,.5), 16:(.7,.5)}.items():
            points[i] = SimpleNamespace(x=xy[0], y=xy[1], presence=1., visibility=1.)
        center, baseline, confidence = extract_features(points, True)
        self.assertEqual(center, (.5, .3))
        self.assertEqual(confidence, [1.] * 6)
        self.assertEqual(extract_features(points)[2][4], 0.)
        points[15].y -= .1
        _, raised, _ = extract_features(points, True)
        self.assertGreater(raised[0], baseline[0])
        self.assertEqual(raised[0], raised[2])
        self.assertEqual(raised[1], raised[3])
        points[11].y -= .05
        self.assertLess(extract_features(points, True)[1][4], baseline[4])
        points[15].visibility = 0.
        _, _, confidence = extract_features(points, True)
        self.assertEqual(confidence, [0., 1., 0., 1., 1., 0.])

    def test_features_are_translation_and_scale_invariant(self):
        points = [SimpleNamespace(x=.5, y=.5, presence=1., visibility=1.) for _ in range(33)]
        for i, xy in {11:(.4,.3),12:(.6,.3),23:(.4,.6),24:(.6,.6),15:(.2,.1),16:(.8,.2),27:(.4,.9),28:(.6,.9)}.items():
            points[i].x, points[i].y = xy
        _, first, _ = extract_features(points)
        for p in points:
            p.x = p.x * .5 + .1
            p.y = p.y * .5 + .2
        _, second, _ = extract_features(points)
        for a, b in zip(first, second):
            self.assertAlmostEqual(a, b)
        points[15].visibility = .1
        _, _, confidence = extract_features(points)
        self.assertEqual(confidence[0], .1)
        self.assertEqual(confidence[1], 1.)


if __name__ == '__main__':
    unittest.main()
