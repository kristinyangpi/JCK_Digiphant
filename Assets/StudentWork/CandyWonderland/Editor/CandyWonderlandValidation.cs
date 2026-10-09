using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DigiPhant;
namespace StudentWork.CandyWonderland.Editor {
 public static class CandyWonderlandValidation {
  public static readonly string Output=Path.Combine(Path.GetTempPath(),"candy-wonderland-validation");
  static void Require(bool condition,string message){if(!condition)throw new Exception("CANDY_WONDERLAND_VALIDATION_FAILED: "+message);}
  [MenuItem("DigiPhant/Candy Wonderland/Capture environment views")]
  public static void CaptureViews(){var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var world=all.Select(t=>t.GetComponent<CandyWorld>()).Single(w=>w);var loco=all.Select(t=>t.GetComponent<DigiPhantLocomotion>()).Single(l=>l);Directory.CreateDirectory(Output);Capture("gameplay-start.png",loco.followCamera.transform.position,loco.followCamera.transform.rotation,world,loco.followCamera.fieldOfView);var end=world.route.Last();Capture("castle.png",end+new Vector3(10,10,-17),Quaternion.LookRotation((end+new Vector3(-2,7,0))-(end+new Vector3(10,10,-17))),world,54);Capture("overview.png",new Vector3(55,45,-68),Quaternion.LookRotation(new Vector3(0,2,0)-new Vector3(55,45,-68)),world,55);Debug.Log("CANDY_WONDERLAND_CAPTURES: "+Output);}
  [MenuItem("DigiPhant/Candy Wonderland/Validate scene and capture views")]
  public static void Validate(){
   var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var world=all.Select(t=>t.GetComponent<CandyWorld>()).Single(w=>w);var loco=all.Select(t=>t.GetComponent<DigiPhantLocomotion>()).Single(l=>l);var movement=loco.GetComponent<StudentObstacleMovement>();var finish=all.Select(t=>t.GetComponent<RibbonFinish>()).Single(f=>f);var intro=all.Select(t=>t.GetComponent<LevelIntroduction>()).Single(i=>i);
   Require(world.elevated&&loco.GetComponent<CandyTerrainGrounding>(),"real terrain grounding is installed");Require(world.candyPrefabs.Length==9&&world.candyPrefabs.All(x=>x),"nine reusable candy variants");Require(loco.GetComponent<LollipopPickup>()&&loco.GetComponent<StudentFlight>()&&loco.GetComponent<FinishBallet>(),"all interaction systems retained");
   foreach(var t in all)Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"missing script on "+t.name);
   foreach(var r in all.Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r&&r.gameObject.activeInHierarchy))Require(r.sharedMaterials.All(m=>m&&m.shader&&!ShaderUtil.ShaderHasError(m.shader)),"invalid material on "+r.name);
   float maxSlope=0;int blocked=0;for(int i=0;i<world.route.Length-1;i++){var a=world.route[i];var b=world.route[i+1];var d=b-a;float slope=Mathf.Atan2(Mathf.Abs(d.y),new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;maxSlope=Mathf.Max(maxSlope,slope);Require(new Vector2(a.x,a.z).magnitude<loco.stageRadius-.1f,"route inside current stage boundary");var p=a+Vector3.up*.095f;var flat=new Vector3(d.x,0,d.z);if(movement.Penetration(p,Quaternion.LookRotation(flat))>.001f)blocked++;}
   Require(maxSlope<12,"gentle slopes "+maxSlope);Require(world.pathSurface&&world.pathSurface.sharedMesh,"continuous path MeshCollider");
   // Validate both directed crossing sections and full route continuity, without disabling static fences.
   var balls=all.Select(t=>t.GetComponent<RollingCandyBall>()).Where(b=>b&&b.gameObject.activeInHierarchy).ToArray();var pond=loco.GetComponent<MagicalPond>();Require(balls.Length==(pond?2:3),"active hazards reflect pond replacement");bool pondBlocked=pond&&pond.barrier.enabled;if(pond)pond.barrier.enabled=false;var originalPositions=balls.Select(b=>b.transform.position).ToArray();foreach(var ball in balls)ball.solid.enabled=false;
   int staticBlocked=0;for(int i=0;i<world.route.Length-1;i++){var a=world.route[i]+Vector3.up*.095f;var d=world.route[i+1]-world.route[i];var rot=Quaternion.LookRotation(new Vector3(d.x,0,d.z));float penetration=movement.Penetration(a,rot);if(penetration>.002f){staticBlocked++;var retained=movement.obstacles;foreach(var box in retained){movement.obstacles=new[]{box};if(movement.Penetration(a,rot)>.002f)Debug.LogWarning("ROUTE_BLOCKER "+box.name+" / "+box.transform.parent.name+" at "+box.transform.position);}movement.obstacles=retained;Debug.LogWarning("ROUTE_OBSTRUCTION sample="+i+" penetration="+penetration+" position="+a);}}
   foreach(var ball in balls)ball.solid.enabled=true;
   if(pond)pond.barrier.enabled=pondBlocked;
   Require(staticBlocked==0,"static route clearance: "+staticBlocked+" blocked samples");
   foreach(var ball in balls){for(int i=0;i<200;i++)ball.Advance(.1f);Require(ball.DistanceRolled>3,"rolling hazard can move "+ball.name);}
   for(int i=0;i<balls.Length;i++)balls[i].transform.position=originalPositions[i];
   Require(Vector3.Distance(finish.transform.position,world.route.Last())<.2f,"existing finish at castle destination");
   CaptureTitle(intro,loco.followCamera);
   intro.SetElapsed(.8f);Require(intro.Visible&&!intro.group.blocksRaycasts,"title shown and controls unobstructed");intro.SetElapsed(intro.Duration);Require(!intro.Visible,"title fades out");
   Directory.CreateDirectory(Output);Capture("gameplay-start.png",loco.followCamera.transform.position,loco.followCamera.transform.rotation,world,loco.followCamera.fieldOfView);
   var end=world.route.Last();Capture("castle.png",end+new Vector3(10,10,-17),Quaternion.LookRotation((end+new Vector3(-2,7,0))-(end+new Vector3(10,10,-17))),world,54);
   Capture("overview.png",new Vector3(55,45,-68),Quaternion.LookRotation(new Vector3(0,2,0)-new Vector3(55,45,-68)),world,55);
   Debug.Log("CANDY_WONDERLAND_VALIDATION_OK: "+world.route.Length+" route samples; max slope "+maxSlope.ToString("F2")+" degrees; static route clear (pond gate tested separately); remaining hazards roll; 9 variants; all gameplay refs; captures: "+Output);
  }
  static void CaptureTitle(LevelIntroduction intro,Camera camera){var canvas=intro.GetComponent<Canvas>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldTarget=camera.targetTexture;var oldRect=camera.rect;var oldAspect=camera.aspect;try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.rect=new Rect(0,0,1,1);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(540,960)}){intro.SetElapsed(1);StudentWork.Editor.LollipopForestValidation.Capture(camera,"wonderland-title-"+size.x+"x"+size.y,size.x,size.y,canvas);}}finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;camera.targetTexture=oldTarget;camera.rect=oldRect;camera.aspect=oldAspect;intro.SetElapsed(intro.Duration);}}
  public static void Capture(string name,Vector3 position,Quaternion rotation,CandyWorld world,float fov=50){var host=new GameObject("Temporary art validation camera");var camera=host.AddComponent<Camera>();camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=400;var target=new RenderTexture(1600,1000,24);camera.targetTexture=target;var previous=RenderTexture.active;try{CandyCameraClearance clearance=null;if(name=="gameplay-start.png"){clearance=world.GetComponent<CandyCameraClearance>();if(clearance)clearance.Apply();}camera.Render();if(clearance)clearance.Restore();RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(host);target.Release();UnityEngine.Object.DestroyImmediate(target);}}
 }
}
