using UnityEngine;
namespace StudentWork {
 public class CourseObstacleProxy : MonoBehaviour {
  public MeshRenderer source;
  public bool SyncBounds() {
   var collider=GetComponent<BoxCollider>();
   if(!source || !source.gameObject.activeInHierarchy) { if(collider) collider.enabled=false; return false; }
   var bounds=source.bounds;
   bool changed=transform.position!=bounds.center || collider.size!=bounds.size || !collider.enabled;
   if(changed) { transform.position=bounds.center;transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;collider.center=Vector3.zero;collider.size=bounds.size;collider.enabled=true; }
   return changed;
  }
 }
}
