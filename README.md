# JCK DigiPhant — group performance

An adapted Unity elephant game performed by **P1 Junling, P2 Celena and P3 Kristin**. The team navigates the candy course, eats food, waters flowers, unlocks flight and crosses the finish for a tutu ballet celebration.

## Submission and evidence

- [Adapted Unity repository / performance branch](https://github.com/kristinyangpi/JCK_Digiphant/tree/group-game-updates)
- [Performance video](https://drive.google.com/file/d/1nuq7_dsvGaAe9VmaOa4Y9Fo7RMF0Ze9G/view?usp=drive_link)
- [Player instruction sheet (PDF)](docs/DigiPhant_Player_Guide.pdf)
- [Editable contribution / gesture table](https://www.figma.com/design/POATy3qU8Iys82aYlSN0xO?node-id=2-2)
- [Editable formation, coordination and mapping discussion](https://www.figma.com/design/POATy3qU8Iys82aYlSN0xO?node-id=5-2)
- [DIGIPHANT_SETUP.md](DIGIPHANT_SETUP.md) is retained unchanged as the original setup/status record. Its historical roles, machine paths and pending checks predate the current game; use the reconstruction instructions below for this submission.

## Reproduce the project

Download only the latest performance-branch snapshot, avoiding heavy files retained in earlier Git history:

```sh
git clone --depth 1 --single-branch --branch group-game-updates https://github.com/kristinyangpi/JCK_Digiphant.git
cd JCK_Digiphant
```

Use **Unity 6000.6.3f1** (see `ProjectSettings/ProjectVersion.txt`). `Packages/manifest.json`, `Packages/packages-lock.json` and ProjectSettings retain the project configuration. Open **`Assets/DigiPhant/Scenes/DigiPhant.unity`**; it is the enabled build scene. Do not replace it with the starter scene.

The supplied heavy Elephant models/textures and pose model are omitted. Restore them from starter commit **`4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f`** with Git and Python 3 installed:

```sh
python3 tools/restore_shared_assets.py
```

On Windows use `py -3 tools/restore_shared_assets.py`. The script clones the starter into ignored `DigiPhantStarter/`, reads assets directly from the pinned commit, verifies the pose SHA-256, restores `Assets/Elephant` with its original `.meta` identities and applies `SharedAssetOverrides/Elephant` materials. It never copies the starter scene or scripts. Existing Elephant imports are preserved, so use a clean checkout for exact reconstruction. The hand and face model files are retained with the adapted bridge.

Create a Python environment (Python **3.14** is the locally verified interpreter):

```sh
python3 -m venv StudentTracking/.venv
StudentTracking/.venv/bin/python -m pip install -r StudentTracking/requirements.txt
StudentTracking/.venv/bin/python -m pip check
StudentTracking/.venv/bin/python -m unittest discover -s StudentTracking -p 'test_*.py'
```

Windows PowerShell equivalents:

```powershell
py -3.14 -m venv StudentTracking/.venv
StudentTracking/.venv/Scripts/python.exe -m pip install -r StudentTracking/requirements.txt
StudentTracking/.venv/Scripts/python.exe -m pip check
StudentTracking/.venv/Scripts/python.exe -m unittest discover -s StudentTracking -p 'test_*.py'
```

Unity's Editor launcher prefers this environment and runs the modified `StudentTracking/bridge.py`. Allow webcam access, close other camera users, open the performance scene, press Play and select three-person Camera control. Keep shoulders/hands visible. P1 holds the displayed neutral calibration; P2 looks straight ahead steadily for two seconds. The Python source, pinned requirements, reference picture, trained SIFT descriptors and tests are included. See [StudentTracking/README.md](StudentTracking/README.md) for diagnostics and reference retraining. No separate untracked Python patch is required.

## Current performance controls

The mirrored preview uses **left=P1, center=P2, right=P3**. Shoulder/face association rejects ambiguous role assignments.

| Performer | Input | Elephant behaviour |
| --- | --- | --- |
| P1 Junling | Raise both hands: palms toward camera for forward, backs toward camera for reverse; extend left/right hand toward camera to steer; lower hands to stop. | Ground navigation and the same horizontal navigation during flight. Palm-size steering is enabled; input smoothing is 6 and gear selection holds 0.35 s. |
| P2 Celena | Open mouth near fruit/log; neutral head does nothing; nod down to drink near basin; return neutral then tilt up to spray stored water. | Trunk pickup/eating, drinking and spray; flowers grow on the supported wet floor. Head triggers are 3° down for 0.1 s and 3° up for 0.15 s, with a neutral return between actions. |
| P3 Kristin | Show the included pink-swirl image near a scene lollipop; hide it 0.8 s before the next. Extend both arms sideways and flap together; stop flapping to land. | Immediate candy eating when reachable; first candy adds a birthday hat, second enlarges ears and unlocks flight. P3 controls elevation/landing while P1 steers. Ears flap in flight instead of walking legs. |

Trunk actions coordinate through exclusive ownership, preventing food/water animations from competing. Ground pickup and drinking are blocked during flight. The third-person camera follows behind and above the elephant. The scene includes background music, interaction/finish effects and the finish tutu ballet. Earrings are removed.

Additional MediaPipe **Hand Landmarker** supports palm orientation/size navigation, and **Face Landmarker** provides P2 mouth/head gestures alongside the starter Pose Landmarker. The lollipop recognizer uses OpenCV SIFT descriptor matching against the supplied image, not a MediaPipe object detector.

## Included / excluded

Custom/adapted C# scripts, scene assets and their Unity `.meta` files are under `Assets/DigiPhant`, `Assets/StudentWork`, `Assets/FloatingCourse`, and supporting project folders. Small material overrides preserve the customized Elephant appearance. Original model assets come only from the pinned starter restore.

Unity Library/Temp/Logs, build outputs, local environments, starter clone, recordings, intermediate frames and scene backups stay out of the submission. The pose model may remain locally after being untracked; `.gitignore` excludes it from future commits. Earlier Git history may still contain that model; no history rewrite is performed. A fresh `git archive` export of the final commit contains only the current tracked submission.

Reconstruction checks do not prove live camera recognition or replace the linked performance evidence. Webcam permissions, platform package availability and visual/audio behaviour should also be checked in Unity on the target machine.
