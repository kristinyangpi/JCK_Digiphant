"""Train/export an isolated MediaPipe detector; evaluate synthetic data only."""
import argparse
import json
from pathlib import Path
import numpy as np


def import_object_detector(detector_only=False):
    if not detector_only:
        from mediapipe_model_maker import object_detector
        return object_detector
    # Root imports unrelated text classification and tensorflow-text eagerly.
    # Import the unchanged detector package without those root side effects.
    import importlib
    import importlib.metadata
    import importlib.machinery
    import sys
    import types
    distribution = importlib.metadata.distribution('mediapipe-model-maker')
    if distribution.version != '0.2.1.4':
        raise RuntimeError('Detector-only import is reviewed for Model Maker 0.2.1.4 only')
    name = 'mediapipe_model_maker'
    if name not in sys.modules:
        directory = Path(distribution.locate_file(name)).resolve()
        if not (directory/'python/vision/object_detector/__init__.py').is_file():
            raise RuntimeError('Installed Model Maker detector package not found')
        package = types.ModuleType(name)
        package.__path__ = [str(directory)]
        package.__package__ = name
        package.__file__ = str(directory/'__init__.py')
        package.__spec__ = importlib.machinery.ModuleSpec(name, loader=None, is_package=True)
        package.__spec__.submodule_search_locations = package.__path__
        sys.modules[name] = package
    return importlib.import_module(name+'.python.vision.object_detector')


def load_with_negatives(folder, cache_dir, object_detector):
    """Keep empty-box examples without modifying Model Maker's global loader.

    Model Maker 0.2.1.4's COCO writer skips negatives. This scoped subclass
    retains them, using the installed package's feature helpers and cache format.
    """
    import collections
    import importlib.metadata
    import tensorflow as tf
    from mediapipe_model_maker.python.vision.object_detector import dataset_util
    from official.vision.data import tfrecord_lib
    version = importlib.metadata.version('mediapipe-model-maker')
    if version != '0.2.1.4':
        raise RuntimeError(f'Negative-preserving loader reviewed for Model Maker 0.2.1.4; found {version}')
    labels = json.loads((folder/'labels.json').read_text())
    grouped = collections.defaultdict(list)
    for annotation in labels['annotations']:
        grouped[annotation['image_id']].append(annotation)
    expected = len(labels['images'])
    expected_negative = sum(not grouped[image['id']] for image in labels['images'])

    class NegativePreservingWriter(dataset_util.COCOCacheFilesWriter):
        def _get_example(self, data_dir):
            for image in labels['images']:
                encoded = (Path(data_dir)/'images'/image['file_name']).read_bytes()
                decoded = tf.io.decode_jpeg(encoded, channels=3)
                height, width, _ = decoded.shape
                features = tfrecord_lib.image_info_to_feature_dict(height, width, image['file_name'], image['id'], encoded, 'jpg')
                boxes, skipped = dataset_util._coco_annotations_to_lists(grouped[image['id']], height, width)
                if skipped:
                    raise ValueError(f'Invalid annotation: {image["file_name"]}')
                # Explicit list types also support empty lists; the upstream
                # helper infers from value[0] and fails on negative examples.
                for coordinate in ('xmin','xmax','ymin','ymax'):
                    features['image/object/bbox/'+coordinate] = tfrecord_lib.convert_to_feature(boxes[coordinate], value_type='float_list')
                features['image/object/class/label'] = tfrecord_lib.convert_to_feature(boxes['category_id'], value_type='int64_list')
                yield tf.train.Example(features=tf.train.Features(feature=features))

    cache = dataset_util.get_cache_files_coco(str(folder), str(cache_dir/'negatives_v1'))
    # Always regenerate this scoped cache, preventing stale positive-only data.
    NegativePreservingWriter(label_map=dataset_util.get_label_map_coco(str(folder))).write_files(cache, str(folder))
    actual = negative = 0
    for record in tf.data.TFRecordDataset(cache.tfrecord_files):
        example = tf.train.Example.FromString(bytes(record.numpy()))
        count = len(example.features.feature['image/object/class/label'].int64_list.value)
        actual += 1
        negative += count == 0
    dataset = object_detector.Dataset.from_cache(cache)
    if actual != expected or negative != expected_negative or len(dataset) != expected:
        raise RuntimeError(f'{folder.name}: expected {expected}/{expected_negative} total/negative; got {actual}/{negative}, Dataset {len(dataset)}')
    print(f'{folder.name}: verified {actual} cached examples, including {negative} negatives', flush=True)
    return dataset, {'total':actual, 'negative':negative}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    root=Path(__file__).resolve().parent
    parser.add_argument('--data',type=Path,default=root/'data')
    parser.add_argument('--output',type=Path,default=root/'model')
    parser.add_argument('--epochs',type=int,default=30)
    parser.add_argument('--batch-size',type=int,default=8)
    parser.add_argument('--check-data-only',action='store_true')
    parser.add_argument('--detector-only-import',action='store_true',help='Scoped Apple Silicon workaround for unused tensorflow-text root import; Model Maker 0.2.1.4 only')
    args=parser.parse_args()
    object_detector = import_object_detector(args.detector_only_import)
    args.output.mkdir(parents=True,exist_ok=True)
    loaded={name:load_with_negatives(args.data/name,args.output/'cache'/name,object_detector) for name in ('train','val','test')}
    datasets={name:value[0] for name,value in loaded.items()}
    counts={name:value[1] for name,value in loaded.items()}
    (args.output/'dataset_counts.json').write_text(json.dumps(counts,indent=2)+'\n')
    if args.check_data_only:
        return
    options=object_detector.ObjectDetectorOptions(supported_model=object_detector.SupportedModels.MOBILENET_MULTI_AVG,hparams=object_detector.HParams(epochs=args.epochs,batch_size=args.batch_size,export_dir=str(args.output)))
    model=object_detector.ObjectDetector.create(train_data=datasets['train'],validation_data=datasets['val'],options=options)
    loss,metrics=model.evaluate(datasets['test'],batch_size=args.batch_size)
    result={'evaluation_kind':'synthetic held-out transformations of a single source image','real_world_validation':False,'epochs':args.epochs,'batch_size':args.batch_size,'loss':loss,'coco_metrics':metrics,'limitation':'Shared source across splits: results cannot establish performance on real camera scenes or unseen lollipops.'}
    def serial(value):
        if hasattr(value,'numpy'): value=value.numpy()
        if isinstance(value,np.ndarray): return value.tolist()
        if isinstance(value,np.generic): return value.item()
        return str(value)
    (args.output/'synthetic_evaluation.json').write_text(json.dumps(result,indent=2,default=serial)+'\n')
    model.export_model(model_name='lollipop_detector.tflite')
    print(json.dumps(result,indent=2,default=serial))
    print('Export:',args.output/'lollipop_detector.tflite')


if __name__=='__main__': main()
