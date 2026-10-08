using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class RibbonFinishValidation {
  [MenuItem("DigiPhant/Student Props/Validate Ribbon Finish")]
  public static void Validate(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
   var scene=EditorSceneManager.OpenPreviewScene(SceneManager.GetActiveScene().path);
   try{
    var roots=scene.GetRootGameObjects();var f=roots.SelectMany(r=>r.GetComponentsInChildren<RibbonFinish>(true)).Single();
    if(!f.travelRoot||!f.flight||!f.confetti||!f.leftRibbon||!f.rightRibbon)throw new Exception("Finish refs missing");
    if(f.ribbonHeight-f.contactOffset.y>f.flight.hoverHeight)throw new Exception("Ribbon above reachable flight limit");
    Vector3 W(float x,float y,float z)=>f.transform.TransformPoint(new Vector3(x,y,z));
    if(f.CheckCrossing(W(0,2.7f,-2),W(0,2.7f,2),false))throw new Exception("Ground walk triggered finish");
    if(f.CheckCrossing(W(6,4,-.1f),W(6,5,.1f),true))throw new Exception("Outside poles triggered finish");
    if(f.CheckCrossing(W(0,7,-2),W(0,7,2),true))throw new Exception("Missed ribbon height triggered finish");
    if(f.CheckCrossing(W(0,3.5f,3),W(0,5,3),true))throw new Exception("Far takeoff triggered finish");
    Capture(scene,f,"before");
    if(!f.CheckCrossing(W(0,4,.7f),W(0,4.8f,.7f),true)||!f.Finished||f.CelebrationCount!=1)throw new Exception("Vertical flap crossing failed");
    if(f.CheckCrossing(W(0,4,.7f),W(0,4.8f,.7f),true)||f.CelebrationCount!=1)throw new Exception("Repeated celebration");
    var original=f.leftRibbon.sharedMesh.vertices;f.AnimateBreak(1.5f);
    var torn=f.leftRibbon.sharedMesh.vertices;
    if(Vector3.Distance(torn[0],original[0])>.001f || Mathf.Abs(torn[torn.Length-1].x)>=Mathf.Abs(original[original.Length-1].x)-1)throw new Exception("Ribbon tie/cut motion wrong");
    f.confetti.Simulate(.65f,true,true,true);
    if(f.confetti.particleCount<100)throw new Exception("Confetti burst missing");
    Capture(scene,f,"celebration");
    f.ResetFinish();if(f.Finished||f.CelebrationCount!=0||f.confetti.particleCount!=0)throw new Exception("Finish reset failed");
    if(!f.CheckCrossing(W(0,f.ribbonHeight,-3),W(0,f.ribbonHeight,3),true))throw new Exception("Swept plane flight crossing failed");
    var movement=roots.SelectMany(r=>r.GetComponentsInChildren<StudentObstacleMovement>(true)).Single();
    var posts=f.GetComponentsInChildren<BoxCollider>();
    if(posts.Length!=2||posts.Any(p=>!movement.obstacles.Contains(p)))throw new Exception("Tall pole collisions missing");
    var center=f.transform.position;
    var rotation=f.transform.rotation;
    if(movement.Penetration(center,rotation)>.001f)throw new Exception("Gate center obstructed");
    if(movement.Penetration(center+Vector3.up*2.2f,rotation)>.001f)throw new Exception("Hover inside gate obstructed");
    Debug.Log("RIBBON_FINISH_VALIDATION_OK: ground/outside/height misses rejected, reachable vertical crossing and swept plane crossing, single confetti burst, center split with fixed tied ends, reset, tall post collisions, clear ascent between posts. Preview images: "+Path.Combine(Path.GetTempPath(),"ribbon-finish-validation"));
   }finally{EditorSceneManager.ClosePreviewScene(scene);}
  }
  static void Capture(Scene scene,RibbonFinish f,string stage){
   var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();camera.scene=scene;
   camera.transform.position=f.transform.TransformPoint(new Vector3(1.4f,4.2f,-13));camera.transform.LookAt(f.transform.TransformPoint(new Vector3(0,3.8f,0)));camera.rect=new Rect(0,0,1,1);
   var rt=new RenderTexture(960,720,24);var image=new Texture2D(960,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();string folder=Path.Combine(Path.GetTempPath(),"ribbon-finish-validation");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,stage+".png"),image.EncodeToPNG());}
   finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
  }
 }
}
