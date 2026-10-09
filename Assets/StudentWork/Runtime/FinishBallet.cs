using System;
using System.Collections.Generic;
using UnityEngine;
using DigiPhant;
namespace StudentWork {
 [DefaultExecutionOrder(300)] public class FinishBallet : MonoBehaviour {
  public DigiPhantLocomotion locomotion;
  public StudentFlight flight;
  public Transform visualRoot,tutu,leftHip,rightHip,leftArm,rightArm,neck;
  public float riseSeconds=1.9f,danceSeconds=7.5f,bowSeconds=1.4f;
  public enum Stage { Idle, Landing, Rising, Dancing, Bow, Complete }
  public Stage CurrentStage {get;private set;}
  public bool ControlsElephant=>isActiveAndEnabled && CurrentStage!=Stage.Idle;
  public float TurnsDegrees {get;private set;}
  public float GroundHeight {get;private set;}
  public int PerformanceCount {get;private set;}
  public string Status=>CurrentStage==Stage.Landing?"Landing for ballet...":CurrentStage==Stage.Rising?"Pink tutu · take your places!":CurrentStage==Stage.Dancing?"Ballet finale!":CurrentStage==Stage.Bow?"Take a bow!":CurrentStage==Stage.Complete?"Bravo!":"";
  struct Rest {public Transform bone;public Vector3 position,scale;public Quaternion rotation;}
  readonly List<Rest> rest=new List<Rest>();
  Vector3 visualPosition,hipPivot,cameraOffset,tutuScale;
  Quaternion visualRotation,startRotation,cameraRotation;
  Vector3 cameraGoal;
  float elapsed;
  bool[] offscreen;SkinnedMeshRenderer[] skins;
  public bool BeginPerformance(){
   if(ControlsElephant||!locomotion||!locomotion.travelRoot||!visualRoot||!leftHip||!rightHip||!tutu)return false;
   var root=locomotion.travelRoot;
   GroundHeight=root.position.y-(flight?flight.HeightOffset:0);
   startRotation=root.rotation;cameraRotation=locomotion.followCamera?locomotion.followCamera.transform.rotation:Quaternion.identity;cameraOffset=locomotion.followCamera?locomotion.followCamera.transform.position-root.position:Vector3.zero;
   // Keep rewards while settling the gait into a clean, reproducible idle pose.
   var pickup=GetComponent<LollipopPickup>();if(pickup)pickup.PrepareForFinish();var ears=pickup?pickup.largeEars:Array.Empty<Transform>();var scales=new Vector3[ears.Length];for(int i=0;i<ears.Length;i++)if(ears[i])scales[i]=ears[i].localScale;
   if(locomotion.idle)locomotion.idle.SampleAnimation(locomotion.elephantAnimator.gameObject,0);
   for(int i=0;i<ears.Length;i++)if(ears[i])ears[i].localScale=scales[i];
   rest.Clear();foreach(var bone in visualRoot.GetComponentsInChildren<Transform>(true))rest.Add(new Rest{bone=bone,position=bone.localPosition,rotation=bone.localRotation,scale=bone.localScale});
   visualPosition=visualRoot.localPosition;visualRotation=visualRoot.localRotation;tutuScale=tutu.localScale;
   hipPivot=root.InverseTransformPoint((leftHip.position+rightHip.position)*.5f);
   skins=visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>();offscreen=new bool[skins.Length];for(int i=0;i<skins.Length;i++){offscreen[i]=skins[i].updateWhenOffscreen;skins[i].updateWhenOffscreen=true;}
   ChooseCamera();locomotion.StopMotion();elapsed=TurnsDegrees=0;CurrentStage=Stage.Landing;PerformanceCount++;tutu.gameObject.SetActive(false);
   Debug.Log("FINISH_BALLET_STARTED: land, rise on hind feet, pink tutu, two pirouettes, bow");return true;
  }
  void LateUpdate(){if(ControlsElephant)Advance(Time.deltaTime);}
  public void Advance(float dt){
   if(!ControlsElephant)return;dt=Mathf.Clamp(dt,0,.1f);var root=locomotion.travelRoot;
   if(CurrentStage==Stage.Landing){
    float y=Mathf.MoveTowards(root.position.y,GroundHeight,.65f*dt);var obstacles=GetComponent<StudentObstacleMovement>();
    var p=obstacles&&obstacles.enabled?obstacles.ResolveVertical(root.position,root.rotation,y):new Vector3(root.position.x,y,root.position.z);root.position=p;
    if(Mathf.Abs(p.y-GroundHeight)<.005f){root.position=new Vector3(p.x,GroundHeight,p.z);flight?.GroundReset();CurrentStage=Stage.Rising;elapsed=0;}
   }else{
    elapsed+=dt;
    if(CurrentStage==Stage.Rising&&elapsed>=riseSeconds){CurrentStage=Stage.Dancing;elapsed=0;}
    else if(CurrentStage==Stage.Dancing&&elapsed>=danceSeconds){CurrentStage=Stage.Bow;elapsed=0;TurnsDegrees=720;}
    else if(CurrentStage==Stage.Bow&&elapsed>=bowSeconds){CurrentStage=Stage.Complete;elapsed=0;Debug.Log("FINISH_BALLET_COMPLETE: two grounded pirouettes and bow");}
   }
   float stand=CurrentStage==Stage.Landing?0:CurrentStage==Stage.Rising?Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.1f,riseSeconds)):1;
   float dance=CurrentStage==Stage.Dancing?elapsed/Mathf.Max(.1f,danceSeconds):0;
   if(CurrentStage==Stage.Dancing)TurnsDegrees=720*Mathf.SmoothStep(0,1,dance);
   root.rotation=startRotation*Quaternion.AngleAxis(TurnsDegrees,Vector3.up);
   float sway=CurrentStage==Stage.Dancing?Mathf.Sin(dance*Mathf.PI*4)*2:0;
   float bow=CurrentStage==Stage.Bow?Mathf.Sin(Mathf.Clamp01(elapsed/Mathf.Max(.1f,bowSeconds))*Mathf.PI)*12:0;
   Pose(stand,65*stand+sway-bow,CurrentStage==Stage.Dancing?Mathf.Sin(dance*Mathf.PI*4)*7:0);
   tutu.gameObject.SetActive(stand>.03f);tutu.localScale=tutuScale*Mathf.Lerp(.01f,1,Mathf.SmoothStep(0,1,Mathf.Clamp01(stand*1.7f)));
   if(locomotion.followElephant&&locomotion.followCamera){
    var camera=locomotion.followCamera.transform;float blend=1-Mathf.Exp(-dt*3);
    camera.position=Vector3.Lerp(camera.position,cameraGoal,blend);
    var target=root.position+Vector3.up*(2.5f+stand*.3f);
    camera.rotation=Quaternion.Slerp(camera.rotation,Quaternion.LookRotation(target-camera.position,Vector3.up),blend);
   }
  }
  void ChooseCamera(){
   if(!locomotion.followCamera)return;
   var root=locomotion.travelRoot;var origin=new Vector3(root.position.x,GroundHeight,root.position.z);
   var obstacles=GetComponent<StudentObstacleMovement>();float best=float.PositiveInfinity;
   Vector3[] offsets={new Vector3(-8,6,-12),new Vector3(8,6,-12),new Vector3(-12,6,8),new Vector3(12,6,8),new Vector3(0,7,-15),new Vector3(0,7,15),new Vector3(-15,7,0),new Vector3(15,7,0)};
   float scale=Mathf.Max(1,3.8f/Mathf.Tan(locomotion.followCamera.fieldOfView*Mathf.Deg2Rad*.5f)/14.4f);
   foreach(var offset in offsets){
    var candidate=origin+startRotation*(offset*scale);float score=0;
    if(obstacles&&obstacles.obstacles!=null)foreach(var box in obstacles.obstacles){
     if(!box||!box.enabled||!box.gameObject.activeInHierarchy)continue;
     foreach(float height in new[]{.4f,2.5f,4.8f}){
      var target=origin+Vector3.up*height;var ray=new Ray(candidate,(target-candidate).normalized);
      if(box.bounds.IntersectRay(ray,out float distance)&&distance<Vector3.Distance(candidate,target)-.2f)score++;
     }
    }
    if(score<best){best=score;cameraGoal=candidate;}
   }
  }
  void Pose(float stand,float angle,float arms){
   foreach(var pose in rest)if(pose.bone){pose.bone.localPosition=pose.position;pose.bone.localRotation=pose.rotation;pose.bone.localScale=pose.scale;}
   var root=locomotion.travelRoot;visualRoot.localPosition=visualPosition;visualRoot.localRotation=visualRotation;
   var pivot=root.TransformPoint(hipPivot);var axis=root.right;var tilt=Quaternion.AngleAxis(-angle,axis);
   visualRoot.SetPositionAndRotation(pivot+tilt*(visualRoot.position-pivot),tilt*visualRoot.rotation);
   // Counter-rotate the hind legs: the hips remain the pivot and both soles stay grounded.
   leftHip.rotation=Quaternion.AngleAxis(angle,axis)*leftHip.rotation;
   rightHip.rotation=Quaternion.AngleAxis(angle,axis)*rightHip.rotation;
   if(leftArm)leftArm.rotation=Quaternion.AngleAxis(-(27+arms)*stand,root.forward)*leftArm.rotation;
   if(rightArm)rightArm.rotation=Quaternion.AngleAxis((27+arms)*stand,root.forward)*rightArm.rotation;
   if(neck)neck.rotation=Quaternion.AngleAxis(32*stand,axis)*neck.rotation;
  }
  public void ResetPerformance(){
   if(CurrentStage==Stage.Idle)return;
   foreach(var pose in rest)if(pose.bone){pose.bone.localPosition=pose.position;pose.bone.localRotation=pose.rotation;pose.bone.localScale=pose.scale;}
   if(locomotion&&locomotion.travelRoot){var root=locomotion.travelRoot;root.rotation=startRotation;root.position=new Vector3(root.position.x,GroundHeight,root.position.z);}
   if(tutu){tutu.localScale=tutuScale;tutu.gameObject.SetActive(false);}
   if(skins!=null)for(int i=0;i<skins.Length;i++)if(skins[i])skins[i].updateWhenOffscreen=offscreen[i];
   if(locomotion&&locomotion.followCamera){locomotion.followCamera.transform.SetPositionAndRotation(locomotion.travelRoot.position+cameraOffset,cameraRotation);}
   flight?.GroundReset();locomotion?.StopMotion();CurrentStage=Stage.Idle;TurnsDegrees=elapsed=0;rest.Clear();
  }
  void OnDisable(){ResetPerformance();}
 }
}
