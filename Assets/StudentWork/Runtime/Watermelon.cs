using DigiPhant;
using UnityEngine;

namespace StudentWork
{
    // Watermelons the elephant can pick up with its trunk: one beside the savannah course's feeding bush and one
    // beside its water basin. Each behaves the same and comes back in its own place after being eaten.
    // It is created automatically in any scene that contains the bush, so no scene needs editing: in the editor
    // by WatermelonPreview (so it can be seen without pressing Play) and at Play start by SpawnOnPlay.
    // Pick up: bring the trunk tip within Pickup Range and curl the trunk. Drop: uncurl the trunk.
    // Reach is judged from where the trunk tip hangs at rest. Curling the trunk swings the tip about 0.4 units
    // along the ground at the grab curl and 0.8 at the eat curl; judged from the moving tip, the grab gesture
    // itself would eat into the pickup range.
    // Eat: lift it by curling the trunk almost fully; the elephant then plays part of its own "eat" animation
    // (lift to the mouth, then chewing), the melon goes into its mouth, and a new melon appears beside the bush later.
    // While that animation plays it poses the whole body except the ears; live control returns when it ends.
    [DefaultExecutionOrder(1000)]  // after DigiPhantController has posed the elephant for the frame
    public class Watermelon : MonoBehaviour
    {
        public const string BushName = "01 Feeding bush";
        [Tooltip("How close, measured along the ground, the trunk tip must be to pick the melon up.")]
        public float pickupRange = 3.5f;
        [Range(0, 1)] public float grabCurl = .4f;
        [Range(0, 1)] public float releaseCurl = .15f;
        [Tooltip("Trunk curl that counts as lifting the melon to the mouth.")]
        [Range(0, 1)] public float eatCurl = .8f;
        [Tooltip("Seconds the melon must stay lifted before the elephant starts eating.")]
        public float eatDelay = .3f;
        [Tooltip("The elephant's own eat animation. Found automatically from its Animator.")]
        public AnimationClip eatClip;
        [Tooltip("Where in the eat animation to start, in seconds. 1.75 to 4.25 is the trunk lifting food to the mouth and lowering again; chewing follows until 7.5.")]
        public float clipStart = 1.75f;
        [Tooltip("How long the animation plays before live control returns: 2.5 s of lifting to the mouth plus 2 s of chewing.")]
        public float eatSeconds = 4.5f;
        [Tooltip("Seconds into the animation at which the melon goes into the mouth.")]
        public float swallowAt = 1.75f;
        [Tooltip("Seconds to blend from live control into the animation, and back out at the end.")]
        public float blend = .3f;
        [Tooltip("Seconds after the last bite before a new melon appears in the same place. 0 = an eaten melon stays eaten until Play is restarted.")]
        public float regrowSeconds = 0;
        [HideInInspector] public Vector3 home, fullScale;
        [HideInInspector] public bool byBasin;
        public bool Held { get; private set; }
        public bool Eating { get; private set; }
        public bool Eaten { get; private set; }
        public int EatenCount { get; private set; }
        float lifted, elapsed, gone;
        ClipOverlay overlay;
        public bool InReach { get; private set; }
        public float Distance { get; private set; }
        BoneControl trunk;
        Transform tip, trunkParent;
        Vector3 restTip, grabbedFrom;
        float groundY, fallSpeed, carried;
        Transform[] legTops, legEnds;
        Transform trunkEnd;
        const float Clearance = .05f;
        public float CollisionRadius => .5f * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));

        void BindCollisionRig(DigiPhantController controller)
        {
            var tops = new System.Collections.Generic.List<Transform>();
            var ends = new System.Collections.Generic.List<Transform>();
            foreach (var control in controller.controls)
                foreach (var bone in control.bones ?? System.Array.Empty<Transform>())
                    if (bone && (bone.name.Contains("Humerus") || bone.name.Contains("Femur")) && !tops.Contains(bone))
                    {
                        var end = bone;
                        foreach (var child in bone.GetComponentsInChildren<Transform>())
                            if (child.position.y < end.position.y) end = child;
                        tops.Add(bone); ends.Add(end);
                    }
            legTops = tops.ToArray(); legEnds = ends.ToArray();
            trunkEnd = tip;
            while (trunkEnd.childCount == 1) trunkEnd = trunkEnd.GetChild(0);
        }

        // Kinematic movement bypasses physics. Resolve the fruit against moving anatomical
        // capsules instead; ears, hats and renderer bounds never affect the footprint.
        public Vector3 SeparateFromElephant(Vector3 point, float radius)
        {
            if (legTops == null || legTops.Length < 4) return point;
            float width = 0;
            for (int i = 0; i < legTops.Length; i++)
                for (int j = i + 1; j < legTops.Length; j++)
                    if (legTops[i].name.Contains("Humerus") == legTops[j].name.Contains("Humerus"))
                        width = Mathf.Max(width, Vector3.Distance(legTops[i].position, legTops[j].position));
            Vector3 front = Vector3.zero, rear = Vector3.zero;
            int nf = 0, nr = 0;
            for (int i = 0; i < legTops.Length; i++)
                if (legTops[i].name.Contains("Humerus")) { front += legTops[i].position; nf++; }
                else { rear += legTops[i].position; nr++; }
            if (nf == 0 || nr == 0) return point;
            front /= nf; rear /= nr;
            Vector3 fallback = Vector3.ProjectOnPlane(ReachPoint - (front + rear) * .5f, Vector3.up).normalized;
            if (fallback.sqrMagnitude < .1f) fallback = Vector3.forward;
            for (int pass = 0; pass < 8; pass++)
            {
                point = OutsideCapsule(point, front, rear, width * .55f + .12f + radius + Clearance, fallback);
                for (int i = 0; i < legTops.Length; i++)
                    point = OutsideCapsule(point, legTops[i].position, legEnds[i].position,
                        Mathf.Max(.16f, width * .19f) + radius + Clearance, fallback);
            }
            return point;
        }

        static Vector3 OutsideCapsule(Vector3 point, Vector3 a, Vector3 b, float radius, Vector3 fallback)
        {
            Vector3 segment = b - a;
            float t = segment.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector3.Dot(point - a, segment) / segment.sqrMagnitude) : 0;
            Vector3 nearest = a + t * segment;
            float dy = point.y - nearest.y;
            float horizontalSquared = radius * radius - dy * dy;
            if (horizontalSquared <= 0) return point;
            Vector3 delta = Vector3.ProjectOnPlane(point - nearest, Vector3.up);
            float minimum = Mathf.Sqrt(horizontalSquared);
            if (delta.sqrMagnitude >= horizontalSquared) return point;
            Vector3 direction = delta.sqrMagnitude > .000001f ? delta.normalized : fallback;
            return point + direction * (minimum - delta.magnitude + .001f);
        }

        public Vector3 CarryPoint(float radius)
        {
            Vector3 end = trunkEnd ? trunkEnd.position : tip.position;
            Vector3 direction = end - (tip.parent ? tip.parent.position : trunkParent.position);
            if (direction.sqrMagnitude < .000001f) direction = Vector3.forward;
            Vector3 point = end + direction.normalized * (radius + Clearance);
            point.y = Mathf.Max(groundY, point.y);
            return SeparateFromElephant(point, radius);
        }

        Vector3 SeparateCarry(Vector3 point, float radius)
        {
            for (int i = 0; i < 8; i++)
                point = SeparateFromElephant(SeparateFromTrunk(point, radius), radius);
            return point;
        }

        Vector3 SeparateFromTrunk(Vector3 point, float radius)
        {
            // The acquisition interpolation must not travel through the trunk itself.
            for (int pass = 0; pass < 4; pass++)
                for (int i = 1; i < trunk.bones.Length; i++)
                    point = OutsideCapsule(point, trunk.bones[i-1].position, trunk.bones[i].position,
                        radius + .1f + Clearance, Vector3.forward);
            return point;
        }

        // Where the trunk tip is when the trunk hangs at rest, whatever the trunk is doing now.
        public Vector3 ReachPoint => trunkParent ? trunkParent.TransformPoint(restTip) : tip.position;

        // Every melon that exists. Unity's ordinary Find calls skip objects marked not-to-be-saved, which is how the
        // editor's preview melons are marked, so those calls would miss them. No scene test either: a leftover that
        // outlived a scene reload no longer belongs to any loaded scene. (The project has no melon prefab to protect.)
        public static Watermelon[] All() => System.Array.FindAll(Resources.FindObjectsOfTypeAll<Watermelon>(), m => m);

        // Removes every melon with the material and texture each built for itself, and any melon-shaped leftover
        // that has lost its script.
        public static void RemoveAll()
        {
            var leftovers = new System.Collections.Generic.List<Object>();
            foreach (var melon in All())
            {
                var skin = melon.GetComponent<Renderer>();
                var material = skin ? skin.sharedMaterial : null;
                leftovers.Add(material ? material.GetTexture("_BaseMap") : null);
                leftovers.Add(material);
                leftovers.Add(melon.gameObject);
            }
            foreach (var thing in Resources.FindObjectsOfTypeAll<GameObject>())
                if (thing && thing.name == "Watermelon" && !thing.transform.parent && !thing.GetComponent<Watermelon>() && thing.GetComponent<MeshRenderer>()) leftovers.Add(thing);
            foreach (var gone in leftovers)
                if (gone) { if (Application.isPlaying) Destroy(gone); else DestroyImmediate(gone); }
        }

        // Each Play starts with exactly the melons listed in Spawn, fresh, whatever was left in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SpawnOnPlay()
        {
            foreach (var melon in All()) melon.gameObject.SetActive(false);   // Destroy only takes effect at the end of the frame
            RemoveAll();
            Spawn(true);
        }

        // Two melons: one beside the feeding bush and one beside the water basin. Returns the one by the bush.
        public static Watermelon Spawn(bool fresh = false)
        {
            var existing = fresh ? null : System.Array.Find(All(), m => m.gameObject.activeInHierarchy && !m.byBasin);
            if (existing) return existing;
            var bush = GameObject.Find(BushName);
            var basin = GameObject.Find("02 Water basin");
            if (!bush || !FindAnyObjectByType<DigiPhantController>()) return null;
            // Positions are in the course's own coordinates (fixed layout 4).
            // Bush: in the gap between the bush and the road, level with the end of the bush so it is not hidden
            // behind it; the road runs along z there, 4 units from the bush in -x, 2.1 wide each side of its centre.
            var first = Create(bush.transform, new Vector3(-1.35f, 0, 1.9f), 80, false);
            // Basin: just outside the bowl (1.25 deep on this side) on the side facing the road.
            if (first && basin) Create(basin.transform, new Vector3(1, 0, 2.1f), 25, true);
            return first;
        }

        static Watermelon Create(Transform landmark, Vector3 offset, float turn, bool besideBasin)
        {
            // A stretched sphere lying on its side, striped from end to end.
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Watermelon";
            var collider = body.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            body.transform.localScale = new Vector3(.9f, 1.25f, .9f);
            body.GetComponent<Renderer>().sharedMaterial = MakeMaterial();
            Transform course = landmark.parent && landmark.parent.parent ? landmark.parent.parent : landmark;
            body.transform.rotation = course.rotation * Quaternion.Euler(0, turn, 90);
            var melon = body.AddComponent<Watermelon>();
            melon.byBasin = besideBasin;
            if (!melon.Bind())
            {
                if (Application.isPlaying) Destroy(body); else DestroyImmediate(body);
                return null;
            }
            Vector3 place = landmark.position + course.TransformVector(offset);
            body.transform.position = melon.home = new Vector3(place.x, melon.groundY, place.z);
            melon.fullScale = body.transform.localScale;
            return melon;
        }

        // Dark and light green stripes running from end to end. The material exists only in memory, and Unity
        // discards such objects when switching between edit and Play mode unless told to keep them; a renderer
        // left without a material draws bright pink.
        static Material MakeMaterial()
        {
            var stripes = new Texture2D(64, 4) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            for (int x = 0; x < 64; x++)
                for (int y = 0; y < 4; y++)
                    stripes.SetPixel(x, y, x % 8 < 3 ? new Color(.04f, .22f, .08f) : new Color(.45f, .8f, .3f));
            stripes.Apply();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.HideAndDontSave };
            material.SetTexture("_BaseMap", stripes);
            material.SetFloat("_Smoothness", .45f);
            return material;
        }

        // Finds the trunk it follows. Done again whenever the link is lost, for example after scripts reload.
        bool Bind()
        {
            var bush = GameObject.Find(BushName);
            var controller = FindAnyObjectByType<DigiPhantController>();
            trunk = controller ? System.Array.Find(controller.controls, c => c.label == "Trunk curl") : null;
            if (!bush || trunk == null || trunk.bones == null || trunk.bones.Length == 0) return false;
            tip = trunk.bones[trunk.bones.Length - 1];
            // Remember the resting tip relative to the head, read with the trunk straightened to its rest pose.
            trunkParent = trunk.bones[0].parent;
            var bent = System.Array.ConvertAll(trunk.bones, b => b.localRotation);
            controller.ResetTrunkToRest(trunk);
            restTip = trunkParent.InverseTransformPoint(tip.position);
            for (int i = 0; i < bent.Length; i++) trunk.bones[i].localRotation = bent[i];
            groundY = bush.transform.position.y + .45f;
            overlay = ClipOverlay.For(controller);
            if (!eatClip) eatClip = controller.GetComponent<P2EatingRole>()?.eatClip;
            if (!eatClip && overlay != null) eatClip = overlay.Find("eat");
            BindCollisionRig(controller);
            return tip;
        }

        // Runs after the controller has posed the trunk for this frame.
        void LateUpdate() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if ((!tip || trunk == null) && !Bind()) return;
            // Rebuild the material if it has been lost, so the melon can never stay pink.
            var skin = GetComponent<Renderer>();
            if (!skin.sharedMaterial || !skin.sharedMaterial.GetTexture("_BaseMap")) skin.sharedMaterial = MakeMaterial();
            float curl = Mathf.Abs(trunk.current);
            if (Eaten)
            {
                // Nothing to see: an eaten melon stays eaten, unless Regrow Seconds asks for a new one.
                if (regrowSeconds <= 0) return;
                gone += dt;
                if (gone < regrowSeconds) return;
                Eaten = false;
                transform.position = home;
                transform.localScale = fullScale;
                GetComponent<Renderer>().enabled = true;
                InReach = false;
                return;
            }
            if (Eating)
            {
                // Once started, eating finishes even if the trunk relaxes.
                elapsed += dt;
                float time = Mathf.Min(elapsed, eatSeconds);
                overlay?.Pose(eatClip, clipStart + time, ClipOverlay.Weight(time, eatSeconds, blend));
                // Keep the full fruit outside the skin. Only the shrinking final bite enters the mouth.
                float biteScale = Mathf.Clamp01((swallowAt - elapsed) / .25f);
                transform.localScale = fullScale * biteScale;
                transform.position = SeparateCarry(CarryPoint(CollisionRadius), CollisionRadius);
                if (elapsed >= swallowAt) GetComponent<Renderer>().enabled = false;
                if (elapsed < eatSeconds) return;
                Eating = Held = false; ClipOverlay.Release(this);
                Eaten = true;
                EatenCount++;
                gone = 0;
                GetComponent<Renderer>().enabled = false;
                return;
            }
            if (Held)
            {
                // Rises from where it lay to the trunk tip over a quarter of a second, then rides on the tip.
                carried += dt;
                Vector3 carriedPoint = Vector3.Lerp(grabbedFrom, CarryPoint(CollisionRadius), Mathf.SmoothStep(0, 1, carried / .25f));
                transform.position = SeparateCarry(carriedPoint, CollisionRadius);
                lifted = curl >= eatCurl ? lifted + dt : 0;
                if (lifted >= eatDelay && ClipOverlay.Owns(this) && eatClip && overlay != null) { Eating = true; DigiPhantAudio.Play("eat-watermelon"); elapsed = 0; lifted = 0; overlay?.Begin(); FindAnyObjectByType<DigiPhantLocomotion>()?.StopMotion(); }
                else if (curl < releaseCurl) { Held = false; ClipOverlay.Release(this); fallSpeed = 0; transform.position = SeparateFromElephant(transform.position, CollisionRadius); }
                return;
            }
            // Not held: fall to the ground where it was let go, then wait to be picked up.
            Vector3 position = transform.position;
            if (position.y > groundY)
            {
                fallSpeed += 9.81f * dt;
                position.y = Mathf.Max(groundY, position.y - fallSpeed * dt);
                transform.position = position;
            }
            position = SeparateFromElephant(position, CollisionRadius);
            transform.position = position;
            Vector3 gap = ReachPoint - position;
            gap.y = 0;
            Distance = gap.magnitude;
            InReach = Distance <= pickupRange;
            // One melon at a time: if two are in reach, the nearer one is taken.
            if (InReach && curl >= grabCurl && this == Focus() && !(FindAnyObjectByType<StudentFlight>()?.SuppressTravel ?? false) && ClipOverlay.TryAcquire(this)) { Held = true; lifted = carried = 0; grabbedFrom = position; }
        }

        // The melon the trunk is dealing with: the one being carried or eaten, otherwise the nearest one not yet eaten.
        public static Watermelon Focus()
        {
            Watermelon best = null;
            foreach (var melon in All())
            {
                if (!melon.gameObject.activeInHierarchy) continue;
                if (melon.Held || melon.Eating) return melon;
                if (!melon.Eaten && (!best || melon.Gap() < best.Gap())) best = melon;
            }
            return best;
        }

        // Distance along the ground from the resting trunk tip to this melon, measured now (not last frame's value).
        float Gap()
        {
            if ((!tip || trunk == null) && !Bind()) return float.MaxValue;
            Vector3 gap = ReachPoint - transform.position;
            gap.y = 0;
            return gap.magnitude;
        }

        void OnDisable() { ClipOverlay.Release(this); Eating = Held = false; }

        void OnGUI()
        {
            // One line for all melons: the one in focus, or this one if every melon has been eaten.
            var focus = Focus();
            if (!Application.isPlaying || !tip || (focus ? focus != this : this != System.Array.Find(All(), m => m.gameObject.activeInHierarchy))) return;
            // Always shown, so it is clear how far the melon is and what the trunk reads.
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            int curl = trunk != null ? Mathf.RoundToInt(Mathf.Abs(trunk.current) * 100) : 0;
            GUI.Box(new Rect(Screen.width * .5f - 100, Screen.height - 70, 500, 44),
                Eaten ? (regrowSeconds > 0 ? "Watermelons eaten! New ones are growing" : "Both watermelons eaten · restart Play for new ones") : Eating ? (elapsed < swallowAt ? "Eating the watermelon..." : "Chewing...") :
                Held ? "Carrying the watermelon · open wider to eat it (trunk " + curl + "%, eats at " + Mathf.RoundToInt(eatCurl * 100) + "%) · close to drop" :
                InReach ? "Watermelon in reach · open your mouth to pick it up (trunk " + curl + "%, grabs at " + Mathf.RoundToInt(grabCurl * 100) + "%)" :
                "Nearest watermelon is " + Distance.ToString("0.0") + " units from the trunk · walk to within " + pickupRange.ToString("0.#") + " to pick it up", style);
        }
    }
}
