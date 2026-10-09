using UnityEngine;
namespace StudentWork {
 [ExecuteAlways] public class GalaxyFloor : MonoBehaviour {
  public MeshRenderer floor;
  MaterialPropertyBlock block;
  public void SetAnimationTime(float time){if(!floor)return;if(block==null)block=new MaterialPropertyBlock();floor.GetPropertyBlock(block);block.SetFloat("_GalaxyTime",time);floor.SetPropertyBlock(block);}
  void Update(){SetAnimationTime(Time.realtimeSinceStartup);}
 }
}
