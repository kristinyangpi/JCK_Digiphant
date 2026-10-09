using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 [InitializeOnLoad] public static class FinishBalletPlayValidation {
  const string Key="StudentWork.FinishBalletPlayCheck";
  static FinishBallet ballet;static RibbonFinish finish;static DigiPhant.DigiPhantController controller;
  static bool started,triggered,captured;static float start,last,triggerTime;static Vector3 danceStart;static int initialCount;
  static FinishBalletPlayValidation(){EditorApplication.playModeStateChanged+=State;}
  [MenuItem("DigiPhant/Student Props/Validate Finish Ballet in Play Mode")]
  public static void Begin(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
  static void State(PlayModeStateChange state){
   if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){started=triggered=captured=false;EditorApplication.update+=Tick;}
   if(state==PlayModeStateChange.ExitingPlayMode){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;ballet=null;finish=null;}
  }
  static void Tick(){try{
   if(!EditorApplication.isPlaying)return;float now=Time.realtimeSinceStartup;
   if(!started){
    var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();ballet=all.Select(t=>t.GetComponent<FinishBallet>()).Single(b=>b);finish=all.Select(t=>t.GetComponent<RibbonFinish>()).Single(f=>f);controller=ballet.GetComponent<DigiPhant.DigiPhantController>();
    var preview=ballet.GetComponent<DigiPhant.DigiPhantCameraPreview>();if(preview)preview.enabled=false;
    controller.enabled=false;controller.SetInputMode(DigiPhant.InputMode.TestSliders);controller.showControls=false;
    finish.ResetFinish();ballet.flight.GroundReset();ballet.flight.SetUnlocked(true);ballet.locomotion.travelRoot.SetPositionAndRotation(finish.transform.position,finish.transform.rotation);
    var pickup=ballet.GetComponent<LollipopPickup>();foreach(var ear in pickup.largeEars)ear.localScale=Vector3.one*2.3f;pickup.birthdayHat.gameObject.SetActive(true);
    initialCount=ballet.PerformanceCount;start=last=now;started=true;
   }
   float dt=Mathf.Clamp(now-last,0,.1f);last=now;
   if(!triggered){
    float height=Mathf.Sin((now-start)*7)*.7f;ballet.flight.Observe(now,true,true,height,height);
    ballet.locomotion.travelRoot.position=ballet.flight.ApplyHeight(ballet.locomotion.travelRoot.position,finish.transform.position.y,dt,ballet.GetComponent<StudentObstacleMovement>());
    if(finish.Finished){triggered=true;triggerTime=now;danceStart=ballet.locomotion.travelRoot.position;controller.enabled=true;}
   }else{
    // Deliberate travel, pose, and flap input must not interrupt the finale.
    ballet.locomotion.testForward=1;ballet.locomotion.testSteering=1;foreach(var control in controller.controls)control.testValue=1;
    float height=Mathf.Sin(now*7)*.7f;ballet.flight.Observe(now,true,true,height,height);
    var position=ballet.locomotion.travelRoot.position;
    if(Mathf.Abs(position.x-danceStart.x)>.001f||Mathf.Abs(position.z-danceStart.z)>.001f)throw new Exception("Input moved elephant during finale");
    if(ballet.CurrentStage==FinishBallet.Stage.Dancing&&!captured&&ballet.TurnsDegrees>180){FinishBalletValidation.Capture(ballet.locomotion.followCamera,"runtime-pirouette");captured=true;}
    if(ballet.CurrentStage==FinishBallet.Stage.Complete){
     if(!ballet.tutu.gameObject.activeSelf||Mathf.Abs(position.y-ballet.GroundHeight)>.005f||ballet.PerformanceCount!=initialCount+1||Mathf.Abs(ballet.TurnsDegrees-720)>.01f)throw new Exception("Runtime finale incomplete");
     if(ballet.GetComponent<LollipopPickup>().largeEars.Any(e=>Mathf.Abs(e.localScale.x-2.3f)>.001f))throw new Exception("Earned ears lost");
     finish.ResetFinish();if(ballet.ControlsElephant||ballet.tutu.gameObject.activeSelf)throw new Exception("Runtime reset failed");
     Debug.Log("FINISH_BALLET_PLAY_VALIDATION_OK: real LateUpdate flight crossing triggered landing, tutu, hind-foot ballet, two turns and bow; travel/pose/flap inputs did not interrupt; large ears preserved; reset restored controls");SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;return;
    }
   }
   if(now-start>30)throw new Exception("Runtime ballet stalled: "+ballet.CurrentStage+", flight="+ballet.flight.HeightOffset+", elapsed="+(now-triggerTime));
  }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}}
 }
}
