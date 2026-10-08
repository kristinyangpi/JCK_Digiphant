# Single-image lollipop detector experiment

Training completed on October 8, 2026 using native Python 3.11, TensorFlow 2.15.1 and Model Maker 0.2.1.4, for 30 epochs with batch size 8. The single project-root `lollipop.png` produced 240 synthetic images split 160/40/40, with 20% negatives in each split. The approximately 12 MB export is `tools/lollipop_training/model/lollipop_detector.tflite`, SHA-256 `f0067b77fa5cd28ba229a948755b9dc970f689379c43f427b87b8b0c150ed347`.

Static verification in the actual tracking environment with MediaPipe 0.10.35 detected all 32 positive synthetic test images at IoU ≥ 0.5 and produced no detections on eight negatives at confidence threshold 0.5. The source image scored 0.9934; a solid-pink control produced no detection. These results do not establish real-webcam accuracy. The shared-camera bridge now uses exact printed-reference verification for Unity pickup because this trained model produced live false positives. SIFT/homography and aligned color/pattern checks independently gate labels and events; raw model detections cannot trigger pickup. No retraining was performed. A complete live pickup remains unverified. See [the project setup guide](../../DIGIPHANT_SETUP.md) for controls and integration details.

Repeat static verification from the project root:

```sh
DigiPhantStarter/Tracking/.venv/bin/python tools/lollipop_training/verify_model.py
```

This verification completed on macOS with access to the graphics runtime required by MediaPipe; a restricted sandbox may need that same access.

`prepare_dataset.py` transforms the project-root `lollipop.png` into synthetic
printed-card views with perspective, rotation, scale, lighting, blur, noise and
procedural backgrounds. The manual object annotation includes the candy and its
visible stick. Each split has 20% negative examples, including pink round distractors.
The reproducible splits contain 160 training, 40 validation and 40 test images.
`data/provenance.json` records seeds, source SHA-256 and annotation; inspect
`data/contact_sheet.jpg` before training.

Run preprocessing using the existing tracking environment's Python, without
changing its dependencies:

```sh
DigiPhantStarter/Tracking/.venv/bin/python tools/lollipop_training/prepare_dataset.py
```

Run training with a separate Python environment containing MediaPipe Model Maker:

```sh
PATH_TO_TRAINING_PYTHON tools/lollipop_training/train.py --epochs 30 --batch-size 8
```

The default export is `model/lollipop_detector.tflite`, with Model Maker metadata.
Evaluation is saved in `model/synthetic_evaluation.json`. Generated data, cached
checkpoints and model output remain local, outside Unity Assets. The scripts follow
the [official customization API](https://developers.google.com/edge/mediapipe/solutions/customization/object_detector).

All views originate from one source image, including held-out splits. The test
score measures synthetic transformations only; it cannot establish recognition of
real lollipops or camera performance. Collect independent real images and negatives
before reporting real-world detection accuracy. Training does not alter Unity,
its pose model or its tracking environment.

The pinned Model Maker 0.2.1.4 COCO reader skips empty annotations. Our scoped
cache-writer subclass preserves negative examples with correctly typed empty
feature lists, then verifies the serialized counts and dataset lengths. Run
`train.py --check-data-only` to validate ingestion without starting training.

On native Apple Silicon, `--detector-only-import` optionally imports the installed
detector package without Model Maker's root initializer, which eagerly imports
unrelated text classification requiring a missing native `tensorflow-text` wheel.
This workaround is guarded to Model Maker 0.2.1.4, does not modify installed files,
and uses the original detector implementation. Normal environments use the public
root import by default. A successful detector-only import and data check are still
required before training; this flag does not guarantee dependency compatibility.

The native Apple Silicon environment has now verified all 160/40/40 examples,
including 32/8/8 negatives. Recreate its isolated Python 3.11 environment using
the explicit package lock (outside the tracking environment):

```sh
python3.11 -m venv tools/lollipop_training/.venv
tools/lollipop_training/.venv/bin/python -m pip install --no-deps -r tools/lollipop_training/requirements-macos-arm64-lock.txt
mkdir -p /private/tmp/lollipop-matplotlib
TMPDIR=/private/tmp MPLCONFIGDIR=/private/tmp/lollipop-matplotlib tools/lollipop_training/.venv/bin/python tools/lollipop_training/train.py --detector-only-import --check-data-only
TMPDIR=/private/tmp MPLCONFIGDIR=/private/tmp/lollipop-matplotlib tools/lollipop_training/.venv/bin/python tools/lollipop_training/train.py --detector-only-import --epochs 30 --batch-size 8
```

`--no-deps` is intentional: the lock lists the installed image-training packages
explicitly, while the unavailable native `tensorflow-text` dependency is omitted
because the scoped image-only import does not use text classification. Installing
Model Maker with ordinary dependency resolution would request that text package.
Pretrained weights download from Google's official model storage on first use.
The temporary training runtime can be recreated; persistent exports and metrics
are saved in this project's `model/` directory. Console output from the current
run is local `training.log`, excluded from Git.
