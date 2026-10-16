using UnityEngine;
namespace StudentWork {
    [DefaultExecutionOrder(1100)]
    public class StudentFlight : MonoBehaviour {
        public float hoverHeight=2.2f;
        public float riseSpeed=1.2f, landingSpeed=.65f;
        public int performer=3;
        public bool Unlocked { get; private set; }
        public float HeightOffset { get; private set; }
        public bool AltitudeBlocked { get; private set; }
        public bool IsAirborne => HeightOffset>.05f;
        public bool Flapping {get;private set;}
        public bool SidewaysPose {get;private set;}
        public bool SuppressTravel => IsAirborne || (Unlocked && (SidewaysPose || Flapping));
        float lastSideways=-1000;
        public string Status {get;private set;} = "Eat both lollipops to unlock flight";
        float lastFlap=-1000,lastSample=-1000,anchorLeft,anchorRight,lastStroke=-1000;
        int direction;bool sampling;float previousLeft,previousRight;
        public void SetUnlocked(bool unlocked) { Unlocked=unlocked;if(!unlocked) ClearGesture(); }
        public void ResetFlight() { SetUnlocked(false); }
        public void GroundReset() { ClearGesture();HeightOffset=0;AltitudeBlocked=false; }
        void ClearGesture() { sampling=false;direction=0;lastFlap=lastStroke=lastSideways=-1000;Flapping=SidewaysPose=false; }
        public void Observe(float now,bool visible,bool sideways,float left,float right) {
            if(visible && sideways) lastSideways=now;
            // A flap arc can briefly reduce lateral wrist separation. Preserve
            // its stroke through that arc, but clear immediately on tracking loss.
            SidewaysPose=Unlocked && visible && (sideways || now-lastSideways<.25f);
            if(!SidewaysPose || now-lastSample>.5f) { sampling=false;direction=0;lastFlap=lastStroke=-1000; }
            if(SidewaysPose) {
                if(!sampling) { anchorLeft=previousLeft=left;anchorRight=previousRight=right;sampling=true; }
                float sampleDt=now-lastSample;
                if(Flapping && sampleDt>0 && sampleDt<.2f && Mathf.Abs(left-previousLeft)>.45f*sampleDt && Mathf.Abs(right-previousRight)>.45f*sampleDt && Mathf.Sign(left-previousLeft)==Mathf.Sign(right-previousRight)) lastFlap=now;
                previousLeft=left;previousRight=right;
                float dl=left-anchorLeft,dr=right-anchorRight;
                if(Mathf.Abs(dl)>=.15f && Mathf.Abs(dr)>=.15f && Mathf.Sign(dl)==Mathf.Sign(dr)) {
                    int next=dl>0?1:-1;
                    if(direction!=0 && next!=direction && now-lastStroke<1.5f) lastFlap=now;
                    if(next!=direction) { direction=next;lastStroke=now; }
                    anchorLeft=left;anchorRight=right;
                }
                lastSample=now;
            }
            Flapping=SidewaysPose && now-lastFlap<1f;
            Status=!Unlocked ? "Eat both lollipops to unlock flight" : !visible ? "P3: keep shoulders and both wrists visible" :
                !SidewaysPose ? "P3: extend both arms sideways" : Flapping ? "Flapping: flying" : "P3: flap both arms up and down";
        }
        public void PollPose(float now) {
            var controller=GetComponent<DigiPhant.DigiPhantController>();float left=0,right=0;bool sideways=false;
            bool visible=!ClipOverlay.Busy && controller && controller.inputMode==DigiPhant.InputMode.Camera && controller.TryReadFlap(performer,now,out left,out right,out sideways);
            Observe(now,visible,sideways,left,right);
            if(Unlocked && ClipOverlay.Busy) Status="Wait for eating or water action to finish";
        }
        public Vector3 ApplyHeight(Vector3 position,float groundHeight,float dt) {
            dt=Mathf.Clamp(dt,0,.1f);
            float desired=groundHeight+(Unlocked && Flapping ? Mathf.Clamp(hoverHeight,0,2.2f) : 0);
            float next=Mathf.MoveTowards(position.y,desired,Mathf.Clamp(desired>position.y?riseSpeed:landingSpeed,.1f,2)*dt);
            Vector3 target=new Vector3(position.x,next,position.z);AltitudeBlocked=false;

            HeightOffset=Mathf.Max(0,target.y-groundHeight);return target;
        }
        void LateUpdate() => PoseFlightEars(Time.time);
        public void PoseFlightEars(float now) {
            if(GetComponent<FinishBallet>()?.ControlsElephant==true || (!Flapping && !IsAirborne))return;
            var pickup=GetComponent<LollipopPickup>();
            if(!pickup || pickup.largeEars==null)return;
            float angle=Mathf.Sin(now*Mathf.PI*5)*(Flapping?30:15);
            for(int i=0;i<pickup.largeEars.Length;i++)if(pickup.largeEars[i])
                pickup.largeEars[i].localRotation*=Quaternion.AngleAxis((i==0?1:-1)*angle,Vector3.up);
        }
        void OnDisable() { SetUnlocked(false);HeightOffset=0;AltitudeBlocked=false; }
    }
}
