using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StudentWork.Editor {
 public static class StudentFlightValidation {
  [MenuItem("DigiPhant/Student Props/Validate Flight Controls")]
  public static void Validate() {
   var scene=EditorSceneManager.NewPreviewScene();
   try {
    var go=new GameObject("Flap validation");SceneManager.MoveGameObjectToScene(go,scene);
    var f=go.AddComponent<StudentFlight>();var m=go.AddComponent<StudentObstacleMovement>();m.obstacles=Array.Empty<BoxCollider>();
    var controller=go.AddComponent<DigiPhant.DigiPhantController>();controller.inputMode=DigiPhant.InputMode.Camera;controller.performerCount=1;
    string packet="{\"version\":1,\"performerCount\":1,\"people\":[{\"slot\":1,\"values\":[0,0,0,0,0,0],\"confidence\":[1,1,1,1,1,1],\"flap\":[0.4,0.4,1,1]}]}";
    if(!controller.AcceptPacket(packet,0)||!controller.TryReadFlap(1,0,out float left,out float right,out bool sideways)||!sideways||Mathf.Abs(left-.4f)>.001f)throw new Exception("Pose packet flap path failed");
    if(controller.TryReadFlap(1,1,out left,out right,out sideways))throw new Exception("Stale flap packet accepted");
    float t=0;Vector3 p=Vector3.zero;
    for(int i=0;i<120;i++){t+=1f/60;f.Observe(t,true,true,Mathf.Sin(t*7)*.7f,Mathf.Sin(t*7)*.7f);p=f.ApplyHeight(p,0,1f/60,m);}
    if(p.y!=0||f.Flapping)throw new Exception("Locked flight activated");
    f.SetUnlocked(true);
    for(int i=0;i<120;i++){t+=1f/60;f.Observe(t,true,true,0,0);p=f.ApplyHeight(p,0,1f/60,m);}
    if(p.y!=0||f.Flapping)throw new Exception("Static T pose activated flight");
    for(int i=0;i<360;i++){t+=1f/60;float h=Mathf.Sin(t*7)*.7f;f.Observe(t,true,true,h,h);var before=p;p=f.ApplyHeight(p,0,1f/60,m);if(p.y-before.y>.0201f||p.y>2.2001f)throw new Exception("Ascent bound failed");}
    if(p.y<2.19f||!f.Flapping||!f.SuppressTravel)throw new Exception("Flap lift failed");
    float stopped=t;
    while(t-stopped<.95f){t+=1f/60;f.Observe(t,false,false,0,0);p=f.ApplyHeight(p,0,1f/60,m);}
    if(p.y<2.19f)throw new Exception("Landed before one-second grace");
    for(int i=0;i<300;i++){t+=1f/60;f.Observe(t,false,false,0,0);var before=p;p=f.ApplyHeight(p,0,1f/60,m);if(before.y-p.y>.0109f)throw new Exception("Abrupt landing");}
    if(p.y>.001f||f.Flapping||f.SuppressTravel)throw new Exception("Tracking loss landing failed");
    for(int i=0;i<120;i++){t+=1f/60;f.Observe(t,true,true,Mathf.Sin(t*7),-Mathf.Sin(t*7));}
    if(f.Flapping)throw new Exception("Asymmetric arms activated flight");
    var roof=new GameObject("Ceiling");SceneManager.MoveGameObjectToScene(roof,scene);roof.transform.position=new Vector3(0,4,0);var box=roof.AddComponent<BoxCollider>();box.size=new Vector3(10,.4f,10);m.obstacles=new[]{box};
    if(m.ResolveVertical(Vector3.zero,Quaternion.identity,2.2f).y>.651f)throw new Exception("Takeoff passed ceiling");
    var current=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LollipopPickup>(true)).Single();
    if(!current.flight||current.largeEars.Length!=2||current.birthdayWings)throw new Exception("Saved ear flight refs wrong");
    Debug.Log("STUDENT_FLIGHT_VALIDATION_OK: locked/static/asymmetric gestures rejected; synchronized sideways flapping lifts to 2.2m; horizontal suppression; one-second grace; slow landing on tracking loss; overhead collision; saved ear refs");
   } finally {EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}
