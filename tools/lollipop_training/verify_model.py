"""Screen exported MediaPipe compatibility and synthetic detections, without a camera."""
import argparse
import json
from pathlib import Path
import time
import cv2
import mediapipe as mp
import numpy as np


def iou(a,b):
    x,y,w,h=a; X,Y,W,H=b
    intersection=max(0,min(x+w,X+W)-max(x,X))*max(0,min(y+h,Y+H)-max(y,Y))
    return intersection/max(w*h+W*H-intersection,1e-9)


def main():
    root=Path(__file__).resolve().parent
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--model',type=Path,default=root/'model/lollipop_detector.tflite')
    parser.add_argument('--data',type=Path,default=root/'data/test')
    parser.add_argument('--source',type=Path,default=root.parents[1]/'lollipop.png')
    parser.add_argument('--output',type=Path,default=root/'model/compatibility_screening')
    parser.add_argument('--threshold',type=float,default=.5)
    args=parser.parse_args(); args.output.mkdir(parents=True,exist_ok=True)
    labels=json.loads((args.data/'labels.json').read_text())
    boxes={image['id']:[] for image in labels['images']}
    for annotation in labels['annotations']: boxes[annotation['image_id']].append(annotation['bbox'])
    options=mp.tasks.vision.ObjectDetectorOptions(base_options=mp.tasks.BaseOptions(model_asset_path=str(args.model),delegate=mp.tasks.BaseOptions.Delegate.CPU),running_mode=mp.tasks.vision.RunningMode.IMAGE,score_threshold=args.threshold)
    records=[]; previews=[]; latencies=[]
    def detect(detector,bgr):
        image=mp.Image(image_format=mp.ImageFormat.SRGB,data=np.ascontiguousarray(cv2.cvtColor(bgr,cv2.COLOR_BGR2RGB)))
        start=time.perf_counter(); result=detector.detect(image); latencies.append((time.perf_counter()-start)*1000)
        detections=[]
        for detection in result.detections:
            box=detection.bounding_box
            for category in detection.categories:
                if category.category_name=='lollipop':
                    detections.append({'bbox':[box.origin_x,box.origin_y,box.width,box.height],'score':float(category.score),'label':category.category_name})
        return detections
    def preview(bgr,detections,groundtruth,name):
        image=bgr.copy()
        for box in groundtruth:
            x,y,w,h=map(int,box); cv2.rectangle(image,(x,y),(x+w,y+h),(0,255,0),2)
        for detection in detections:
            x,y,w,h=map(int,detection['bbox']); cv2.rectangle(image,(x,y),(x+w,y+h),(0,140,255),2)
            cv2.putText(image,f'{detection["score"]:.2f}',(x,max(16,y)),cv2.FONT_HERSHEY_SIMPLEX,.5,(0,140,255),1)
        image=cv2.resize(image,(256,256)); cv2.putText(image,name,(8,18),cv2.FONT_HERSHEY_SIMPLEX,.45,(255,255,255),2)
        previews.append(image)
    with mp.tasks.vision.ObjectDetector.create_from_options(options) as detector:
        for item in labels['images']:
            path=args.data/'images'/item['file_name']; bgr=cv2.imread(str(path))
            if bgr is None: raise ValueError(f'Unreadable image: {path}')
            detections=detect(detector,bgr); groundtruth=boxes[item['id']]
            matched=sum(any(iou(box,d['bbox'])>=.5 for d in detections) for box in groundtruth)
            records.append({'image':item['file_name'],'ground_truth':groundtruth,'detections':detections,'matched_targets_iou_05':matched})
            if len(previews)<24: preview(bgr,detections,groundtruth,item['file_name'])
        source=cv2.imread(str(args.source))
        if source is None: raise ValueError('Source unreadable')
        source_detections=detect(detector,source)
        negative=np.full((512,512,3),(205,125,220),dtype=np.uint8)
        negative_detections=detect(detector,negative)
    positive=[r for r in records if r['ground_truth']]; negative_records=[r for r in records if not r['ground_truth']]
    report={'kind':'MediaPipe CPU compatibility and synthetic screening','mediapipe_version':mp.__version__,'threshold':args.threshold,'positive_images':len(positive),'positive_images_matched_iou_05':sum(r['matched_targets_iou_05']>0 for r in positive),'negative_images':len(negative_records),'negative_images_with_false_trigger':sum(bool(r['detections']) for r in negative_records),'latency_ms':{'mean':float(np.mean(latencies)),'median':float(np.median(latencies)),'p95':float(np.percentile(latencies,95))},'source_image_detections':source_detections,'solid_pink_negative_detections':negative_detections,'images':records,'real_camera_accuracy_measured':False,'limitation':'Synthetic held-out transforms share the training source. Source-image detection is an in-distribution check; these results do not measure real-world generalization.'}
    (args.output/'results.json').write_text(json.dumps(report,indent=2)+'\n')
    if previews:
        while len(previews)%8: previews.append(np.zeros((256,256,3),np.uint8))
        cv2.imwrite(str(args.output/'detections.jpg'),np.concatenate([np.concatenate(previews[i:i+8],axis=1) for i in range(0,len(previews),8)],axis=0))
    print(json.dumps({k:v for k,v in report.items() if k!='images'},indent=2))


if __name__=='__main__': main()
