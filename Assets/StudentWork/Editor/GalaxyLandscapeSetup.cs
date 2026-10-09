using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class GalaxyLandscapeSetup {
  [MenuItem("DigiPhant/Student Props/Apply Galaxy Floor and Rounded Road Edges")]
  public static void Apply(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
   var scene=SceneManager.GetActiveScene();
   if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()||scene.isDirty)return;
   var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
   var floor=all.Single(t=>t.name=="Ground"&&t.GetComponent<MeshRenderer>());
   var road=all.Single(t=>t.name=="Open path"&&t.GetComponent<MeshRenderer>());
   string backup=Path.ChangeExtension(scene.path,null)+"_BeforeGalaxyFloor.unity";
   if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(backup)&&!AssetDatabase.CopyAsset(scene.path,backup))throw new Exception("Backup failed");
   var shader=Shader.Find("StudentWork/Navy Galaxy Floor");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Galaxy shader unavailable");
   const string materialPath="Assets/StudentWork/CandyLandscape/Galaxy Floor.mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
   if(!mat){mat=new Material(shader){name="Dark navy galaxy floor"};AssetDatabase.CreateAsset(mat,materialPath);}
   Undo.RecordObject(floor.GetComponent<MeshRenderer>(),"Galaxy floor");floor.GetComponent<MeshRenderer>().sharedMaterial=mat;
   var stars=floor.GetComponent<GalaxyFloor>();if(!stars)stars=Undo.AddComponent<GalaxyFloor>(floor.gameObject);stars.floor=floor.GetComponent<MeshRenderer>();stars.SetAnimationTime(0);
   var edges=road.GetComponent<RoundedRoadEdges>();if(!edges)edges=Undo.AddComponent<RoundedRoadEdges>(road.gameObject);edges.road=road.GetComponent<MeshRenderer>();edges.Rebuild();
   EditorUtility.SetDirty(stars);EditorUtility.SetDirty(edges);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("GALAXY_LANDSCAPE_SETUP_OK: navy floor, twinkling stars, "+edges.CornerCount+" rounded road corners. Backup: "+backup);
  }
  [MenuItem("DigiPhant/Student Props/Validate Galaxy Landscape")]
  public static void Validate(){
   var path=SceneManager.GetActiveScene().path;var before=EditorSceneManager.OpenPreviewScene(Path.ChangeExtension(path,null)+"_BeforeGalaxyFloor.unity");var after=EditorSceneManager.OpenPreviewScene(path);
   try{
    Transform[] All(Scene s)=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
    var old=All(before);var all=All(after);
    foreach(string name in new[]{"Ground","Open path"}){
     var a=old.Single(t=>t.name==name&&t.GetComponent<MeshFilter>());var b=all.Single(t=>t.name==name&&t.GetComponent<MeshFilter>());
     if(a.localPosition!=b.localPosition||a.localRotation!=b.localRotation||a.localScale!=b.localScale||a.GetComponent<MeshFilter>().sharedMesh!=b.GetComponent<MeshFilter>().sharedMesh)throw new Exception(name+" geometry changed");
    }
    var floor=all.Single(t=>t.name=="Ground"&&t.GetComponent<GalaxyFloor>()).GetComponent<GalaxyFloor>();floor.enabled=false;
    var road=all.Single(t=>t.name=="Open path"&&t.GetComponent<RoundedRoadEdges>());var edges=road.GetComponent<RoundedRoadEdges>();edges.Rebuild();if(edges.CornerCount<4)throw new Exception("Rounded corners missing");
    if(ShaderUtil.ShaderHasError(floor.floor.sharedMaterial.shader)||ShaderUtil.ShaderHasError(edges.road.sharedMaterial.shader))throw new Exception("Shader errors");
    var rainbow=road.GetComponent<RainbowRoad>();rainbow.enabled=false;rainbow.SetAnimationTime(0);
    var camera=all.Select(t=>t.GetComponent<Camera>()).First(c=>c);camera.scene=after;camera.rect=new Rect(0,0,1,1);camera.transform.position=new Vector3(34,44,-39);camera.transform.LookAt(new Vector3(0,1,1));
    floor.SetAnimationTime(0);var first=Capture(camera,"overview");floor.SetAnimationTime(2);var second=Capture(camera,"twinkle");if(first.SequenceEqual(second))throw new Exception("Stars not twinkling");
    camera.transform.position=new Vector3(7,6,12);camera.transform.LookAt(new Vector3(1,0,7));Capture(camera,"rounded-edge");
    Debug.Log("GALAXY_LANDSCAPE_VALIDATION_OK: original floor and road geometry/transforms exact, "+edges.CornerCount+" rounded corners, shaders compiled, twinkle renders differ. Images: "+Path.Combine(Path.GetTempPath(),"galaxy-landscape-validation"));
   }finally{EditorSceneManager.ClosePreviewScene(after);EditorSceneManager.ClosePreviewScene(before);}
  }
  static byte[] Capture(Camera camera,string name){
   var rt=new RenderTexture(1280,960,24);var image=new Texture2D(1280,960,TextureFormat.RGB24,false);var previous=RenderTexture.active;
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,960),0,0);image.Apply();var bytes=image.EncodeToPNG();var folder=Path.Combine(Path.GetTempPath(),"galaxy-landscape-validation");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),bytes);return bytes;}
   finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
  }
 }
}
