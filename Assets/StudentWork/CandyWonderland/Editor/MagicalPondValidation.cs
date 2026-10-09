using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DigiPhant;
namespace StudentWork.CandyWonderland.Editor {
 [InitializeOnLoad] public static class MagicalPondValidation {
  const string Active="MagicalPondValidation.Active";
  static MagicalPondValidation(){EditorApplication.playModeStateChanged+=Changed;}
  [MenuItem("DigiPhant/Candy Wonderland/Validate magical pond in Play Mode")]
  public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Stop Play before validation");SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;}
  static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Active,false))return;if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Test;if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Active,false);}
  static double webcamStart,lastWebcamLog;static MagicalPond webcamPond;
  [MenuItem("DigiPhant/Candy Wonderland/Position at pond for webcam test")]
  public static void Webcam(){if(!EditorApplication.isPlaying)throw new Exception("Enter Play first");webcamPond=UnityEngine.Object.FindFirstObjectByType<MagicalPond>();var loco=webcamPond.locomotion;var offset=loco.followCamera.transform.position-loco.travelRoot.position;var p=webcamPond.pond.TransformPoint(new Vector3(0,0,-6));p.y=webcamPond.world.GroundHeight(p)+.09515186f;webcamPond.ResetPond();loco.StopMotion();loco.travelRoot.SetPositionAndRotation(p,webcamPond.pond.rotation);loco.followCamera.transform.position=p+offset;webcamPond.controller.SetInputMode(InputMode.Camera);webcamStart=EditorApplication.timeSinceStartup;lastWebcamLog=0;EditorApplication.update-=WebcamTick;EditorApplication.update+=WebcamTick;Debug.Log("POND_WEBCAM_READY: raw landmark rule; no synthetic pose injected");}
  static void WebcamTick(){if(!EditorApplication.isPlaying||!webcamPond){EditorApplication.update-=WebcamTick;return;}double now=EditorApplication.timeSinceStartup;if(now-lastWebcamLog>1){lastWebcamLog=now;float score,quality;bool seen=webcamPond.controller.TryReadPondPose(webcamPond.performer,Time.realtimeSinceStartup,out score,out quality);Debug.Log("POND_WEBCAM_SAMPLE: tracked="+seen+" score="+score.ToString("F2")+" quality="+quality.ToString("F2")+" near="+webcamPond.IsNear()+" state="+webcamPond.State);}if(webcamPond.State==MagicalPond.PondState.PathUnlocked||now-webcamStart>60)EditorApplication.update-=WebcamTick;}
  static double previewStart;static MagicalPond previewPond;static int captureStage;
  [MenuItem("DigiPhant/Candy Wonderland/Preview pond sequence in Play Mode")]
  public static void Preview(){if(!EditorApplication.isPlaying)throw new Exception("Enter Play first, then select Preview pond sequence");previewPond=UnityEngine.Object.FindFirstObjectByType<MagicalPond>();var loco=previewPond.locomotion;previewPond.controller.inputMode=InputMode.TestSliders;loco.testForward=loco.testSteering=0;var p=previewPond.pond.TransformPoint(new Vector3(0,0,-6));p.y=previewPond.world.GroundHeight(p)+.09515186f;var offset=loco.followCamera.transform.position-loco.travelRoot.position;loco.travelRoot.SetPositionAndRotation(p,previewPond.pond.rotation);loco.followCamera.transform.position=p+offset;previewPond.ResetPond();previewPond.BeginInteraction();previewStart=EditorApplication.timeSinceStartup;captureStage=0;EditorApplication.update-=PreviewTick;EditorApplication.update+=PreviewTick;}
  static void PreviewTick(){if(!EditorApplication.isPlaying||!previewPond){EditorApplication.update-=PreviewTick;return;}double time=EditorApplication.timeSinceStartup-previewStart;float[] times={3.8f,7.9f,9.8f,13};if(captureStage<4&&time>times[captureStage]){var camera=previewPond.locomotion.followCamera;var p=previewPond.locomotion.travelRoot.position;var oldPos=camera.transform.position;var oldRot=camera.transform.rotation;var oldRect=camera.rect;try{camera.transform.position=p+previewPond.pond.right*11-previewPond.pond.forward*17+Vector3.up*9;camera.transform.LookAt(previewPond.pond.position+Vector3.up*4);camera.rect=new Rect(0,0,1,1);Capture(camera,"live-pond-"+captureStage+".png");}finally{camera.transform.SetPositionAndRotation(oldPos,oldRot);camera.rect=oldRect;}captureStage++;}if(captureStage>=4){Debug.Log("MAGICAL_POND_LIVE_CAPTURE_OK: "+previewPond.State);EditorApplication.update-=PreviewTick;}}
  static void Require(bool test,string why){if(!test)throw new Exception("MAGICAL_POND_FAILED: "+why);}
  public static void Test(){try{ValidateRuntime(true);}catch(Exception e){Debug.LogException(e);}finally{EditorApplication.isPlaying=false;}}
  public static void ValidateRuntime(bool captures){
   var pond=UnityEngine.Object.FindFirstObjectByType<MagicalPond>();Require(pond,"pond installed");var loco=pond.locomotion;var controller=pond.controller;var preview=controller.GetComponent<DigiPhantCameraPreview>();if(preview)preview.enabled=false;controller.enabled=false;loco.Initialise();var movement=controller.GetComponent<StudentObstacleMovement>();
   var balls=UnityEngine.Object.FindObjectsByType<RollingCandyBall>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled).ToArray();Require(balls.Length==2,"only second active ball replaced");foreach(var ball in balls)ball.solid.enabled=false;
   var initial=loco.travelRoot.position;var initialRot=loco.travelRoot.rotation;var camera=loco.followCamera;var cameraPos=camera.transform.position;var cameraRot=camera.transform.rotation;
   pond.ResetPond();var p=pond.pond.TransformPoint(new Vector3(0,0,-6));p.y=pond.world.GroundHeight(p)+.09515186f;loco.travelRoot.SetPositionAndRotation(p,pond.pond.rotation);
   for(int i=0;i<=8;i++){float x=-4+i;var a=pond.pond.TransformPoint(new Vector3(x,0,-6));a.y=p.y;var rot=pond.pond.rotation;var end=movement.Resolve(a,ref rot,0,Vector3.forward*12);Require(pond.pond.InverseTransformPoint(end).z<-3,"blocked across path width at x="+x);}
   var root=loco.travelRoot.position;
   pond.ObservePose(0,true,.99f,.2f);pond.ObservePose(1,true,.99f,.2f);Require(!pond.ControlsElephant,"occluded pose rejected");pond.ObservePose(2,true,.2f,.99f);pond.ObservePose(3,true,.2f,.99f);Require(!pond.ControlsElephant,"wrong pose rejected");pond.ObservePose(4,true,.95f,.95f);pond.ObservePose(4.2f,true,.95f,.95f);Require(!pond.ControlsElephant,"brief pose rejected");pond.ObservePose(4.3f,false,0,0);pond.ObservePose(5,true,.95f,.95f);pond.ObservePose(5.7f,true,.95f,.95f);Require(pond.ControlsElephant&&pond.AttemptCount==1,"held reference pose starts once");
   Require(!pond.pickup.TryPick(5.7f),"conflicting candy pickup locked");float previous=1;float contact=0;float maxAngle=0;var previousTip=pond.pickup.tip.rotation;
   if(captures){camera.transform.position=p+pond.pond.right*10-pond.pond.forward*14+Vector3.up*8;camera.transform.LookAt(pond.pond.position+Vector3.up*2);camera.rect=new Rect(0,0,1,1);}
   for(int i=0;i<320;i++){pond.Advance(.05f);Require(pond.WaterFraction<=previous+.00001f,"water drains monotonically");previous=pond.WaterFraction;pond.ObservePose(6+i*.05f,true,.95f,.95f);if(pond.ControlsElephant){Require(Vector3.Distance(root,loco.travelRoot.position)<.01f,"no unintended travel");loco.Evaluate(10+i*.05f,.05f);Require(Vector3.Distance(root,loco.travelRoot.position)<.01f,"controller travel blocked during effect");}
    if(i==94)contact=pond.LastTipError;maxAngle=Mathf.Max(maxAngle,Quaternion.Angle(previousTip,pond.pickup.tip.rotation));previousTip=pond.pickup.tip.rotation;
    if(captures&&(i==76||i==158||i==196||i==250))Capture(camera,"pond-"+i+".png");
   }
   Require(pond.State==MagicalPond.PondState.PathUnlocked&&!pond.barrier.enabled&&pond.WaterFraction<.001f,"fully drained and unlocked");Require(contact<.5f,"trunk reaches water; error="+contact);Require(pond.RainbowAlpha>.5f,"rainbow lingers after movement restored");Require(pond.AttemptCount==1,"pose cannot retrigger");
   var rotation=pond.pond.rotation;var crossed=movement.Resolve(p,ref rotation,0,Vector3.forward*12);Require(pond.pond.InverseTransformPoint(crossed).z>3,"dry path can be crossed");
   for(int i=0;i<150;i++)pond.Advance(.05f);Require(pond.RainbowAlpha>.5f,"rainbow persists several seconds");for(int i=0;i<80;i++)pond.Advance(.05f);Require(pond.RainbowAlpha<.01f,"rainbow softly disappears");
   loco.ResetPosition();Require(pond.State==MagicalPond.PondState.Blocked&&pond.barrier.enabled&&pond.WaterFraction==1,"existing return/reset restores pond");
   foreach(var ball in balls){ball.solid.enabled=true;for(int i=0;i<200;i++)ball.Advance(.1f);Require(ball.DistanceRolled>1,"remaining candy ball rolls");}
   loco.travelRoot.SetPositionAndRotation(initial,initialRot);camera.transform.SetPositionAndRotation(cameraPos,cameraRot);
   Debug.Log("MAGICAL_POND_PLAY_OK: pose hold/rejections; route blocked across width; one continuous drain/spray/rainbow; dry route crossing; reset; remaining two balls. Water tip error="+contact.ToString("F3")+"m; max sampled tip delta="+maxAngle.ToString("F2")+"deg");
  }
  static void Capture(Camera camera,string name){string dir=Path.Combine(Path.GetTempPath(),"magical-pond-validation");Directory.CreateDirectory(dir);var target=new RenderTexture(1600,1000,24);var previous=camera.targetTexture;var active=RenderTexture.active;try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(dir,name),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);}}
 }
}
