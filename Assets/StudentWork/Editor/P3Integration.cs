using System;
using System.IO;
using System.Linq;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace StudentWork.Editor {
    public static class P3Integration {
        const string ScenePath = "Assets/DigiPhant/Scenes/DigiPhant.unity";
        static void Check(bool ok, string reason) { if (!ok) throw new Exception("P3: " + reason); }
        public static void RunAll() {
            BatchInstall();
            Validate();
            DigiPhant.Editor.P2EatingIntegrationValidation.Run();
            DigiPhant.Editor.AutoRecoveryValidation.Run();
            Debug.Log("P3_ALL_VALIDATION_OK");
        }
        public static void BatchInstall() {
            Check(Application.isBatchMode, "Close Unity and run installer in batch mode.");
            var tree = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StudentWork/LollipopTree/PinkSwirlLollipops.prefab");
            var hatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StudentWork/BirthdayHat/BirthdayHat.prefab");
            Check(tree && hatPrefab, "Candy and hat prefabs must import before scene modification.");
            foreach (var prefab in new[]{tree,hatPrefab}) {
                Check(prefab.GetComponentsInChildren<MeshFilter>(true).All(x => x.sharedMesh), "Missing prefab mesh");
                Check(prefab.GetComponentsInChildren<Renderer>(true).All(x => x.sharedMaterials.All(m => m && m.shader)), "Missing prefab material or shader");
            }
            var sourceCandy = tree.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Growing pink lollipop "));
            Check(sourceCandy, "Candy child missing");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            var c = all.Select(x => x.GetComponent<DigiPhantController>()).Single(x => x);
            Check(c.GetComponent<DigiPhantDriverNavigation>() && c.GetComponent<P2EatingRole>(), "P1/P2 installation missing");
            if (c.GetComponent<LollipopPickup>()) { CheckReferences(c.GetComponent<LollipopPickup>()); Debug.Log("P3_ALREADY_INSTALLED"); return; }
            var bush = all.Single(x => x.name == "01 Feeding bush");
            var basin = all.Single(x => x.name == "02 Water basin");
            var head = all.Single(x => x.name == "elephant_Head_bone");
            var jaw = all.Single(x => x.name == "elephant_Jaw_bone");
            var trunk = Enumerable.Range(1,7).Select(i => all.Single(t => t.name == "elephant_Trunk"+i+"_bone")).ToArray();
            var tip = all.Single(x => x.name == "elephant_Trunk7-nub_bone");
            var ears = new[]{all.Single(x => x.name == "elephant_l_Ear1_bone"),all.Single(x => x.name == "elephant_r_Ear1_bone")};
            Directory.CreateDirectory("Logs");
            string backup = "Logs/DigiPhant-before-P3-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
            File.Copy(ScenePath, backup, false);
            var pickup = c.gameObject.AddComponent<LollipopPickup>();
            pickup.controller=c; pickup.locomotion=c.GetComponent<DigiPhantLocomotion>(); pickup.trunk=trunk; pickup.tip=tip; pickup.largeEars=ears;
            pickup.flight=c.gameObject.AddComponent<StudentFlight>(); pickup.flight.performer=3;
            var group = new GameObject("P3 Lollipops");
            pickup.candies = new Transform[2];
            var landmarks = new[]{bush,basin};
            var offsets = new[]{new Vector3(-1.35f,1.25f,-1.8f),new Vector3(-1f,1.25f,2.1f)};
            for (int i=0;i<2;i++) {
                var candy = UnityEngine.Object.Instantiate(sourceCandy.gameObject,group.transform);
                candy.name="P3 Lollipop "+(i+1);
                var course=landmarks[i].parent && landmarks[i].parent.parent ? landmarks[i].parent.parent : landmarks[i];
                candy.transform.position=landmarks[i].position+course.TransformVector(offsets[i]);
                candy.transform.rotation=course.rotation; candy.transform.localScale=Vector3.one;
                pickup.candies[i]=candy.transform;
            }
            var mouth = new GameObject("Lollipop Mouth Target"); mouth.transform.SetParent(jaw,false);
            mouth.transform.position=jaw.position+pickup.locomotion.travelRoot.forward*.30f+Vector3.up*.10f; pickup.mouth=mouth.transform;
            var hat=(GameObject)PrefabUtility.InstantiatePrefab(hatPrefab,scene);
            hat.transform.position=head.position+Vector3.up*.65f;hat.transform.rotation=Quaternion.identity;hat.transform.SetParent(head,true);hat.SetActive(false);pickup.birthdayHat=hat.transform;
            CheckReferences(pickup);
            Check(pickup.candies.SelectMany(x => x.GetComponentsInChildren<MeshFilter>()).All(x => AssetDatabase.Contains(x.sharedMesh)), "Candy geometry is not persistent");
            EditorUtility.SetDirty(pickup); EditorUtility.SetDirty(pickup.flight);
            EditorSceneManager.MarkSceneDirty(scene);
            Check(EditorSceneManager.SaveScene(scene), "Scene save failed");
            Debug.Log("P3_INSTALLED: current savannah scene preserved; backup="+backup);
        }
        static void CheckReferences(LollipopPickup p) {
            Check(p && p.controller && p.locomotion && p.tip && p.mouth && p.birthdayHat && p.flight && p.flight.performer==3, "Missing controller/hat/mouth/flight refs");
            Check(p.trunk.Length==7 && p.trunk.All(x=>x) && p.largeEars.Length==2 && p.largeEars.All(x=>x) && p.candies.Length==2 && p.candies.All(x=>x), "Missing trunk/ear/candy refs");
        }
        [MenuItem("DigiPhant/Validate lollipop image to eating")]
        public static void Validate() {
            Check(!EditorApplication.isPlaying, "Stop Play mode before running preview-scene validation.");
            var scene=EditorSceneManager.OpenPreviewScene(ScenePath);
            try {
                var p=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<LollipopPickup>(true)).Single(); CheckReferences(p);
                var c=p.controller; var f=p.flight;
                c.performerCount=3; c.upperBodyOnly=true; c.inputMode=InputMode.Camera;
                PerformerFrame Person(int slot,float height,bool flaps) => new PerformerFrame {slot=slot,values=new float[6],confidence=Enumerable.Repeat(1f,6).ToArray(),handDepth=new float[2],handDepthConfidence=new[]{1f,1f},palmFacing=new[]{1f,1f},palmConfidence=new[]{1f,1f},handScale=new[]{.2f,.2f},handScaleConfidence=new[]{1f,1f},flap=flaps ? new[]{height,height,1f,1f}:null};
                void Packet(float t,int slot,float h,bool flaps=true) { Check(c.AcceptPacket(JsonUtility.ToJson(new PoseFrame{version=1,upperBodyOnly=true,performerCount=3,people=new[]{Person(slot,h,flaps)}}),t), "Pose packet rejected");f.PollPose(t); }
                Packet(1,3,0);Packet(1.1f,3,.4f);Packet(1.2f,3,0);Check(!f.Flapping && !f.Unlocked,"Flight unlocked before rewards");
                f.SetUnlocked(true);
                for(int slot=1;slot<=2;slot++){Packet(2+slot,slot,0);Packet(2.1f+slot,slot,.4f);Packet(2.2f+slot,slot,0);Check(!f.Flapping,"P1/P2 incorrectly caused flight");}
                Packet(6,3,0);Packet(6.1f,3,.4f);Packet(6.2f,3,0);Check(f.Flapping,"P3 alternating arms did not fly");
                f.GroundReset();f.SetUnlocked(true);
                f.Observe(6.3f,true,true,0,0);f.Observe(6.5f,true,true,.18f,.18f);
                f.Observe(6.6f,true,false,.18f,.18f); // Short arc through reduced sideways separation.
                f.Observe(7f,true,true,.18f,.18f);f.Observe(7.3f,true,true,.18f,.18f);
                f.Observe(7.5f,true,true,0,0);
                Check(f.Flapping,"Small slower webcam flap or brief sideways dropout failed");
                f.Observe(7.6f,false,true,0,0);Check(!f.Flapping,"Tracking loss retained flight");
                Packet(7.7f,3,0);Packet(7.8f,3,.4f);Packet(7.9f,3,0);
                var position=Vector3.zero;
                for(int i=0;i<100;i++)position=f.ApplyHeight(position,0,.1f);
                Check(Mathf.Abs(position.y-2.2f)<.001f,"Height cap failed");
                Packet(7,3,0,false); Check(!f.Flapping,"Legacy absent flap did not stop gesture");
                for(int i=0;i<100;i++) position=f.ApplyHeight(position,0,.1f);
                Check(position.y==0 && !f.IsAirborne,"Tracking loss did not land");
                var invalid=Person(3,0,true);invalid.flap=new[]{0f,0f,1f,2f};
                Check(!c.AcceptPacket(JsonUtility.ToJson(new PoseFrame{version=1,upperBodyOnly=true,performerCount=3,people=new[]{invalid}}),8),"Invalid confidence accepted");
                var owner=new object();Check(ClipOverlay.TryAcquire(owner),"Could not acquire trunk");Check(!p.TryPick(9),"P3 stole occupied trunk");ClipOverlay.Release(p);Check(ClipOverlay.Owns(owner),"Foreign release cleared ownership");ClipOverlay.Release(owner);
                f.GroundReset();f.SetUnlocked(false);p.ResetPicks();
                // Full consumption simulation proves rewards follow actual mouth contact.
                var candy=p.candies[0]; var travel=p.locomotion.travelRoot;
                var locals=p.trunk.Select(t=>t.localRotation).ToArray();
                for(int bite=0;bite<2;bite++) {
                    candy=p.candies[bite]; var grip=candy.TransformPoint(p.gripLocal); var offset=p.trunk[0].position-travel.position;
                    travel.position=new Vector3(grip.x-offset.x,travel.position.y,grip.z-offset.z);
                    var others=p.candies;p.candies=new[]{candy};
                    float start=10+bite*30;
                    Check(p.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":false,\"score\":0}",start-1),"Absence packet rejected");
                    p.StepDetection(start-1);p.StepDetection(start-.1f);
                    const string imagePacket="{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.94}";
                    Check(p.AcceptPacket(imagePacket,start),"Verified image packet rejected");p.StepDetection(start);
                    Check(p.MotionPhase=="Reach"&&!p.IsArmed,"Detected image did not immediately trigger eating");
                    Check(ClipOverlay.Busy && ClipOverlay.BlocksTravel,"Candy reach did not own/brake trunk");
                    var competitor=new object();Check(!ClipOverlay.TryAcquire(competitor),"P2 could acquire occupied trunk");
                    for(int frame=1;frame<=1500 && p.MotionPhase!="Idle";frame++) {for(int j=0;j<locals.Length;j++)p.trunk[j].localRotation=locals[j];p.StepMotion(start+frame/60f);}
                    p.candies=others;
                    Check(p.ConsumedCount==bite+1,"Candy did not complete mouth contact: "+p.Status);
                    Check(p.birthdayHat.gameObject.activeSelf,"First reward hat absent");
                    Check(f.Unlocked==(bite==1),"Flight did not unlock after second complete bite");
                    Check(!ClipOverlay.Busy,"Completed bite retained trunk ownership");
                }
                var loco=p.locomotion; var nav=c.GetComponent<DigiPhantDriverNavigation>();
                Check(loco.Initialise(),"Locomotion initialization failed");
                void FlightPacket(float t, float h, bool active) {
                    var driver=Person(1,0,false);
                    if(t>100) {driver.values[0]=driver.values[1]=.8f;driver.handDepth[0]=.5f;}
                    Check(c.AcceptPacket(JsonUtility.ToJson(new PoseFrame {version=1,performerCount=3,upperBodyOnly=true,people=new[]{driver,Person(3,h,active)}}),t),"Flight integration packet rejected");
                }
                nav.usePalmSizeSteering=false;
                FlightPacket(100,0,true); Check(nav.CalibrateAt(100),"P1 calibration for flight failed");
                Vector3 ground=loco.travelRoot.position;Quaternion initialHeading=loco.travelRoot.rotation;
                Check(loco.followCamera && loco.followElephant,"Following camera missing");
                Vector3 cameraOffset=loco.followCamera.transform.position-ground;
                for(int i=0;i<30;i++) {float t=100+i*.1f;FlightPacket(t,i%2==0?0:.4f,true);Check(loco.Evaluate(t,.1f),"Flight locomotion failed");}
                Check(loco.CurrentAction=="Flying","Airborne movement still used walking animation");
                var earRotations=p.largeEars.Select(e=>e.localRotation).ToArray();f.PoseFlightEars(.1f);
                Check(p.largeEars.Select((ear,i)=>Quaternion.Angle(earRotations[i],ear.localRotation)).All(angle=>angle>5),"Flight ears did not flap");
                for(int i=0;i<p.largeEars.Length;i++)p.largeEars[i].localRotation=earRotations[i];
                Check(loco.travelRoot.position.y>ground.y+.1f && nav.IsCalibrated,"Flight failed to rise or cleared P1 calibration");
                Check(Vector2.Distance(new Vector2(ground.x,ground.z),new Vector2(loco.travelRoot.position.x,loco.travelRoot.position.z))>.1f && Quaternion.Angle(initialHeading,loco.travelRoot.rotation)>1,"P1 could not move and steer during P3 flight");
                Check(Vector3.Dot(loco.followCamera.transform.position-loco.travelRoot.position,loco.travelRoot.forward)<-1,"Camera is not behind elephant during flight");
                for(int i=0;i<60;i++) {float t=104+i*.1f;FlightPacket(t,0,false);loco.Evaluate(t,.1f);}
                Check(Mathf.Abs(loco.travelRoot.position.y-ground.y)<.001f && nav.IsCalibrated,"Landing failed or cleared P1 calibration");
                c.inputMode=InputMode.TestSliders;FlightPacket(111,0,true);f.PollPose(111);FlightPacket(111.1f,.4f,true);f.PollPose(111.1f);FlightPacket(111.2f,0,true);f.PollPose(111.2f);
                Check(!f.Flapping,"Camera flap active in slider mode");loco.StopAnimation();
                Debug.Log("P3_VALIDATION_OK: P1/P2 no flight; P3 only; packet validation; loss landing; capped height; exclusive trunk; two consumed rewards.");
            } finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
