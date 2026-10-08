using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using DigiPhant;
using UnityEngine;

namespace StudentWork
{
    [DefaultExecutionOrder(100)]
    public class LollipopPickup : MonoBehaviour
    {
        public DigiPhantController controller;
        public DigiPhantLocomotion locomotion;
        public Transform[] trunk;
        public Transform tip;
        public Transform[] candies;
        public Transform mouth;
        public Transform birthdayHat;
        public Transform[] largeEars;
        [SerializeField] Vector3[] earScales;
        public Transform birthdayWings;
        public Transform[] wingPivots;
        public StudentFlight flight;
        [Range(60,180)] public float jointDegreesPerSecond=100;
        [Range(90,300)] public float tipDegreesPerSecond=240;
        [Range(60,120)] public float jointSwingLimit=105;
        public Vector3 gripLocal = new Vector3(0, -1.105f, 0);
        [Range(1,2.5f)] public float maxPickupReachMultiplier=2.5f;
        public string Status { get; private set; } = "Detecting: show the lollipop picture";
        [Serializable] public class Packet { public int version; public string label; public bool detected; public float score = -1; }
        UdpClient socket;
        float seen = -1000, hold = -1, absent = -1, phaseStart;
        float nextAttempt;
        bool detected, armed = true;
        enum Phase { Idle, Reach, Mouth, Eat, Return }
        Phase phase;
        Transform target;
        int growingReward;
        Vector3 startTip;
        Quaternion[] baseline, reached;
        Vector3[] naturalPositions;
        Vector3 naturalTipPosition;
        float naturalLength, extension=1, desiredExtension=1;
        SkinnedMeshRenderer[] skin;
        bool[] originalOffscreen;
        Vector3 hatScale=Vector3.one;
        bool hatCached;
        Vector3 mouthStart;
        float mouthExtension;
        float lastMotionTime, motionDt;
        Vector3 wingsScale=Vector3.one;
        Quaternion[] wingRest;
        bool wingsCached;
        Quaternion previousTipRotation;
        public int ConsumedCount { get; private set; }
        public float LastMouthContactError { get; private set; }=float.PositiveInfinity;
        readonly HashSet<Transform> picked = new HashSet<Transform>();
        class Home { public Transform parent; public Vector3 position; public Quaternion rotation; public Quaternion worldRotation; public Vector3 worldScale; public Vector3 scale; public bool active; }
        readonly Dictionary<Transform, Home> homes = new Dictionary<Transform, Home>();
        public bool IsArmed => armed;
        public int PickedCount => picked.Count;
        public string MotionPhase => phase.ToString();
        public float LastReachError { get; private set; }
        public float ReachLength
        {
            get { CacheNaturalChain(); return naturalLength; }
        }
        public float PickupReachLength => ReachLength*Mathf.Clamp(maxPickupReachMultiplier,1,2.5f);
        void CacheNaturalChain() {
            if(trunk==null || trunk.Length!=7 || !tip || Array.Exists(trunk,t=>!t)) return;
            if(naturalPositions!=null && naturalPositions.Length==trunk.Length && naturalLength>0) return;
            naturalPositions=Array.ConvertAll(trunk,t=>t.localPosition); naturalTipPosition=tip.localPosition;
            naturalLength=0;
            for(int i=1;i<trunk.Length;i++) naturalLength+=Vector3.Distance(trunk[i-1].position,trunk[i].position);
            naturalLength+=Vector3.Distance(trunk[trunk.Length-1].position,tip.position);
        }
        void SetExtension(float factor) {
            CacheNaturalChain(); if(naturalPositions==null) return;
            extension=Mathf.Clamp(factor,1,Mathf.Clamp(maxPickupReachMultiplier,1,2.5f));
            // Move each chain segment once; no transform scales or base displacement.
            for(int i=1;i<trunk.Length;i++) if(trunk[i]) trunk[i].localPosition=naturalPositions[i]*extension;
            if(tip) tip.localPosition=naturalTipPosition*extension;
        }
        void SetReachSkin(bool active) {
            if(active && skin==null) {
                skin=trunk[0].root.GetComponentsInChildren<SkinnedMeshRenderer>();
                originalOffscreen=Array.ConvertAll(skin,r=>r.updateWhenOffscreen);
            }
            if(skin==null) return;
            for(int i=0;i<skin.Length;i++) if(skin[i] && originalOffscreen!=null && i<originalOffscreen.Length) skin[i].updateWhenOffscreen=active || originalOffscreen[i];
        }
        void OnEnable()
        {
            CacheNaturalChain();
            if (!Application.isPlaying) return;
            CacheHat(); if(birthdayHat) birthdayHat.gameObject.SetActive(false);
            try { socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, controller.port + 3)); socket.Client.Blocking = false; }
            catch (Exception e) { Status = "Lollipop receiver: " + e.Message; }
        }
        public bool AcceptPacket(string json, float now)
        {
            try {
                if(string.IsNullOrEmpty(json) || float.IsNaN(now) || float.IsInfinity(now) || now < seen) return false;
                if(!Regex.IsMatch(json,"\"detected\"\\s*:\\s*(true|false)\\s*[,}]") ||
                    !Regex.IsMatch(json,"\"score\"\\s*:\\s*-?\\d+(\\.\\d+)?([eE][+-]?\\d+)?\\s*[,}]")) return false;
                var p = JsonUtility.FromJson<Packet>(json);
                if (p == null || p.version != 1 || p.label != "lollipop" || float.IsNaN(p.score) || float.IsInfinity(p.score) || p.score < 0 || p.score > 1) return false;
                if(now-seen>.35f) hold=-1;
                detected = p.detected && p.score >= .7f;
                if(!detected) hold=-1;
                seen = now; return true;
            } catch (Exception) { return false; }
        }
        void Poll(float now)
        {
            if (socket == null) return;
            try {
                for (int i = 0; i < 32 && socket.Available > 0; i++) {
                    var from = new IPEndPoint(IPAddress.Loopback, 0); var bytes = socket.Receive(ref from);
                    if (IPAddress.IsLoopback(from.Address) && bytes.Length < 1024) AcceptPacket(Encoding.UTF8.GetString(bytes), now);
                }
            } catch (SocketException) { }
        }
        public void StepDetection(float now)
        {
            bool visible = detected && now - seen < .35f;
            if (!visible) {
                hold = -1; if (absent < 0) absent = now;
                if (now - absent >= .8f) armed = true;
                if (phase == Phase.Idle && picked.Count==0 && !Status.StartsWith("Walk closer") && !Status.StartsWith("Candy outside") && !Status.StartsWith("Reach cancelled")) Status = "Detecting: show the lollipop picture";
                return;
            }
            absent = -1;
            if (!armed || phase != Phase.Idle) return;
            if (hold < 0) hold = now;
            if(now-hold<.5f) Status = "Hold picture still...";
            else if(now>=nextAttempt) {
                nextAttempt=now+.25f;
                if(TryPick(now)) { armed=false; hold=-1; }
                // Keep a verified picture pending while the user walks closer.
            }
        }
        public Transform ClosestReachable()
        {
            if (trunk == null || trunk.Length != 7 || !tip || candies == null) return null;
            float length = PickupReachLength; Transform nearest = null; float best = float.MaxValue;
            foreach (var candy in candies) {
                if (!candy || picked.Contains(candy) || !candy.gameObject.activeInHierarchy) continue;
                float distance = Vector3.Distance(trunk[0].position, candy.TransformPoint(gripLocal));
                if (distance <= length * .97f && distance < best) { best = distance; nearest = candy; }
            }
            return nearest;
        }
        [ContextMenu("Test pick nearest reachable lollipop")]
        public void TestPick() { if (Application.isPlaying && phase == Phase.Idle) TryPick(Time.realtimeSinceStartup); }
        public bool TryPick(float now)
        {
            if(phase!=Phase.Idle) return false;
            foreach(var candy in picked) if(candy && candy.gameObject.activeSelf) { Status="Eating one lollipop at a time"; return false; }
            target = ClosestReachable();
            if (!target) {
                float nearest=float.PositiveInfinity;
                if(candies!=null && trunk!=null && trunk.Length>0) foreach(var candy in candies)
                    if(candy && !picked.Contains(candy)) nearest=Mathf.Min(nearest,Vector3.Distance(trunk[0].position,candy.TransformPoint(gripLocal)));
                Status = float.IsInfinity(nearest) ? "No unpicked candies available" : "Walk closer: nearest stick "+nearest.ToString("F1")+"m, pickup reach "+PickupReachLength.ToString("F1")+"m. Keep picture visible.";
                return false;
            }
            growingReward=0; startTip = tip.position; phase = Phase.Reach; phaseStart = now;
            lastMotionTime=now;
            previousTipRotation=tip.rotation;
            desiredExtension=Mathf.Clamp(Vector3.Distance(trunk[0].position,target.TransformPoint(gripLocal))/(ReachLength*.92f),1,Mathf.Clamp(maxPickupReachMultiplier,1,2.5f));
            SetReachSkin(true);
            reached = Array.ConvertAll(trunk,t=>t.localRotation); LastReachError=float.PositiveInfinity;
            Status = "Reaching for a candy stick"; return true;
        }
        public void StepMotion(float now)
        {
            motionDt=Mathf.Clamp(now-lastMotionTime,0,.05f); lastMotionTime=now;
            UpdateEars(now);
            if (phase == Phase.Idle) { ApplyCarry(); return; }
            if (!target || (!target.gameObject.activeInHierarchy && phase!=Phase.Return)) {
                CancelReach(); Status="Pickup cancelled: candy target unavailable"; return;
            }
            baseline = Array.ConvertAll(trunk, t => t.localRotation);
            if (phase == Phase.Reach) {
                Vector3 grip = target.TransformPoint(gripLocal);
                if (Vector3.Distance(trunk[0].position, grip) > PickupReachLength) { CancelReach(); Status = "Reach cancelled: walk closer"; return; }
                float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01((now-phaseStart)/2f));
                SetExtension(Mathf.Lerp(1,desiredExtension,progress));
                Vector3 forward=locomotion && locomotion.travelRoot ? locomotion.travelRoot.forward : Vector3.forward;
                // Curve forward around the face rather than collapsing through the
                // chain base on the way from a hanging trunk to an elevated stick.
                Vector3 aim = Vector3.Lerp(startTip, grip, progress)+forward*(Mathf.Sin(progress*Mathf.PI)*ReachLength*.45f);
                // Controller refreshes its gesture baseline every LateUpdate. Keep
                // the solver's previous bend as a warm start, independently of it.
                Solve(aim,baseline);
                LastReachError=Vector3.Distance(tip.position,grip);
                if (now-phaseStart >= 2f && Vector3.Distance(tip.position,grip) < .12f) {
                    homes[target] = new Home { parent=target.parent,position=target.localPosition,rotation=target.localRotation,worldRotation=target.rotation,worldScale=target.lossyScale,scale=target.localScale,active=target.gameObject.activeSelf };
                    target.SetParent(tip,true);
                    if(target.parent!=tip) { homes.Remove(target); CancelReach(); Status="Candy could not attach to trunk"; return; }
                    target.position += tip.position-target.TransformPoint(gripLocal);
                    picked.Add(target); phase=Phase.Mouth; phaseStart=now; mouthStart=tip.position; mouthExtension=extension; Status="Curling lollipop toward mouth";
                } else if (now-phaseStart > 5f) { CancelReach(); Status="Candy outside bounded trunk pose: walk closer (error "+LastReachError.ToString("F2")+"m)"; }
            } else if(phase==Phase.Mouth) {
                if(!mouth) { CancelReach(); Status="Mouth target missing"; return; }
                float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01((now-phaseStart)/1.8f));
                SetExtension(Mathf.Lerp(mouthExtension,1,progress));
                var savedHome=homes[target];
                Vector3 gripAtMouth=mouth.position+savedHome.worldRotation*Vector3.Scale(gripLocal,savedHome.worldScale);
                Vector3 forward=locomotion && locomotion.travelRoot ? locomotion.travelRoot.forward : Vector3.forward;
                Vector3 aim=Vector3.Lerp(mouthStart,gripAtMouth,progress)+forward*(Mathf.Sin(progress*Mathf.PI)*.4f);
                Solve(aim,baseline);
                ApplyCarry();
                LastReachError=Vector3.Distance(tip.position,gripAtMouth);
                if(now-phaseStart>=1.8f && LastReachError<.15f) { phase=Phase.Eat; phaseStart=now; mouthStart=tip.position; Status="Eating lollipop"; }
                else if(now-phaseStart>6f) { ReleaseFailedCandy(); phase=Phase.Return; phaseStart=now; desiredExtension=extension; Status="Could not reach mouth; candy restored for retry"; }
            } else if(phase==Phase.Eat) {
                SetExtension(1);
                float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01((now-phaseStart)/1.6f));
                if(ConsumedCount==1) GrowEars(progress);
                var home=homes[target];
                float shrink=Mathf.Lerp(1,.05f,progress);
                Vector3 desiredScale=home.worldScale*shrink;
                Vector3 parentScale=tip.lossyScale;
                target.localScale=new Vector3(desiredScale.x/parentScale.x,desiredScale.y/parentScale.y,desiredScale.z/parentScale.z);
                // The candy remains held at the stick throughout the bite. Move
                // the grip toward the mouth as it shrinks; never teleport its head.
                Vector3 eatingGrip=mouth.position+home.worldRotation*Vector3.Scale(gripLocal,desiredScale);
                Solve(eatingGrip,baseline);
                ApplyCarry();
                LastMouthContactError=Mathf.Max(Vector3.Distance(tip.position,mouth.position),Vector3.Distance(target.position,mouth.position));
                if(progress>=1 && LastMouthContactError<.12f) {
                    target.gameObject.SetActive(false); ConsumedCount++;growingReward=ConsumedCount;
                    CacheHat(); if(birthdayHat) { birthdayHat.gameObject.SetActive(true); if(ConsumedCount==1) birthdayHat.localScale=Vector3.zero; }
                    CacheEars();
                    phase=Phase.Return; phaseStart=now; desiredExtension=1; Status=ConsumedCount==1 ? "Birthday hat growing" : ConsumedCount==2 ? "Large ears ready; finishing bite" : "Lollipop eaten; trunk returning";
                } else if(now-phaseStart>5f) { ReleaseFailedCandy();if(ConsumedCount==1) GrowEars(0);phase=Phase.Return;phaseStart=now;desiredExtension=1;Status="Mouth contact failed; candy restored for retry"; }
            } else {
                float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((now-phaseStart)/1.2f));
                SetExtension(Mathf.Lerp(desiredExtension,1,blend));
                var returnPrevious=(Quaternion[])reached.Clone();
                for(int i=0;i<trunk.Length;i++) {
                    trunk[i].localRotation=Quaternion.RotateTowards(reached[i],baseline[i],jointDegreesPerSecond*motionDt);
                    reached[i]=trunk[i].localRotation;
                }
                LimitTipRotation(returnPrevious);
                ApplyCarry();
                if(birthdayHat && growingReward==1) birthdayHat.localScale=hatScale*Mathf.SmoothStep(0,1,blend);
                if(growingReward==2) GrowEars(1);
                bool returned=true; for(int i=0;i<trunk.Length;i++) if(Quaternion.Angle(reached[i],baseline[i])>.1f) returned=false;
                if(blend>=1 && returned) { phase=Phase.Idle; target=null; SetReachSkin(false); if(ConsumedCount>=2 && flight) flight.SetUnlocked(true); Status=ConsumedCount>=2 ? "Large ears ready! Flap both arms sideways to fly." : ConsumedCount>0 ? "Lollipop eaten! Birthday hat ready. Hide/show picture for another." : "Ready to reset pickup"; }
            }
        }
        void Solve(Vector3 aim,Quaternion[] limits) {
            var previous=(Quaternion[])reached.Clone();
            for(int i=0;i<trunk.Length;i++) trunk[i].localRotation=Quaternion.RotateTowards(limits[i],reached[i],jointSwingLimit);
            for(int pass=0;pass<28;pass++) for(int i=trunk.Length-1;i>=0;i--) {
                Vector3 a=tip.position-trunk[i].position,b=aim-trunk[i].position;
                if(a.sqrMagnitude<.000001f || b.sqrMagnitude<.000001f) continue;
                Quaternion correction;
                if(Vector3.Dot(a.normalized,b.normalized)<-.999f) {
                    Vector3 axis=locomotion && locomotion.travelRoot ? locomotion.travelRoot.right : Vector3.right;
                    axis=Vector3.ProjectOnPlane(axis,a.normalized).normalized;
                    if(axis.sqrMagnitude<.01f) axis=Vector3.Cross(a.normalized,Vector3.forward).normalized;
                    correction=Quaternion.AngleAxis(180,axis);
                } else correction=Quaternion.FromToRotation(a,b);
                trunk[i].rotation=correction*trunk[i].rotation;
                trunk[i].localRotation=Quaternion.RotateTowards(limits[i],trunk[i].localRotation,jointSwingLimit);
            }
            var solved=Array.ConvertAll(trunk,t=>t.localRotation);
            for(int i=0;i<trunk.Length;i++) { trunk[i].localRotation=Quaternion.RotateTowards(previous[i],solved[i],jointDegreesPerSecond*motionDt); reached[i]=trunk[i].localRotation; }
            LimitTipRotation(previous);
        }
        void LimitTipRotation(Quaternion[] previous) {
            var smoothed=(Quaternion[])reached.Clone();
            float fraction=1;
            for(int attempt=0;attempt<8 && Quaternion.Angle(previousTipRotation,tip.rotation)>tipDegreesPerSecond*motionDt+.02f;attempt++) {
                fraction*=.5f;
                for(int i=0;i<trunk.Length;i++) { trunk[i].localRotation=Quaternion.Slerp(previous[i],smoothed[i],fraction); reached[i]=trunk[i].localRotation; }
            }
            previousTipRotation=tip.rotation;
        }
        void CacheEars() {
            if(largeEars==null) return;
            // Unity reloads can restore an empty array instead of null. Keep the
            // original sizes serialized and repair partial caches before indexing.
            if(earScales==null || earScales.Length!=largeEars.Length) {
                var previous=earScales;
                earScales=new Vector3[largeEars.Length];
                for(int i=0;i<largeEars.Length;i++) earScales[i]=previous!=null && i<previous.Length && previous[i].sqrMagnitude>.001f ? previous[i] : largeEars[i] ? largeEars[i].localScale : Vector3.one;
            }
        }
        void GrowEars(float blend) { CacheEars();if(earScales==null)return;for(int i=0;i<largeEars.Length;i++) if(largeEars[i]) largeEars[i].localScale=earScales[i]*Mathf.Lerp(1,2.3f,blend); }
        void UpdateEars(float now) { if(ConsumedCount>=2 && phase!=Phase.Return) GrowEars(1); }
        void CacheWings() {
            if(wingsCached || !birthdayWings) return; wingsScale=birthdayWings.localScale.sqrMagnitude>.001f ? birthdayWings.localScale : Vector3.one;
            if(wingPivots!=null) wingRest=Array.ConvertAll(wingPivots,t=>t ? t.localRotation : Quaternion.identity); wingsCached=true;
        }
        void UpdateWings(float now) {
            CacheWings(); if(!birthdayWings || !birthdayWings.gameObject.activeSelf || wingPivots==null || wingRest==null) return;
            for(int i=0;i<Math.Min(wingPivots.Length,wingRest.Length);i++) if(wingPivots[i]) wingPivots[i].localRotation=wingRest[i]*Quaternion.AngleAxis(Mathf.Sin(now*2.4f)*6,Vector3.forward);
        }
        void CacheHat() { if(!hatCached && birthdayHat) { hatScale=birthdayHat.localScale.sqrMagnitude>.001f ? birthdayHat.localScale : Vector3.one; hatCached=true; } }
        void ApplyCarry() {
            // Keep the candy upright as the tip returns to its ordinary hanging pose.
            // Its stick-bottom remains exactly anchored without changing any bone position.
            foreach(var candy in picked) if(candy && candy.gameObject.activeSelf && homes.TryGetValue(candy,out var home)) {
                candy.rotation=home.worldRotation;
                candy.position+=tip.position-candy.TransformPoint(gripLocal);
            }
        }
        void ReleaseFailedCandy() {
            if(!target || !homes.TryGetValue(target,out var home)) return;
            target.SetParent(home.parent,false);target.localPosition=home.position;target.localRotation=home.rotation;target.localScale=home.scale;target.gameObject.SetActive(home.active);
            homes.Remove(target);picked.Remove(target);
        }
        void CancelReach() {
            // Reset may run before any reach, or after editor domain reload has
            // restored an empty/partial private array. Idle reset must also leave
            // the controller's current gesture pose untouched.
            if(phase!=Phase.Idle && baseline!=null && trunk!=null)
                for(int i=0;i<Math.Min(trunk.Length,baseline.Length);i++) if(trunk[i]) trunk[i].localRotation=baseline[i];
            phase=Phase.Idle; target=null;
            baseline=null; reached=null;
            SetExtension(1);
            SetReachSkin(false);
        }
        void LateUpdate() { float now=Time.realtimeSinceStartup; Poll(now); StepDetection(now); StepMotion(now); }
        void OnGUI() {
            GUILayout.BeginArea(new Rect(Mathf.Max(380,Screen.width-360),Screen.height-155,350,145),GUI.skin.box);
            GUILayout.Label("LOLLIPOP PICKUP: "+Status);
            if(flight && flight.Unlocked) GUILayout.Label("FLIGHT (person "+flight.performer+"): "+(flight.Flapping?"Flapping":flight.IsAirborne?"Landing slowly":"Extend arms sideways and flap up/down"));
            if(GUILayout.Button("Test pick nearest reachable candy")) TestPick();
            if(GUILayout.Button("Reset picks")) ResetPicks();
            GUILayout.EndArea();
        }
        void OnDisable() {
            socket?.Close(); socket=null; CancelReach();
            ResetPicks();
        }
        public void ResetPicks() {
            CancelReach();
            foreach(var pair in homes) if(pair.Key) { pair.Key.SetParent(pair.Value.parent,false); pair.Key.localPosition=pair.Value.position; pair.Key.localRotation=pair.Value.rotation; pair.Key.localScale=pair.Value.scale; pair.Key.gameObject.SetActive(pair.Value.active); }
            homes.Clear(); picked.Clear();
            hold=absent=-1; seen=-1000; detected=false; armed=true; Status="Detecting: show the lollipop picture";
            nextAttempt=0;
            GrowEars(0); ConsumedCount=0; CacheHat(); if(birthdayHat) { birthdayHat.localScale=hatScale; birthdayHat.gameObject.SetActive(false); }
            LastMouthContactError=float.PositiveInfinity;
            CacheWings(); if(birthdayWings) { birthdayWings.localScale=wingsScale; birthdayWings.gameObject.SetActive(false); }
            if(flight) flight.ResetFlight();
        }
    }
}
