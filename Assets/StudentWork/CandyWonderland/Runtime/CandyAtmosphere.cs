using UnityEngine;
namespace StudentWork.CandyWonderland {
 // Decorations alone move; no physics or player/camera ownership.
 public class CandyAtmosphere : MonoBehaviour {
  public Transform[] balloons, clouds;
  Vector3[] balloonRest,cloudRest;
  void Start(){balloonRest=Capture(balloons);cloudRest=Capture(clouds);}
  Vector3[] Capture(Transform[] items){var p=new Vector3[items.Length];for(int i=0;i<p.Length;i++)if(items[i])p[i]=items[i].localPosition;return p;}
  void Update(){float t=Time.time;for(int i=0;i<balloons.Length;i++)if(balloons[i])balloons[i].localPosition=balloonRest[i]+new Vector3(Mathf.Sin(t*.035f+i)*2,Mathf.Sin(t*.22f+i)*.65f,0);for(int i=0;i<clouds.Length;i++)if(clouds[i])clouds[i].localPosition=cloudRest[i]+new Vector3(Mathf.Sin(t*.025f+i)*1.4f,Mathf.Sin(t*.09f+i)*.12f,0);}
 }
}
