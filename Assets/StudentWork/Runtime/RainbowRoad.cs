using UnityEngine;
namespace StudentWork {
 [ExecuteAlways] public class RainbowRoad : MonoBehaviour {
  public MeshRenderer road;
  MaterialPropertyBlock block;
  public void SetAnimationTime(float seconds){
   if(!road)return;if(block==null)block=new MaterialPropertyBlock();
   road.GetPropertyBlock(block);block.SetFloat("_FlowTime",seconds);road.SetPropertyBlock(block);
  }
  void Update(){SetAnimationTime(Time.realtimeSinceStartup);}
 }
}
