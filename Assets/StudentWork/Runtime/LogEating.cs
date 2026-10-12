using DigiPhant;
using UnityEngine;

namespace StudentWork
{
    // Opening your mouth with the savannah course's fallen log in front of the elephant (and no watermelon in reach)
    // makes it eat the whole log: it bends down and takes slow bites with its own "eat" animation while its head
    // moves from the log's left end to its right end, and the log disappears from left to right as it goes.
    // Live control returns when the log is gone. The log stays eaten until Play is restarted; nothing is saved in the scene.
    [DefaultExecutionOrder(1000)]  // after DigiPhantController has posed the elephant for the frame
    public class LogEating : MonoBehaviour
    {
        public const string LogName = "03 Fallen log";
        [Tooltip("How far, along the ground, the resting trunk tip may be from the nearest part of the log and still eat it.")]
        public float reach = 2.5f;
        [Tooltip("The log must be within this many degrees of straight ahead.")]
        [Range(10, 180)] public float facingDegrees = 60;
        [Tooltip("When eating starts the elephant shuffles up so its resting trunk tip is this far short of the middle line of the log. Negative = past it; the eat animation puts the trunk down about a unit nearer the feet than where it rests.")]
        public float standOff = -.4f;
        [Tooltip("Seconds the shuffle up to the log takes.")]
        public float shuffleSeconds = 1;
        [Tooltip("How far the trunk must curl (how wide the mouth must open) to start eating, 0 to 1.")]
        [Range(0, 1)] public float startCurl = .4f;
        [Tooltip("Seconds the mouth must stay open before eating starts.")]
        public float holdSeconds = .3f;
        public AnimationClip eatClip;
        [Tooltip("The part of the eat animation used for one bite, in seconds: the trunk reaches down, lifts to the mouth and lowers again.")]
        public float clipFrom = .5f, clipTo = 4.25f;
        [Tooltip("Playback speed of the eat animation. Below 1 is slower.")]
        [Range(.2f, 1.5f)] public float speed = .8f;
        [Tooltip("Bites taken to finish the log.")]
        [Range(1, 6)] public int bites = 2;
        [Tooltip("The furthest the head turns to either side while working along the log, in degrees.")]
        [Range(0, 60)] public float maxTurnDegrees = 40;
        [Tooltip("Extra bend of the head towards the ground while eating, in degrees.")]
        [Range(0, 30)] public float bendDegrees = 10;
        public float blend = .5f;
        [Tooltip("Wood chips thrown up per second from the place being eaten while the trunk is down on the log; a quarter as many while it is up at the mouth.")]
        public float chipsPerSecond = 70;
        public ParticleSystem Chips { get; private set; }
        public int ChipsReleased { get; private set; }
        public bool Eating { get; private set; }
        public bool Eaten { get; private set; }
        public bool InReach { get; private set; }
        public bool Facing { get; private set; }
        public float Distance { get; private set; }
        // Fraction of the log eaten so far, and the head's turn in degrees (negative = to the elephant's left).
        public float Progress { get; private set; }
        public float Turn { get; private set; }
        public float BiteSeconds => (clipTo - clipFrom) / Mathf.Max(.05f, speed);
        public float TotalSeconds => bites * BiteSeconds;
        DigiPhantController controller;
        ClipOverlay overlay;
        BoneControl trunk;
        Transform head, neck, body, log, travel, trunkParent;
        Vector3 leftEnd, rightEnd, fullScale, turnFrom, restTip, shuffle;
        float held, elapsed, shuffled, chipsOwed;
        bool savedLog; Vector3 savedPosition, savedScale; Renderer[] savedRenderers; bool[] savedEnabled;
        void OnDisable() { ClipOverlay.Release(this); Eating=Eaten=false; Progress=Turn=held=elapsed=chipsOwed=0; if(savedLog && log) { log.position=savedPosition; log.localScale=savedScale; for(int i=0;i<savedRenderers.Length;i++) if(savedRenderers[i]) savedRenderers[i].enabled=savedEnabled[i]; } }

        // Where the trunk tip is when the trunk hangs at rest, whatever the trunk is doing now.
        public Vector3 ReachPoint => trunkParent.TransformPoint(restTip);

        // Installed as a serialized scene component; no global scene spawning.
        static void SpawnOnPlay() => Spawn();

        public static LogEating Spawn()
        {
            var existing = FindAnyObjectByType<LogEating>();
            if (existing) return existing;
            if (!GameObject.Find(LogName) || !FindAnyObjectByType<DigiPhantController>()) return null;
            var eating = new GameObject("Log eating").AddComponent<LogEating>();
            eating.Bind();
            return eating;
        }

        bool Bind()
        {
            controller = FindAnyObjectByType<DigiPhantController>();
            var found = GameObject.Find(LogName);
            overlay = ClipOverlay.For(controller);
            if (!found || overlay == null) return false;
            log = found.transform; if(!savedLog) { savedLog=true; savedPosition=log.position; savedScale=log.localScale; savedRenderers=log.GetComponentsInChildren<Renderer>(); savedEnabled=System.Array.ConvertAll(savedRenderers, x=>x.enabled); }
            trunk = System.Array.Find(controller.controls, c => c.label == "Trunk curl");
            var locomotion = controller.GetComponent<DigiPhantLocomotion>();
            body = locomotion.elephantAnimator.transform;
            travel = locomotion.travelRoot ? locomotion.travelRoot : body;
            var skeleton = body.GetComponentsInChildren<Transform>();
            head = System.Array.Find(skeleton, t => t.name == "elephant_Head_bone");
            neck = System.Array.Find(skeleton, t => t.name == "elephant_Neck_bone");
            if (!eatClip) eatClip = overlay.Find("eat");
            if (!Chips) Chips = MakeChips();
            if (!head || !eatClip || trunk == null || trunk.bones == null || trunk.bones.Length == 0) return false;
            // Remember the resting tip relative to the head, read with the trunk straightened to its rest pose.
            trunkParent = trunk.bones[0].parent;
            var bent = System.Array.ConvertAll(trunk.bones, b => b.localRotation);
            controller.ResetTrunkToRest(trunk);
            restTip = trunkParent.InverseTransformPoint(trunk.bones[trunk.bones.Length - 1].position);
            for (int i = 0; i < bent.Length; i++) trunk.bones[i].localRotation = bent[i];
            return true;
        }

        // Small brown and tan squares that are thrown up from the log and fall. Released by hand from Step.
        ParticleSystem MakeChips()
        {
            var holder = new GameObject("Wood chips");
            holder.transform.SetParent(transform, false);
            var chips = holder.AddComponent<ParticleSystem>();
            chips.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = chips.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.6f;
            main.maxParticles = 600;
            // As with the water spray: Unity would pause an empty system it considers off screen.
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = chips.emission;
            emission.enabled = false;
            var shape = chips.shape;
            shape.enabled = false;
            var spin = chips.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-12, 12);
            var shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            holder.GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return chips;
        }

        // Throws chips up and outwards from the cut face of the log at the place being eaten.
        void ThrowChips(Vector3 front, float dt)
        {
            if (!Chips) return;
            if (Application.isPlaying && !Chips.isPlaying) Chips.Play();
            float tipHeight = trunk.bones[trunk.bones.Length - 1].position.y - travel.position.y;
            chipsOwed += chipsPerSecond * (tipHeight < 1.2f ? 1 : .25f) * dt;
            Vector3 towardsLeft = Flat(leftEnd - rightEnd).normalized;
            for (; chipsOwed >= 1; chipsOwed--)
            {
                Vector3 sideways = Quaternion.AngleAxis(Random.Range(-70f, 70f), Vector3.up) * towardsLeft;
                Chips.Emit(new ParticleSystem.EmitParams
                {
                    position = front + Vector3.up * Random.Range(.3f, .8f) + Random.insideUnitSphere * .25f,
                    velocity = sideways * Random.Range(.5f, 3.5f) + Vector3.up * Random.Range(3f, 7f),
                    startSize = Random.Range(.06f, .2f),
                    startLifetime = Random.Range(.8f, 1.4f),
                    rotation = Random.Range(0f, 360f),
                    startColor = Color.Lerp(new Color(.33f, .2f, .1f), new Color(.85f, .68f, .42f), Random.value),
                }, 1);
                ChipsReleased++;
            }
        }

        void LateUpdate()
        {
            if ((!trunkParent || !eatClip) && !Bind()) return;
            float mouth = 0; bool valid = controller.GetComponent<P2EatingRole>()?.RoleEnabled == true && controller.TryReadMouthOpen(2, Time.realtimeSinceStartup, out mouth);
            Step(Time.deltaTime, valid ? Mathf.Clamp01(mouth) : 0);
        }

        // The two ends of the log on the ground, named from where the elephant stands.
        void FindEnds()
        {
            var bounds = new Bounds();
            bool any = false;
            // Measured in the log's own space, where the timber lies along x.
            foreach (var part in log.GetComponentsInChildren<MeshFilter>())
            {
                if (!part.sharedMesh) continue;
                Vector3 centre = part.sharedMesh.bounds.center, half = part.sharedMesh.bounds.extents;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = centre + Vector3.Scale(half, new Vector3((corner & 1) * 2 - 1, (corner >> 1 & 1) * 2 - 1, (corner >> 2 & 1) * 2 - 1));
                    point = log.InverseTransformPoint(part.transform.TransformPoint(point));
                    if (any) bounds.Encapsulate(point); else { bounds = new Bounds(point, Vector3.zero); any = true; }
                }
            }
            Vector3 a = log.TransformPoint(new Vector3(bounds.min.x, 0, 0)), b = log.TransformPoint(new Vector3(bounds.max.x, 0, 0));
            bool aIsLeft = Vector3.Dot(a - b, body.right) < 0;
            leftEnd = aIsLeft ? a : b;
            rightEnd = aIsLeft ? b : a;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        // curl: how far the trunk is curled by the open mouth, 0 to 1.
        public void Step(float dt, float curl)
        {
            if ((!trunkParent || !eatClip) && !Bind()) return;
            if (Eaten) return;
            if (Eating)
            {
                // Once started, the elephant finishes the log even if the mouth closes.
                elapsed += dt;
                float total = TotalSeconds, time = Mathf.Min(elapsed, total), bite = BiteSeconds;
                // First it shuffles up to the log, so the trunk comes down on the wood and not on the ground in front of it.
                float moved = Mathf.SmoothStep(0, 1, time / Mathf.Max(.01f, shuffleSeconds));
                // Navigation root deliberately stays fixed during the bite.
                shuffled = moved;
                int index = Mathf.Min(bites - 1, (int)(time / bite));
                float into = time - index * bite, join = Mathf.Min(.4f, bite * .25f);
                float clipTime = clipFrom + into * speed;
                // Each bite ends by easing into the start of the next one, so the trunk does not jump.
                float cross = index < bites - 1 ? Mathf.SmoothStep(0, 1, (into - (bite - join)) / join) : 0;
                float weight = ClipOverlay.Weight(time, total, blend);
                overlay.Pose(eatClip, clipTime, weight);

                // The log goes from its left end to its right end, and the head follows the place being eaten.
                Progress = Mathf.Clamp01(time / total);
                Vector3 front = Vector3.Lerp(leftEnd, rightEnd, Progress);
                // The turn runs evenly from the left end's direction to the right end's, each limited to the furthest turn.
                Vector3 from = body.TransformPoint(turnFrom);
                float Towards(Vector3 end) => Mathf.Clamp(Vector3.SignedAngle(Flat(body.forward), Flat(end - from), Vector3.up), -maxTurnDegrees, maxTurnDegrees);
                Turn = Mathf.Lerp(Towards(leftEnd), Towards(rightEnd), Progress) * weight;
                // Shared between the neck and the head, turned about the upright axis and bent about the elephant's side-to-side axis.
                Quaternion part = Quaternion.AngleAxis(Turn * .5f, Vector3.up) * Quaternion.AngleAxis(bendDegrees * .5f * weight, body.right);
                if (neck) neck.rotation = part * neck.rotation;
                head.rotation = part * head.rotation;

                float left = 1 - Progress;
                Vector3 centre = Vector3.Lerp(rightEnd, front, .5f);
                log.position = new Vector3(centre.x, log.position.y, centre.z);
                log.localScale = new Vector3(fullScale.x * Mathf.Max(left, .0001f), fullScale.y, fullScale.z);
                if (elapsed < total - blend) ThrowChips(front, dt);
                if (elapsed < total) return;
                Eating = false; ClipOverlay.Release(this);
                Eaten = true;
                Turn = 0;
                foreach (var shown in log.GetComponentsInChildren<Renderer>()) shown.enabled = false;
                return;
            }
            FindEnds();
            // Distance along the ground from the resting trunk tip to the nearest part of the log's middle line.
            Vector3 along = Flat(rightEnd - leftEnd), toTip = Flat(ReachPoint - leftEnd);
            Vector3 nearest = leftEnd + along * Mathf.Clamp01(Vector3.Dot(toTip, along) / Mathf.Max(.0001f, along.sqrMagnitude));
            Vector3 gap = Flat(nearest - ReachPoint);
            Distance = gap.magnitude;
            Facing = Distance < .5f || Vector3.Angle(Flat(body.forward), gap) <= facingDegrees;
            InReach = Distance <= reach && Facing;
            // A watermelon comes first: the log is eaten only when no melon is being handled or could be picked up.
            bool melon = false;
            foreach (var m in Watermelon.All())
                if (m.gameObject.activeInHierarchy && !m.Eaten && (m.Held || m.Eating || m.InReach)) melon = true;
            held = !(FindAnyObjectByType<StudentFlight>()?.SuppressTravel ?? false) && curl >= startCurl && InReach && !melon ? held + dt : 0;
            if (held < holdSeconds || ClipOverlay.Busy) return;
            if (!ClipOverlay.TryAcquire(this)) return; Eating = true;
            elapsed = held = 0;
            fullScale = log.localScale;
            shuffle = gap.normalized * (Distance - standOff);
            shuffled = chipsOwed = 0;
            // The turn is measured from where the head was when eating began, so the bites themselves do not sway it.
            turnFrom = body.InverseTransformPoint(head.position);
            overlay.Begin();
        }

        void OnGUI()
        {
            // Shown only near the log, so it does not crowd the screen elsewhere on the course.
            if (!Application.isPlaying || !trunkParent || Eaten || (!Eating && Distance > reach * 3)) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            string text = Eating ? "Eating the log... " + Mathf.RoundToInt(Progress * 100) + "%"
                : InReach ? "Log in reach · open your mouth to eat it (trunk " + Mathf.RoundToInt(Mathf.Abs(trunk.current) * 100) + "%, starts at " + Mathf.RoundToInt(startCurl * 100) + "%)"
                : Distance <= reach ? "Log is beside the elephant · turn to face it to eat it"
                : "Log is " + Distance.ToString("0.0") + " units from the trunk · walk to within " + reach.ToString("0.#") + " to eat it";
            GUI.Box(new Rect(Screen.width * .5f - 100, Screen.height - 226, 500, 44), text, style);
        }
    }
}
