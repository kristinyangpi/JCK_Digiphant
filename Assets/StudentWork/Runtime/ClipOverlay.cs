using System.Collections.Generic;
using DigiPhant;
using UnityEngine;

namespace StudentWork
{
    // Plays part of one of the elephant's own animation clips over its live pose, then gives the pose back.
    // While a clip plays it poses the whole body except the ears; the elephant itself stays where it is.
    // Call Pose after DigiPhantController has posed the elephant for the frame (script order 1000).
    public class ClipOverlay
    {
        // Only one overlaid action (eating, drinking) at a time.
        public static bool Busy;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearBusy() => Busy = false;

        Transform rig;
        Transform[] bones;
        bool[] keepLive, posedEveryFrame;
        DigiPhantLocomotion locomotion;
        AnimationClip[] clips;
        Vector3[] startPosition, startScale;
        Quaternion[] startRotation;

        public static ClipOverlay For(DigiPhantController controller)
        {
            var locomotion = controller ? controller.GetComponent<DigiPhantLocomotion>() : null;
            var animator = locomotion ? locomotion.elephantAnimator : null;
            if (!animator) return null;
            var overlay = new ClipOverlay { rig = animator.transform, locomotion = locomotion };
            // The walking system removes the Animator's controller while it runs, so list the clips now.
            overlay.clips = animator.runtimeAnimatorController ? animator.runtimeAnimatorController.animationClips : new AnimationClip[0];
            overlay.bones = overlay.rig.GetComponentsInChildren<Transform>();
            var ears = new HashSet<Transform>();
            var driven = new HashSet<Transform>();
            foreach (var c in controller.controls)
                if (c.bones != null)
                    foreach (var bone in c.bones) { driven.Add(bone); if (c.label == "Left ear" || c.label == "Right ear") ears.Add(bone); }
            overlay.keepLive = System.Array.ConvertAll(overlay.bones, b => b == overlay.rig || b == locomotion.animationMotionRoot || ears.Contains(b));
            overlay.posedEveryFrame = System.Array.ConvertAll(overlay.bones, b => driven.Contains(b));
            return overlay;
        }

        public AnimationClip Find(string name)
        {
            var found = System.Array.Find(clips, clip => clip && clip.name == name);
#if UNITY_EDITOR
            // The Animator's controller is unavailable while the walking system runs; read the clip from the elephant's files.
            if (!found)
                foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Elephant/Animations/elephant@" + name + ".fbx"))
                    if (asset is AnimationClip clip && clip.name == name) found = clip;
#endif
            return found;
        }

        // The pose to come back to. Joints the controller or the walking animation re-pose every frame are read live instead.
        public void Begin()
        {
            startPosition = System.Array.ConvertAll(bones, b => b.localPosition);
            startRotation = System.Array.ConvertAll(bones, b => b.localRotation);
            startScale = System.Array.ConvertAll(bones, b => b.localScale);
        }

        // 0 at the start and end of the action, 1 in between, eased over the blend time.
        public static float Weight(float time, float total, float blend)
        {
            float edge = Mathf.Max(.01f, blend);
            return Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Min(time / edge, (total - time) / edge)));
        }

        // Blends the elephant from its live pose towards the clip at clipTime; weight 0 leaves the live pose untouched.
        public void Pose(AnimationClip clip, float clipTime, float weight)
        {
            if (!clip || startRotation == null) return;
            bool walking = locomotion && locomotion.isActiveAndEnabled && locomotion.enableLocomotion;
            int n = bones.Length;
            var position = new Vector3[n]; var rotation = new Quaternion[n]; var scale = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                bool live = walking || posedEveryFrame[i] || keepLive[i];
                position[i] = live ? bones[i].localPosition : startPosition[i];
                rotation[i] = live ? bones[i].localRotation : startRotation[i];
                scale[i] = live ? bones[i].localScale : startScale[i];
            }
            clip.SampleAnimation(rig.gameObject, Mathf.Clamp(clipTime, 0, clip.length));
            for (int i = 0; i < n; i++)
            {
                float w = keepLive[i] ? 0 : weight;   // ears keep flapping; the elephant itself stays where it is
                bones[i].localPosition = Vector3.Lerp(position[i], bones[i].localPosition, w);
                bones[i].localRotation = Quaternion.Slerp(rotation[i], bones[i].localRotation, w);
                bones[i].localScale = Vector3.Lerp(scale[i], bones[i].localScale, w);
            }
        }
    }
}
