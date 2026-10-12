using DigiPhant;
using UnityEngine;

namespace StudentWork
{
    // Tilting your head upwards makes the elephant raise its trunk (its own "trumpet" animation, played once).
    // If it has drunk since it last sprayed, water sprays from the trunk tip while the trunk is up, and that
    // water is used up. If it has not drunk, the trunk lifts and nothing comes out.
    // Flowers grow where the sprayed water lands (see FlowerPatch).
    // Created when Play starts in any scene where the elephant can drink; nothing is saved in the scene.
    [DefaultExecutionOrder(1001)]  // after DigiPhantController has posed the elephant for the frame
    public class ElephantSpraying : MonoBehaviour
    {
        [Tooltip("How far above neutral, in degrees, the head must tilt upwards.")]
        [Range(0, 45)] public float headUpDegrees = 10;
        [Tooltip("Seconds the head must stay up before the trunk lifts.")]
        public float holdSeconds = .3f;
        public AnimationClip liftClip;
        [Tooltip("How long the animation plays. Set to the whole clip when it is found.")]
        public float liftSeconds = 3.67f;
        [Tooltip("Water comes out between these times, while the trunk is raised.")]
        public float sprayFrom = 1.25f, sprayTo = 2.75f;
        public float blend = .3f;
        public bool Lifting { get; private set; }
        public bool WithWater { get; private set; }
        public bool SprayingWater { get; private set; }
        public int SprayCount { get; private set; }
        public int DryLiftCount { get; private set; }
        [Tooltip("Droplets released per second while spraying.")]
        public float dropletsPerSecond = 420;
        public ParticleSystem Water { get; private set; }
        public int DropletsReleased { get; private set; }
        [Tooltip("Flowers sown per second of spraying; each grows where its water lands.")]
        public float flowersPerSecond = 8;
        public FlowerPatch Flowers { get; private set; }
        // The droplets' flight, shared with the flowers so they come up where the water is seen to land.
        const float MinSpeed = 7, MaxSpeed = 10, ConeDegrees = 10, GravityScale = 1.3f;
        float owed, flowersOwed, groundY;
        Transform basin;
        ElephantDrinking drinking;
        ClipOverlay overlay;
        Transform tip, beforeTip;
        float held, elapsed;

        // Installed as a serialized scene component; no global scene spawning.
        static void SpawnOnPlay() => Spawn();

        public static ElephantSpraying Spawn()
        {
            var existing = FindAnyObjectByType<ElephantSpraying>();
            if (existing) return existing;
            var drinking = ElephantDrinking.Spawn();
            if (!drinking) return null;
            var spraying = new GameObject("Elephant spraying").AddComponent<ElephantSpraying>();
            spraying.drinking = drinking;
            spraying.Bind();
            return spraying;
        }

        bool Bind()
        {
            var controller = FindAnyObjectByType<DigiPhantController>();
            if (!drinking) drinking = FindAnyObjectByType<ElephantDrinking>();
            overlay = ClipOverlay.For(controller);
            var trunk = controller ? System.Array.Find(controller.controls, c => c.label == "Trunk curl") : null;
            if (!drinking || overlay == null || trunk == null || trunk.bones == null || trunk.bones.Length < 2) return false;
            tip = trunk.bones[trunk.bones.Length - 1];
            beforeTip = trunk.bones[trunk.bones.Length - 2];
            if (!liftClip)
            {
                liftClip = overlay.Find("trumpet");
                if (liftClip) liftSeconds = liftClip.length;
            }
            if (!Water) Water = MakeWater();
            if (!Flowers)
            {
                Flowers = new GameObject("Flowers").AddComponent<FlowerPatch>();
                Flowers.transform.SetParent(transform, false);
            }
            var locomotion = controller.GetComponent<DigiPhantLocomotion>();
            groundY = locomotion && locomotion.travelRoot ? locomotion.travelRoot.position.y : 0;
            var bowl = GameObject.Find(ElephantDrinking.BasinName);
            basin = bowl ? bowl.transform : null;
            return tip && liftClip;
        }

        // A narrow jet of pale blue droplets that arc and fall. Emission is switched on only while spraying.
        ParticleSystem MakeWater()
        {
            var jet = new GameObject("Water spray");
            jet.transform.SetParent(transform, false);
            var water = jet.AddComponent<ParticleSystem>();
            water.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = water.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(MinSpeed, MaxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(.1f, .26f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.55f, .8f, 1, .95f), new Color(.85f, .95f, 1, .9f));
            main.gravityModifier = GravityScale;
            main.maxParticles = 2000;
            // Unity pauses a looping system it considers off screen, and an empty jet counts as off screen, so it
            // would never start. Keep it simulating; droplets are also released by hand each frame (see SetSpray).
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = water.emission;
            emission.enabled = false;   // droplets are released from SetSpray, not by the system's own timer
            var shape = water.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = ConeDegrees;
            shape.radius = .08f;
            // A soft round dot, so droplets are not squares.
            var dot = new Texture2D(32, 32, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    dot.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1.15f - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 14)));
            dot.Apply();
            var shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            jet.GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(shader) { mainTexture = dot, hideFlags = HideFlags.HideAndDontSave };
            return water;
        }

        void OnDisable() { Lifting=SprayingWater=WithWater=false; held=elapsed=owed=flowersOwed=0; ClipOverlay.Release(this); if(Water) Water.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }

        void LateUpdate()
        {
            if ((!tip || !liftClip) && !Bind()) return;
            Step(Time.deltaTime, -drinking.HeadDown);
        }

        // headUp: degrees the head is tilted upwards from the performer's neutral, 0 when the face cannot be read.
        public void Step(float dt, float headUp)
        {
            if ((!tip || !liftClip) && !Bind()) return;
            if (Lifting)
            {
                // Once started, the lift finishes even if the head comes back down.
                elapsed += dt;
                float time = Mathf.Min(elapsed, liftSeconds);
                overlay.Pose(liftClip, time, ClipOverlay.Weight(time, liftSeconds, blend));
                SetSpray(WithWater && time >= sprayFrom && time < sprayTo, dt);
                if (elapsed < liftSeconds) return;
                Lifting = false; ClipOverlay.Release(this);
                if (WithWater) SprayCount++; else DryLiftCount++;
                held = 0;
                return;
            }
            SetSpray(false, dt);
            held = drinking.ActionAllowed && drinking.GestureReady && headUp >= headUpDegrees ? held + dt : 0;
            if (held < holdSeconds || ClipOverlay.Busy) return;
            if (!ClipOverlay.TryAcquire(this)) return; Lifting = true; drinking.ConsumeGesture();
            elapsed = 0;
            // The water drunk is used by this one spray.
            WithWater = drinking.HoldsWater;
            if (WithWater) drinking.UseWater();
            overlay.Begin();
        }

        // The jet leaves the trunk tip in the direction the end of the trunk points.
        void SetSpray(bool on, float dt)
        {
            if (Water)
            {
                Vector3 along = tip.position - beforeTip.position;
                Water.transform.SetPositionAndRotation(tip.position, Quaternion.LookRotation(along.sqrMagnitude > .0001f ? along : Vector3.up));
                // The system runs all the time so droplets already in the air keep falling after the spray stops.
                if (Application.isPlaying && !Water.isPlaying) Water.Play();
                if (on)
                {
                    owed += dropletsPerSecond * dt;
                    int count = Mathf.FloorToInt(owed);
                    owed -= count;
                    if (count > 0) { Water.Emit(count); DropletsReleased += count; }
                    // A few of those jets are followed to the ground, where each one grows a flower.
                    flowersOwed += flowersPerSecond * dt;
                    for (; flowersOwed >= 1; flowersOwed--)
                    {
                        Vector3 forward = Water.transform.forward;
                        Vector3 aim = Quaternion.AngleAxis(Random.Range(0f, 360f), forward) * (Quaternion.AngleAxis(Random.Range(0f, ConeDegrees), Water.transform.right) * forward);
                        Flowers.Sow(tip.position, aim * Random.Range(MinSpeed, MaxSpeed), -Physics.gravity.y * GravityScale, groundY,
                            basin ? basin.position : (Vector3?)null, 1.8f);
                    }
                }
                else owed = flowersOwed = 0;
            }
            SprayingWater = on;
        }

        void OnGUI()
        {
            if (!Application.isPlaying || !Lifting) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            GUI.Box(new Rect(Screen.width * .5f - 100, Screen.height - 174, 500, 44),
                WithWater ? "Spraying water!" : "Trunk up, but no water · drink at the water basin first", style);
        }
    }
}
