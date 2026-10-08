# DigiPhant setup

Initial setup: **2026-10-05**. Savannah integration: **2026-10-07**. Dates use America/Los_Angeles.

The starter is integrated into this existing Unity project. Automated Python and isolated Unity validation passed. The Savannah course has since been added to the scene selected in Unity. On October 8, live camera preview, printed-lollipop detection and its UDP-triggered distant-tree status were verified. Calibrated pose locomotion, a detector-triggered pickup near the tree and a real performance recording remain unverified.

## Installed environment

| Component | Local setup |
| --- | --- |
| Computer | Apple Silicon, arm64; macOS 26.5.1, build 25F80 |
| Unity | 6000.6.3f1 |
| Rendering / input | URP 17.7; new Input System |
| Python | Homebrew `/opt/homebrew/bin/python3.14`, version 3.14.8 |
| Virtual environment | `DigiPhantStarter/Tracking/.venv` |
| Tracking dependencies | MediaPipe 0.10.35, opencv-contrib-python 4.14.0.94, NumPy 2.5.3 |
| Starter source | `https://github.com/kommanderpi/studentstarter.git` |
| Pinned revision | `4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f` |

The shell's default Python remains 3.8.1. Use the explicit Homebrew executable when creating this environment, and the virtual-environment executable when running tracking. The installed dependency snapshot is [tools/tracking-requirements-lock.txt](tools/tracking-requirements-lock.txt).

## Project structure and changes

- `DigiPhantStarter/`: pristine source clone beside `Assets`; excluded from the outer student repository.
- `Assets/Elephant/`: shared elephant rig, animations and materials, imported with original metadata; excluded from student Git and recoverable from the pinned starter.
- `Assets/DigiPhant/`: imported runtime controls, camera preview, recording, locomotion, materials, reference scene and editor tools; retained in student Git so adaptations are preserved.
- `Assets/DigiPhant/Scenes/DigiPhant.unity`: selected working scene, now containing the Savannah course.
- `Assets/DigiPhant/Scenes/DigiPhant_BeforeSavannah.unity`: exact pre-injection scene backup, with its own metadata.
- `Assets/StudentWork/DigiPhant.unity`: separate course-free working copy with a unique scene GUID, untouched by Savannah integration.
- `Assets/SavannahCourse/`: course assets and editor tools, retained with metadata in student Git.
- `SavannahStarter/`: separate source clone used for the course import; excluded from student Git.
- `Assets/Scenes/SampleScene.unity`: original scene, preserved.
- `tools/starter_assets.py`: verifies source checksums and restores omitted shared Elephant assets without overwriting existing work.

Project settings and package configuration were preserved. The Savannah course was imported on October 7 as described below. Original asset GUIDs were retained, with no duplicate GUIDs found and all 38 imported scene references resolving.

Build settings still point to SampleScene. Run the working DigiPhant scene by opening it in the Editor; this setup does not configure a DigiPhant built player.

One imported file was adapted: `Assets/DigiPhant/Editor/DigiPhantSetup.cs` now restores Camera input mode after its UDP slider validation step, allowing the subsequent seated-mode harness check to run correctly. Runtime behavior and starter Python source were unchanged. The scene originally imported as the reference is `Assets/DigiPhant/Scenes/DigiPhant.unity`; it now contains the course.

## Savannah course integration

The selected scene received **Inject Course into Open Scene**, using the separate `SavannahStarter` clone at the same pinned revision listed above. All 51 imported package files, including metadata, match that source. The original `DigiPhantStarter` clone was left unchanged.

The generator used **layout 4**, seed **20261005**, with a path length of **144.28 units**, 18 trees and four landmarks. The original course package supplied decorative meshes without collisions, triggers or audio; student obstacle behavior was added later as described below. Existing tracking, performer mappings, camera and locomotion were retained. The single `DigiPhantLocomotion` remains on `Elephant Travel` at `(0, 0.01015, 0)` with yaw zero; the course was anchored to that start automatically.

The pre-injection backup is `Assets/DigiPhant/Scenes/DigiPhant_BeforeSavannah.unity`. Its scene-file SHA-256 is `025440bc73cc8775465178c95f9170858e25970b9febbcec3e949276b3fe0935`. Open this backup to inspect or recover the earlier scene; the independent StudentWork scene also remains course-free.

The course import initially preserved Stage Radius 15. On October 8, the user requested a larger walking area, and the working scene was saved with **Stage Radius 30**. The full yellow path fits within this boundary.

To test, open the selected scene, press Play and choose **Test sliders** first. Enable locomotion and try Travel and Turn while following the reachable path. Then select Camera, match the performer count and Full body/Seated mode, and complete the 10-second neutral calibration before trying the same movements. The original course does not detect completion. Student obstacle proxies now block movement as described below.

Review confirmed that original scene objects and control mappings were retained. Of 93 baseline files, only the selected scene changed; saving also serialized the existing default `upperBodyOnly: 0` and added default URP AdditionalLightData to Key Light without changing its original light values. Package and generator validation produced `SAVANNAH_BUNDLE_OK`, `SAVANNAH_VALIDATION_OK`, `SAVANNAH_SCENE_COPY_OK` and `SAVANNAH_INJECTION_OK`; the course contains 130 renderers, 4,852 triangles, four meshes and nine materials. Rendering was observed outside Play. Post-injection live locomotion, camera control and recording have not been tested.

## Student obstacles and birthday gameplay

The working scene now has **97 separate Student Course Obstacle Proxies** following the original renderers: trees, branches, low canopies, bushes, pond/log and gate posts/beam. Oriented elephant body bounds and conservative footprints are checked through small movement and turn steps. When blocked, steer or reverse away and navigate around the obstacle; there is no automatic pathfinding. Yellow route paint, ground, route and candy are not blockers. The separate proxy bank respects course regeneration without adding components to the original Savannah children. Stage Radius 30, input mappings and the approximately 5.12 m pickup limit are retained.

Backups are `Assets/DigiPhant/Scenes/DigiPhant_BeforeCourseObstacles.unity` and `Assets/DigiPhant/Scenes/DigiPhant_BeforeBirthdayHat.unity`. Updated Unity validation passed the approximately 4.1 m pickup, mouth contact/consumption, hat appearance and reset restoration, plus obstacle sweeps, reverse/bypass, turning, circular boundary projection, overlapping obstacles, high-canopy clearance and saved references. Visual inspection confirmed the hat sits on the head and checked eating from front and side; the tree partly occluded the mouth in one preview. These new gameplay behaviors have automated coverage; an earlier live pickup was confirmed by the user.

## Run and calibrate

On October 8, trunk pickup was connected in `Assets/DigiPhant/Scenes/DigiPhant.unity`; its backup is `Assets/DigiPhant/Scenes/DigiPhant_BeforeLollipopPickup.unity`. The scene launches `tools/lollipop_tracking/bridge.py`, sharing one webcam between pose tracking and printed-reference verification, with pickup events on localhost UDP **5058**. After false positives from the weak single-image trained model, the bridge now verifies the exact project-root `lollipop.png` using SIFT matches, a homography and aligned pink/grayscale pattern checks. Raw model output cannot draw a lollipop label or trigger pickup. This is printed-reference matching, not generic lollipop recognition; no retraining was performed. See [the training README](tools/lollipop_training/README.md) for the original model and static tests.

To pick a candy, show the printed reference until the preview says **Reference verified**, holding it steady for **0.5 seconds**, then walk the elephant close to the tree while keeping the picture visible. A failed distance check remains pending while fresh verified frames arrive, so no hide/show cycle is needed merely to approach. At the user’s request, pickup now uses a bounded animated trunk extension up to **2.5×** its natural approximately **2.05 m** reach, giving a maximum pickup reach of approximately **5.12 m**. The status reports nearest-stick distance and the pickup limit. Stage Radius is **30**. Once pickup begins, the picture can be lowered. After pickup, the trunk curls the candy toward its mouth. Actual tip-to-mouth contact within **0.12 m** is required before the candy shrinks and becomes inactive; a pink birthday hat with yellow dots and a purple pom then grows on the head. Hide/show the reference to pick another unconsumed candy. **Reset picks** restores candy activation, scale and parent transforms and hides the hat. **Test pick nearest reachable candy** bypasses picture detection but still requires a reachable candy. Hiding the picture for **0.8 seconds** rearms detection when applicable.

Live verification sampled 600 no-picture frames over approximately 20 seconds with zero matches, and 601 picture frames with 444 matches. The best reference score was **0.921**, a heuristic match score rather than calibrated model confidence; mean verification time was **14.9 ms**. Six Python regression tests passed. Updated Unity validation passed pending detection while far, approach into reach without hiding the picture, interruption of the hold by absence, trunk pickup, attachment, return and reset. The user confirmed an earlier live pickup; the new obstacle, eating and hat behavior has been verified by automated checks and visual inspection, but has not yet been demonstrated in a complete live performance.

The tree nearest the finish, Acacia14, now has seven pink-and-cream spiral discs in its canopy, saved in `Assets/DigiPhant/Scenes/DigiPhant.unity`. The reusable decoration is `Assets/StudentWork/LollipopTree/PinkSwirlLollipops.prefab`, available through **DigiPhant → Student Props → Add Pink Swirl Lollipop Tree**. The pre-addition backup is `Assets/DigiPhant/Scenes/DigiPhant_BeforeLollipops.unity`; Unity Scene view inspection confirmed the pink-and-cream swirls are visible.

On October 7, a decorative banana peel was added beside the pond in `Assets/DigiPhant/Scenes/DigiPhant.unity` at `(11.8, 0.045, -20.4)`. Its reusable prefab, meshes and materials are in `Assets/StudentWork/BananaPeel/`; `Assets/StudentWork/Editor/BananaPeelSetup.cs` provides the idempotent **DigiPhant → Student Props → Add Banana Peel Beside Pond** menu. The peel has four curled yellow/cream flaps with a brown stem and tips, with no collider, physics or runtime behavior. It is a separate scene root, so course regeneration will not remove it. The pre-addition backup is `Assets/DigiPhant/Scenes/DigiPhant_BeforeBananaPeel.unity`. Review confirmed no original objects were altered apart from the scene root list; compilation, Scene view visual inspection and `BANANA_PEEL_OK` validation passed.

1. Open this project in Unity 6000.6.3f1. Open `Assets/DigiPhant/Scenes/DigiPhant.unity` and select the Game view.
2. Press Play. The Editor launches the Python bridge from the installed environment and displays its camera preview.
3. Begin with **Test sliders** to check individual joints. **Reset body sliders** returns gesture offsets to neutral. With locomotion enabled, **Travel** moves backward/forward, larger forward input runs, and **Turn** steers. **Stop moving** centers travel and turn.
4. Select the performer count, **Full body** or **Seated / upper body**, then **Camera**. Default group size is three; select one person for solo testing.
5. Wait for all performer labels. Click **Set neutral pose (10 seconds)**, get into position and hold still until the countdown finishes. For travel, hold P1's left hand near waist height so it can move both up and down.
6. Test one gesture at a time, then combine actions. Changing group size or movement mode, reassigning people, or returning to the starting position requires fresh calibration.
7. Stop Play to stop the bridge Unity launched. A bridge started independently remains running and must be stopped separately.

Full-body mode needs the required hips, feet, shoulders and hands visible. Seated mode uses shoulders and hands; hand height substitutes for foot-lift inputs and shoulder tilt supplies lean. In solo seated mode, a hand raise may consequently drive several controls together.

| Group size | Default body-part roles |
| --- | --- |
| 1 | P1 controls all parts |
| 2 | P1 front legs; P2 rear legs, tail, head, trunk and ears |
| 3 | P1 front legs; P2 rear legs and tail; P3 head, trunk and ears |
| 4 | Three-person roles, with trunk assigned to P4 |

Default locomotion uses **P1 left-hand height** for forward/backward travel and **P1 sideways lean** for steering. Controls and locomotion sources can be adapted in the Inspector after stopping Play; save the working scene afterward. Test sliders exercise rig controls directly and do not validate camera calibration.

## Data path and recovery

The bridge alone opens the webcam, runs the bundled MediaPipe model locally, assigns performers and sends body-relative measurements to Unity. These are image-coordinate movement signals normalized by torso size, not shared 3D world coordinates. Processing and communication run locally.

Local UDP ports are **5055** for tracking JSON, **5056** for group/mode requests and **5057** for the annotated JPEG preview. The preview fits within 320×240 and updates at up to 10 Hz. Run only one receiving DigiPhant session at a time.

On October 5, Camera 0 returned no frame because macOS camera access was not authorized, and Computer Use permissions were pending. On October 7, the course was observed rendering outside Play. On October 8, live camera preview and printed-lollipop detection were verified; camera permissions are no longer a reported blocker. If camera access fails again, check **System Settings → Privacy & Security → Camera** for the requesting application as actually listed by macOS, close other webcam applications, then retry the preview or restart Play.

For an independent camera test from the project root:

```sh
DigiPhantStarter/Tracking/.venv/bin/python DigiPhantStarter/Tracking/bridge.py --people 1 --camera 0
```

Quit that window with **Q** before returning to automatic Unity launch. Try `--camera 1` if another device is intended, or `--upper-body-only` for seated tracking.

If identities are wrong, choose **Reassign people**, arrange the group visibly and recalibrate. Crossings and occlusion can confuse position-based identities. Missing inputs return affected joints toward rest; unavailable locomotion sources stop travel and turning. Test recovery when people return to view. Locomotion stays on a flat plane and has no foot-planting IK, terrain following or collision navigation.

## Record and submit

After live calibration, keep the Game view visible with both camera preview and elephant. Click **Record performance**, perform, then **Stop recording and save** and wait for the saved result. The recorder is available through the existing runtime integration; it captures the Game view at up to 10 fps and 1280×720, with no microphone audio.

The output is `Recordings/<session>/performance.mp4`. Timestamped frames and session metadata remain beside it for recovery. Export uses the same Python environment and OpenCV `mp4v` codec. Avoid switching away from Game view, minimizing Unity or resizing during capture. Check the saved video's beginning, end, timing and each performer's action.

Retry a failed export with the actual session folder:

```sh
DigiPhantStarter/Tracking/.venv/bin/python DigiPhantStarter/Tracking/encode_recording.py --session "Recordings/SESSION_FOLDER"
```

Upload the checked MP4 separately to the team's Drive folder, verify playback and instructor access, and add its link to the root README. No Drive destination was supplied and no video was uploaded. Recordings and intermediate frames are excluded from Git. See [starter recording instructions](DigiPhantStarter/RECORDING.md) for further recovery details.

## Lightweight Git and reconstruction

The outer project repository is published privately at `https://github.com/kristinyangpi/JCK_Digiphant`. Its `.gitignore` excludes Unity caches, IDE-generated files, virtual environments, recordings, both nested source clones and shared Elephant assets. Retain student scenes and their `.meta` files, adapted DigiPhant files, `Assets/SavannahCourse` and its metadata, `Packages`, `ProjectSettings`, documentation, tools, the printed lollipop reference and custom `lollipop_detector.tflite` export. The `origin` remote points to that repository.

For a future clean checkout, clone the student's actual project repository, enter its root, then run:

```sh
python3 tools/starter_assets.py restore
/opt/homebrew/bin/python3.14 -m venv DigiPhantStarter/Tracking/.venv
DigiPhantStarter/Tracking/.venv/bin/python -m pip install -r tools/tracking-requirements-lock.txt
DigiPhantStarter/Tracking/.venv/bin/python -m pip check
DigiPhantStarter/Tracking/.venv/bin/python -m unittest discover -s DigiPhantStarter/Tracking -v
python3 tools/starter_assets.py check
```

Restore clones the starter at the pinned revision when missing, checks source fingerprints and restores Elephant only when absent. SavannahCourse is retained in student Git and does not require this restore tool. The tool retains tracked DigiPhant adaptations and refuses mismatched or incomplete shared assets. Do not replace adapted DigiPhant files by blindly copying the starter over them. Opening the project in the recorded Unity version regenerates caches. Python installation paths must be adjusted on other machines; the recorded dependency snapshot was validated on this Apple Silicon Mac.

## Verification and remaining checks

Recorded evidence is in [tools/setup-verification.txt](tools/setup-verification.txt).

| Check | Result |
| --- | --- |
| Python dependency consistency | `pip check` passed |
| Supplied Python tests | All 11 passed with local loopback socket access |
| Bundled pose model | Initialized successfully |
| Imported asset checksums | 123 files checked; one intentional editor harness adaptation |
| Lightweight reconstruction | Real temporary Git commit/clone, pinned starter restoration, resolved working-scene references and retained adapted script passed; reconstructed checkout remained clean |
| Unity compilation and validation | Isolated project copy exited 0 with `DIGIPHANT_VALIDATION_OK` and `DIGIPHANT_LOCOMOTION_VALIDATION_OK` |
| Rig and controls | Automated checks covered sliders/reset, calibration, packet validation, UDP, presets and timeout |
| Locomotion | Automated checks covered gaits, travel, turns, additive gestures without accumulation, compound signals, tracking loss, limits and reset |
| Savannah integration | Package/generator validation passed; course rendered outside Play; post-injection live control remains unverified |
| Camera / lollipop detection | October 8: live preview, printed-image detection and UDP-triggered distant-tree status verified |
| Live pose / near-tree pickup | Calibrated pose locomotion and an actual detector-triggered pickup near the tree remain unverified |
| Real Game view recording / Drive | Not verified / not performed |

The isolated Unity run avoids disturbing the open editor's project state. Reconstruction used a local pinned starter clone and the existing Unity PackageCache; fresh-checkout Unity reopening was not tested. Automated results do not establish live rendering quality, camera framing or performance responsiveness. A pre-existing vendor demo `Skybox.mat` references six missing textures; this does not affect the DigiPhant scene.

Complete these remaining live checks:

1. Confirm the working Game view, elephant materials and camera/skeleton preview before the performance.
2. Calibrate a visible solo performer and confirm one gesture produces the intended joint response; repeat with the intended group and formation.
3. Check travel and steering together with body gestures, then deliberately lose tracking and confirm safe stopping and recovery.
4. Approach the tree and demonstrate a detector-triggered pickup; then record a short real performance, open the MP4 and verify timing and combined preview/elephant content.
5. Upload the verified video to Drive, confirm sharing access and add the final link to README.

On October 8, at the user’s request, the working scene’s Stage Radius was increased from 15 to **30 units**. The yellow path’s outer edge reaches approximately 25.69 units from the start, so this covers the complete path with steering room. Movement speeds, input mappings, camera and tracking were retained.

## Large ears and flapping flight (October 8)

The current scene is `Assets/DigiPhant/Scenes/DigiPhant.unity`. Backup before this change: `Assets/DigiPhant/Scenes/DigiPhant_BeforeLargeEars.unity`.

The first lollipop grows the birthday hat. While eating the second, both existing elephant ears grow smoothly to 2.3 times their original size. The second completed eating sequence enables arm-flapping flight. The third lollipop can still be eaten but adds no reward. The feathered wings were removed from the working scene; their assets and earlier backups are preserved.

Person 1 controls flight: keep both shoulders and wrists visible, extend both arms sideways, and flap them up/down together. A static T-pose, crossed/hanging arms, or opposite-direction arm movements do not trigger flight. The bridge sends additional shoulder-relative wrist measurements through the existing pose packet; movement calibration and the six original pose channels remain unchanged. Recognition requires synchronized movement and an up/down reversal. Once recognized, continued synchronized movement refreshes the flight timer.

Flight rises at 1.2 units/second to a maximum 2.2 units above the starting floor. After one second without detected flapping, it lands at 0.65 units/second; losing tracking also leads to landing. Unity world Y is this scene's height axis. Flapping adds no horizontal movement or steering. Existing forward/reverse/steering inputs are suppressed while arms are in the flight pose or the elephant is airborne; ground navigation resumes after landing and lowering the arms. Body collision checks remain active for ascent/descent. An overhead obstacle can block ascent, so start from clear ground. The camera follows height.

Reset picks restores the candies, original ear sizes, and hat state, locks flight and initiates slow landing. Reset position returns the elephant to its starting ground position and clears flapping without removing its earned ear reward. Pickup/eating retain the previous stable mouth target and rotation smoothing.

Validation passed near/4.1-unit pickup and three separate bites at 30/60 fps, reward order and reset; a rendered preview confirmed enlarged ears and no wings. Flight validation passed pose packet extraction/freshness, locked/static/asymmetric gesture rejection, synchronized flap lift, height cap, horizontal suppression state, one-second grace, slow landing on tracking loss and overhead collision. Four Python tests cover sideways geometry, hanging/crossed arms, mirrored/scaled poses and visibility. These checks simulate pose input; a new live webcam flapping test has not been performed.

Open the working scene, enter Play, eat two candies (hide/show the printed picture between bites), then perform the sideways arm flap. The pickup panel shows person 1's flight status. Editor checks: **DigiPhant > Student Props > Validate Lollipop Pickup** and **Validate Flight Controls**. Installer: **Enable Large Ears Flap Flight**.

The subsequent second-bite fix keeps the stick attached during both mouth curling and the entire shrinking bite. The shrinking candy's grip target moves toward the mouth rather than teleporting the candy there. Consumption checks both the trunk tip and candy head at the mouth. Failed mouth contact restores the candy for retry; existing rewards are not regrown during a failed return. Consecutive-candy regression now uses different target positions and checks exact stick attachment each frame at 30/60 fps.

Live second-bite stall: the Unity log identified `IndexOutOfRangeException` in `GrowEars`, because the private ear-size cache could be an empty array rather than null. Original ear sizes are now serialized, and cache repair checks the length before indexing. Regression forces an empty cache before the second bite. **Validate Second Bite in Play Mode** then passed with real runtime `LateUpdate`: both candies disappeared, ears reached 2.3× and flap flight unlocked. This automated Play-mode check temporarily uses test inputs and stops/restores the scene afterward; it is distinct from a webcam-driven performance.

## Raised ribbon finish and confetti

The finish marker in `Assets/DigiPhant/Scenes/DigiPhant.unity` is replaced by a separate `Student Ribbon Finish` object at the original marker's position and orientation. Backup: `Assets/DigiPhant/Scenes/DigiPhant_BeforeRibbonFinish.unity`. The original generated marker is preserved inactive; its old collision proxies are disabled. New poles are 6.2 units tall, spaced 9.2 units apart, with solid body collision checks. A pink ribbon joins them 4.45 units above their base.

A flying elephant crossing the ribbon breaks it in the center: the two inner ends pull apart and fall while their outer ties stay attached. A single burst of 280 multicolored confetti pieces celebrates the finish, with a **YOU FINISHED!** message. Ground walking, passes outside the poles, and crossings away from the ribbon height do not finish. **Reset finish ribbon** restores the ribbon and clears the celebration.

Flight controls remain vertical-only. To finish, eat two lollipops, navigate on the ground into the middle of the gate, then flap both arms sideways together. Rising through the ribbon counts; the ribbon is reachable within the existing 2.2-unit flight-height cap. Horizontal flight controls were not added.

Validation passed missed/ground crossings, vertical and swept plane crossings, one-shot celebration, tied-end/center-break mesh motion, confetti particle emission, reset, pole collision references and clear ascent at gate center. Rendered before/after previews confirmed the gate and confetti appearance. A separate actual Play-mode check drove ascent from simulated flapping, confirmed automatic runtime crossing detection, one ribbon break and live confetti, then restored the scene. Course obstacle validation also passed. This is automated runtime verification, not a new webcam performance test.

Menus: **DigiPhant > Student Props > Install Ribbon Finish Celebration**, **Validate Ribbon Finish**, and **Validate Ribbon Finish in Play Mode**.
