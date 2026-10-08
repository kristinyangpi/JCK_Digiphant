using UnityEngine;
namespace StudentWork {
    public class StudentFlight : MonoBehaviour {
        public float hoverHeight=2.2f;
        public float riseSpeed=1.2f, landingSpeed=.65f;
        public int performer=1;
        public bool Unlocked { get; private set; }
        public float HeightOffset { get; private set; }
        public bool AltitudeBlocked { get; private set; }
        public bool IsAirborne => HeightOffset>.05f;
        public bool Flapping {get;private set;}
        public bool SidewaysPose {get;private set;}
        public bool SuppressTravel => Unlocked && (SidewaysPose || IsAirborne || Flapping);
        float lastFlap=-1000,lastSample=-1000,anchorLeft,anchorRight,lastStroke=-1000;
        int direction;bool sampling;float previousLeft,previousRight;
        public void SetUnlocked(bool unlocked) { Unlocked=unlocked;if(!unlocked) ClearGesture(); }
        public void ResetFlight() { SetUnlocked(false); }
        public void GroundReset() { ClearGesture();HeightOffset=0;AltitudeBlocked=false; }
        void ClearGesture() { sampling=false;direction=0;lastFlap=lastStroke=-1000;Flapping=SidewaysPose=false; }
        public void Observe(float now,bool visible,bool sideways,float left,float right) {
            SidewaysPose=Unlocked && visible && sideways;
            if(!SidewaysPose || now-lastSample>.5f) { sampling=false;direction=0;lastStroke=-1000; }
            if(SidewaysPose) {
                if(!sampling) { anchorLeft=previousLeft=left;anchorRight=previousRight=right;sampling=true; }
                float sampleDt=now-lastSample;
                if(Flapping && sampleDt>0 && sampleDt<.2f && Mathf.Abs(left-previousLeft)>.45f*sampleDt && Mathf.Abs(right-previousRight)>.45f*sampleDt && Mathf.Sign(left-previousLeft)==Mathf.Sign(right-previousRight)) lastFlap=now;
                previousLeft=left;previousRight=right;
                float dl=left-anchorLeft,dr=right-anchorRight;
                if(Mathf.Abs(dl)>=.22f && Mathf.Abs(dr)>=.22f && Mathf.Sign(dl)==Mathf.Sign(dr)) {
                    int next=dl>0?1:-1;
                    if(direction!=0 && next!=direction && now-lastStroke<1f) lastFlap=now;
                    if(next!=direction) { direction=next;lastStroke=now; }
                    anchorLeft=left;anchorRight=right;
                }
                lastSample=now;
            }
            Flapping=Unlocked && now-lastFlap<1f;
        }
        public void PollPose(float now) {
            var controller=GetComponent<DigiPhant.DigiPhantController>();float left=0,right=0;bool sideways=false;
            bool visible=controller && controller.TryReadFlap(performer,now,out left,out right,out sideways);
            Observe(now,visible,sideways,left,right);
        }
        public Vector3 ApplyHeight(Vector3 position,float groundHeight,float dt,StudentObstacleMovement obstacles) {
            dt=Mathf.Clamp(dt,0,.1f);
            float desired=groundHeight+(Unlocked && Flapping ? Mathf.Clamp(hoverHeight,0,2.5f) : 0);
            float next=Mathf.MoveTowards(position.y,desired,Mathf.Clamp(desired>position.y?riseSpeed:landingSpeed,.1f,2)*dt);
            Vector3 target=new Vector3(position.x,next,position.z);AltitudeBlocked=false;
            if(obstacles && obstacles.enabled) {
                var root=GetComponent<DigiPhant.DigiPhantLocomotion>()?.travelRoot;
                target=obstacles.ResolveVertical(position,root ? root.rotation : Quaternion.identity,next);
                AltitudeBlocked=Mathf.Abs(target.y-next)>.0001f;
            }
            HeightOffset=Mathf.Max(0,target.y-groundHeight);return target;
        }
        void OnDisable() { SetUnlocked(false);HeightOffset=0;AltitudeBlocked=false; }
    }
}
