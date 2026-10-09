using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DigiPhant;
namespace StudentWork.CandyWonderland.Editor {
 [InitializeOnLoad] public static class CandyWonderlandPlayValidation {
  const string Key="CandyWonderland.PlayValidation";
  static double started;static int frames;static double seconds;static bool measured;
  static CandyWonderlandPlayValidation(){EditorApplication.playModeStateChanged+=Changed;}
  [MenuItem("DigiPhant/Candy Wonderland/Validate walking, flying and finish in Play Mode")]
  public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before validation");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
  static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;frames=0;seconds=0;measured=false;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;}}
  static void Require(bool ok,string message){if(!ok)throw new Exception("CANDY_WONDERLAND_PLAY_FAILED: "+message);}
  static void Tick(){if(!EditorApplication.isPlaying)return;try{
   var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var loco=all.Select(t=>t.GetComponent<DigiPhantLocomotion>()).Single(l=>l);var intro=all.Select(t=>t.GetComponent<LevelIntroduction>()).Single(i=>i);double elapsed=EditorApplication.timeSinceStartup-started;
   if(elapsed<.4)return;
   var controller=loco.GetComponent<DigiPhantController>();controller.inputMode=InputMode.TestSliders;loco.testForward=loco.testSteering=0;
   var preview=loco.GetComponent<DigiPhantCameraPreview>();if(preview)preview.enabled=false;
   if(elapsed>.7&&elapsed<1.8)Require(intro.Visible&&intro.group.alpha>.8f,"automatic title visible");
   if(elapsed<7){frames++;seconds+=Time.unscaledDeltaTime;return;}
   if(measured)return;measured=true;Require(!intro.Visible,"automatic title faded away");
   var ground=loco.GetComponent<CandyTerrainGrounding>();var world=ground.world;var movement=loco.GetComponent<StudentObstacleMovement>();var flight=loco.GetComponent<StudentFlight>();var finish=all.Select(t=>t.GetComponent<RibbonFinish>()).Single(f=>f);var ballet=loco.GetComponent<FinishBallet>();var balls=all.Select(t=>t.GetComponent<RollingCandyBall>()).Where(b=>b&&b.gameObject.activeInHierarchy).ToArray();
   Require(ground.Ready,"grounding initialized");Require(balls.All(b=>b.DistanceRolled>1),"all hazards animate in live Play");
   controller.enabled=false;foreach(var ball in balls){ball.enabled=false;ball.solid.enabled=false;}
   // Traverse every section with the original movement evaluator, steering and collision solver.
   // Hazards are tested separately to make this terrain/bridge regression deterministic.
   loco.ResetPosition();var pond=loco.GetComponent<MagicalPond>();if(pond)pond.barrier.enabled=false; // Pond lock/sequence/crossing has its own Play validation.
   ground.GroundNow();int index=1,steps=0;float biggestStep=0;float now=Time.time;
   while(index<world.route.Length&&steps<10000){var root=loco.travelRoot;var p=root.position;while(index<world.route.Length-1&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(world.route[index].x,world.route[index].z))<1.3f)index++;var target=world.route[index];var d=new Vector3(target.x-p.x,0,target.z-p.z);if(index==world.route.Length-1&&d.magnitude<.5f){index++;break;}float angle=Vector3.SignedAngle(root.forward,d,Vector3.up);loco.testSteering=Mathf.Clamp(angle/24,-1,1);loco.testForward=Mathf.Abs(angle)>55?.18f:.56f;ground.GroundNow();float before=root.position.y;loco.Evaluate(now,.05f);ground.GroundNow();biggestStep=Mathf.Max(biggestStep,Mathf.Abs(root.position.y-before));Require(Mathf.Abs(root.position.y-(world.GroundHeight(root.position)+.09515186f))<.012f,"stays grounded at route sample "+index);now+=.05f;steps++;}
   Require(index==world.route.Length,"whole route traversed; stopped near sample "+index);Require(biggestStep<.08f,"no sudden terrain jumps "+biggestStep);Require(Vector2.Distance(new Vector2(loco.travelRoot.position.x,loco.travelRoot.position.z),new Vector2(world.route.Last().x,world.route.Last().z))<.6f,"arrived castle");
   // Existing flight stays relative to the raised surface, capped and landing slowly.
   loco.StopMotion();flight.SetUnlocked(true);float baseline=ground.CurrentGround;for(int i=0;i<110;i++){float t=now+i*.05f;float arm=(i/5)%2==0?-.6f:.6f;flight.Observe(t,true,true,arm,arm);loco.travelRoot.position=flight.ApplyHeight(loco.travelRoot.position,baseline,.05f,movement);ground.GroundNow();}Require(flight.HeightOffset>1.8f&&flight.HeightOffset<=2.201f,"same capped flap flight above new terrain");
   float previous=flight.HeightOffset;for(int i=0;i<110;i++){flight.Observe(now+20+i*.05f,false,false,0,0);loco.travelRoot.position=flight.ApplyHeight(loco.travelRoot.position,baseline,.05f,movement);ground.GroundNow();Require(flight.HeightOffset<=previous+.001f,"gentle landing");previous=flight.HeightOffset;}Require(flight.HeightOffset<.01f,"landed on elevated ground");
   // Preserve the existing airborne ribbon completion and ballet on the raised forecourt.
   for(int i=0;i<100;i++){float t=now+40+i*.05f;float arm=(i/5)%2==0?-.6f:.6f;flight.Observe(t,true,true,arm,arm);loco.travelRoot.position=flight.ApplyHeight(loco.travelRoot.position,baseline,.05f,movement);ground.GroundNow();}
   bool crossed=finish.CheckCrossing(finish.transform.TransformPoint(new Vector3(0,finish.ribbonHeight,-.4f)),finish.transform.TransformPoint(new Vector3(0,finish.ribbonHeight,.4f)),flight.IsAirborne);Require(crossed&&finish.Finished&&ballet.ControlsElephant,"original airborne finish triggers confetti and ballet");for(int i=0;i<220;i++)ballet.Advance(.1f);Require(ballet.CurrentStage==FinishBallet.Stage.Complete&&ballet.TurnsDegrees>719,"ballet completes on raised ground");Require(Mathf.Abs(loco.travelRoot.position.y-baseline)<.03f,"ballet landed at new ground height");
   Debug.Log("CANDY_WONDERLAND_PLAY_OK: full route walked with original evaluator ("+steps+" steps), hills and both bridges, maximum ground step="+biggestStep.ToString("F3")+"m; live hazards; auto title; capped flight/landing; castle ribbon/confetti/ballet. Editor update sample="+(frames/Math.Max(.001,seconds)).ToString("F1")+"Hz (not a player-build benchmark).");EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
  }catch(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}}
 }
}
