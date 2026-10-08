using UnityEngine;
namespace StudentWork {
    // An oriented body footprint against conservative obstacle bounds. Small
    // translation/rotation steps prevent tunnelling without physics root motion.
    public class StudentObstacleMovement : MonoBehaviour {
        public BoxCollider[] obstacles;
        public Vector3 bodyCenter=new Vector3(0,1.65f,.2f);
        public Vector3 bodyHalfSize=new Vector3(1.5f,1.55f,2.5f);
        public bool Blocked { get; private set; }
        public bool StageEdge { get; private set; }
        public Vector3 ResolveBounded(Vector3 position,ref Quaternion rotation,float turnDegrees,Vector3 localTravel,Vector3 stageStart,float radius) {
            Quaternion initialRotation=rotation;
            Vector3 destination=Resolve(position,ref rotation,turnDegrees,localTravel);
            bool earlierBlocked=Blocked;
            Vector3 offset=destination-stageStart;offset.y=0;
            StageEdge=radius>0 && offset.magnitude>radius;
            if(StageEdge) {
                Vector3 clamped=stageStart+Vector3.ClampMagnitude(offset,radius);clamped.y=position.y;
                Vector3 checkedPoint=Resolve(destination,ref rotation,0,Quaternion.Inverse(rotation)*(clamped-destination));
                if(Vector3.Distance(checkedPoint,clamped)>.0001f) { destination=position;rotation=initialRotation; }
                else destination=clamped;
                Blocked |= earlierBlocked;
            }
            return destination;
        }
        public Vector3 Resolve(Vector3 position, ref Quaternion rotation,float turnDegrees,Vector3 localTravel) {
            Blocked=false;
            bool updated=false;
            if(obstacles!=null) foreach(var c in obstacles) if(c) { var proxy=c.GetComponent<CourseObstacleProxy>(); if(proxy) updated |= proxy.SyncBounds(); }
            if(updated) Physics.SyncTransforms();
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Mathf.Abs(turnDegrees),localTravel.magnitude/.05f)));
            steps=Mathf.Min(steps,1024);
            for(int step=0;step<steps;step++) {
                Quaternion candidate=Quaternion.AngleAxis(turnDegrees/steps,Vector3.up)*rotation;
                if(CanEnter(position,rotation,position,candidate)) rotation=candidate; else Blocked=true;
                Vector3 destination=position+rotation*(localTravel/steps);
                if(CanEnter(position,rotation,destination,rotation)) position=destination; else Blocked=true;
            }
            return position;
        }
        public Vector3 ResolveVertical(Vector3 position,Quaternion rotation,float targetY) {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(targetY-position.y)/.05f));
            steps=Mathf.Min(steps,1024);
            float delta=(targetY-position.y)/steps;
            for(int i=0;i<steps;i++) {
                Vector3 next=position+Vector3.up*delta;
                if(!CanEnter(position,rotation,next,rotation)) break;
                position=next;
            }
            return position;
        }
        public bool CanEnter(Vector3 from,Quaternion oldRotation,Vector3 to,Quaternion newRotation) {
            if(obstacles==null) return true;
            bool improved=false,wasOverlapping=false;
            foreach(var collider in obstacles) {
                float before=Depth(collider,from,oldRotation),after=Depth(collider,to,newRotation);
                if(after>before+.00001f) return false;
                wasOverlapping |= before>.0001f; improved |= after<before-.00001f;
            }
            return !wasOverlapping || improved;
        }
        public float Penetration(Vector3 position,Quaternion rotation) {
            float total=0;
            if(obstacles!=null) foreach(var collider in obstacles) total+=Depth(collider,position,rotation);
            return total;
        }
        float Depth(BoxCollider collider,Vector3 position,Quaternion rotation) {
            if(!collider || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger) return 0;
            Vector3 center=position+rotation*bodyCenter;
            Vector3 right=rotation*Vector3.right,forward=rotation*Vector3.forward;
            Vector3 half=collider.size*.5f;
            Vector3 x=collider.transform.TransformVector(Vector3.right*half.x),y=collider.transform.TransformVector(Vector3.up*half.y),z=collider.transform.TransformVector(Vector3.forward*half.z);
            Vector3 worldHalf=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
            Bounds b=new Bounds(collider.transform.TransformPoint(collider.center),worldHalf*2);
            if(center.y+bodyHalfSize.y<=b.min.y || center.y-bodyHalfSize.y>=b.max.y) return 0;
            Vector3 delta=b.center-center;
            float px=bodyHalfSize.x*Mathf.Abs(right.x)+bodyHalfSize.z*Mathf.Abs(forward.x)+b.extents.x-Mathf.Abs(delta.x);
            float pz=bodyHalfSize.x*Mathf.Abs(right.z)+bodyHalfSize.z*Mathf.Abs(forward.z)+b.extents.z-Mathf.Abs(delta.z);
            float pr=bodyHalfSize.x+b.extents.x*Mathf.Abs(right.x)+b.extents.z*Mathf.Abs(right.z)-Mathf.Abs(Vector3.Dot(delta,right));
            float pf=bodyHalfSize.z+b.extents.x*Mathf.Abs(forward.x)+b.extents.z*Mathf.Abs(forward.z)-Mathf.Abs(Vector3.Dot(delta,forward));
            return px>0 && pz>0 && pr>0 && pf>0 ? Mathf.Min(Mathf.Min(px,pz),Mathf.Min(pr,pf)) : 0;
        }
    }
}
