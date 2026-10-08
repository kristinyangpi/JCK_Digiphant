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
