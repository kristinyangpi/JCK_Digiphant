"""Train a multiscale SIFT reference model for the supplied printed image.

This is descriptor/template training, not neural-network training. It recognizes
this specific image; synthetic checks do not establish live camera accuracy.
"""
import argparse
import hashlib
from pathlib import Path
import cv2
import numpy as np

WIDTHS = (120, 180, 280, 500)


def train_reference(path):
    image = cv2.imread(str(path))
    if image is None:
        raise ValueError(f'Reference unreadable: {path}')
    sift = cv2.SIFT_create(nfeatures=1200)
    variants = []
    for width in WIDTHS:
        scaled = cv2.resize(image, (width, round(image.shape[0]*width/image.shape[1])))
        for mirrored in (False, True):
            reference = cv2.flip(scaled, 1) if mirrored else scaled
            gray = cv2.cvtColor(reference, cv2.COLOR_BGR2GRAY)
            points, features = sift.detectAndCompute(gray, None)
            variants.append((reference, gray, np.float32([p.pt for p in points]), features, mirrored))
    return variants


def source_hash(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def save_model(path, output):
    variants = train_reference(path)
    values = dict(source_sha256=np.array(source_hash(path)), format_version=np.array(1))
    for i, (reference, gray, points, features, mirrored) in enumerate(variants):
        values[f'image_{i}'] = reference
        values[f'points_{i}'] = points
        values[f'features_{i}'] = features
    np.savez_compressed(output, **values)
    return variants


def load_model(path, model):
    with np.load(model, allow_pickle=False) as data:
        if int(data['format_version']) != 1 or str(data['source_sha256']) != source_hash(path):
            raise ValueError('Reference model does not match source image; retrain it.')
        variants = []
        for i in range(len(WIDTHS)*2):
            image = data[f'image_{i}']
            variants.append((image, cv2.cvtColor(image, cv2.COLOR_BGR2GRAY),
                             data[f'points_{i}'], data[f'features_{i}'], bool(i % 2)))
        return variants


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', type=Path, default=Path(__file__).with_name('lollipop.png'))
    parser.add_argument('--output', type=Path, default=Path(__file__).with_name('lollipop.reference.npz'))
    args = parser.parse_args()
    variants = save_model(args.image, args.output)
    print(f'Trained {len(variants)} reference views: {sum(len(v[2]) for v in variants)} descriptors')
    print(f'Saved {args.output}')
