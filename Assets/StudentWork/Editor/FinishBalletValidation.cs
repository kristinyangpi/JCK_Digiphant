using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class FinishBalletValidation {
  [MenuItem("DigiPhant/Student Props/Validate Finish Ballet")]
  public static void Validate(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
   var scene=EditorSceneManager.OpenPreviewScene(SceneManager.GetActiveScene().path);
   try{
    var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var ballet=all.Select(t=>t.GetComponent<FinishBallet>()).Single(b=>b);var finish=all.Select(t=>t.GetComponent<RibbonFinish>()).Single(f=>f);
    var root=ballet.locomotion.travelRoot;var flight=ballet.flight;root.SetPositionAndRotation(finish.transform.position,finish.transform.rotation);flight.GroundReset();flight.SetUnlocked(true);
    for(int i=0;i<240;i++){float t=i/60f;flight.Observe(t,true,true,Mathf.Sin(t*7)*.7f,Mathf.Sin(t*7)*.7f);root.position=flight.ApplyHeight(root.position,finish.transform.position.y,1f/60,null);}
    var start=root.position;var rotation=root.rotation;var camera=all.Select(t=>t.GetComponent<Camera>()).First(c=>c);camera.scene=scene;camera.rect=new Rect(0,0,1,1);
    ballet.locomotion.followElephant=true;camera.transform.position=finish.transform.TransformPoint(new Vector3(-9,6,-12));camera.transform.LookAt(finish.transform.TransformPoint(new Vector3(0,2.6f,0)));
    var a=finish.transform.TransformPoint(new Vector3(0,finish.ribbonHeight-.1f,0));var b=finish.transform.TransformPoint(new Vector3(0,finish.ribbonHeight+.1f,0));
    if(!finish.CheckCrossing(a,b,true)||!ballet.ControlsElephant||ballet.PerformanceCount!=1)throw new Exception("Automatic finish ballet did not trigger");
    if(ballet.tutu.gameObject.activeSelf)throw new Exception("Tutu appeared before landing");
    float initialY=start.y;int steps=0;
    while(ballet.CurrentStage==FinishBallet.Stage.Landing && steps++<200){float y=root.position.y;ballet.Advance(.05f);if(y-root.position.y>.0326f)throw new Exception("Landing abrupt");}
    if(steps>=200||Mathf.Abs(root.position.y-ballet.GroundHeight)>.005f)throw new Exception("Landing failed");
    var feet=all.Where(t=>t.name=="elephant_l_Toe-nub_bone"||t.name=="elephant_r_Toe-nub_bone").ToArray();var soles=feet.Select(t=>t.position.y).ToArray();
    float maxFootError=0;bool standingCaptured=false,turnCaptured=false;float previousTurn=0;
    for(int i=0;i<260;i++){
     ballet.Advance(.05f);finish.AnimateBreak(2);
     foreach(int f in Enumerable.Range(0,feet.Length))maxFootError=Mathf.Max(maxFootError,Mathf.Abs(feet[f].position.y-soles[f]));
     if(Mathf.Abs(root.position.x-start.x)>.001f||Mathf.Abs(root.position.z-start.z)>.001f)throw new Exception("Dance drifted horizontally");
     if(ballet.TurnsDegrees+ .001f<previousTurn||ballet.TurnsDegrees-previousTurn>8)throw new Exception("Pirouette speed jump");previousTurn=ballet.TurnsDegrees;
     if(!standingCaptured&&ballet.CurrentStage==FinishBallet.Stage.Dancing){Capture(camera,"standing-tutu");standingCaptured=true;}
     if(!turnCaptured&&ballet.TurnsDegrees>160){Capture(camera,"pirouette");turnCaptured=true;}
    }
    Capture(camera,"final-bow");
    if(ballet.CurrentStage!=FinishBallet.Stage.Complete||Mathf.Abs(ballet.TurnsDegrees-720)>.01f||!ballet.tutu.gameObject.activeSelf||maxFootError>.04f)throw new Exception("Grounded ballet finale failed: foot error="+maxFootError+", stage="+ballet.CurrentStage);
    if(finish.CheckCrossing(a,b,true)||ballet.PerformanceCount!=1)throw new Exception("Repeated finish replayed ballet");
    finish.ResetFinish();if(ballet.ControlsElephant||ballet.tutu.gameObject.activeSelf||Quaternion.Angle(root.rotation,rotation)>.01f)throw new Exception("Finish reset failed");
    Debug.Log("FINISH_BALLET_VALIDATION_OK: ribbon automatically starts slow landing, tutu waits for landing, two smooth 360 turns, no horizontal drift, hind soles within "+maxFootError+"m, bow and reset restore controls. Images: "+Path.Combine(Path.GetTempPath(),"finish-ballet-validation"));
   }finally{EditorSceneManager.ClosePreviewScene(scene);}
  }
  public static void Capture(Camera camera,string name){
   var rt=new RenderTexture(1280,960,24);var image=new Texture2D(1280,960,TextureFormat.RGB24,false);var old=RenderTexture.active;
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,960),0,0);image.Apply();var folder=Path.Combine(Path.GetTempPath(),"finish-ballet-validation");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());}
   finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
  }
 }
}
