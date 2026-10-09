using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DigiPhant;
namespace StudentWork.Editor {
 public static class ElephantThirdPersonCameraSetup {
  [MenuItem("DigiPhant/Student Props/Install rear third person camera")]
  public static void Install(){if(EditorApplication.isPlaying)throw new Exception("Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(string.IsNullOrEmpty(scene.path))throw new Exception("Save scene first");var loco=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DigiPhantLocomotion>(true)).Single();if(!loco.followCamera||!loco.travelRoot)throw new Exception("Existing camera/player references missing");var backup=AssetDatabase.GenerateUniqueAssetPath(scene.path.Replace(".unity","_BeforeThirdPersonCamera.unity"));EditorSceneManager.SaveScene(scene,backup,true);var rig=loco.GetComponent<ElephantThirdPersonCamera>();if(!rig)rig=loco.gameObject.AddComponent<ElephantThirdPersonCamera>();rig.locomotion=loco;loco.followElephant=true;rig.Snap();Validate(rig);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("THIRD_PERSON_CAMERA_INSTALLED: existing gameplay camera follows elephant heading from behind; ballet retains cinematic camera. Backup="+backup);}
  static void Validate(ElephantThirdPersonCamera rig){var root=rig.locomotion.travelRoot;var camera=rig.locomotion.followCamera.transform;var pos=root.position;var rot=root.rotation;try{foreach(float yaw in new[]{0f,90f,180f,270f}){root.rotation=Quaternion.Euler(0,yaw,0);rig.Follow(.016f);Require(Vector3.Dot(camera.position-root.position,root.forward)<-2.9f,"camera stays behind through heading change");var aim=(root.position+Vector3.up*rig.lookHeight-camera.position).normalized;Require(Vector3.Dot(camera.forward,aim)>.999f,"camera looks at elephant");}root.position+=new Vector3(20,2,10);rig.Follow(.016f);Require(Vector3.Distance(camera.position,root.position)>4,"camera follows relocated elephant");}finally{root.SetPositionAndRotation(pos,rot);rig.Snap();}Debug.Log("THIRD_PERSON_CAMERA_CHECK_OK: four headings, rear hemisphere, player aim, relocation");}
  static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 }
}
