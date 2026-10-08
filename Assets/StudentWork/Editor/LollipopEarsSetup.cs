using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class LollipopEarsSetup {
  [MenuItem("DigiPhant/Student Props/Enable Large Ears Flap Flight")]
  public static void Enable() {
   if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first");
   var scene=SceneManager.GetActiveScene();
   if(string.IsNullOrEmpty(scene.path)||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()||scene.isDirty)return;
   string backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeLargeEars.unity");
   if(!AssetDatabase.CopyAsset(scene.path,backup))throw new IOException("Backup failed");
   var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
   var pickup=all.Select(t=>t.GetComponent<LollipopPickup>()).Single(p=>p);
   Undo.RecordObject(pickup,"Enable large ears");
   pickup.largeEars=new[]{all.Single(t=>t.name=="elephant_l_Ear1_bone"),all.Single(t=>t.name=="elephant_r_Ear1_bone")};
   if(pickup.birthdayWings) Undo.DestroyObjectImmediate(pickup.birthdayWings.gameObject);
   pickup.birthdayWings=null;pickup.wingPivots=Array.Empty<Transform>();
   var flight=pickup.GetComponent<StudentFlight>();if(!flight)flight=Undo.AddComponent<StudentFlight>(pickup.gameObject);
   Undo.RecordObject(flight,"Configure flap flight");flight.hoverHeight=2.2f;flight.riseSpeed=1.2f;flight.landingSpeed=.65f;flight.performer=1;pickup.flight=flight;
   EditorUtility.SetDirty(pickup);EditorUtility.SetDirty(flight);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("LARGE_EARS_SETUP_OK: second bite grows ears; sideways arm flapping controls vertical flight only. Backup: "+backup);
  }
 }
}
