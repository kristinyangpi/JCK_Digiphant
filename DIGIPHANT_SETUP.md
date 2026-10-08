# DigiPhant setup status

Inspected on 2026-10-06 on Windows. Project: `D:\Project_mdes\TM_PE\JACK_Digiphant`.

## Source and import

- Starter: https://github.com/kommanderpi/studentstarter
- Commit: `4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f`.
- Clone: `DigiPhantStarter`, beside Assets; Git reports no local changes.
- Imported `Elephant`, `Elephant.meta`, `DigiPhant`, and `DigiPhant.meta` into Assets. Existing student assets were preserved.
- All 124 supplied checksum entries verified: 87 text entries required CRLF-to-LF normalization because of Windows Git checkout. There were no other mismatches. All 123 imported files were then byte-compared to the local source; the remaining entry is the pose model kept outside Assets.
- Optional SavannahCourse was not imported.

## Local environment

- Installed editor and prior project logs identify Unity `6000.6.3f1`.
- Package manifest contains URP `17.6.0`; quality settings reference existing render pipeline assets. No render or input settings were changed.
- `ProjectSettings/ProjectVersion.txt` was absent at initial inspection.
- Git is available. Git inspection uses a per-command safe.directory exception for this specific user-owned clone; no global trust setting was changed.
- The Python launcher is installed, but `py -0p` reports no installed interpreters for the agent account. No tracking virtual environment exists.
- Required pins: MediaPipe `0.10.35`, opencv-contrib-python `4.14.0.94`. Neither has been installed or tested.
- Camera availability and permissions have not been verified. Default camera index is 0, with three performers in the reference scene.

## Finish Python setup

Install a compatible 64-bit Python in your Windows account. The reference uses Python 3.14. First run `py -0p` in your own PowerShell to check whether Python is already installed there; the agent account may have different visibility. From the project root, if Python 3.14 is available:

```powershell
py -3.14 -m venv .\DigiPhantStarter\Tracking\.venv
& .\DigiPhantStarter\Tracking\.venv\Scripts\python.exe -m pip install -r .\DigiPhantStarter\Tracking\requirements.txt
& .\DigiPhantStarter\Tracking\.venv\Scripts\python.exe -m pip check
& .\DigiPhantStarter\Tracking\.venv\Scripts\python.exe -c "import sys, cv2, mediapipe; print(sys.executable); print(mediapipe.__version__, cv2.__version__)"
& .\DigiPhantStarter\Tracking\.venv\Scripts\python.exe -m unittest discover -s .\DigiPhantStarter\Tracking -p 'test_*.py'
```

Do not recreate an existing environment. Network access from the agent shell to GitHub failed earlier; package download access has not been verified.

## Run and calibrate

Open `Assets/DigiPhant/Scenes/DigiPhant.unity`. Press Play and use Test sliders for an initial rig check. The camera launcher and recording exporter already resolve the expected Windows `.venv/Scripts/python.exe` path.

After dependencies are installed, Play automatically launches the bridge. For a solo first test choose 1 person, Seated / upper body, then Camera. Keep shoulders and hands visible. Choose Set neutral pose (10 seconds), hold still and wait for calibration. Test one gesture at a time. Change group size or tracking mode, or use Reassign people, then recalibrate. Stop Play to stop Unity's bridge.

For an independent camera test:

```powershell
& .\DigiPhantStarter\Tracking\.venv\Scripts\python.exe .\DigiPhantStarter\Tracking\bridge.py --people 1 --upper-body-only
```

Press Q in that preview to stop it before using Unity auto-launch. R resets identities; recalibrate in Unity afterward. If camera 0 fails, test `--camera 1` and adjust the scene's cameraIndex accordingly.

## Data path and roles

Webcam -> Python/OpenCV -> MediaPipe pose estimation -> six body-relative signals -> localhost UDP JSON -> Unity confidence checks and neutral calibration -> mapped elephant bones and locomotion. UDP ports 5055/5056 carry tracking/configuration; 5057 carries a compressed annotated preview. Measurements are image-relative, not world-space coordinates.

Three-person defaults: P1 front legs and travel/steering; P2 rear legs and tail; P3 head, trunk and ears. Seated mode substitutes hand raises for foot lifts and shoulder tilt for lean. Tracking loss stops locomotion and fades affected controls; recovery and person crossings still need live testing.

## Recording and student Git

The imported integration includes DigiPhantRecording and the Python encoder remains in Tracking. After live control works, keep Game view visible, select Record performance, then Stop recording and save. Verify `Recordings/<session>/performance.mp4` plays and shows both camera and elephant. Upload the checked MP4 to the team's Drive manually and add its link to README.

A lightweight Unity `.gitignore` and reconstruction README were added. Shared Elephant assets, starter clone, environments, recordings and Unity caches are excluded. Do not push to the instructor repository. No student remote, commit, or fresh-checkout reconstruction test has been completed.

## Verification limitations

Passed: clean clone inspection, supplied checksums including bundled model, exact imported file comparisons, launcher path inspection.

Pending: Python dependencies/imports/tests, Unity compilation and scene validation, material appearance, webcam access, live control/calibration, tracking loss/recovery, three-person performance, real Game view recording/export/playback and fresh-checkout reconstruction. A Unity batch compilation check was attempted; see `Logs/DigiPhantSetupCheck.log` for its result.
