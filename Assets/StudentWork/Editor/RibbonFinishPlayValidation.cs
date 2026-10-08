using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 [InitializeOnLoad] public static class RibbonFinishPlayValidation {
  const string Key="StudentWork.RibbonFinishPlayCheck";
  static RibbonFinish finish;static float startedAt,lastTime,celebratedAt;static bool started;
  static RibbonFinishPlayValidation(){EditorApplication.playModeStateChanged+=State;}
  [MenuItem("DigiPhant/Student Props/Validate Ribbon Finish in Play Mode")]
  public static void Begin(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
   if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
  }
  static void State(PlayModeStateChange state){
   if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){started=false;celebratedAt=-1;EditorApplication.update+=Tick;}
   if(state==PlayModeStateChange.ExitingPlayMode){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;finish=null;}
  }
  static void Tick(){
   try{
    if(!EditorApplication.isPlaying)return;
    float now=Time.realtimeSinceStartup;
    if(!started){
     var roots=SceneManager.GetActiveScene().GetRootGameObjects();finish=roots.SelectMany(r=>r.GetComponentsInChildren<RibbonFinish>(true)).Single();
     var pickup=roots.SelectMany(r=>r.GetComponentsInChildren<LollipopPickup>(true)).Single();
     pickup.controller.enabled=false;pickup.locomotion.enabled=false;pickup.enabled=false;
     finish.ResetFinish();finish.flight.GroundReset();finish.flight.SetUnlocked(true);
     finish.travelRoot.SetPositionAndRotation(finish.transform.position,finish.transform.rotation);
     var camera=Camera.main;camera.transform.position=finish.transform.TransformPoint(new Vector3(1,4.5f,-13));camera.transform.LookAt(finish.transform.TransformPoint(new Vector3(0,3.5f,0)));
     startedAt=lastTime=now;started=true;
    }
    float dt=Mathf.Clamp(now-lastTime,0,.1f);lastTime=now;
    float height=Mathf.Sin((now-startedAt)*7)*.7f;
    finish.flight.Observe(now,true,true,height,height);
    var movement=finish.flight.GetComponent<StudentObstacleMovement>();
    finish.travelRoot.position=finish.flight.ApplyHeight(finish.travelRoot.position,finish.transform.position.y,dt,movement);
    if(finish.Finished && celebratedAt<0)celebratedAt=now;
    if(celebratedAt>=0 && now-celebratedAt>1.5f){
     if(finish.CelebrationCount!=1 || finish.confetti.particleCount<20 || finish.flight.HeightOffset>2.2001f)throw new Exception("Runtime finish/confetti/height cap failed");
     Debug.Log("RIBBON_FINISH_PLAY_VALIDATION_OK: actual LateUpdate detected a flapping-driven ascent through the ribbon, one center break and live confetti burst, within the 2.2m flight cap");
     SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;return;
    }
    if(now-startedAt>15)throw new Exception("Runtime ribbon crossing stalled: flight height="+finish.flight.HeightOffset+", blocked="+finish.flight.AltitudeBlocked);
   }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
  }
 }
}
