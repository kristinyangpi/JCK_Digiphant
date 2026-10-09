using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 [InitializeOnLoad] public static class LollipopForestPlayValidation {
  const string Key="StudentWork.LollipopForestPlayCheck";
  static LollipopForest level;static bool started,sawTitle,captured;static float began,maxAlpha;
  static LollipopForestPlayValidation(){EditorApplication.playModeStateChanged+=State;}
  [MenuItem("DigiPhant/Level 1/Validate Lollipop Forest in Play Mode")]
  public static void Begin(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
  static void State(PlayModeStateChange state){
   if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){started=sawTitle=captured=false;maxAlpha=0;began=Time.realtimeSinceStartup;EditorApplication.update+=Tick;}
   if(state==PlayModeStateChange.ExitingPlayMode){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;level=null;}
  }
  static void Tick(){try{
   if(!EditorApplication.isPlaying)return;float now=Time.realtimeSinceStartup;
   if(!started){
    var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();level=all.Select(t=>t.GetComponent<LollipopForest>()).Single(l=>l);
    var controller=all.Select(t=>t.GetComponent<DigiPhant.DigiPhantController>()).Single(c=>c);var preview=controller.GetComponent<DigiPhant.DigiPhantCameraPreview>();if(preview)preview.enabled=false;controller.SetInputMode(DigiPhant.InputMode.TestSliders);controller.GetComponent<DigiPhant.DigiPhantLocomotion>().StopMotion();started=true;
   }
   var intro=level.introduction;maxAlpha=Mathf.Max(maxAlpha,intro.group.alpha);sawTitle|=intro.Visible;
   if(!captured&&intro.Elapsed>1&&intro.Elapsed<2.8f&&intro.Visible){
    var canvas=intro.GetComponent<Canvas>();var mode=canvas.renderMode;var camera=Camera.main;var rect=camera.rect;var oldAspect=camera.aspect;
    try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.rect=new Rect(0,0,1,1);LollipopForestValidation.Capture(camera,"runtime-title",1280,720,canvas);captured=true;}
    finally{canvas.renderMode=mode;canvas.worldCamera=null;camera.rect=rect;camera.aspect=oldAspect;}
   }
   if(now-began>8){
    if(!sawTitle||maxAlpha<.99f||intro.Visible||intro.group.gameObject.activeSelf||intro.group.blocksRaycasts)throw new Exception("Runtime startup title did not fade automatically: "+intro.Elapsed+", peak="+maxAlpha);
    if(level.rollingBalls.Any(b=>b.DistanceRolled<5))throw new Exception("Runtime candy balls not rolling: "+string.Join(",",level.rollingBalls.Select(b=>b.DistanceRolled)));
    Debug.Log("LOLLIPOP_FOREST_PLAY_VALIDATION_OK: startup title appeared automatically, reached full opacity, held and faded away without blocking input; all three balls rolled in actual Update. Existing controls/camera restored on exiting Play");SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;return;
   }
   if(now-began>15)throw new Exception("Forest runtime check stalled");
  }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}}
 }
}
