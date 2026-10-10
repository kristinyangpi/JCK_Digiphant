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
Three-person mode uses fixed zones in the mirrored view: left=P1 driver, center=P2 mouth/eating, right=P3 lollipop/flap. Shoulder centers assign zones; ambiguous/boundary zones send no input. Face associations also require a central face and an unambiguous nearby pose nose. Face inference shares the existing webcam and UDP stream.

Face model official source:
https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/latest/face_landmarker.task

Mouth ratio adapted from celenashen04-boop/JCK_digiphant StudentTracking/face_bridge.py (digiphant-integration branch). Current tests: 52.

## P3 printed card and flight
P3 uses the raw-left third of the same camera frame (mirrored-right preview), with a 2% frame-width inset from the P2 boundary. Recognition runs only while exactly one reliable shoulder center owns P3. P1/P2 pixels never reach the printed-card verifier. Recognition is specific to the included lollipop.png, not arbitrary lollipops; print that image and hold the complete card inside the P3 area.

The existing OpenCV SIFT verifier sends JSON to 127.0.0.1 at the configured base port + 3 (default 5058): version=1, label=lollipop, detected=true/false, score=number. Checks run at most 10 Hz. Losing P3, ambiguity, camera failure, and shutdown clear detection; negatives on ownership loss bypass throttling. The score is a heuristic match score, not a calibrated probability.

Each assigned person keeps the existing tracking fields and adds flap=[leftHeight,rightHeight,sideways,confidence]. Only P3 in three-person mode has active values. Heights use anatomical wrist heights relative to shoulder center, divided by shoulder width; sideways is 1 only with both hands extended outward. Unity handles flap cycles and reward consumption. One/two/four-person modes cannot trigger P3 cards or flight.

No TFLite model or extra camera is used. On this machine Unity reuses DigiPhantStarter/Tracking/.venv/Scripts/python.exe (Python 3.14). Run the tests with that interpreter; the global Python installation does not have tracking dependencies. Offline checks passed all 52 tests; a live three-person camera/play-mode check remains necessary.
