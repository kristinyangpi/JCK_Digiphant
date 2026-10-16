using UnityEngine;
namespace StudentWork {
 // Ribbon/confetti celebration inspired by yangpi's RibbonFinish; walking gate, no input ownership.
 public class CourseFinishCelebration : MonoBehaviour {
  public FinishBallet ballet;
  public Transform travelRoot; public Transform[] ribbons; public ParticleSystem confetti;
  public Vector3 startPosition; public float halfWidth=2.7f;
  public bool Finished {get; private set;} public int CelebrationCount {get; private set;}
  Vector3 previous; bool sampled, leftStart; float elapsed;
  void Update(){if(!travelRoot)return;var p=travelRoot.position;
   if(Vector3.Distance(p,startPosition)>6)leftStart=true;
   if(Finished&&leftStart&&Vector3.Distance(p,startPosition)<2){ResetCelebration();return;}
   if(sampled)CheckCrossing(previous,p); previous=p;sampled=true;
   if(Finished){elapsed+=Time.deltaTime;for(int i=0;i<ribbons.Length;i++)if(ribbons[i])ribbons[i].localRotation=Quaternion.Euler(0,0,(i==0?-1:1)*Mathf.Min(elapsed*55,65));}
  }
  public bool CheckCrossing(Vector3 from,Vector3 to){
   if(Finished)return false;var a=transform.InverseTransformPoint(from);var b=transform.InverseTransformPoint(to);
   if(a.z>=0||b.z<0||Mathf.Abs(b.z-a.z)<.0001f)return false;
   var hit=Vector3.Lerp(a,b,-a.z/(b.z-a.z));if(Mathf.Abs(hit.x)>halfWidth||hit.y<-.5f||hit.y>3.5f)return false;
   Finished=true;CelebrationCount++;DigiPhantAudio.Play("finish");elapsed=0;if(confetti)confetti.Play();if(ballet)ballet.BeginPerformance();return true;
  }
  public void ResetCelebration(){if(ballet)ballet.ResetPerformance();Finished=false;sampled=false;leftStart=false;elapsed=0;foreach(var r in ribbons)if(r)r.localRotation=Quaternion.identity;if(confetti)confetti.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
  void OnGUI(){if(!Finished)return;var style=new GUIStyle(GUI.skin.box){fontSize=26,alignment=TextAnchor.MiddleCenter};GUI.Box(new Rect(Screen.width*.3f,25,Screen.width*.4f,65),"YOU FINISHED!",style);}
 }
}
