# JACK DigiPhant

Unity movement-controlled elephant project. See [DIGIPHANT_SETUP.md](DIGIPHANT_SETUP.md) for setup and verification status.

Open `Assets/DigiPhant/Scenes/DigiPhant.unity` for the current customized elephant and Savannah course.

GitHub: https://github.com/nobodybutjo/JCK-Digiphant . The Drive performance link has not been configured. Recordings stay outside Git.

## Restore shared assets

From the Unity project root:

```powershell
git clone https://github.com/kommanderpi/studentstarter.git DigiPhantStarter
git -C DigiPhantStarter checkout 4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f
Copy-Item -LiteralPath .\DigiPhantStarter\Elephant -Destination .\Assets\Elephant -Recurse
Copy-Item -LiteralPath .\DigiPhantStarter\Elephant.meta -Destination .\Assets\Elephant.meta
```

Only restore Elephant if its destination folder and metadata are absent. The student repository should track imported `Assets/DigiPhant`; do not overwrite those tracked files with reference copies. Create the tracking environment following DIGIPHANT_SETUP.md. The model is included in the starter clone.

## Current navigation
See DRIVER_NAVIGATION.md. Install Python dependencies using StudentTracking/README.md; the custom hand/pose models are included.
After restoring the shared Elephant folder above, copy SharedAssetOverrides/Elephant into Assets/Elephant (merge/overwrite materials only) to restore the customized appearance. Do not replace the current DigiPhant scene with the reference scene.

## Three-person roles

The mirrored camera preview is divided into fixed zones: **left = P1 driver; center = P2 mouth/eating; right = P3 lollipop/flight**. Empty zones do not block the other roles. See [DRIVER_NAVIGATION.md](DRIVER_NAVIGATION.md) for the complete test sequence.

- P1 keeps the existing hand navigation and automatic recovery after tracking loss.
- P2 opens their mouth to pick up/eat watermelons. Holding a melon permits driving; the eating animation pauses travel.
- P3 shows the specific printed [lollipop reference](StudentTracking/lollipop.png) to collect a nearby candy. The first eaten candy adds a birthday hat; the second enlarges the ears and unlocks synchronized sideways arm flapping for vertical flight. Stop flapping to land slowly.
- P2 and P3 share exclusive trunk ownership. Candy pickup and flight pause horizontal travel without clearing P1 calibration.

The existing scene, pink elephant, earrings, camera placement, and navigation are retained. The floating candy backdrop below adds scenery around the original course; no finish dance was imported.

P2 is adapted from https://github.com/celenashen04-boop/JCK_digiphant/tree/digiphant-integration (eating/animation overlay only). P3 is adapted from https://github.com/kristinyangpi/JCK_Digiphant at main commit `66065793cfe581f3648e31c7e16ac499ad48d623` (printed-card verification, candy rewards, and vertical flight only).

## Watermelon clearance fix

Fix `291324e` keeps loose watermelons clear of the animated legs and torso, offsets carried fruit from the trunk tip, and checks clearance when fruit is released. Navigation controls are unchanged.

## Floating candy backdrop

The original Savannah route is retained, including its mesh, height, object positions, and P1/P2/P3 control settings. Visual-only decorations from Yangpi's **Candy Wonderland** surround it. The castle and three clouds are positioned in the starting Game camera view, so decorations are visible immediately. Other cloud islands, waterfalls, candy bridges, and balloons remain around the course and become visible as you travel. The skybox is changed without moving the existing camera.

This imports artwork from `kristinyangpi/JCK_Digiphant` commit `66065793cfe581f3648e31c7e16ac499ad48d623`, not its elevated route or gameplay. The decoration root `Yangpi - Floating Candy Backdrop` contains no colliders or gameplay scripts. Bridges are background scenery, not a new route.

The installer `StudentWork.Editor.YangpiBackgroundIntegration.BatchInstallAndValidate` and temporary source scene are local maintenance tools, not included in the shared repository. The shared scene and its used art assets contain the installed backdrop. The original pre-backdrop backup is `Logs/DigiPhant-before-floating-backdrop-20261010-162828.unity`. The current Game-camera preview is `Logs/Backdrop-visible-game.png`; the wider overview is `Logs/CandyBackdrop-overview.png`.

## Verification and backup

The visibility adjustment passed with Unity exit code 0 in `Logs/BackdropVisibilityVerified.log` (`BACKDROP_VISIBILITY_OK`). It moves the castle and three clouds into the starting view while preserving the original route, controls, and camera transform. Backup: `Logs/DigiPhant-before-visible-backdrop-20261010-215419.unity`. The visibility helper is a local maintenance tool and is not shared.

Final backdrop checks passed with Unity exit code 0 in `Logs/CandyBackdropVerified.log`: `CANDY_BACKDROP_VALIDATION_OK`, `WATERMELON_ALL_CHECKS_PASSED`, and `CANDY_BACKDROP_ALL_CHECKS_PASSED`. Checks verify unchanged route geometry, original transforms and control configuration, decoration-only components, and watermelon/P1/P2/P3 regressions. This does not establish live-camera recognition reliability.

Watermelon validation passed with Unity exit code 0: `Logs/WatermelonCollisionValidation.log` contains `WATERMELON_ALL_CHECKS_PASSED`. Checks cover loose-fruit clearance, carried offset, release, and P1/P2/P3 regressions. Real-camera visual confirmation remains a separate step.

52 Python tests passed for the tracking integration. Unity validation passed with exit code 0 in `Logs/P3AllValidationRetry.log`: `P3_VALIDATION_OK`, `P2_EATING_VALIDATION_PASSED`, `AUTO_RECOVERY_VALIDATION_OK`, and `P3_ALL_VALIDATION_OK`. An additional locomotion evaluation check confirmed that flight raises the movement root, the camera follows, P1 calibration persists, tracking loss leads to landing, and slider mode ignores flight. Live three-person camera interaction has not yet been verified; automated checks do not establish real-camera recognition reliability.

Pre-P3 scene backup: `Logs/DigiPhant-before-P3-20261009-175954.unity` (local, excluded from Git). The previous P2 backup is `Logs/DigiPhant-before-P2-eating-20261009-105411.unity`.

