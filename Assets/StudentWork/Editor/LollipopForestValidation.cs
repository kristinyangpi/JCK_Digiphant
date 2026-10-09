using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace StudentWork.Editor {
 public static class LollipopForestValidation {
  [MenuItem("DigiPhant/Level 1/Validate Lollipop Forest")]
  public static void Validate(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
   var path=SceneManager.GetActiveScene().path;var before=EditorSceneManager.OpenPreviewScene(Path.ChangeExtension(path,null)+"_BeforeLollipopForest.unity");var scene=EditorSceneManager.OpenPreviewScene(path);
   try{
    Transform[] All(Scene s)=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var old=All(before);var all=All(scene);
    var level=all.Select(t=>t.GetComponent<LollipopForest>()).Single(l=>l);var pickup=all.Select(t=>t.GetComponent<LollipopPickup>()).Single(p=>p);var oldPickup=old.Select(t=>t.GetComponent<LollipopPickup>()).Single(p=>p);
    if(level.giantLollipops.Length!=18||level.replacedTrees.Any(t=>t.activeSelf)||level.rollingBalls.Length!=3)throw new Exception("Forest replacement incomplete");
    foreach(var name in new[]{"Open path","Ground"}){
     var a=old.Single(t=>t.name==name&&t.GetComponent<MeshFilter>());var b=all.Single(t=>t.name==name&&t.GetComponent<MeshFilter>());
     if(a.GetComponent<MeshFilter>().sharedMesh!=b.GetComponent<MeshFilter>().sharedMesh||a.localPosition!=b.localPosition||a.localScale!=b.localScale||a.localRotation!=b.localRotation||a.GetComponent<MeshRenderer>().sharedMaterial!=b.GetComponent<MeshRenderer>().sharedMaterial)throw new Exception("Existing road/floor changed");
    }
    if(pickup.candies.Length!=oldPickup.candies.Length)throw new Exception("Edible targets changed");
    for(int i=0;i<pickup.candies.Length;i++)if(pickup.candies[i].position!=oldPickup.candies[i].position||pickup.candies[i].rotation!=oldPickup.candies[i].rotation||pickup.candies[i].localScale!=oldPickup.candies[i].localScale)throw new Exception("Candy interaction moved");
    if(!pickup.flight||!pickup.GetComponent<FinishBallet>()||!all.Any(t=>t.GetComponent<RibbonFinish>())||!all.Any(t=>t.name=="Student Banana Peel"||t.name.Contains("Banana")))throw new Exception("Existing game feature missing");
    var movement=pickup.GetComponent<StudentObstacleMovement>();
    if(level.giantLollipops.Any(t=>t.GetComponentsInChildren<BoxCollider>().Any(b=>!movement.obstacles.Contains(b))))throw new Exception("Giant lollipop collision missing");
    if(level.rollingBalls.Any(b=>!movement.obstacles.Contains(b.solid)||ShaderUtil.ShaderHasError(b.visual.GetComponent<MeshRenderer>().sharedMaterial.shader)))throw new Exception("Rolling candy collider/shader missing");
    foreach(var ball in level.rollingBalls)ball.Initialize();
    for(int i=0;i<500;i++)foreach(var ball in level.rollingBalls)ball.Advance(.05f);
    if(level.rollingBalls.Any(b=>b.DistanceRolled<15))throw new Exception("A rolling candy lane stalled: "+string.Join(",",level.rollingBalls.Select(b=>b.name+"="+b.DistanceRolled)));
    var camera=all.Select(t=>t.GetComponent<Camera>()).First(c=>c);camera.scene=scene;camera.rect=new Rect(0,0,1,1);camera.transform.position=new Vector3(34,44,-39);camera.transform.LookAt(new Vector3(0,1,1));
    Capture(camera,"forest-overview",1280,960);
    var giant=level.giantLollipops[0];camera.transform.position=giant.position+giant.forward*12+Vector3.up*6;camera.transform.LookAt(giant.position+Vector3.up*4);Capture(camera,"giant-lollipop",1280,960);
    var candy=level.rollingBalls[1];camera.transform.position=candy.transform.position+new Vector3(5,3,-5);camera.transform.LookAt(candy.transform.position);Capture(camera,"rolling-candy",1280,960);
    var intro=level.introduction;if(!intro||intro.title.text!="Level 1: Lollipop Forest"||intro.group.blocksRaycasts||intro.group.interactable||intro.title.raycastTarget||Mathf.Abs(intro.holdSeconds-3)>.01f)throw new Exception("Introduction setup invalid");
    intro.SetElapsed(0);if(intro.group.alpha!=0)throw new Exception("Fade-in starts opaque");intro.SetElapsed(intro.fadeInSeconds/2);if(intro.group.alpha<.4f||intro.group.alpha>.6f)throw new Exception("Fade-in not smooth");intro.SetElapsed(1);if(intro.group.alpha!=1)throw new Exception("Title hold missing");intro.SetElapsed(intro.Duration-.1f);if(intro.group.alpha>=.5f)throw new Exception("Fade-out missing");intro.SetElapsed(intro.Duration);if(intro.group.gameObject.activeSelf)throw new Exception("Title did not disappear");
    camera.transform.position=new Vector3(7,9,-13);camera.transform.LookAt(new Vector3(0,2,5));var canvas=intro.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
    foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(540,960)}){
     intro.SetElapsed(1);Capture(camera,"title-"+size.x+"x"+size.y,size.x,size.y,canvas);
    }
    ValidateMovingGuard();
    Debug.Log("LOLLIPOP_FOREST_VALIDATION_OK: 18 replacement lollipops, three rolling lanes, solids registered, original road/floor and seven edible targets preserved, nonblocking three-second hold with smooth fade, landscape/4:3/portrait title renders. Images: "+Path.Combine(Path.GetTempPath(),"lollipop-forest-validation"));
   }finally{EditorSceneManager.ClosePreviewScene(scene);EditorSceneManager.ClosePreviewScene(before);}
  }
  static void ValidateMovingGuard(){
   var scene=EditorSceneManager.NewPreviewScene();try{
    var elephant=new GameObject("Guard-test elephant");SceneManager.MoveGameObjectToScene(elephant,scene);var movement=elephant.AddComponent<StudentObstacleMovement>();
    var host=new GameObject("Guard-test rolling candy");SceneManager.MoveGameObjectToScene(host,scene);var visual=new GameObject("Visual");visual.transform.SetParent(host.transform,false);var box=host.AddComponent<BoxCollider>();box.size=Vector3.one*2.8f;
    var ball=host.AddComponent<RollingCandyBall>();ball.visual=visual.transform;ball.solid=box;ball.movement=movement;ball.elephant=elephant.transform;ball.radius=1.4f;ball.speed=2;ball.start=new Vector3(-6,1.425f,0);ball.end=new Vector3(6,1.425f,0);movement.obstacles=new[]{box};ball.Initialize();
    for(int i=0;i<150;i++){ball.Advance(.05f);if(movement.Penetration(elephant.transform.position,Quaternion.identity)>.0001f)throw new Exception("Moving ball penetrated elephant");}
    if(!ball.WaitingForClearance||ball.transform.position.x>-2.89f)throw new Exception("Rolling ball did not stop safely");float rolled=ball.DistanceRolled;elephant.transform.position=Vector3.forward*8;for(int i=0;i<40;i++)ball.Advance(.05f);if(ball.DistanceRolled<rolled+3)throw new Exception("Rolling ball did not resume after bypass");
    var rot=Quaternion.Euler(0,90,0);var blocked=movement.Resolve(new Vector3(-8,0,0),ref rot,0,Vector3.forward*15);if(!movement.Blocked||movement.Penetration(blocked,rot)>.001f)throw new Exception("Elephant walked through candy obstacle");
   }finally{EditorSceneManager.ClosePreviewScene(scene);}
  }
  public static void Capture(Camera camera,string name,int width,int height,Canvas canvas=null){
   var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);var old=RenderTexture.active;
   try{camera.targetTexture=rt;camera.aspect=width/(float)height;Canvas.ForceUpdateCanvases();if(canvas){canvas.GetComponent<LevelIntroduction>().RefreshLayout();Canvas.ForceUpdateCanvases();var corners=new Vector3[4];canvas.GetComponent<LevelIntroduction>().title.rectTransform.GetWorldCorners(corners);
     var projected=corners.Select(c=>camera.WorldToViewportPoint(c)).ToArray();
     if(projected.Any(p=>p.x<0||p.x>1||p.y<0||p.y>1)||Mathf.Abs((projected[0].x+projected[2].x)*.5f-.5f)>.01f||Mathf.Abs((projected[0].y+projected[2].y)*.5f-.5f)>.01f)throw new Exception("Title clipped or off-center at "+width+"x"+height);
    }
    camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();var folder=Path.Combine(Path.GetTempPath(),"lollipop-forest-validation");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());}
   finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
  }
 }
}
