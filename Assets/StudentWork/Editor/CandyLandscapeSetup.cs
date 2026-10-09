using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class CandyLandscapeSetup {
  const string Folder="Assets/StudentWork/CandyLandscape";
  [MenuItem("DigiPhant/Student Props/Apply Cotton Candy Trees and Rainbow Road")]
  public static void Apply(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
   var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
   var road=all.Single(t=>t.name=="Open path" && t.GetComponent<MeshFilter>());
   if(road.GetComponent<RainbowRoad>()){Debug.Log("CANDY_LANDSCAPE_ALREADY_APPLIED");return;}
   if(string.IsNullOrEmpty(scene.path)||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()||scene.isDirty)return;
   string backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeCandyLandscape.unity");if(!AssetDatabase.CopyAsset(scene.path,backup))throw new IOException("Backup failed");
   var rainbow=Shader.Find("StudentWork/Animated Rainbow Road");var cotton=Shader.Find("StudentWork/Pastel Cotton Candy");if(!rainbow||!cotton)throw new Exception("Landscape shaders not compiled");
   var source=road.GetComponent<MeshFilter>().sharedMesh;
   if(source.vertexCount%2!=0)throw new Exception("Road edge pairs unavailable; route left untouched");
   var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Rainbow road - original route";var verts=mesh.vertices;var uv=new Vector2[verts.Length];float distance=0;
   for(int i=0;i<verts.Length;i+=2){if(i>0)distance+=Vector3.Distance((verts[i]+verts[i+1])*.5f,(verts[i-2]+verts[i-1])*.5f);uv[i]=new Vector2(0,distance);uv[i+1]=new Vector2(1,distance);}
   mesh.uv=uv;AssetDatabase.CreateAsset(mesh,Folder+"/Rainbow Road.asset");
   var roadMat=new Material(rainbow){name="Flowing rainbow road"};AssetDatabase.CreateAsset(roadMat,Folder+"/Rainbow Road.mat");
   Undo.RecordObject(road.GetComponent<MeshFilter>(),"Recolor unchanged road");road.GetComponent<MeshFilter>().sharedMesh=mesh;
   Undo.RecordObject(road.GetComponent<MeshRenderer>(),"Rainbow material");road.GetComponent<MeshRenderer>().sharedMaterial=roadMat;
   var animation=Undo.AddComponent<RainbowRoad>(road.gameObject);animation.road=road.GetComponent<MeshRenderer>();
   var cloud=Cloud();AssetDatabase.CreateAsset(cloud,Folder+"/Cotton Candy Cloud.asset");
   Color[] colors={new Color(1,.65f,.82f),new Color(.79f,.68f,1),new Color(.62f,.84f,1),new Color(1,.85f,.64f),new Color(.73f,1,.89f),new Color(1,.82f,.97f)};
   string[] names={"Strawberry pink","Lavender","Baby blue","Peach","Pastel mint","Rose cream"};var mats=new Material[colors.Length];
   for(int i=0;i<mats.Length;i++){mats[i]=new Material(cotton){name=names[i]};mats[i].SetColor("_BaseColor",colors[i]);AssetDatabase.CreateAsset(mats[i],Folder+"/"+names[i]+".mat");}
   var canopies=all.Where(t=>new[]{"Flat canopy","Foliage","Foliage crown"}.Contains(t.name)&&t.GetComponent<MeshFilter>()).ToArray();
   int count=0;var parents=new Dictionary<Transform,int>();
   foreach(var canopy in canopies){if(!parents.ContainsKey(canopy.parent))parents[canopy.parent]=parents.Count;var filter=canopy.GetComponent<MeshFilter>();var renderer=canopy.GetComponent<MeshRenderer>();Undo.RecordObject(filter,"Cotton candy canopy");Undo.RecordObject(renderer,"Pastel canopy");filter.sharedMesh=cloud;renderer.sharedMaterial=mats[parents[canopy.parent]%mats.Length];count++;}
   foreach(var proxy in all.Select(t=>t.GetComponent<CourseObstacleProxy>()).Where(p=>p))proxy.SyncBounds();
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("CANDY_LANDSCAPE_SETUP_OK: "+count+" pastel cotton canopies; road vertices/triangles/transform unchanged, animated rainbow UV color. Backup: "+backup);
  }
  static Mesh Cloud(){
   var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
   var centers=new[]{new Vector3(0,.25f,0),new Vector3(-.22f,.2f,-.12f),new Vector3(.22f,.22f,-.12f),new Vector3(-.19f,.45f,.13f),new Vector3(.19f,.43f,.14f),new Vector3(0,.68f,0),new Vector3(0,.1f,.22f)};
   var sizes=new[]{new Vector3(.32f,.57f,.32f),new Vector3(.27f,.46f,.26f),new Vector3(.27f,.46f,.26f),new Vector3(.27f,.47f,.26f),new Vector3(.27f,.47f,.26f),new Vector3(.27f,.45f,.27f),new Vector3(.28f,.4f,.27f)};
   for(int lobe=0;lobe<centers.Length;lobe++){
    int start=vertices.Count;int rings=12,segments=20;
    for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++){float phi=Mathf.PI*y/rings,theta=Mathf.PI*2*x/segments;var n=new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta));vertices.Add(centers[lobe]+Vector3.Scale(n,sizes[lobe]));normals.Add(new Vector3(n.x/sizes[lobe].x,n.y/sizes[lobe].y,n.z/sizes[lobe].z).normalized);}
    for(int y=0;y<rings;y++)for(int x=0;x<segments;x++){int a=start+y*(segments+1)+x,b=a+segments+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
   }
   var mesh=new Mesh{name="Fluffy cotton candy lobes"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
  }
 }
}
