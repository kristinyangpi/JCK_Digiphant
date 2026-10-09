# JACK DigiPhant

Unity movement-controlled elephant project. See [DIGIPHANT_SETUP.md](DIGIPHANT_SETUP.md) for setup and verification status.

Open `Assets/DigiPhant/Scenes/DigiPhant.unity` to try the supplied reference. Duplicate it in Unity under `Assets/StudentWork` before adapting it.

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

Validated: 26 Python tests and Unity recovery/steering checks. Real-camera steering and three-person reliability still need user testing.
