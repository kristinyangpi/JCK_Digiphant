using System;
using System.Linq;
using System.Reflection;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 [InitializeOnLoad] public static class SecondBitePlayValidation {
  const string Key="StudentWork.SecondBitePlayCheck";
  static LollipopPickup pickup;static int bite;static float deadline;static bool started;
  static SecondBitePlayValidation(){EditorApplication.playModeStateChanged+=State;}
  [MenuItem("DigiPhant/Student Props/Validate Second Bite in Play Mode")]
  public static void Begin(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
   if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
  }
  static void State(PlayModeStateChange state){
   if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){
    started=false;bite=0;deadline=Time.realtimeSinceStartup+25;EditorApplication.update+=Tick;
   }
   if(state==PlayModeStateChange.ExitingPlayMode){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;pickup=null;}
  }
  static void Tick(){
   try{
    if(!EditorApplication.isPlaying)return;
    if(!started){
     pickup=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LollipopPickup>(true)).Single();
     pickup.controller.SetInputMode(InputMode.TestSliders);
     foreach(var control in pickup.controller.controls)control.testValue=0;
     pickup.locomotion.testForward=pickup.locomotion.testSteering=0;
     pickup.ResetPicks();pickup.candies=pickup.candies.Take(2).ToArray();
     started=true;
    }
    if(Time.realtimeSinceStartup>deadline)throw new Exception("Live bite stalled: "+pickup.Status+", phase="+pickup.MotionPhase+", count="+pickup.ConsumedCount);
    if(pickup.MotionPhase!="Idle")return;
    if(bite>0 && (pickup.ConsumedCount!=bite || pickup.candies[bite-1].gameObject.activeSelf))throw new Exception("Live candy did not disappear");
    if(bite==2){
     if(!pickup.flight.Unlocked || pickup.largeEars.Any(e=>Vector3.Distance(e.localScale,Vector3.one*2.3f)>.01f))throw new Exception("Live second bite did not grow ears/unlock");
     Debug.Log("SECOND_BITE_PLAY_VALIDATION_OK: actual LateUpdate at runtime completed two bites, removed both candies, enlarged ears and unlocked flap flight despite an empty cache before the second bite");
     SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;return;
    }
    var candy=pickup.candies[bite];
    candy.position+=pickup.trunk[0].position+new Vector3(bite==0?-.4f:.5f,.8f,3.4f)-candy.TransformPoint(pickup.gripLocal);
    // Select this bite alone while preserving previously consumed candy checks.
    if(bite==1)typeof(LollipopPickup).GetField("earScales",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pickup,Array.Empty<Vector3>());
    if(!pickup.TryPick(Time.realtimeSinceStartup))throw new Exception("Live test candy not selectable: "+pickup.Status);
    bite++;deadline=Time.realtimeSinceStartup+25;
   }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
  }
 }
}
