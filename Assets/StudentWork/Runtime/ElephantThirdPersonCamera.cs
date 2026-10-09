using UnityEngine;
using DigiPhant;
namespace StudentWork {
 [DefaultExecutionOrder(350)] public class ElephantThirdPersonCamera : MonoBehaviour {
  public DigiPhantLocomotion locomotion;
  public float distance=12,height=6,lookHeight=2.4f,smoothing=6;
  Vector3 position;bool initialized;
  public bool OwnsCamera=>isActiveAndEnabled&&locomotion&&locomotion.followElephant&&locomotion.followCamera&&!(GetComponent<FinishBallet>()?.ControlsElephant??false);
  void LateUpdate(){if(OwnsCamera)Follow(Time.deltaTime);else initialized=false;}
  public void Snap(){initialized=false;Follow(0);}
  public void Follow(float dt){
   if(!OwnsCamera||!locomotion.travelRoot)return;
   var root=locomotion.travelRoot;var forward=Vector3.ProjectOnPlane(root.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
   var goal=root.position-forward*Mathf.Max(4,distance)+Vector3.up*Mathf.Max(2,height);
   float blend=1-Mathf.Exp(-Mathf.Max(1,smoothing)*Mathf.Max(0,dt));
   if(!initialized||Vector3.Distance(position,goal)>distance*1.5f){position=goal;initialized=true;}else position=Vector3.Lerp(position,goal,blend);
   // Keep the camera in the rear hemisphere even during an abrupt reversal.
   var offset=position-root.position;float along=Vector3.Dot(offset,forward);float rear=-Mathf.Max(3,distance*.35f);if(along>rear)position+=forward*(rear-along);
   var camera=locomotion.followCamera.transform;camera.position=position;camera.rotation=Quaternion.LookRotation(root.position+Vector3.up*lookHeight-position,Vector3.up);
  }
  void OnDisable(){initialized=false;}
 }
}
