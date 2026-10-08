using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DigiPhant;
namespace StudentWork.Editor {
 public static class CourseObstacleSetup {
  [MenuItem("DigiPhant/Student Props/Enable Course Obstacles")]
  public static void Enable() {
   if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
   var scene=SceneManager.GetActiveScene();
   if(string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save scene first.");
   if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
   var backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeCourseObstacles.unity");
   if(!AssetDatabase.CopyAsset(scene.path,backup)) throw new IOException("Backup failed");
   var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
   var course=all.Single(t=>t.name.StartsWith("Savannah Course"));
   string[] solidNames={"Trunk","Branch","Flat canopy","Foliage","Foliage crown","Stone bowl","Timber","Post","Finish beam"};
   var existing=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Student Course Obstacle Proxies" && r.GetComponentsInChildren<CourseObstacleProxy>(true).Length>0);
   var proxyRoot=existing ? existing : new GameObject("Student Course Obstacle Proxies");
   if(!existing) { SceneManager.MoveGameObjectToScene(proxyRoot,scene);Undo.RegisterCreatedObjectUndo(proxyRoot,"Create obstacle proxies"); }
   var colliders=course.GetComponentsInChildren<MeshFilter>(true).Where(m=>solidNames.Contains(m.name) && m.sharedMesh).Select(m=> {
    var source=m.GetComponent<MeshRenderer>();
    var proxy=proxyRoot.GetComponentsInChildren<CourseObstacleProxy>(true).FirstOrDefault(p=>p.source==source);
    if(!proxy) {
     var go=new GameObject("Obstacle - "+m.name);Undo.RegisterCreatedObjectUndo(go,"Create solid obstacle");go.transform.SetParent(proxyRoot.transform);
     proxy=Undo.AddComponent<CourseObstacleProxy>(go);Undo.AddComponent<BoxCollider>(go);proxy.source=source;
    }
    proxy.SyncBounds();return proxy.GetComponent<BoxCollider>();
   }).ToArray();
   var locomotion=all.Select(t=>t.GetComponent<DigiPhantLocomotion>()).Where(c=>c).Single();
   var movement=locomotion.GetComponent<StudentObstacleMovement>();
   if(!movement) movement=Undo.AddComponent<StudentObstacleMovement>(locomotion.gameObject);
   Undo.RecordObject(movement,"Configure obstacle blocking");movement.obstacles=colliders;
   movement.bodyCenter=new Vector3(0,1.65f,.2f);movement.bodyHalfSize=new Vector3(1.5f,1.55f,2.5f);
   EditorUtility.SetDirty(movement);Physics.SyncTransforms();
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   if(!File.Exists(backup)) throw new IOException("Backup missing");
   Debug.Log("COURSE_OBSTACLES_SETUP_OK: "+colliders.Length+" solids | backup: "+backup);
  }
  [MenuItem("DigiPhant/Student Props/Validate Course Obstacles")]
  public static void Validate() {
   if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
   var scene=EditorSceneManager.NewPreviewScene();
   try {
    var host=new GameObject("Validation elephant");SceneManager.MoveGameObjectToScene(host,scene);
    var movement=host.AddComponent<StudentObstacleMovement>();
    var tree=new GameObject("Validation trunk");SceneManager.MoveGameObjectToScene(tree,scene);
    tree.transform.position=new Vector3(0,1.5f,6); var box=tree.AddComponent<BoxCollider>();box.size=new Vector3(.4f,3,.4f);
    movement.obstacles=new[]{box};Physics.SyncTransforms();
    Quaternion rotation=Quaternion.identity;
    var blocked=movement.Resolve(Vector3.zero,ref rotation,0,Vector3.forward*12);
    if(blocked.z>3.66f || !movement.Blocked || movement.Penetration(blocked,rotation)>.001f) throw new Exception("Forward sweep passed trunk");
    var reverse=movement.Resolve(new Vector3(0,0,10),ref rotation,0,Vector3.back*10);
    if(reverse.z<8.44f || !movement.Blocked) throw new Exception("Reverse sweep passed trunk");
    var clear=movement.Resolve(new Vector3(5,0,0),ref rotation,0,Vector3.forward*12);
    if(Vector3.Distance(clear,new Vector3(5,0,12))>.01f) throw new Exception("Bypass route blocked");
    // A sideways tree must block rotational penetration as well as translation.
    tree.transform.position=new Vector3(2,1.5f,0);Physics.SyncTransforms();rotation=Quaternion.identity;
    movement.Resolve(Vector3.zero,ref rotation,90,Vector3.zero);
    if(Quaternion.Angle(rotation,Quaternion.Euler(0,90,0))<1 || !movement.Blocked || movement.Penetration(Vector3.zero,rotation)>.001f) throw new Exception("Turning penetrated obstacle");
    tree.transform.position=new Vector3(0,5,6);Physics.SyncTransforms();rotation=Quaternion.identity;
    clear=movement.Resolve(Vector3.zero,ref rotation,0,Vector3.forward*12);
    if(clear.z<11.99f) throw new Exception("High canopy blocked path below it");
    var second=new GameObject("Second overlap obstacle");SceneManager.MoveGameObjectToScene(second,scene);
    var secondBox=second.AddComponent<BoxCollider>();secondBox.size=new Vector3(.4f,3,.4f);
    tree.transform.position=new Vector3(-1.2f,1.5f,0);second.transform.position=new Vector3(1.85f,1.5f,0);
    Physics.SyncTransforms();movement.obstacles=new[]{box,secondBox};
    if(movement.CanEnter(Vector3.zero,Quaternion.identity,new Vector3(.2f,0,0),Quaternion.identity)) throw new Exception("Escaping one obstacle entered another");
    movement.obstacles=new[]{box};tree.transform.position=new Vector3(7.25f,1.5f,6.6f);box.size=new Vector3(.2f,3,.2f);Physics.SyncTransforms();
    rotation=Quaternion.identity;
    var boundary=movement.ResolveBounded(new Vector3(9,0,0),ref rotation,0,Vector3.forward*8,Vector3.zero,10);
    if(boundary.magnitude>10.001f || movement.Penetration(boundary,rotation)>.001f || !movement.StageEdge) throw new Exception("Stage projection crossed obstacle");
    var current=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StudentObstacleMovement>(true)).Single();
    if(current.obstacles==null || current.obstacles.Length<20 || current.obstacles.Any(c=>!c || c.isTrigger)) throw new Exception("Saved obstacle refs invalid");
    Debug.Log("COURSE_OBSTACLES_VALIDATION_OK: forward/backward sweep, bypass, blocked rotation, high canopy clearance, saved collider references");
   } finally { EditorSceneManager.ClosePreviewScene(scene); }
  }
 }
}
