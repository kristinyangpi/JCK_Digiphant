using DigiPhant;
using UnityEngine;

namespace StudentWork
{
    // Tilting your head downwards makes the elephant drink, if the savannah course's water basin is within reach of its head.
    // The elephant's own "drink" animation plays once (trunk into the water, up to the mouth, back to rest), then
    // live control returns. Created when Play starts in any scene that has the basin; nothing is saved in the scene.
    [DefaultExecutionOrder(1000)]  // after DigiPhantController has posed the elephant for the frame
    public class ElephantDrinking : MonoBehaviour
    {
        public const string BasinName = "02 Water basin";
        [Tooltip("How far, along the ground, the elephant's head may be from the middle of the basin and still drink.")]
        public float reach = 3;
        [Tooltip("How far below neutral, in degrees, the head must tilt downwards.")]
        [Range(0, 45)] public float headDownDegrees = 7;
        [Tooltip("Seconds the head must stay down before drinking starts.")]
        public float holdSeconds = .3f;
        public AnimationClip drinkClip;
        public float clipStart;
        [Tooltip("How long the animation plays. Set to the whole clip when it is found.")]
        public float drinkSeconds = 6.67f;
        public float blend = .3f;
        public bool Drinking { get; private set; }
        public bool WaterInReach { get; private set; }
        public bool HeadIsDown { get; private set; }
        public int DrinkCount { get; private set; }
        // True from the end of a drink until that water is sprayed (see ElephantSpraying).
        public bool HoldsWater { get; private set; }
        public void UseWater() => HoldsWater = false;
        public float Distance { get; private set; }
        public float HeadDown { get; private set; }
        DigiPhantController controller;
        ClipOverlay overlay;
        Transform head, basin;
        float held, elapsed;
        P2HeadGate gate = new P2HeadGate();
        public bool GestureReady => gate.Ready;
        public void ConsumeGesture() => gate.Consume();
        public bool ActionAllowed => controller && controller.GetComponent<P2EatingRole>()?.RoleEnabled == true && !(FindAnyObjectByType<StudentFlight>()?.SuppressTravel ?? false);
        void OnDisable() { Drinking=false; held=elapsed=0; gate=new P2HeadGate(); ClipOverlay.Release(this); }

        // Installed as a serialized scene component; no global scene spawning.
        static void SpawnOnPlay() => Spawn();

        public static ElephantDrinking Spawn()
        {
            var existing = FindAnyObjectByType<ElephantDrinking>();
            if (existing) return existing;
            if (!GameObject.Find(BasinName) || !FindAnyObjectByType<DigiPhantController>()) return null;
            // Kept even if something is missing at this moment: Bind is retried, and the on-screen line says what is wrong.
            var drinking = new GameObject("Elephant drinking").AddComponent<ElephantDrinking>();
            drinking.Bind();
            return drinking;
        }

        bool Bind()
        {
            controller = FindAnyObjectByType<DigiPhantController>();
            var water = GameObject.Find(BasinName);
            overlay = ClipOverlay.For(controller);
            if (!water || overlay == null) return false;
            basin = water.transform;
            var locomotion = controller.GetComponent<DigiPhantLocomotion>();
            head = System.Array.Find(locomotion.elephantAnimator.GetComponentsInChildren<Transform>(), t => t.name == "elephant_Head_bone");
            if (!drinkClip)
            {
                drinkClip = overlay.Find("drink");
                if (drinkClip) drinkSeconds = drinkClip.length - clipStart;
            }
            return head && drinkClip;
        }

        void LateUpdate()
        {
            if ((!head || !drinkClip) && !Bind()) return;
            Step(Time.deltaTime, ReadHeadDown());
        }

        // Degrees the trunk's performer has tilted their head downwards from neutral; 0 when the face cannot be read.
        // The performer is looked up every time, because Face Only mode and the group-size buttons reassign the trunk
        // after Play has started. HeadDown is sent in units of 30 degrees.
        public float ReadHeadDown()
        {
            gate ??= new P2HeadGate();
            bool valid = controller && controller.GetComponent<P2EatingRole>()?.RoleEnabled == true && controller.TryReadHeadPitch(2, Time.realtimeSinceStartup, out _);
            float pitch = 0; if(valid) controller.TryReadHeadPitch(2, Time.realtimeSinceStartup, out pitch);
            gate.Read(valid, pitch, Time.deltaTime, controller ? controller.TrackingEpoch : "");
            return gate.Ready ? gate.Delta : 0;
        }

        // headDown: degrees the head is tilted downwards from the performer's neutral, 0 when the face cannot be read.
        public void Step(float dt, float headDown)
        {
            if ((!head || !drinkClip) && !Bind()) return;
            Vector3 gap = head.position - basin.position;
            gap.y = 0;
            Distance = gap.magnitude;
            HeadDown = headDown;
            WaterInReach = Distance <= reach;
            if (Drinking)
            {
                // Once started, the drink finishes even if the head comes back up or the elephant is steered away.
                elapsed += dt;
                float time = Mathf.Min(elapsed, drinkSeconds);
                overlay.Pose(drinkClip, clipStart + time, ClipOverlay.Weight(time, drinkSeconds, blend));
                if (elapsed < drinkSeconds) return;
                Drinking = false; ClipOverlay.Release(this);
                HoldsWater = true;
                DrinkCount++;
                held = 0;
                return;
            }
            HeadIsDown = ActionAllowed && gate.Ready && headDown >= headDownDegrees;
            held = HeadIsDown ? held + dt : 0;
            if (held < holdSeconds || !WaterInReach || ClipOverlay.Busy) return;
            if (!ClipOverlay.TryAcquire(this)) return; Drinking = true; gate.Consume();
            elapsed = 0;
            overlay.Begin();
        }

        void OnGUI()
        {
            if (!Application.isPlaying) return;
            // Always shown on the course, so it is clear how far the water is and what the head angle reads.
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            string text = !gate.Calibrated ? "P2: look straight ahead for 2 seconds to set head neutral" : !head || !drinkClip ? "Drinking is not available: the elephant's drink animation or head was not found"
                : Drinking ? "Drinking..."
                : WaterInReach ? "Water in reach · tilt your head down " + Mathf.RoundToInt(headDownDegrees) + "° to drink (now " + Mathf.RoundToInt(HeadDown) + "°)"
                : "Water is " + Distance.ToString("0.0") + " units from the elephant's head · walk to within " + reach.ToString("0.#") + " to drink";
            if (HoldsWater && !Drinking) text = "Trunk is full of water · tilt your head up to spray it";
            GUI.Box(new Rect(Screen.width * .5f - 100, Screen.height - 122, 500, 44), text, style);
        }
    }
}
