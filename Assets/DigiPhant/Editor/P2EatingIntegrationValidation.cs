using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StudentWork;

namespace DigiPhant.Editor
{
    public static class P2EatingIntegrationValidation
    {
        const string ScenePath = "Assets/DigiPhant/Scenes/DigiPhant.unity";
        public static void BatchInstallAndValidate()
        {
            if (!Application.isBatchMode) throw new Exception("Close Unity, then run this installer in batch mode to preserve open-scene edits.");
            string backup = "Logs/DigiPhant-before-P2-eating-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
            Directory.CreateDirectory("Logs");
            File.Copy(ScenePath, backup, false);
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var c = UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();
            if (!c) throw new Exception("Existing scene controller is missing");
            var nav = c.GetComponent<DigiPhantDriverNavigation>();
            if (!nav) throw new Exception("Existing driver navigation is missing");
            var role = c.GetComponent<P2EatingRole>() ?? c.gameObject.AddComponent<P2EatingRole>();
            role.enabled = role.enableFixedRoles = true;
            role.eatClip = AssetDatabase.LoadAllAssetsAtPath("Assets/Elephant/Animations/elephant@eat.fbx")
                .OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview__"));
            if (!role.eatClip) throw new Exception("Eat animation asset is missing");
            nav.driverSlot = 1;
            nav.enableDriverNavigation = true;
            c.performerCount = 3;
            c.upperBodyOnly = true;
            // Existing bone mappings, sensitivities, locomotion and scenery remain intact.
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
            Debug.Log("P2_EATING_INSTALLED: backup=" + backup);
            Run();
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var c = UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();
            var role = c.GetComponent<P2EatingRole>();
            var nav = c.GetComponent<DigiPhantDriverNavigation>();
            var loco = c.GetComponent<DigiPhantLocomotion>();
            void Check(bool value, string why) { if (!value) throw new Exception("P2 eating: " + why); }
            Check(role && role.RoleEnabled && role.eatClip && nav && loco && loco.Initialise(), "installation incomplete");
            c.inputMode = InputMode.Camera;
            nav.usePalmSizeSteering = false;
            var trunk = c.controls.First(x => x.label == "Trunk curl");
            float oldSmoothing = trunk.smoothing;
            trunk.smoothing = 0;
            PerformerFrame Person(int slot, float mouth, float mc, float height) => new PerformerFrame {
                slot=slot, mouthOpen=mouth, mouthConfidence=mc,
                values=new[]{height,height,1f,1f,1f,1f}, confidence=Enumerable.Repeat(1f,6).ToArray(),
                handDepth=new[]{0f,0f}, handDepthConfidence=new[]{1f,1f},
                palmFacing=new[]{1f,1f},palmConfidence=new[]{1f,1f},
                handScale=new[]{.2f,.2f},handScaleConfidence=new[]{1f,1f}
            };
            void Packet(float now, float p2=0, float mc=1, float height=-1, bool omitP2=false)
            {
                var people = new[]{Person(1,1,1,height),Person(2,p2,mc,1),Person(3,1,1,1)};
                if (omitP2) people=people.Where(p=>p.slot!=2).ToArray();
                Check(c.AcceptPacket(JsonUtility.ToJson(new PoseFrame {version=1,performerCount=3,upperBodyOnly=true,people=people}), now), "valid packet rejected");
            }
            Packet(1);
            Check(nav.CalibrateAt(1), "driver calibration failed");
            c.ApplyControls(1,.1f);
            Check(!c.IsCalibrated && trunk.current==0, "P1/P3 mouth triggered trunk or body neutral was required");
            Check(c.controls.All(x=>x.current==0), "unassigned body gestures moved");
            Packet(1.1f,.5f); c.ApplyControls(1.1f,.1f);
            Check(Mathf.Abs(trunk.current-.5f)<.001f, "P2 mouth did not drive trunk");
            Packet(1.2f,1,0); c.ApplyControls(1.2f,.1f);
            Check(trunk.current==0, "confidence loss retained curl");
            Packet(1.3f,1); c.ApplyControls(1.3f,.1f);
            Packet(1.4f,omitP2:true); c.ApplyControls(1.4f,.1f);
            Check(trunk.current==0 && !c.TryReadMouthOpen(2,1.4f,out _), "missing slot retained mouth");
            Packet(1.5f,1); c.ApplyControls(2.1f,.1f);
            Check(trunk.current==0, "stale mouth retained curl");
            Packet(3);
            Check(nav.CalibrateAt(3), "driver recalibration failed");
            Packet(3.1f,height:0); c.ApplyControls(3.1f,.1f);
            Packet(3.5f,height:0); c.ApplyControls(3.5f,.1f);
            Check(loco.CurrentSpeed>0, "navigation no longer moves");
            Vector3 pos=loco.travelRoot.position;
            Quaternion rot=loco.travelRoot.rotation;
            ClipOverlay.Busy=true;
            Packet(3.6f,height:0); c.ApplyControls(3.6f,.1f);
            Check(loco.CurrentSpeed==0 && loco.travelRoot.position==pos && Quaternion.Angle(loco.travelRoot.rotation,rot)<.001f && nav.IsCalibrated,
                "eating moved root or erased driver calibration");
            ClipOverlay.Busy=false;
            Packet(3.7f,height:0); c.ApplyControls(3.7f,.1f);
            Check(loco.CurrentSpeed>0 && nav.IsCalibrated,"navigation did not resume");
            var melon=Watermelon.Spawn();
            Check(melon && melon.eatClip==role.eatClip,"melon lacks persistent eat reference");
            melon.transform.position=melon.ReachPoint+Vector3.right*(melon.pickupRange+5);
            Packet(3.8f,1); c.ApplyControls(3.8f,.1f); melon.Step(.1f);
            Check(!melon.Held&&!melon.Eating,"mouth picked up an out-of-reach melon");
            melon.transform.position=melon.ReachPoint;
            Packet(4,.5f); c.ApplyControls(4,.1f); melon.Step(.1f);
            Check(melon.Held,"P2 grab within reach failed");
            Packet(4.1f,0,0); c.ApplyControls(4.1f,.1f); melon.Step(.1f);
            Check(!melon.Held&&!melon.Eating,"lost mouth did not release");
            melon.transform.position=melon.ReachPoint;
            Packet(4.2f,1); c.ApplyControls(4.2f,.1f); melon.Step(.1f); melon.Step(.4f);
            Check(melon.Eating&&ClipOverlay.Busy,"P2 eat threshold failed");
            pos=loco.travelRoot.position;rot=loco.travelRoot.rotation;
            Packet(4.3f,0,0,height:0); c.ApplyControls(4.3f,.1f); melon.Step(.3f);
            Check(melon.Eating&&loco.travelRoot.position==pos&&Quaternion.Angle(loco.travelRoot.rotation,rot)<.001f&&nav.IsCalibrated,
                "animation root drift or mouth loss interrupted eating");
            melon.Step(5);
            Check(melon.Eaten&&!ClipOverlay.Busy,"eating did not complete/release overlay");
            string invalid=JsonUtility.ToJson(new PoseFrame {version=1,performerCount=3,upperBodyOnly=true,people=new[]{Person(2,3.1f,1,0)}});
            Check(!c.AcceptPacket(invalid,5) && !c.TryReadMouthOpen(2,5,out _),"out-of-range mouth accepted or previous mouth retained");
            int[] assignments=c.controls.Select(x=>x.performer).ToArray();
            c.SetPerformerCount(3);
            Check(c.controls.Select(x=>x.performer).SequenceEqual(assignments),"group count rebound preserved body settings");
            trunk.smoothing=oldSmoothing;
            Watermelon.RemoveAll();
            loco.StopAnimation();
            Debug.Log("P2_EATING_VALIDATION_PASSED");
        }
    }
}
