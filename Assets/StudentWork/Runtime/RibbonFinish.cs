using UnityEngine;
namespace StudentWork {
 [DefaultExecutionOrder(200)] public class RibbonFinish : MonoBehaviour {
  public Transform travelRoot;
  public StudentFlight flight;
  public FinishBallet ballet;
  public MeshFilter leftRibbon,rightRibbon;
  public ParticleSystem confetti;
  public float ribbonHeight=4.45f,halfWidth=4.6f;
  public Vector3 contactOffset=new Vector3(0,2.7f,.7f);
  public bool Finished {get;private set;}
  public int CelebrationCount {get;private set;}
  Vector3 previous;bool sampled;float finishedAt;
  Mesh leftMesh,rightMesh;Mesh originalLeft,originalRight;Vector3[] leftVertices,rightVertices;
  void OnEnable(){sampled=false;Finished=false;CelebrationCount=0;}
  public bool CheckCrossing(Vector3 oldWorld,Vector3 newWorld,bool airborne) {
   if(Finished || !airborne) return false;
   Vector3 a=transform.InverseTransformPoint(oldWorld),b=transform.InverseTransformPoint(newWorld);
   // Side-to-side passage through the gate's plane, or rising through the
   // ribbon from below, both count. Ground travel and an outside-pole pass do not.
   bool pass=false;
   if(a.y<ribbonHeight && b.y>=ribbonHeight && b.y>a.y) {
    var p=Vector3.Lerp(a,b,(ribbonHeight-a.y)/(b.y-a.y));
    pass=Mathf.Abs(p.x)<halfWidth-.45f && Mathf.Abs(p.z)<1.2f;
   }
   if(!pass && a.z*b.z<=0 && Mathf.Abs(b.z-a.z)>.00001f) {
    var p=Vector3.Lerp(a,b,-a.z/(b.z-a.z));
    pass=Mathf.Abs(p.x)<halfWidth-.45f && Mathf.Abs(p.y-ribbonHeight)<.6f;
   }
   if(!pass)return false;
   Finished=true;CelebrationCount++;finishedAt=Time.realtimeSinceStartup;
   if(confetti){confetti.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);confetti.Play(true);}
   if(ballet)ballet.BeginPerformance();
   Debug.Log("RIBBON_FINISH: ribbon broken, confetti celebration");return true;
  }
  void LateUpdate(){
   if(!travelRoot || !flight)return;
   var contact=travelRoot.TransformPoint(contactOffset);
   if(sampled)CheckCrossing(previous,contact,flight.Unlocked && flight.IsAirborne);
   previous=contact;sampled=true;
   if(Finished)AnimateBreak(Time.realtimeSinceStartup-finishedAt);
  }
  void CacheMeshes(){
   if(leftMesh || !leftRibbon || !rightRibbon)return;
   originalLeft=leftRibbon.sharedMesh;originalRight=rightRibbon.sharedMesh;
   leftMesh=Instantiate(originalLeft);rightMesh=Instantiate(originalRight);
   leftMesh.name="Runtime left torn ribbon";rightMesh.name="Runtime right torn ribbon";
   leftVertices=originalLeft.vertices;rightVertices=originalRight.vertices;
   leftRibbon.sharedMesh=leftMesh;rightRibbon.sharedMesh=rightMesh;
  }
  public void AnimateBreak(float elapsed){
   CacheMeshes();if(!leftMesh)return;
   float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/1.1f));
   Deform(leftMesh,leftVertices,1,p,elapsed);Deform(rightMesh,rightVertices,-1,p,elapsed);
  }
  void Deform(Mesh mesh,Vector3[] rest,int side,float blend,float elapsed){
   var verts=new Vector3[rest.Length];
   for(int i=0;i<verts.Length;i++){
    var v=rest[i];float distance=Mathf.Clamp01(Mathf.Abs(v.x)/halfWidth);
    // The outer tied end stays fixed; only the cut middle pulls apart and drops.
    v.x-=side*distance*halfWidth*.42f*blend;
    v.y-=distance*1.8f*blend;
    v.z+=side*distance*(1.2f+Mathf.Sin(elapsed*5-distance*3)*.13f)*blend;
    verts[i]=v;
   }
   mesh.vertices=verts;mesh.RecalculateNormals();mesh.RecalculateBounds();
  }
  public void ResetFinish(){
   if(ballet)ballet.ResetPerformance();
   Finished=false;CelebrationCount=0;sampled=false;
   if(confetti)confetti.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   if(leftMesh){leftMesh.vertices=leftVertices;leftMesh.RecalculateBounds();rightMesh.vertices=rightVertices;rightMesh.RecalculateBounds();}
  }
  void OnGUI(){if(!Finished)return;
   GUILayout.BeginArea(new Rect(Screen.width*.5f,20,350,115),GUI.skin.box);
   var style=new GUIStyle(GUI.skin.label){fontSize=26,alignment=TextAnchor.MiddleCenter};
   GUILayout.Label("YOU FINISHED!",style);
   if(ballet&&ballet.ControlsElephant)GUILayout.Label(ballet.Status);
   if(GUILayout.Button("Reset finish ribbon"))ResetFinish();GUILayout.EndArea();
  }
  void OnDisable(){ResetFinish();}
  void OnDestroy(){
   if(leftRibbon && originalLeft)leftRibbon.sharedMesh=originalLeft;
   if(rightRibbon && originalRight)rightRibbon.sharedMesh=originalRight;
   if(leftMesh)DestroyImmediate(leftMesh);if(rightMesh)DestroyImmediate(rightMesh);
  }
 }
}
