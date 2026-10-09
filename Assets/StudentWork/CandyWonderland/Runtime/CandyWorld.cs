using UnityEngine;
namespace StudentWork.CandyWonderland {
 public class CandyWorld : MonoBehaviour {
  public int placementSeed=4817;
  public Vector3[] route;
  public float pathHalfWidth=4.8f;
  public MeshCollider pathSurface;
  public GameObject[] candyPrefabs;
  public Transform castle;
  public bool elevated;
  public float GroundHeight(Vector3 position){
   float best=float.PositiveInfinity,y=0;
   for(int i=0;i<route.Length-1;i++){var a=route[i];var d=route[i+1]-a;var flat=new Vector2(d.x,d.z);float t=Mathf.Clamp01(Vector2.Dot(new Vector2(position.x-a.x,position.z-a.z),flat)/Mathf.Max(.0001f,flat.sqrMagnitude));var p=Vector3.Lerp(a,route[i+1],t);float dist=new Vector2(p.x-position.x,p.z-position.z).sqrMagnitude;if(dist<best){best=dist;y=p.y;}}
   return y;
  }
 }
}
