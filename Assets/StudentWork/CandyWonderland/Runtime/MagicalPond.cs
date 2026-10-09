using System;
using System.Linq;
using UnityEngine;
using DigiPhant;
namespace StudentWork.CandyWonderland {
 [DefaultExecutionOrder(150)] public class MagicalPond : MonoBehaviour {
  public enum PondState { Blocked, PoseDetected, AbsorbingWater, SprayingWater, RainbowAppearing, PathUnlocked }
  public CandyWorld world;public DigiPhantLocomotion locomotion;public DigiPhantController controller;public LollipopPickup pickup;
  public Transform pond,water;public BoxCollider barrier;public Material streamMaterial,rainbowMaterial,sparkleMaterial;
  public float poseHoldSeconds=.65f,rainbowHoldSeconds=9;
  public int performer=1;
  public PondState State {get;private set;}
  public bool ControlsElephant=>isActiveAndEnabled&&State!=PondState.Blocked&&State!=PondState.PathUnlocked;
  public float WaterFraction {get;private set;}=1;
  public float RainbowAlpha {get;private set;}
  public int AttemptCount {get;private set;}
  public float MatchScore {get;private set;}
  public float LastTipError {get;private set;}
  public string Status {get;private set;}="At the pond: extend one arm horizontally; hold its elbow with your other hand.";
  float hold=-1,elapsed,unlockedElapsed;Quaternion[] rest,bend;Vector3[] positions;
  Vector3 tipPosition,startTip,rootPosition,waterHome,waterScale;Quaternion rootRotation;bool cached;
  bool[] skinFlags;SkinnedMeshRenderer[] skins;GameObject effects;LineRenderer[] vortex,spray,rainbow;ParticleSystem sparkles;
  Vector3 sprayOrigin,arcRight;Quaternion previousTip;bool wasClose;
  static readonly Color[] Pastels={new Color(1,.53f,.67f),new Color(1,.72f,.56f),new Color(1,.91f,.6f),new Color(.66f,.93f,.76f),new Color(.56f,.87f,1),new Color(.7f,.7f,1),new Color(.9f,.69f,1)};
  void Start(){Cache();ResetPond();}
  void Cache(){if(cached)return;if(!pond||!water||!pickup||pickup.trunk.Length!=7||!pickup.tip)throw new InvalidOperationException("Magical pond rig/setup incomplete");waterHome=water.localPosition;waterScale=water.localScale;cached=true;EnsureEffects();}
  void EnsureEffects(){if(effects)return;effects=new GameObject("Pond magic - bounded runtime effects");effects.transform.SetParent(pond,false);vortex=Lines("Suction spiral",4,streamMaterial,.065f);spray=Lines("Sparkling water arc",4,streamMaterial,.09f);rainbow=Lines("Pastel rainbow band",7,rainbowMaterial,.2f);for(int i=0;i<7;i++)rainbow[i].startColor=rainbow[i].endColor=Pastels[i];
   var host=new GameObject("Magic droplets");host.transform.SetParent(effects.transform,false);sparkles=host.AddComponent<ParticleSystem>();var main=sparkles.main;main.loop=true;main.startLifetime=1.5f;main.startSpeed=.35f;main.startSize=.05f;main.maxParticles=160;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.8f,.96f,1,.8f);var emission=sparkles.emission;emission.rateOverTime=25;var shape=sparkles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.35f;host.GetComponent<ParticleSystemRenderer>().sharedMaterial=sparkleMaterial;HideEffects();}
  LineRenderer[] Lines(string name,int count,Material mat,float width){var result=new LineRenderer[count];for(int i=0;i<count;i++){var host=new GameObject(name+" "+i);host.transform.SetParent(effects.transform,false);var line=host.AddComponent<LineRenderer>();line.sharedMaterial=mat;line.useWorldSpace=true;line.positionCount=81;line.widthMultiplier=width;line.numCapVertices=5;line.numCornerVertices=3;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;result[i]=line;}return result;}
  void HideEffects(){foreach(var a in new[]{vortex,spray,rainbow})if(a!=null)foreach(var line in a)line.enabled=false;if(sparkles)sparkles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);RainbowAlpha=0;}
  public bool IsNear(){if(!locomotion||!locomotion.travelRoot)return false;var p=pond.InverseTransformPoint(locomotion.travelRoot.position);return Mathf.Abs(p.x)<5.5f&&p.z<0&&p.z>-9&&(!pickup.flight||!pickup.flight.IsAirborne);}
  public void ObservePose(float now,bool tracked,float score,float quality){MatchScore=tracked?score:0;if(State!=PondState.Blocked)return;if(!IsNear()||!tracked||score<.8f||quality<.75f||pickup.MotionPhase!="Idle"||GetComponent<FinishBallet>().ControlsElephant){hold=-1;return;}if(hold<0)hold=now;if(now-hold>=poseHoldSeconds)BeginInteraction();}
  public bool BeginInteraction(){Cache();if(State!=PondState.Blocked||!IsNear()||pickup.MotionPhase!="Idle")return false;
   locomotion.StopMotion();if(locomotion.idle&&locomotion.elephantAnimator)locomotion.idle.SampleAnimation(locomotion.elephantAnimator.gameObject,0);
   rest=pickup.trunk.Select(t=>t.localRotation).ToArray();bend=(Quaternion[])rest.Clone();positions=pickup.trunk.Select(t=>t.localPosition).ToArray();tipPosition=pickup.tip.localPosition;startTip=pickup.tip.position;previousTip=pickup.tip.rotation;rootPosition=locomotion.travelRoot.position;rootRotation=locomotion.travelRoot.rotation;
   skins=locomotion.travelRoot.GetComponentsInChildren<SkinnedMeshRenderer>();skinFlags=skins.Select(s=>s.updateWhenOffscreen).ToArray();foreach(var skin in skins)skin.updateWhenOffscreen=true;
   elapsed=0;AttemptCount++;State=PondState.PoseDetected;Status="Pose recognised — gathering magical water";return true;}
  void LateUpdate(){if(!Application.isPlaying)return;float score=0,quality=0;bool tracked=controller.TryReadPondPose(performer,Time.realtimeSinceStartup,out score,out quality);ObservePose(Time.realtimeSinceStartup,tracked,score,quality);Advance(Time.deltaTime);}
  public void Advance(float dt){Cache();dt=Mathf.Clamp(dt,0,.1f);
   if(State==PondState.Blocked){bool close=IsNear();if(close&&!wasClose){sparkles.transform.position=pond.position+Vector3.up*.25f;sparkles.Play();}if(!close&&wasClose)sparkles.Stop();wasClose=close;return;}
   if(State==PondState.PathUnlocked){unlockedElapsed+=dt;float alpha=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((unlockedElapsed-rainbowHoldSeconds)/2));DrawRainbow(1,alpha);if(alpha<=0){foreach(var line in rainbow)line.enabled=false;sparkles.Stop();}return;}
   elapsed+=dt;var root=locomotion.travelRoot;
   // Bounded turn only; no displacement, teleport or camera changes during the sequence.
   var rotation=root.rotation;var obstacles=GetComponent<StudentObstacleMovement>();var delta=Vector3.SignedAngle(root.forward,pond.forward,Vector3.up);if(elapsed<1.5f&&obstacles)obstacles.Resolve(root.position,ref rotation,Mathf.Clamp(delta,-45*dt,45*dt),Vector3.zero);root.rotation=rotation;root.position=rootPosition;
   var waterTarget=pond.TransformPoint(new Vector3(0,1.1f,-1.8f));var basePoint=pickup.trunk[0].position;float natural=pickup.ReachLength;
   float reachBlend=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/1.8f));
   float drain=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-1.6f)/4.2f));
   float lift=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-5.3f)/2.1f));
   Vector3 upTarget=basePoint+pond.forward*1.35f+Vector3.up*(natural*1.25f);
   Vector3 aim=Vector3.Lerp(Vector3.Lerp(startTip,waterTarget,reachBlend),upTarget,lift);
   float extension=Mathf.Clamp(Vector3.Distance(basePoint,aim)/Mathf.Max(.01f,natural)+.12f,1,2.1f);for(int i=1;i<positions.Length;i++)pickup.trunk[i].localPosition=positions[i]*extension;pickup.tip.localPosition=tipPosition*extension;
   if(elapsed<10.8f)Solve(aim,dt);else{float blend=Mathf.SmoothStep(0,1,(elapsed-10.8f)/1.6f);var previous=(Quaternion[])bend.Clone();for(int i=0;i<bend.Length;i++)pickup.trunk[i].localRotation=Quaternion.RotateTowards(previous[i],Quaternion.Slerp(previous[i],rest[i],1-Mathf.Exp(-dt*5)),85*dt);var candidate=pickup.trunk.Select(t=>t.localRotation).ToArray();float fraction=1;for(int j=0;j<8&&Quaternion.Angle(previousTip,pickup.tip.rotation)>115*dt+.02f;j++){fraction*=.5f;for(int i=0;i<bend.Length;i++)pickup.trunk[i].localRotation=Quaternion.Slerp(previous[i],candidate[i],fraction);}bend=pickup.trunk.Select(t=>t.localRotation).ToArray();previousTip=pickup.tip.rotation;for(int i=1;i<positions.Length;i++)pickup.trunk[i].localPosition=Vector3.Lerp(positions[i]*extension,positions[i],blend);pickup.tip.localPosition=Vector3.Lerp(tipPosition*extension,tipPosition,blend);}
   WaterFraction=1-drain;water.localPosition=waterHome+Vector3.down*(.27f*drain);water.localScale=new Vector3(waterScale.x*Mathf.Lerp(1,.08f,drain),waterScale.y*Mathf.Max(.02f,WaterFraction),waterScale.z*Mathf.Lerp(1,.08f,drain));water.gameObject.SetActive(drain<.999f);
   if(elapsed<1.6f)State=PondState.PoseDetected;else if(elapsed<7.3f){State=PondState.AbsorbingWater;Status="Water spiralling into trunk";}else if(elapsed<8.7f){State=PondState.SprayingWater;Status="Spraying a sparkling arc";}else{State=PondState.RainbowAppearing;Status="Water becoming a pastel rainbow";}
   DrawVortex(elapsed,drain,lift);if(elapsed>=7.3f){if(sprayOrigin==Vector3.zero){sprayOrigin=pickup.tip.position;arcRight=pond.right;}DrawSpray(Mathf.Clamp01((elapsed-7.3f)/1.3f),1-Mathf.SmoothStep(0,1,(elapsed-9)/1.8f));DrawRainbow(Mathf.Clamp01((elapsed-8.2f)/2.4f),Mathf.SmoothStep(0,1,(elapsed-8.2f)/2.4f));}
   sparkles.transform.position=pickup.tip.position;if(!sparkles.isPlaying)sparkles.Play();
   if(elapsed>=12.4f&&(bend.Select((q,i)=>Quaternion.Angle(q,rest[i])).All(a=>a<.5f)||elapsed>15.5f)){RestoreRig();foreach(var line in vortex)line.enabled=false;foreach(var line in spray)line.enabled=false;barrier.enabled=false;State=PondState.PathUnlocked;unlockedElapsed=0;Status="Pond cleared! Walk beneath your rainbow.";locomotion.StopMotion();}
  }
  void Solve(Vector3 aim,float dt){var previous=(Quaternion[])bend.Clone();for(int i=0;i<bend.Length;i++)pickup.trunk[i].localRotation=bend[i];for(int pass=0;pass<18;pass++)for(int i=bend.Length-1;i>=0;i--){var t=pickup.trunk[i];var a=pickup.tip.position-t.position;var b=aim-t.position;if(a.sqrMagnitude<.00001f||b.sqrMagnitude<.00001f)continue;var correction=Quaternion.FromToRotation(a,b);t.rotation=correction*t.rotation;t.localRotation=Quaternion.RotateTowards(rest[i],t.localRotation,100);}
   var solved=pickup.trunk.Select(t=>t.localRotation).ToArray();for(int i=0;i<bend.Length;i++)pickup.trunk[i].localRotation=Quaternion.RotateTowards(previous[i],solved[i],85*dt);
   var candidate=pickup.trunk.Select(t=>t.localRotation).ToArray();float f=1;for(int j=0;j<8&&Quaternion.Angle(previousTip,pickup.tip.rotation)>115*dt+.02f;j++){f*=.5f;for(int i=0;i<bend.Length;i++)pickup.trunk[i].localRotation=Quaternion.Slerp(previous[i],candidate[i],f);}bend=pickup.trunk.Select(t=>t.localRotation).ToArray();previousTip=pickup.tip.rotation;LastTipError=Vector3.Distance(pickup.tip.position,aim);
  }
  void DrawVortex(float time,float drain,float lift){float alpha=Mathf.Sin(Mathf.Clamp01(drain)*Mathf.PI)*(1-lift);foreach(var line in vortex){line.enabled=alpha>.01f;line.startColor=line.endColor=new Color(.5f,.92f,1,alpha*.85f);}if(alpha<=.01f)return;var end=pickup.tip.position;for(int j=0;j<vortex.Length;j++)for(int i=0;i<=80;i++){float t=i/80f;float a=t*18-time*5+j*Mathf.PI*.5f;var start=pond.TransformPoint(new Vector3(Mathf.Cos(a)*2.3f,.18f,Mathf.Sin(a)*1.3f));var point=Vector3.Lerp(start,end,t)+new Vector3(Mathf.Cos(a),Mathf.Sin(a),Mathf.Sin(a))*(Mathf.Sin(t*Mathf.PI)*.3f);vortex[j].SetPosition(i,point);}}
  Vector3 Arc(float t,int band=0){return sprayOrigin+arcRight*((t-.5f)*8)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*(3.2f+band*.19f));}
  void DrawSpray(float reveal,float alpha){for(int j=0;j<spray.Length;j++){spray[j].enabled=alpha>.01f;spray[j].startColor=spray[j].endColor=new Color(.63f,.93f,1,alpha*.7f);for(int i=0;i<=80;i++){float t=i/80f*reveal;spray[j].SetPosition(i,Vector3.Lerp(pickup.tip.position,Arc(t),Mathf.SmoothStep(0,1,t*5))+pond.forward*Mathf.Sin(t*36+elapsed*6+j)*.05f);}}}
  void DrawRainbow(float reveal,float alpha){RainbowAlpha=alpha;for(int j=0;j<rainbow.Length;j++){rainbow[j].enabled=alpha>.01f;var c=Pastels[j];c.a=alpha*.7f;rainbow[j].startColor=rainbow[j].endColor=c;for(int i=0;i<=80;i++)rainbow[j].SetPosition(i,Arc(i/80f*reveal,j));}}
  void RestoreRig(){if(rest!=null){for(int i=0;i<rest.Length;i++){if(pickup&&pickup.trunk[i]){pickup.trunk[i].localRotation=rest[i];pickup.trunk[i].localPosition=positions[i];}}if(pickup&&pickup.tip)pickup.tip.localPosition=tipPosition;}if(skins!=null)for(int i=0;i<skins.Length;i++)if(skins[i])skins[i].updateWhenOffscreen=skinFlags[i];rest=bend=null;}
  public void ResetPond(){Cache();RestoreRig();water.localPosition=waterHome;water.localScale=waterScale;water.gameObject.SetActive(true);barrier.enabled=true;HideEffects();State=PondState.Blocked;WaterFraction=1;hold=-1;elapsed=unlockedElapsed=0;sprayOrigin=Vector3.zero;wasClose=false;Status="At the pond: extend one arm horizontally; hold its elbow with your other hand.";}
  void OnDisable(){if(cached){RestoreRig();if(barrier)barrier.enabled=true;if(water){water.gameObject.SetActive(true);water.localPosition=waterHome;water.localScale=waterScale;}State=PondState.Blocked;}if(effects){if(Application.isPlaying)Destroy(effects);else DestroyImmediate(effects);}effects=null;}
  void OnGUI(){if(!controller||!controller.showControls||!IsNear()&&State==PondState.Blocked)return;float w=Mathf.Min(500,Screen.width*.65f);GUILayout.BeginArea(new Rect(Screen.width-w-12,15,w,85),GUI.skin.box);GUILayout.Label("MAGICAL POND: "+Status);if(State==PondState.Blocked)GUILayout.Label("Pose match: "+MatchScore.ToString("P0")+" — hold for "+poseHoldSeconds.ToString("F1")+" seconds");GUILayout.EndArea();}
 }
}
