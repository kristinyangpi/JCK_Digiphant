using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace StudentWork.Editor {
public static class P2CourseActionsInstall {
 const string PathScene="Assets/DigiPhant/Scenes/DigiPhant.unity";
 static void Check(bool v,string message){if(!v)throw new Exception("P2 course: "+message);}
 static AnimationClip Clip(string n)=>AssetDatabase.LoadAllAssetsAtPath("Assets/Elephant/Animations/elephant@"+n+".fbx").OfType<AnimationClip>().FirstOrDefault(x=>x.name==n);
 [MenuItem("DigiPhant/Validate P2 head drink and spray")]
 public static void ValidateGestures(){
  Check(!EditorApplication.isPlaying,"Stop Play before validating");
  Check(!EditorSceneManager.GetActiveScene().isDirty,"Save your scene edits before validating");
  try{P2HeadInputValidation.Run();Run();}
  finally{EditorSceneManager.OpenScene(PathScene);}
 }
 public static void FinalValidation(){P2HeadInputValidation.Run();Run();VisualChecks();WatermelonCollisionValidation.RunAll();Debug.Log("P2_FINAL_VALIDATION_OK");}
 public static void RunAll(){Install();Run();WatermelonCollisionValidation.RunAll();Debug.Log("P2_COURSE_ALL_VALIDATION_OK");}
 public static void Install(){
  Check(Application.isBatchMode,"Close editor and use batch installer");Directory.CreateDirectory("Logs");
  string backup="Logs/DigiPhant-before-P2-course-actions-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";File.Copy(PathScene,backup);
  var scene=EditorSceneManager.OpenScene(PathScene);var old=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Select(t=>(t,t.position,t.rotation,t.localScale)).ToArray();
  var root=GameObject.Find("P2 course actions")??new GameObject("P2 course actions");
  var d=root.GetComponent<ElephantDrinking>()??root.AddComponent<ElephantDrinking>();
  var s=root.GetComponent<ElephantSpraying>()??root.AddComponent<ElephantSpraying>();
  var l=root.GetComponent<LogEating>()??root.AddComponent<LogEating>();
  d.drinkClip=Clip("drink");s.liftClip=Clip("trumpet");l.eatClip=Clip("eat");Check(d.drinkClip&&s.liftClip&&l.eatClip,"Missing animation clips");d.drinkSeconds=d.drinkClip.length;s.liftSeconds=s.liftClip.length;
  foreach(var x in old)Check(x.t.position==x.position&&x.t.rotation==x.rotation&&x.t.localScale==x.localScale,"Original transform changed: "+x.t.name);
  EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Save failed");File.WriteAllLines("Logs/P2CourseDependencies.txt",AssetDatabase.GetDependencies(PathScene,true));Debug.Log("P2_COURSE_INSTALLED backup="+backup);
 }
 static P2HeadGate Gate(ElephantDrinking d)=>(P2HeadGate)typeof(ElephantDrinking).GetField("gate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(d);
 public static void Run(){
  EditorSceneManager.OpenScene(PathScene);var c=UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();var loco=c.GetComponent<DigiPhantLocomotion>();Check(loco.Initialise(),"Locomotion bind");
  var d=UnityEngine.Object.FindAnyObjectByType<ElephantDrinking>();var s=UnityEngine.Object.FindAnyObjectByType<ElephantSpraying>();var l=UnityEngine.Object.FindAnyObjectByType<LogEating>();Check(d&&s&&l,"Components missing");
  d.Step(0,0); var gate=Gate(d);gate.Read(true,0,2.1f,"test");gate.Read(true,0,.1f,"test");Check(gate.Ready,"Neutral failed");gate.Read(false,20,.5f,"test");Check(!gate.Ready&&gate.Calibrated,"Loss must preserve neutral but invalidate input");gate.Read(true,10,.1f,"test");
  gate.Read(true,0,.1f,"test");d.Step(.5f,0);s.Step(.5f,0);Check(!d.Drinking&&!s.Lifting,"Neutral head triggered water action");
  s.Step(.16f,3.5f);Check(s.Lifting&&!s.WithWater&&ClipOverlay.Owns(s),"Dry lift failed");s.Step(s.liftSeconds+.1f,20);Check(s.DryLiftCount==1&&s.DropletsReleased==0&&!ClipOverlay.Busy,"Dry lift produced water or retained lock");
  gate.Read(true,-20,.1f,"test");s.Step(.4f,20);Check(!s.Lifting,"Held gesture retriggered");
  gate.Read(true,0,.31f,"test");gate.Read(true,20,.1f,"test");
  var owner=new object();ClipOverlay.TryAcquire(owner);d.Step(.4f,20);Check(!d.Drinking,"Foreign action lock ignored");ClipOverlay.Release(owner);
  var head=loco.elephantAnimator.GetComponentsInChildren<Transform>().First(t=>t.name=="elephant_Head_bone");var basin=GameObject.Find(ElephantDrinking.BasinName).transform;Vector3 shift=basin.position-head.position;shift.y=0;loco.travelRoot.position+=shift;
  var flight=c.GetComponent<StudentFlight>();flight.SetUnlocked(true);flight.Observe(1,true,true,0,0);d.Step(.4f,20);Check(!d.Drinking,"Flight conflict ignored");flight.ResetFlight();
  Vector3 before=loco.travelRoot.position;d.Step(.16f,3.5f);Check(d.Drinking&&ClipOverlay.Owns(d),"Reachable drink failed");d.Step(d.drinkSeconds+.1f,0);Check(d.HoldsWater&&d.DrinkCount==1&&!ClipOverlay.Busy&&loco.travelRoot.position==before,"Drink completion drift/lock/water");
  gate.Read(true,0,.31f,"test");gate.Read(true,-20,.1f,"test");s.Step(.4f,20);Check(s.Lifting&&s.WithWater&&!d.HoldsWater,"Spray failed to consume stored water");s.Step(1.5f,0);Check(s.SprayingWater&&s.DropletsReleased>0,"No wet jet");s.Step(s.liftSeconds,0);Check(s.SprayCount==1&&!ClipOverlay.Busy,"Spray completion failed");
  s.Flowers.Step(5);Check(s.Flowers.Count>=5,"Actual water spray did not grow a flower garden");
  Shot(loco,s.Flowers.Flowers[0].position+Vector3.up*.5f,"Spray-flower-garden");
  Watermelon.RemoveAll();l.reach=100;l.facingDegrees=180;before=loco.travelRoot.position;l.Step(.4f,1);Check(l.Eating,"Log eating failed");l.Step(l.TotalSeconds+.1f,0);Check(l.Eaten&&!ClipOverlay.Busy&&loco.travelRoot.position==before,"Log consumption moved root or retained lock");
  var patch=s.Flowers;var pad=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).First(x=>x.name=="Island 0 icing");Vector3 centre=pad.GetComponent<Renderer>().bounds.center;Check(patch.TrySurface(centre,out float y),"No floating top surface");int n=patch.Sown;patch.Sow(new Vector3(centre.x,y+2,centre.z),Vector3.down,9.81f,y);Check(patch.Sown==n+1,"Supported flower rejected");patch.Sow(new Vector3(10000,2,10000),Vector3.down,9.81f,0);Check(patch.Sown==n+1,"Flower planted in void");patch.Step(5);Check(patch.Count>0,"Flowers did not grow");
  loco.StopAnimation();Debug.Log("P2_COURSE_VALIDATION_OK");
 }
 public static void VisualChecks(){
  EditorSceneManager.OpenScene(PathScene);var c=UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();var loco=c.GetComponent<DigiPhantLocomotion>();Check(loco.Initialise(),"Visual bind");Watermelon.RemoveAll();
  var log=GameObject.Find(LogEating.LogName).transform;var l=UnityEngine.Object.FindAnyObjectByType<LogEating>();l.Step(0,0);
  Vector3 forward=loco.travelRoot.forward;forward.y=0;forward.Normalize();
  loco.travelRoot.position+=log.position-l.ReachPoint-forward*.35f;var rootPosition=loco.travelRoot.position;var rootRotation=loco.travelRoot.rotation;
  l.Step(.35f,1);Check(l.Eating&&l.InReach&&l.Facing,"Actual default log range/facing failed");
  var rs=log.GetComponentsInChildren<Renderer>();Check(rs.Any(x=>x.enabled),"Log visuals disabled/batched");float originalWidth=log.localScale.x;
  l.Step(l.TotalSeconds*.4f,0);Check(log.localScale.x<originalWidth&&rs.Any(x=>x.enabled),"Visible log did not shrink");
  Check(loco.travelRoot.position==rootPosition&&loco.travelRoot.rotation==rootRotation,"Mid-bite moved root");Shot(loco,log.position+Vector3.up,"P2-log-midbite");
  l.enabled=false;l.SendMessage("OnDisable");Check(!l.Eaten&&!l.Eating&&log.localScale.x==originalWidth,"Disable did not reset log");l.enabled=true;l.Step(.35f,1);Check(l.Eating,"Reenabled log stuck");l.enabled=false;l.SendMessage("OnDisable");
  var d=UnityEngine.Object.FindAnyObjectByType<ElephantDrinking>();d.Step(0,0);var head=loco.elephantAnimator.GetComponentsInChildren<Transform>().First(x=>x.name=="elephant_Head_bone");var basin=GameObject.Find(ElephantDrinking.BasinName).transform;
  Vector3 shift=basin.position-head.position;shift.y=0;loco.travelRoot.position+=shift;
  var gate=Gate(d);gate.Read(true,0,2.1f,"visual");gate.Read(true,20,.1f,"visual");d.Step(.35f,20);Check(d.Drinking,"Visual drink failed");d.Step(1.5f,0);Shot(loco,basin.position+Vector3.up,"P2-drink");d.Step(d.drinkSeconds,0);
  var spraying=UnityEngine.Object.FindAnyObjectByType<ElephantSpraying>();gate.Read(true,0,.31f,"visual");gate.Read(true,-20,.1f,"visual");spraying.Step(.35f,20);spraying.Step(1.4f,0);Check(spraying.SprayingWater,"Visual spray failed");spraying.Water.Simulate(.2f,true,false);Shot(loco,head.position,"P2-spray");spraying.Step(spraying.liftSeconds,0);
  loco.StopAnimation();Debug.Log("P2_COURSE_VISUAL_CHECKS_OK");
 }
 static void Shot(DigiPhantLocomotion loco,Vector3 target,string filename){
  var camera=loco.followCamera;var position=camera.transform.position;var rotation=camera.transform.rotation;var rect=camera.rect;var old=camera.targetTexture;var active=RenderTexture.active;
  var rt=new RenderTexture(1440,900,24);var texture=new Texture2D(1440,900,TextureFormat.RGB24,false);
  try{camera.rect=new Rect(0,0,1,1);camera.transform.position=target+(filename=="Spray-flower-garden"?new Vector3(0,7,.1f):new Vector3(7,5,-9));camera.transform.LookAt(target);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1440,900),0,0);texture.Apply();File.WriteAllBytes("Logs/"+filename+".png",texture.EncodeToPNG());}
  finally{camera.transform.SetPositionAndRotation(position,rotation);camera.rect=rect;camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
 }

}}
