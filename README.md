# JCK DigiPhant

A Unity elephant game with webcam pose controls, Level 1: Lollipop Forest with oversized pink-and-white swirl lollipops, rolling candy obstacles, a fading title introduction, a galaxy floor, and an animated rainbow road, printed-lollipop recognition, pickup/eating animations, a birthday hat, enlarged ears, flapping flight, and a breakable finish ribbon with confetti and a pink-tutu ballet finale.

Repository: [kristinyangpi/JCK_Digiphant](https://github.com/kristinyangpi/JCK_Digiphant).

Open with **Unity 6000.6.3f1**. The working scene is **`Assets/DigiPhant/Scenes/DigiPhant.unity`**. The walking boundary is 30 units. The first lollipop grows a hat, the second enlarges the ears and unlocks vertical flapping flight; the third adds no reward. Flight rises at most 2.2 units above the floor. Navigate between the finish poles on the ground, then flap to rise through the ribbon and trigger confetti, automatic landing, and a short ballet with two pirouettes. Use **Reset finish ribbon** afterward to return to normal controls.

## Clone and restore

The instructor's shared elephant assets and pose model are recovered from [studentstarter](https://github.com/kommanderpi/studentstarter), pinned to `4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f`, following its lightweight student Git instructions. Scenes, custom/adapted scripts, metadata, course assets, Unity settings, the printed reference and custom trained lollipop model are included here. Caches, recordings, local Python environments and training intermediates are excluded.

```sh
git clone https://github.com/kristinyangpi/JCK_Digiphant.git
cd JCK_Digiphant
python3 tools/starter_assets.py restore
python3.14 -m venv DigiPhantStarter/Tracking/.venv
DigiPhantStarter/Tracking/.venv/bin/python -m pip install -r tools/tracking-requirements-lock.txt
DigiPhantStarter/Tracking/.venv/bin/python -m pip check
python3 tools/starter_assets.py check
```

The tracking dependency lock was tested with Python 3.14.8 on Apple Silicon macOS. On Windows, use the virtual environment's `Scripts/python.exe` instead of `bin/python`. The restore command keeps tracked DigiPhant adaptations and existing shared assets intact. Do not copy the starter's DigiPhant scripts over this project's adapted files.

Open the working scene, press Play and choose **Camera** input mode for webcam control. Allow the camera, wait for preview, and use **Set neutral pose (10 seconds)**. Hide the printed lollipop picture between bites. After the second bite, person 1 can extend both arms sideways and flap up/down together. Flapping controls height only; after one second without flapping, the elephant lands slowly.

See [DIGIPHANT_SETUP.md](DIGIPHANT_SETUP.md) for calibration, roles, backups, validation results, recording and remaining live checks. Unity checks include **Validate Lollipop Pickup**, **Validate Flight Controls**, **Validate Second Bite in Play Mode**, **Validate Ribbon Finish** and **Validate Ribbon Finish in Play Mode** under **DigiPhant > Student Props**.

## Verification and recordings

Automated Play-mode checks verified the second bite removes the candy and grows ears, and simulated flapping triggers the ribbon break and live confetti. These checks do not establish a complete webcam-driven performance. Bridge regression tests passed; earlier live checks confirmed printed-image recognition.

Scene backups and the original sample scenes are preserved. Recordings belong on the team's Drive and are excluded from Git. No Drive destination or performance video link has been configured.

The new floating candy-world variant is
`Assets/StudentWork/Scenes/Level1_CandyWonderland.unity`: nine procedural candy
styles, a gently elevated continuous trail, bridges, waterfalls, cloud islands,
balloons and a castle surrounding the existing flying ribbon/ballet finish.
The original `DigiPhant.unity` and its gameplay scripts remain unchanged. Open
the variant in Unity and press Play; use the same pose controls and lollipop
picture. See the Candy Wonderland section of `DIGIPHANT_SETUP.md` for the new
terrain adapter, generation seed, regression checks and recovery instructions.
