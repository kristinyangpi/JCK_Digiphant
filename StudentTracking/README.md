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
Three-person mode uses fixed zones in the mirrored view: left=P1 driver, center=P2 mouth/eating, right=P3 unassigned. Shoulder centers assign zones; ambiguous/boundary zones send no input. Face associations also require a central face and an unambiguous nearby pose nose. Face inference shares the existing webcam and UDP stream.

Face model official source:
https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/latest/face_landmarker.task

Mouth ratio adapted from celenashen04-boop/JCK_digiphant StudentTracking/face_bridge.py (digiphant-integration branch). Current tests: 41.
