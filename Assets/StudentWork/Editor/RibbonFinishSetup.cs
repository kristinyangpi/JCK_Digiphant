using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class RibbonFinishSetup {
  const string Folder="Assets/StudentWork/FinishRibbon";
  [MenuItem("DigiPhant/Student Props/Install Ribbon Finish Celebration")]
  public static void Install(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
   var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
   if(all.Any(t=>t.GetComponent<RibbonFinish>())){Debug.Log("RIBBON_FINISH_ALREADY_INSTALLED");return;}
   if(string.IsNullOrEmpty(scene.path)||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()||scene.isDirty)return;
   string backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeRibbonFinish.unity");
   if(!AssetDatabase.CopyAsset(scene.path,backup))throw new IOException("Backup failed");
   var old=all.Single(t=>t.name=="04 Finish marker");var pickup=all.Select(t=>t.GetComponent<LollipopPickup>()).Single(p=>p);
   if(Directory.Exists(Folder))throw new InvalidOperationException("Existing finish assets require reconciliation");
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
   var bark=Material("Tall finish poles",new Color(.28f,.12f,.06f));var pink=Material("Pink finish ribbon",new Color(1,.12f,.48f));var gold=Material("Gold pole caps",new Color(1,.75f,.1f));
   var confettiMat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="Celebration confetti"};confettiMat.SetColor("_BaseColor",Color.white);confettiMat.SetFloat("_Cull",0);AssetDatabase.CreateAsset(confettiMat,Folder+"/Confetti.mat");
   var root=new GameObject("Student Ribbon Finish");Undo.RegisterCreatedObjectUndo(root,"Add ribbon finish");SceneManager.MoveGameObjectToScene(root,scene);root.transform.SetPositionAndRotation(old.position,old.rotation);
   var finish=Undo.AddComponent<RibbonFinish>(root);finish.travelRoot=pickup.locomotion.travelRoot;finish.flight=pickup.flight;
   var posts=new BoxCollider[2];
   for(int i=0;i<2;i++){
    int side=i==0?-1:1;var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name=i==0?"Tall left finish pole":"Tall right finish pole";post.transform.SetParent(root.transform,false);post.transform.localPosition=new Vector3(side*4.6f,3.1f,0);post.transform.localScale=new Vector3(.28f,6.2f,.28f);post.GetComponent<MeshRenderer>().sharedMaterial=bark;posts[i]=post.GetComponent<BoxCollider>();
    var cap=GameObject.CreatePrimitive(PrimitiveType.Sphere);cap.name="Golden finial";cap.transform.SetParent(root.transform,false);cap.transform.localPosition=new Vector3(side*4.6f,6.35f,0);cap.transform.localScale=Vector3.one*.4f;UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());cap.GetComponent<MeshRenderer>().sharedMaterial=gold;
    var ribbon=new GameObject(i==0?"Left ribbon tied end":"Right ribbon tied end");ribbon.transform.SetParent(root.transform,false);ribbon.transform.localPosition=new Vector3(side*4.6f,finish.ribbonHeight,0);
    var mesh=Ribbon(i==0?1:-1,finish.halfWidth);AssetDatabase.CreateAsset(mesh,Folder+(i==0?"/Left ribbon.asset":"/Right ribbon.asset"));var filter=ribbon.AddComponent<MeshFilter>();filter.sharedMesh=mesh;ribbon.AddComponent<MeshRenderer>().sharedMaterial=pink;
    if(i==0)finish.leftRibbon=filter;else finish.rightRibbon=filter;
   }
   var label=new GameObject("Finish lettering");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,5.35f,0);var text=label.AddComponent<TextMesh>();text.text="FINISH";text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.characterSize=.1f;text.fontSize=60;text.color=new Color(1,.9f,.4f);
   var effect=new GameObject("Finish confetti burst");effect.transform.SetParent(root.transform,false);effect.transform.localPosition=new Vector3(0,finish.ribbonHeight+.3f,0);effect.transform.localRotation=Quaternion.Euler(-90,0,0);
   var particles=effect.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=particles.main;main.playOnAwake=false;main.loop=false;main.duration=1;main.startLifetime=new ParticleSystem.MinMaxCurve(3,5);main.startSpeed=new ParticleSystem.MinMaxCurve(2.5f,5.5f);main.startSize=new ParticleSystem.MinMaxCurve(.08f,.17f);main.gravityModifier=.7f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=400;
   var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.15f,.5f),0),new GradientColorKey(Color.yellow,.25f),new GradientColorKey(Color.cyan,.5f),new GradientColorKey(new Color(.6f,.2f,1),.75f),new GradientColorKey(Color.green,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,1)});
   main.startColor=new ParticleSystem.MinMaxGradient(gradient){mode=ParticleSystemGradientMode.RandomColor};
   var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,280)});
   var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=55;shape.radius=1.3f;
   var rotation=particles.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-4,4);
   particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=confettiMat;finish.confetti=particles;
   Undo.RecordObject(old.gameObject,"Replace original gate");old.gameObject.SetActive(false);
   var movement=pickup.GetComponent<StudentObstacleMovement>();Undo.RecordObject(movement,"Add tall pole obstacles");movement.obstacles=movement.obstacles.Concat(posts).ToArray();
   foreach(var proxy in all.Select(t=>t.GetComponent<CourseObstacleProxy>()).Where(p=>p))proxy.SyncBounds();
   EditorUtility.SetDirty(movement);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("RIBBON_FINISH_SETUP_OK: 6.2m poles, 4.45m breakable ribbon, confetti, vertical flight crossing. Backup: "+backup);
  }
  static Material Material(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Cull",0);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
  static Mesh Ribbon(int side,float length){
   var vertices=new Vector3[26];var triangles=new int[12*6];
   for(int i=0;i<=12;i++){float t=i/12f;vertices[i*2]=new Vector3(side*length*t,-.18f,0);vertices[i*2+1]=new Vector3(side*length*t,.18f,0);}
   for(int i=0;i<12;i++){int a=i*2,b=a+1,c=a+2,d=a+3;int[] q={a,b,c,b,d,c};Array.Copy(q,0,triangles,i*6,6);}
   var m=new Mesh{name=side>0?"Left finish ribbon":"Right finish ribbon"};m.vertices=vertices;m.triangles=triangles;m.RecalculateNormals();m.RecalculateBounds();return m;
  }
 }
}
