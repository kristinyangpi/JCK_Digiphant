using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class FinishBalletSetup {
  const string Folder="Assets/StudentWork/FinishBallet";
  [MenuItem("DigiPhant/Student Props/Install Finish Ballet")]
  public static void Install(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
   var scene=SceneManager.GetActiveScene();if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()||scene.isDirty)return;
   var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var locomotion=all.Select(t=>t.GetComponent<DigiPhant.DigiPhantLocomotion>()).Single(l=>l);var finish=all.Select(t=>t.GetComponent<RibbonFinish>()).Single(f=>f);
   if(locomotion.GetComponent<FinishBallet>()){Debug.Log("FINISH_BALLET_ALREADY_INSTALLED");return;}
   string backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeFinishBallet.unity");if(!AssetDatabase.CopyAsset(scene.path,backup))throw new Exception("Backup failed");
   if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/StudentWork","FinishBallet");
   var ballet=Undo.AddComponent<FinishBallet>(locomotion.gameObject);ballet.locomotion=locomotion;ballet.flight=locomotion.GetComponent<StudentFlight>();ballet.visualRoot=locomotion.elephantAnimator.transform;
   Transform Bone(string name)=>ballet.visualRoot.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
   ballet.leftHip=Bone("elephant_l_Femur_bone");ballet.rightHip=Bone("elephant_r_Femur_bone");ballet.leftArm=Bone("elephant_l_Scapula_bone");ballet.rightArm=Bone("elephant_r_Scapula_bone");ballet.neck=Bone("elephant_Neck_bone");
   var tutu=new GameObject("Pink Ballet Tutu");Undo.RegisterCreatedObjectUndo(tutu,"Add pink tutu");tutu.transform.SetParent(Bone("elephant_Spine1_bone"),false);
   tutu.transform.position=locomotion.travelRoot.TransformPoint(new Vector3(0,2.45f,-.58f));tutu.transform.rotation=ballet.visualRoot.rotation*Quaternion.Euler(90,0,0);
   var pink=Mat("Rose pink tulle",new Color(1,.26f,.59f));var light=Mat("Blush pink tulle",new Color(1,.58f,.79f));var waist=Mat("Pink satin waistband",new Color(.88f,.12f,.42f));
   for(int i=0;i<3;i++){
    var go=new GameObject("Layered ruffled tulle "+(i+1));go.transform.SetParent(tutu.transform,false);
    var mesh=Skirt(i);AssetDatabase.CreateAsset(mesh,Folder+"/Tulle layer "+(i+1)+".asset");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=i%2==0?light:pink;
   }
   var band=new GameObject("Satin waistband");band.transform.SetParent(tutu.transform,false);var belt=Band();AssetDatabase.CreateAsset(belt,Folder+"/Waistband.asset");band.AddComponent<MeshFilter>().sharedMesh=belt;band.AddComponent<MeshRenderer>().sharedMaterial=waist;
   for(int i=0;i<3;i++){var bow=GameObject.CreatePrimitive(PrimitiveType.Sphere);bow.name=i==2?"Bow knot":"Satin bow loop";bow.transform.SetParent(tutu.transform,false);bow.transform.localPosition=new Vector3(i==2?0:i==0?-.22f:.22f,.015f,1.03f);bow.transform.localScale=i==2?new Vector3(.18f,.18f,.15f):new Vector3(.42f,.27f,.17f);bow.transform.localRotation=Quaternion.Euler(0,0,i==0?-18:18);UnityEngine.Object.DestroyImmediate(bow.GetComponent<Collider>());bow.GetComponent<MeshRenderer>().sharedMaterial=pink;}
   ballet.tutu=tutu.transform;tutu.SetActive(false);Undo.RecordObject(finish,"Attach finish ballet");finish.ballet=ballet;EditorUtility.SetDirty(finish);EditorUtility.SetDirty(ballet);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("FINISH_BALLET_SETUP_OK: automatic landing, hind-foot standing, layered pink tutu, two smooth turns and bow. Backup: "+backup);
  }
  static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Cull",0);m.SetFloat("_Smoothness",.22f);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
  static Mesh Skirt(int layer){
   var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();const int segments=128,rings=8;
   for(int r=0;r<=rings;r++)for(int s=0;s<=segments;s++){
    float t=r/(float)rings,a=s/(float)segments*Mathf.PI*2;float radial=Mathf.Lerp(1,1.85f-layer*.13f,Mathf.Pow(t,.75f));float ripple=Mathf.Sin(a*24+layer*.7f)*.075f*t;
    vertices.Add(new Vector3(Mathf.Cos(a)*(radial+ripple),-.055f*layer-t*(.48f+layer*.03f)+Mathf.Sin(a*24+layer*.7f)*.065f*t,Mathf.Sin(a)*(radial*.91f+ripple)));uv.Add(new Vector2(s/(float)segments,t));
    if(r<rings&&s<segments){int k=r*(segments+1)+s;triangles.AddRange(new[]{k,k+segments+1,k+1,k+1,k+segments+1,k+segments+2});}
   }
   var mesh=new Mesh{name="Pleated pink ballet skirt"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  static Mesh Band(){
   var v=new List<Vector3>();var tr=new List<int>();const int segments=96;
   for(int i=0;i<=segments;i++){float a=i/(float)segments*Mathf.PI*2;v.Add(new Vector3(Mathf.Cos(a)*1.035f,-.07f,Mathf.Sin(a)*.945f));v.Add(new Vector3(Mathf.Cos(a)*1.035f,.09f,Mathf.Sin(a)*.945f));if(i<segments){int k=i*2;tr.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}}
   var m=new Mesh{name="Satin waistband"};m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;
  }
 }
}
