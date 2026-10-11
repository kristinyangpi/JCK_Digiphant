# Savannah Course installation — 2026-10-08

> Historical installation record below. For current controls, use DRIVER_NAVIGATION.md. The current stage radius is 30, not the original 15 described below. The scene now has floating candy platforms and a directed walking finish celebration. Original interaction positions and ground height are preserved; broad ground/Stage renderers are hidden. Navigation has no added platform-edge or falling constraint.
>
> Latest validation: Logs/FloatingCoursePolish.log, FLOATING_FINAL_ALL_CHECKS_PASSED, exit 0. Pre-change scene backup: Logs/DigiPhant-before-floating-route-20261011-094319.unity (local only). Open Assets/DigiPhant/Scenes/DigiPhant.unity; installation/polish helpers are local maintenance tools, not a repeatable setup procedure.

- Project: D:/Project_mdes/TM_PE/JACK_Digiphant
- Unity: 6000.6.3f1; URP 17.6.0 (active quality pipeline PC_RPAsset).
- Source: https://github.com/kommanderpi/studentstarter, main commit 4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f.
- Separate source checkout: D:/Project_mdes/SavannahCourse-main-20261008. Existing repositories preserved.
- Imported full SavannahCourse and sibling SavannahCourse.meta into Assets, preserving included metadata.
- Working scene: Assets/DigiPhant/Scenes/DigiPhant.unity, identified from the editor's saved scene setup.
- Exact pre-injection backup: Assets/DigiPhant/Scenes/DigiPhant_BeforeSavannah.unity.
- Integration: existing DigiPhantLocomotion.travelRoot references Elephant Travel. Controller and locomotion sources match reference main; no adapter or control changes needed.

## Verification

Supplied InjectFromCommandLine and ValidateImportedBundle both exited successfully. Logs: Logs/SavannahInjection.log and Logs/SavannahValidation.log.
Passed: SAVANNAH_BUNDLE_OK, SAVANNAH_VALIDATION_OK, SAVANNAH_SCENE_COPY_OK, SAVANNAH_INJECTION_OK, SAVANNAH_IMPORT_OK.
Existing file hash audit: only the intended scene changed. Backup hash equals the original scene. All 140 original scene YAML documents remain unchanged except the SceneRoots list, which adds the course. Imported metadata matches the source. Pink elephant, gold earrings, camera, controls, tracking/model files and project settings preserved.
Course: fixed layout 4, seed 20261005, 144.28-unit route, 18 trees, 4 decorative landmarks. No generated colliders, runtime behaviours, sound or animation.

## Known movement limitation

Existing stageRadius remains 15. The full course needs approximately 30; its finish at local (-16,0,18) lies about 24.08 units from the start and is outside the current limit. No automatic boundary adjustment was made. Movement sets position directly and fixes height to starting Y. Scenery has no collision behaviour.

## Test in Unity

1. Open Assets/DigiPhant/Scenes/DigiPhant.unity and press Play.
2. Choose Test sliders, enable locomotion, then use Travel and Turn sliders.
3. Check the pink elephant and earrings, existing camera, and nearby course scenery. Stop moving or Return to starting position when needed.
4. For live tracking, choose Camera, select performer count and body mode, then Set neutral pose (10 seconds). Existing travel/steering mappings use performer 1 left-hand height and lean.
5. The full course cannot be traversed with radius 15. Discuss a separate radius adjustment before attempting the finish.

Live Play-mode camera/body tracking and complete course traversal have not been verified during installation. Automated validation does not replace this check.
