using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigiPhant.Editor
{
    public static class DigiPhantDriverNavigationValidation
    {
        const string ScenePath = "Assets/DigiPhant/Scenes/DigiPhant.unity";
        public static void Install()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var c = UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();
            if (!c) throw new Exception("No existing elephant controller in saved scene");
            var loco = c.GetComponent<DigiPhantLocomotion>();
            if (!loco || !loco.travelRoot) throw new Exception("No existing locomotion movement root");
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/DigiPhant/Scenes/DigiPhant_BeforeDriverNavigation.unity");
            if (!AssetDatabase.CopyAsset(ScenePath, backup) || !File.Exists(backup))
                throw new Exception("Cannot create navigation scene backup");
            var nav = c.GetComponent<DigiPhantDriverNavigation>();
            if (!nav) nav = c.gameObject.AddComponent<DigiPhantDriverNavigation>();
            nav.enableDriverNavigation = true;
            nav.driverSlot = 1;
            nav.maximumForward = Mathf.Min(.6f, loco.runThreshold);
            c.upperBodyOnly = true;
            // Group count is already three; do not remap the existing body controls.
            if (c.performerCount != 3) throw new Exception("Expected existing three-performer setup; inspect before changing count");
            loco.enableLocomotion = true;
            loco.stageRadius = 30;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save driver navigation scene");
            Debug.Log("DRIVER_NAVIGATION_INSTALLED: scene=" + ScenePath + "; backup=" + backup);
            Run();
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var c = UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();
            var loco = c.GetComponent<DigiPhantLocomotion>();
            var nav = c.GetComponent<DigiPhantDriverNavigation>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
            Check(nav && loco && loco.Initialise(), "Navigation missing");
            c.inputMode = InputMode.Camera;
            nav.enableDriverNavigation = true; nav.usePalmSizeSteering = false; nav.driverSlot = 1; nav.ResetCalibration();
            string epoch = "test1";
            void Packet(float now, float height=-1, float ld=0, float rd=0, float palm=1,
                bool extras=true, float other=0, bool visible=true, float rightPalm=99)
            {
                PerformerFrame Person(int slot, float h, float d, float face) => new PerformerFrame {
                    handScale=new[]{.2f,.2f},handScaleConfidence=new[]{1f,1f},slot=slot, values=new[]{h,h,0f,0f,0f,0f}, confidence=Enumerable.Repeat(slot==1&&!visible?0f:1f,6).ToArray(),
                    handDepth=extras?new[]{slot==1?ld:d,slot==1?rd:d}:null,
                    handDepthConfidence=extras?new[]{1f,1f}:null,
                    palmFacing=extras?new[]{face,slot==1&&rightPalm!=99?rightPalm:face}:null,palmConfidence=extras?new[]{1f,1f}:null};
                string json = JsonUtility.ToJson(new PoseFrame{version=1,performerCount=3,
                    upperBodyOnly=c.upperBodyOnly,trackingEpoch=epoch,
                    people=new[]{Person(1,height,0,palm),Person(2,other,other,-1),Person(3,-other,-other,1)}});
                if (!extras) json = System.Text.RegularExpressions.Regex.Replace(json, ",\"(?:handDepth|handDepthConfidence|palmFacing|palmConfidence)\":(?:null|\\[\\])", "");
                Check(c.AcceptPacket(json,now),"Packet rejected: " + json);
            }
            Packet(1, extras:false);
            Check(!nav.CalibrateAt(1), "Missing hand calibration accepted");
            string failure = nav.Status;
            Check(failure.Contains("Left hand") && failure.Contains("depth"), "Specific hand failure missing");
            Check(!nav.TryRead(1.1f,out _,out _) && nav.Status == failure, "Calibration failure was overwritten");
            Packet(2, palm:.1f);
            Check(!nav.CalibrateAt(2) && nav.Status.Contains("edge-on"), "Palm angle failure missing");
            Packet(3);
            nav.BeginCalibration(3);
            Packet(6.1f);
            Check(!nav.TryRead(6.1f,out _,out _) && nav.CalibrationPending && !nav.IsCalibrated,
                "Calibration ended at old three-second deadline");
            Packet(11);
            Check(!nav.TryRead(11,out _,out _) && !nav.CalibrationPending && nav.IsCalibrated,
                "Eight-second calibration did not finish safely");
            nav.ResetCalibration();
            Packet(10);
            Check(nav.CalibrateAt(10)&&!c.IsCalibrated,"Independent calibration failed");
            Packet(10.1f,0);
            Check(!nav.TryRead(10.1f,out _,out _),"Gear engaged without dwell");
            Packet(10.5f,0);
            Check(nav.TryRead(10.5f,out float f,out float turn)&&f>0&&turn==0,"Forward failed");
            Packet(10.6f,0,other:2);
            nav.TryRead(10.6f,out float f2,out float turn2);
            Check(Mathf.Abs(f-f2)<.001f&&turn2==0,"Other people interfered");
            Packet(10.7f,0,ld:.6f);
            nav.TryRead(10.7f,out _,out turn);
            Check(turn<0,"Left hand forward must turn left");
            Packet(10.8f,0,rd:.6f);
            nav.TryRead(10.8f,out _,out turn);
            Check(turn>0,"Right hand forward must turn right");
            loco.Evaluate(10.8f,.1f);
            Check(loco.CurrentSpeed>0,"Forward world movement failed");
            Packet(10.9f,0,palm:-1);
            Vector3 position=loco.travelRoot.position;
            loco.Evaluate(10.9f,.1f);
            Check(loco.CurrentSpeed==0&&Vector3.Distance(position,loco.travelRoot.position)<.00001f,"Direction change drifted");
            Packet(11.1f,0,palm:-1);
            Check(!nav.TryRead(11.1f,out _,out _),"Reverse dwell too short");
            Packet(11.4f,0,palm:-1);
            Check(nav.TryRead(11.4f,out f,out _)&&f<0&&f>=-.6001f,"Reverse not capped");
            loco.Evaluate(11.4f,.1f);
            Check(loco.CurrentSpeed<0,"Reverse world movement failed");
            Packet(11.5f,-1,rd:.6f,palm:-1);
            position=loco.travelRoot.position;
            Quaternion rotation=loco.travelRoot.rotation;
            loco.Evaluate(11.5f,.1f);
            Check(loco.CurrentSpeed==0&&Vector3.Distance(position,loco.travelRoot.position)<.00001f&&Quaternion.Angle(rotation,loco.travelRoot.rotation)<.001f,"Hands down failed hard stop");
            Packet(11.6f,0,extras:false);
            Check(!nav.TryRead(11.6f,out _,out _)&&nav.IsCalibrated,"Missing hands must stop without losing body calibration");
            Packet(11.7f,0,palm:.1f);
            Check(!nav.TryRead(11.7f,out _,out _),"Edge-on palm moved");
            Packet(11.75f,0,palm:1,rightPalm:-1);
            Check(!nav.TryRead(11.75f,out _,out _),"Mixed palms moved");
            Packet(11.8f,0,visible:false);
            Check(!nav.TryRead(11.8f,out _,out _)&&nav.IsCalibrated,"Driver loss must retain calibration while stopped");
            Packet(12);
            Check(nav.CalibrateAt(12),"Recalibration failed");
            Check(!nav.TryRead(13,out _,out _)&&nav.IsCalibrated,"Stale data moved");
            Packet(14); Check(nav.CalibrateAt(14),"Calibrate failed");
            epoch="test2"; Packet(14.1f);
            Check(!nav.IsCalibrated,"New bridge epoch retained calibration");
            Check(nav.CalibrateAt(14.1f),"Epoch recalibrate failed");
            c.BeginCalibrationCountdown(14.2f);
            Check(!nav.IsCalibrated&&!nav.TryRead(14.2f,out _,out _),"Body calibration failed to stop driver");
            c.CancelCalibrationCountdown();
            Packet(14.4f); Check(nav.CalibrateAt(14.4f),"Malformed setup calibration failed");
            Check(!c.AcceptPacket("{}",14.5f),"Malformed packet accepted");
            Check(!nav.TryRead(14.5f,out _,out _)&&nav.IsCalibrated,"Malformed packet must stop motion");
            nav.enableDriverNavigation=false;
            Packet(15); Check(c.CalibrateAt(15),"Legacy calibration failed");
            c.inputMode=InputMode.TestSliders; loco.testForward=.4f; loco.Evaluate(15,.1f);
            Check(loco.CurrentSpeed>0,"Legacy sliders failed");
            loco.StopAnimation();
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("DRIVER_NAVIGATION_VALIDATION_OK: palm gears and dwell, depth directions, P2/P3 isolation, hard stops, loss, missing hands, epoch, global calibration, legacy sliders");
        }
    }
}

