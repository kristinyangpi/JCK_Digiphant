using UnityEngine;
namespace StudentWork.CandyWonderland {
 // Only new decorative candy/cloud renderers are culled when they conceal the elephant.
 // Camera transforms, player inputs and physical obstacle colliders are never changed.
 [DefaultExecutionOrder(400)] public class CandyCameraClearance : MonoBehaviour {
  public Camera gameplayCamera;public Transform elephant;public Renderer[] decorations;
  void LateUpdate(){Apply();}
  public void Apply(){if(!gameplayCamera||!elephant||decorations==null)return;var origin=gameplayCamera.transform.position;foreach(var r in decorations){if(!r)continue;bool hidden=false;foreach(float h in new[]{1.5f,3.3f}){var delta=elephant.position+Vector3.up*h-origin;var ray=new Ray(origin,delta.normalized);if(r.bounds.IntersectRay(ray,out float distance)&&distance<delta.magnitude-.8f){hidden=true;break;}}r.enabled=!hidden;}}
  public void Restore(){if(decorations!=null)foreach(var r in decorations)if(r)r.enabled=true;}
  void OnDisable(){Restore();}
 }
}
