# Navigation tracking
Unity launches this project-specific camera bridge. The starter clone stays unchanged.

Windows setup (Python 3.10 recommended):
```powershell
py -3.10 -m venv StudentTracking/.venv
StudentTracking/.venv/Scripts/python.exe -m pip install -r StudentTracking/requirements.txt
```
On macOS use python3 and .venv/bin/python. Existing installations may reuse DigiPhantStarter/Tracking/.venv automatically.

Pose and Hand Landmarker model files are included. Official sources:
- https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_full/float16/1/pose_landmarker_full.task
- https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task

Tests: python -m unittest discover -s StudentTracking -p "test_*.py"
See ../DRIVER_NAVIGATION.md for controls. Do not commit recordings or virtual environments.

## Three-person zones and mouth interaction
Three-person mode uses fixed zones in the mirrored view: left=P1 driver, center=P2 mouth/head interactions, right=P3 lollipop/flap. Shoulder centers assign zones; ambiguous/boundary zones send no input. Face associations also require a central face and an unambiguous nearby pose nose. Face inference shares the existing webcam and UDP stream.

Face model official source:
https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/latest/face_landmarker.task

Mouth ratio adapted from celenashen04-boop/JCK_digiphant StudentTracking/face_bridge.py (digiphant-integration branch). Current tests: 61.

## P2 head input
The existing face association also sends `headPitchDegrees` and `headConfidence` for P2. Invalid, missing or ambiguous associations clear the head input; the other performers do not control drinking or spraying. Unity establishes a stable two-second head neutral and requires a short neutral return between head actions. Ordinary tracking loss preserves that neutral; a new tracking epoch resets it. P1 driver calibration is independent. See the main controls guide for drinking, spraying and log-eating distances and gestures.

## P3 printed card and flight
P3 uses the raw-left third of the same camera frame (mirrored-right preview), with a 2% frame-width inset from the P2 boundary. Recognition runs only while exactly one reliable shoulder center owns P3. P1/P2 pixels never reach the printed-card verifier. Recognition is specific to the included lollipop.png, not arbitrary lollipops; print that image and hold the complete card inside the P3 area.

The existing OpenCV SIFT verifier sends JSON to 127.0.0.1 at the configured base port + 3 (default 5058): version=1, label=lollipop, detected=true/false, score=number. Checks run at most 10 Hz. Losing P3, ambiguity, camera failure, and shutdown clear detection; negatives on ownership loss bypass throttling. The score is a heuristic match score, not a calibrated probability.

Each assigned person keeps the existing tracking fields and adds flap=[leftHeight,rightHeight,sideways,confidence]. Only P3 in three-person mode has active values. Heights use anatomical wrist heights relative to shoulder center, divided by shoulder width; sideways is 1 only with both hands extended outward. Unity handles flap cycles and reward consumption. One/two/four-person modes cannot trigger P3 cards or flight.

No TFLite model or extra camera is used. On this machine Unity reuses DigiPhantStarter/Tracking/.venv/Scripts/python.exe (Python 3.14). Run the tests with that interpreter; the global Python installation does not have tracking dependencies. Offline checks passed all 61 tests; a live three-person camera/play-mode check remains necessary.

## Multiscale printed-image training (October 13, 2026)

The printed-image recognizer now uses a saved SIFT descriptor model trained from
`lollipop.png` at widths 120, 180, 280 and 500, including mirrored views (8 views,
926 descriptors). This is template/descriptor training, not a neural-network
object detector. Rebuild after changing the source image:

```sh
StudentTracking/.venv/bin/python StudentTracking/train_reference.py
```

The exported `lollipop.reference.npz` includes the source SHA-256 and is loaded
without pickle. Recognition retains homography, spatial spread, pink overlap
and aligned swirl correlation checks. Synthetic webcam cards at 100/120/140/180
pixels wide pass; cards in P1/P2 and existing distractors fail. These synthetic
checks do not establish live-camera accuracy.

Use three-person mode. Exactly one reliably tracked shoulder center must own P3
(the mirrored preview's right third), and the whole card must fit that area.
Keep shoulders visible, show the image until it matches, and move the
elephant within reach of a scene lollipop. It automatically reaches, curls the
candy toward its mouth and eats it. Hide the card for 0.8 seconds before showing
it again to eat another. The pickup panel now reports absent P3, a missing image
match, camera disconnection or a candy that is too far away.

The camera tolerates 3 seconds of startup warmup and 1 second of intermittent
frame loss, clearing card and body input while waiting. Unity runs in the
background so switching windows does not suspend tracking.

## Gesture fixes (October 13, 2026)

P2 pitch uses chin depth minus forehead depth, so lowering the head is
positive and lifting it is negative. MediaPipe depth increases away from the
camera: https://github.com/google-ai-edge/mediapipe/blob/master/docs/solutions/face_mesh.md
P3 sideways recognition accepts lateral wrist separation above 0.35 shoulder
widths; flight accepts 0.15-width strokes with reversal within 1.5 seconds and
retains the stroke through a sideways-pose gap shorter than 0.25 seconds.
Missing tracking still clears flight immediately. Both candies remain required.

P2: stay in the center, look straight for two seconds, approach within 3 units
of the basin, lower your head to drink, let the drink finish, return to neutral
for 0.3 seconds, then lift your head to spray the stored water. Head and wrist
visibility diagnostics are displayed in the water and flight panels.

Unity checks: DigiPhant > Validate lollipop image to eating (includes flight),
and DigiPhant > Validate P2 head drink and spray. Run outside Play mode with
scene edits saved. These use synthetic gestures, not a live performer check.

## Shared flight controls and water head states

P1 retains forward/reverse and steering controls while airborne. P3 flapping
controls elevation; stopping flaps lowers the elephant to land. Ground pickup
and drinking remain unavailable while P3 is flying. Both gold hoop earrings
have been removed from the main DigiPhant scene.

P2 neutral head does nothing. Lowering by 3 degrees for 0.15 seconds starts
drinking near the basin; lifting by 3 degrees for 0.15 seconds lifts the trunk
and sprays stored water. Return within 2 degrees of neutral for 0.3 seconds
between actions. Complete a drink first to have water available for spraying.

## Finish ballet, flight pose, and close-up P2 nods

The finish gate again triggers landing, a layered pink tutu, two pirouettes and
a bow. A scene backup was created before installation. While flying, P1 uses
unchanged movement and steering input; the body holds an idle flight pose and
both large ears flap. P3 still controls ascent and landing.

A downward rigid-head nod now produces positive pitch. The earlier reversed
pitch correction was removed. An unambiguous center-zone face can provide P2
head/mouth input even when shoulders are not visible; multiple central faces
or ambiguous central bodies are rejected. Drinking needs a 3-degree downward
nod for 0.1 seconds after two seconds of neutral calibration, within 3 units
of the basin. Neutral does nothing; head up sprays water after a completed drink.

Validation: 67 tracking tests, flight ear animation plus P1 movement checks,
finish crossing/landing/tutu/two-turn/bow checks, and water action checks passed.
Live performer accuracy still needs a webcam check.

## Spray garden, immediate card pickup, and rear follow camera

Spray droplets target nearby floor tops and flower seeds follow the same emitted
velocity/gravity. The spray grows a colorful garden at its landing area, at up
to 24 flowers per second (80 flowers retained). Floating icing caps are accepted
as floor, and no flowers are planted in empty space.

The reference matcher now accepts fewer feature matches and more lighting/blur
variation, while retaining geometric and swirl/color verification. A matched
P3 card immediately triggers automatic pickup/eating when a candy is in reach.
Hide/show still rearms the next pickup. No half-second hold is required.

The game camera smoothly follows behind the elephant's heading, with an elevated
third-person view during walking and flight. The finish ballet keeps its own
cinematic camera.
