using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace StudentWork.Editor {
 public static class LollipopForestSetup {
  const string Folder="Assets/StudentWork/LollipopForest";
  [MenuItem("DigiPhant/Level 1/Install Lollipop Forest")]
  public static void Install(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
   var scene=SceneManager.GetActiveScene();if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()||scene.isDirty)return;
   var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();if(all.Any(t=>t.GetComponent<LollipopForest>())){var intro=all.Select(t=>t.GetComponent<LevelIntroduction>()).Single(i=>i);intro.existingUI=all.Select(t=>t.GetComponent<DigiPhant.DigiPhantController>()).Single(c=>c);intro.SetElapsed(intro.Duration);EditorUtility.SetDirty(intro);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("LOLLIPOP_FOREST_ALREADY_INSTALLED: refreshed responsive title layout");return;}
   var trees=all.Where(t=>Regex.IsMatch(t.name,@"^Acacia \d+")&&t.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.name=="Flat canopy")).ToArray();if(trees.Length==0)throw new Exception("No existing tree scenery found");
   var pickup=all.Select(t=>t.GetComponent<LollipopPickup>()).Single(p=>p);var movement=pickup.GetComponent<StudentObstacleMovement>();var finish=all.Select(t=>t.GetComponent<RibbonFinish>()).Single(f=>f);
   var disc=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/StudentWork/LollipopTree/CandyDisc.asset");var spiral=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/StudentWork/LollipopTree/CreamSpiral.asset");
   var pink=AssetDatabase.LoadAssetAtPath<Material>("Assets/StudentWork/LollipopTree/Raspberry pink candy.mat");var cream=AssetDatabase.LoadAssetAtPath<Material>("Assets/StudentWork/LollipopTree/Strawberry cream swirl.mat");var rim=AssetDatabase.LoadAssetAtPath<Material>("Assets/StudentWork/LollipopTree/Dark pink candy edge.mat");
   if(!disc||!spiral||!pink||!cream||!rim||!movement)throw new Exception("Existing candy assets or movement obstacles unavailable");
   string backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeLollipopForest.unity");if(!AssetDatabase.CopyAsset(scene.path,backup))throw new Exception("Backup failed");
   var root=new GameObject("Level 1 - Lollipop Forest");Undo.RegisterCreatedObjectUndo(root,"Add Lollipop Forest");SceneManager.MoveGameObjectToScene(root,scene);var level=root.AddComponent<LollipopForest>();level.replacedTrees=trees.Select(t=>t.gameObject).ToArray();
   var broad=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Giant Cream Swirl.asset");if(!broad){broad=UnityEngine.Object.Instantiate(spiral);broad.name="Wide cream spiral on both candy faces";var v=broad.vertices;for(int i=0;i<v.Length;i+=2){var center=(v[i]+v[i+1])*.5f;v[i]=center+(v[i]-center)*1.5f;v[i+1]=center+(v[i+1]-center)*1.5f;}broad.vertices=v;broad.RecalculateBounds();AssetDatabase.CreateAsset(broad,Folder+"/Giant Cream Swirl.asset");}
   var solids=new List<BoxCollider>();var giants=new List<Transform>();
   for(int i=0;i<trees.Length;i++){
    var tree=trees[i];Undo.RecordObject(tree.gameObject,"Replace tree with giant lollipop");tree.gameObject.SetActive(false);
    var giant=new GameObject("Giant pink swirl lollipop "+(i+1).ToString("00"));giant.transform.SetParent(root.transform,false);giant.transform.position=tree.position;giant.transform.rotation=Quaternion.Euler(0,i*137.5f,0);giants.Add(giant.transform);
    float height=5.5f+(i%3)*.22f,scale=4.8f+(i%3)*.22f;
    var stick=GameObject.CreatePrimitive(PrimitiveType.Cylinder);stick.name="Candy cane trunk";stick.transform.SetParent(giant.transform,false);stick.transform.localPosition=Vector3.up*height*.5f;stick.transform.localScale=new Vector3(.26f,height*.5f,.26f);UnityEngine.Object.DestroyImmediate(stick.GetComponent<Collider>());stick.GetComponent<MeshRenderer>().sharedMaterial=cream;
    var stickBox=stick.AddComponent<BoxCollider>();stickBox.size=new Vector3(1,2,1);solids.Add(stickBox);
    for(int s=0;s<8;s++){var stripe=GameObject.CreatePrimitive(PrimitiveType.Cylinder);stripe.name="Pink candy cane stripe";stripe.transform.SetParent(giant.transform,false);stripe.transform.localPosition=Vector3.up*((s+.5f)*height/8);stripe.transform.localScale=new Vector3(.275f,height/32,.275f);UnityEngine.Object.DestroyImmediate(stripe.GetComponent<Collider>());stripe.GetComponent<MeshRenderer>().sharedMaterial=pink;}
    var head=new GameObject("Oversized pink and white swirl");head.transform.SetParent(giant.transform,false);head.transform.localPosition=Vector3.up*height;head.transform.localRotation=Quaternion.Euler(0,0,i%2==0?-9:9);head.transform.localScale=Vector3.one*scale;
    Part("Pink candy disc",head.transform,disc,new[]{pink,rim});Part("Cream spiral on both faces",head.transform,broad,new[]{cream});
    var headBox=head.AddComponent<BoxCollider>();headBox.size=new Vector3(.86f,.86f,.14f);solids.Add(headBox);
   }
   foreach(var proxy in all.Select(t=>t.GetComponent<CourseObstacleProxy>()).Where(p=>p))proxy.SyncBounds();
   Undo.RecordObject(movement,"Add forest solids without changing movement");movement.obstacles=movement.obstacles.Concat(solids).ToArray();Physics.SyncTransforms();level.giantLollipops=giants.ToArray();
   var road=all.Single(t=>t.name=="Open path"&&t.GetComponent<MeshFilter>());var vertices=road.GetComponent<MeshFilter>().sharedMesh.vertices;var centers=Enumerable.Range(0,vertices.Length/2).Select(i=>road.TransformPoint((vertices[i*2]+vertices[i*2+1])*.5f)).ToArray();
   int[] segments={3,5,7};Color[] colors={new Color(1,.2f,.51f),new Color(.56f,.95f,.81f),new Color(.73f,.52f,1)};var balls=new List<RollingCandyBall>();
   for(int i=0;i<segments.Length;i++){
    int segment=Mathf.Clamp(segments[i],0,centers.Length-2);var along=(centers[segment+1]-centers[segment]).normalized;var across=Vector3.Cross(Vector3.up,along).normalized;
    Vector3 laneStart=Vector3.zero,laneEnd=Vector3.zero;bool found=false;float radius=1.35f+i*.08f;
    foreach(float fraction in new[]{.5f,.3f,.7f,.15f,.85f}){ if(found)break;foreach(float halfLength in new[]{6f,5.5f,5f}){
     var center=Vector3.Lerp(centers[segment],centers[segment+1],fraction);center.y=radius+.025f;
     var a=center-across*halfLength;var b=center+across*halfLength;
     if(ClearLane(a,b,radius,movement.obstacles)&&Vector3.Distance(new Vector3(center.x,0,center.z),new Vector3(finish.transform.position.x,0,finish.transform.position.z))>9){laneStart=a;laneEnd=b;found=true;break;}
    }
    }
    if(!found)throw new Exception("Could not find a safe rolling lane for candy ball "+i+"; inspect the preserved backup before retrying");
    var host=new GameObject("Rolling candy ball "+(i+1));host.transform.SetParent(root.transform,false);host.transform.position=Vector3.Lerp(laneStart,laneEnd,.12f+i*.32f);
    var visual=GameObject.CreatePrimitive(PrimitiveType.Sphere);visual.name="Striped rolling candy";visual.transform.SetParent(host.transform,false);visual.transform.localScale=Vector3.one*radius*2;UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
    var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Rolling candy "+(i+1)+".mat");if(!material){material=new Material(Shader.Find("StudentWork/Rolling Striped Candy")){name="Rolling candy "+(i+1)};material.SetColor("_BaseColor",colors[i]);material.SetColor("_CreamColor",cream.GetColor("_BaseColor"));AssetDatabase.CreateAsset(material,Folder+"/Rolling candy "+(i+1)+".mat");}visual.GetComponent<MeshRenderer>().sharedMaterial=material;
    var box=host.AddComponent<BoxCollider>();box.size=Vector3.one*radius*2;var ball=host.AddComponent<RollingCandyBall>();ball.visual=visual.transform;ball.solid=box;ball.start=laneStart;ball.end=laneEnd;ball.radius=radius;ball.speed=1.45f+i*.15f;ball.initialProgress=.12f+i*.32f;ball.movement=movement;ball.elephant=pickup.locomotion.travelRoot;ball.finish=finish;balls.Add(ball);movement.obstacles=movement.obstacles.Append(box).ToArray();
   }
   level.rollingBalls=balls.ToArray();level.introduction=Introduction(root.transform,finish);EditorUtility.SetDirty(movement);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("LOLLIPOP_FOREST_SETUP_OK: "+trees.Length+" giant pink/white lollipops replace tree scenery, three rolling candy obstacles, fading responsive level introduction. Existing candy targets and gameplay components preserved. Backup: "+backup);
  }
  public static bool ClearLane(Vector3 a,Vector3 b,float radius,BoxCollider[] obstacles){
   for(int i=0;i<=48;i++){var bounds=new Bounds(Vector3.Lerp(a,b,i/48f),Vector3.one*radius*2);foreach(var box in obstacles)if(box&&box.enabled&&box.gameObject.activeInHierarchy&&!box.isTrigger&&bounds.Intersects(box.bounds))return false;}return true;
  }
  static void Part(string name,Transform parent,Mesh mesh,Material[] materials){var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;}
  static LevelIntroduction Introduction(Transform parent,RibbonFinish finish){
   var root=new GameObject("Level title canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(parent,false);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
   var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
   var panel=new GameObject("Fading level title",typeof(RectTransform),typeof(CanvasGroup),typeof(Image));panel.transform.SetParent(root.transform,false);var rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.1f,.39f);rect.anchorMax=new Vector2(.9f,.61f);rect.offsetMin=rect.offsetMax=Vector2.zero;
   var image=panel.GetComponent<Image>();image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");image.type=Image.Type.Sliced;image.color=new Color(.075f,.075f,.09f,.88f);image.raycastTarget=false;
   var textObject=new GameObject("Level 1: Lollipop Forest",typeof(RectTransform),typeof(Text),typeof(Shadow));textObject.transform.SetParent(panel.transform,false);var textRect=textObject.GetComponent<RectTransform>();textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(30,20);textRect.offsetMax=new Vector2(-30,-20);
   var text=textObject.GetComponent<Text>();var existing=finish.GetComponentInChildren<TextMesh>();text.font=existing&&existing.font?existing.font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text="Level 1: Lollipop Forest";text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.fontStyle=FontStyle.Normal;text.fontSize=56;text.resizeTextForBestFit=true;text.resizeTextMinSize=20;text.resizeTextMaxSize=56;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;
   var shadow=textObject.GetComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.6f);shadow.effectDistance=new Vector2(2,-2);
   var intro=root.AddComponent<LevelIntroduction>();intro.group=panel.GetComponent<CanvasGroup>();intro.title=text;intro.existingUI=finish.flight.GetComponent<DigiPhant.DigiPhantController>();intro.SetElapsed(intro.Duration);return intro;
  }
 }
}
