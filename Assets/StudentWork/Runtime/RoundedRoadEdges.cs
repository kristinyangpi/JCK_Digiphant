using System.Collections.Generic;
using UnityEngine;
namespace StudentWork {
 [ExecuteAlways] public class RoundedRoadEdges : MonoBehaviour {
  public MeshRenderer road;
  public float cornerRadius=.42f;
  Mesh previousMesh;float previousRadius;MaterialPropertyBlock block;
  public int CornerCount {get;private set;}
  void OnEnable(){Rebuild();}
  void Update(){var filter=GetComponent<MeshFilter>();if(filter && (filter.sharedMesh!=previousMesh || cornerRadius!=previousRadius))Rebuild();}
  public void Rebuild(){
   var filter=GetComponent<MeshFilter>();if(!road||!filter||!filter.sharedMesh)return;
   previousMesh=filter.sharedMesh;previousRadius=cornerRadius;var v=previousMesh.vertices;if(v.Length%2!=0)return;
   var polygon=new List<Vector2>();for(int i=0;i<v.Length;i+=2)polygon.Add(new Vector2(v[i].x,v[i].z));for(int i=v.Length-1;i>=1;i-=2)polygon.Add(new Vector2(v[i].x,v[i].z));
   float area=0;for(int i=0;i<polygon.Count;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Count];area+=a.x*b.y-a.y*b.x;}
   var centers=new Vector4[24];var vertices=new Vector4[24];var next=new Vector4[24];int count=0;
   for(int i=0;i<polygon.Count && count<24;i++){
    var p=polygon[i];var e1=(polygon[(i+polygon.Count-1)%polygon.Count]-p).normalized;var e2=(polygon[(i+1)%polygon.Count]-p).normalized;
    if((e1.x*e2.y-e1.y*e2.x)*area>=-.00001f)continue;
    float half=Mathf.Acos(Mathf.Clamp(Vector2.Dot(e1,e2),-.9999f,.9999f))*.5f;
    float r=Mathf.Clamp(cornerRadius,.02f,.6f),t=r/Mathf.Tan(half);var center=p+(e1+e2).normalized*(r/Mathf.Sin(half));
    centers[count]=new Vector4(center.x,center.y,r,t);vertices[count]=new Vector4(p.x,p.y,e1.x,e1.y);next[count]=new Vector4(e2.x,e2.y,0,0);count++;
   }
   CornerCount=count;if(block==null)block=new MaterialPropertyBlock();road.GetPropertyBlock(block);block.SetFloat("_RoundCornerCount",count);block.SetVectorArray("_RoundCenters",centers);block.SetVectorArray("_RoundVertices",vertices);block.SetVectorArray("_RoundNext",next);road.SetPropertyBlock(block);
  }
 }
}
