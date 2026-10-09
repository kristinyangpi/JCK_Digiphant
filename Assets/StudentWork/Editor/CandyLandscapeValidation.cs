using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class CandyLandscapeValidation {
  [MenuItem("DigiPhant/Student Props/Validate Candy Landscape")]
  public static void Validate(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
   string path=SceneManager.GetActiveScene().path;
   string backup=Path.ChangeExtension(path,null)+"_BeforeCandyLandscape.unity";
   var oldScene=EditorSceneManager.OpenPreviewScene(backup);var scene=EditorSceneManager.OpenPreviewScene(path);
   try {
    Transform[] All(Scene s)=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
    var old=All(oldScene);var all=All(scene);
    var before=old.Single(t=>t.name=="Open path"&&t.GetComponent<MeshFilter>());var after=all.Single(t=>t.name=="Open path"&&t.GetComponent<MeshFilter>());
    var original=before.GetComponent<MeshFilter>().sharedMesh;var rainbow=after.GetComponent<MeshFilter>().sharedMesh;
    if(!original.vertices.SequenceEqual(rainbow.vertices)||!original.triangles.SequenceEqual(rainbow.triangles)||before.localPosition!=after.localPosition||before.localRotation!=after.localRotation||before.localScale!=after.localScale)throw new Exception("Road route/shape changed");
    if(rainbow.uv.Length!=rainbow.vertexCount)throw new Exception("Rainbow road UV missing");
    var cotton=all.Where(t=>new[]{"Flat canopy","Foliage","Foliage crown"}.Contains(t.name)&&t.GetComponent<MeshFilter>()).ToArray();
    if(cotton.Length<36 || cotton.Any(t=>t.GetComponent<MeshRenderer>().sharedMaterial.shader.name!="StudentWork/Pastel Cotton Candy" || t.GetComponent<MeshFilter>().sharedMesh.vertexCount<1000))throw new Exception("Cotton canopy missing");
    if(cotton.Select(t=>t.GetComponent<MeshRenderer>().sharedMaterial).Distinct().Count()!=6)throw new Exception("Pastel variation missing");
    if(ShaderUtil.ShaderHasError(after.GetComponent<MeshRenderer>().sharedMaterial.shader)||ShaderUtil.ShaderHasError(cotton[0].GetComponent<MeshRenderer>().sharedMaterial.shader))throw new Exception("Landscape shader compile error");
    var animation=after.GetComponent<RainbowRoad>();if(!animation)throw new Exception("Rainbow animation missing");
    animation.enabled=false;
    var camera=all.Select(t=>t.GetComponent<Camera>()).First(c=>c);camera.scene=scene;camera.rect=new Rect(0,0,1,1);
    camera.transform.position=new Vector3(34,44,-39);camera.transform.LookAt(new Vector3(0,1,1));
    animation.SetAnimationTime(0);var first=Capture(camera,"overview");
    animation.SetAnimationTime(5);var second=Capture(camera,"overview-color-shift");
    if(first.SequenceEqual(second))throw new Exception("Road colors not animated");
    var cloud=cotton.First(t=>t.name=="Flat canopy");camera.transform.position=cloud.position+new Vector3(5,2,-5);camera.transform.LookAt(cloud.position+Vector3.up*.35f);Capture(camera,"cotton-closeup");
    animation.SetAnimationTime(0);camera.transform.position=new Vector3(5,7,-7);camera.transform.LookAt(new Vector3(0,.5f,6));Capture(camera,"rainbow-closeup");
    var proxies=all.Select(t=>t.GetComponent<CourseObstacleProxy>()).Where(p=>p&&p.source&&p.source.gameObject.activeInHierarchy).ToArray();
    foreach(var proxy in proxies){proxy.SyncBounds();if(Vector3.Distance(proxy.GetComponent<BoxCollider>().size,proxy.source.bounds.size)>.001f)throw new Exception("Cotton collider bounds stale");}
    Debug.Log("CANDY_LANDSCAPE_VALIDATION_OK: "+cotton.Length+" fluffy canopies in six pastels; original road vertices/triangles/transforms exact; color-change renders differ; shaders compiled; obstacle proxies match. Images: "+Path.Combine(Path.GetTempPath(),"candy-landscape-validation"));
   }finally{EditorSceneManager.ClosePreviewScene(scene);EditorSceneManager.ClosePreviewScene(oldScene);}
  }
  static byte[] Capture(Camera camera,string name){
   var rt=new RenderTexture(1280,960,24);var image=new Texture2D(1280,960,TextureFormat.RGB24,false);var previous=RenderTexture.active;
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,960),0,0);image.Apply();var bytes=image.EncodeToPNG();var folder=Path.Combine(Path.GetTempPath(),"candy-landscape-validation");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),bytes);return bytes;}
   finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
  }
 }
}
