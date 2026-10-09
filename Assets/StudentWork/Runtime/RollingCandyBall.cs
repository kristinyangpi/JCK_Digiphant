using UnityEngine;
namespace StudentWork {
 [DefaultExecutionOrder(-100)] public class RollingCandyBall : MonoBehaviour {
  public Transform visual;
  public BoxCollider solid;
  public StudentObstacleMovement movement;
  public Transform elephant;
  public RibbonFinish finish;
  public Vector3 start,end;
  public float radius=1.4f,speed=1.6f;
  [Range(0,1)]public float initialProgress;
  public float endpointPause=.7f;
  public bool WaitingForClearance {get;private set;}
  public float DistanceRolled {get;private set;}
  float progress,pause;int direction=1;bool initialized;
  void OnEnable(){initialized=false;DistanceRolled=0;}
  void Start(){Initialize();}
  public void Initialize(){if(initialized)return;progress=Mathf.Clamp01(initialProgress);transform.position=Vector3.Lerp(start,end,progress);initialized=true;}
  void Update(){if(Application.isPlaying)Advance(Time.deltaTime);}
  public void Advance(float dt){
   Initialize();WaitingForClearance=false;if(finish&&finish.Finished)return;
   dt=Mathf.Clamp(dt,0,.1f);if(pause>0){pause-=dt;return;}float length=Vector3.Distance(start,end);if(length<.01f||!visual||!solid)return;
   float travel=Mathf.Max(0,speed)*dt;int steps=Mathf.Max(1,Mathf.CeilToInt(travel/.04f));
   for(int i=0;i<steps;i++){
    float next=Mathf.Clamp01(progress+direction*travel/steps/length);var old=transform.position;var candidate=Vector3.Lerp(start,end,next);
    float before=movement&&elephant?movement.Penetration(elephant.position,elephant.rotation):0;
    transform.position=candidate;Physics.SyncTransforms();
    bool blocked=movement&&elephant&&movement.Penetration(elephant.position,elephant.rotation)>before+.00001f;
    if(!blocked&&movement&&movement.obstacles!=null)foreach(var obstacle in movement.obstacles){
     if(!obstacle||obstacle==solid||!obstacle.enabled||!obstacle.gameObject.activeInHierarchy||obstacle.isTrigger)continue;
     if(solid.bounds.Intersects(obstacle.bounds)){blocked=true;break;}
    }
    if(blocked){transform.position=old;Physics.SyncTransforms();WaitingForClearance=true;break;}
    var delta=candidate-old;float distance=delta.magnitude;
    if(distance>.000001f){var axis=Vector3.Cross(Vector3.up,delta.normalized);visual.rotation=Quaternion.AngleAxis(distance/Mathf.Max(.1f,radius)*Mathf.Rad2Deg,axis)*visual.rotation;DistanceRolled+=distance;}
    progress=next;if(next<=0||next>=1){direction=-direction;pause=endpointPause;break;}
   }
  }
 }
}
