using System;
using System.IO;
using System.Linq;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StudentWork.Editor
{
    public static class LollipopPickupSetup
    {
        [MenuItem("DigiPhant/Student Props/Enable Lollipop Camera Pickup")]
        public static void Enable()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene=SceneManager.GetActiveScene();
            if(string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save scene first.");
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var controller=all.Select(t=>t.GetComponent<DigiPhantController>()).Where(c=>c).Single();
            var tree=all.Single(t=>t.name=="Pink Swirl Lollipop Tree");
            var candies=tree.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Growing pink lollipop ")).ToArray();
            var trunk=Enumerable.Range(1,7).Select(i=>all.Single(t=>t.name=="elephant_Trunk"+i+"_bone")).ToArray();
            var tip=all.Single(t=>t.name=="elephant_Trunk7-nub_bone");
            if(candies.Length==0) throw new InvalidOperationException("No candy targets found.");
            if(controller.GetComponent<LollipopPickup>()) { Debug.Log("LOLLIPOP_PICKUP_ALREADY_ENABLED: existing setup preserved"); return; }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
            var backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeLollipopPickup.unity");
            if(!AssetDatabase.CopyAsset(scene.path,backup)) throw new IOException("Backup failed.");
            var pickup=Undo.AddComponent<LollipopPickup>(controller.gameObject);
            pickup.controller=controller; pickup.locomotion=controller.GetComponent<DigiPhantLocomotion>();
            pickup.trunk=trunk; pickup.tip=tip; pickup.candies=candies;
            var preview=controller.GetComponent<DigiPhantCameraPreview>();
            if(!preview) preview=Undo.AddComponent<DigiPhantCameraPreview>(controller.gameObject);
            Undo.RecordObject(preview,"Use combined pose/lollipop bridge");
            preview.bridgeScriptPath="tools/lollipop_tracking/bridge.py";
            EditorUtility.SetDirty(preview); EditorUtility.SetDirty(pickup);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("LOLLIPOP_PICKUP_SETUP_OK: "+candies.Length+" candies, trunk reach "+pickup.ReachLength.ToString("F2")+"m | backup: "+backup);
        }

        [MenuItem("DigiPhant/Student Props/Validate Lollipop Pickup")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var originals=SceneManager.GetActiveScene().GetRootGameObjects();
            var source=originals.SelectMany(r=>r.GetComponentsInChildren<LollipopPickup>(true)).Single();
            if(!source.controller || !source.tip || !source.mouth || !source.birthdayHat || source.largeEars==null || source.largeEars.Length!=2 || !source.flight || source.trunk.Length!=7 || source.trunk.Any(t=>!t) || source.candies.Length<3) throw new Exception("Missing pickup/eating/hat/wings/flight refs; run setup menus first");
            // A separately deserialized preview scene remaps ALL cross-root references.
            var previewScene=EditorSceneManager.OpenPreviewScene(SceneManager.GetActiveScene().path);
            var test=previewScene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LollipopPickup>(true)).Single();
            try {
                test.enabled=false;
                var allCandies=test.candies.ToArray();
                test.locomotion.travelRoot.position=new Vector3(100,test.locomotion.travelRoot.position.y,100);
                var startupPose=test.trunk.Select(t=>t.localRotation).ToArray();
                // Reproduce the empty private cache restored by a live script reload.
                var earCache=typeof(LollipopPickup).GetField("earScales",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                earCache.SetValue(test,Array.Empty<Vector3>());
                test.ResetPicks(); test.ResetPicks();
                if(((Vector3[])earCache.GetValue(test)).Length!=test.largeEars.Length) throw new Exception("Empty ear cache not repaired");
                if(!test.IsArmed || test.PickedCount!=0 || test.MotionPhase!="Idle") throw new Exception("Startup reset state failed");
                for(int i=0;i<startupPose.Length;i++) if(Quaternion.Angle(startupPose[i],test.trunk[i].localRotation)>.001f) throw new Exception("Idle reset changed gesture pose");
                if(test.AcceptPacket("{\"version\":2,\"label\":\"lollipop\",\"detected\":true,\"score\":1}",0)) throw new Exception("Invalid version accepted");
                if(test.AcceptPacket("{\"version\":1,\"label\":\"other\",\"detected\":true,\"score\":1}",0)) throw new Exception("Invalid label accepted");
                if(!test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",0)) throw new Exception("Valid packet rejected");
                test.StepDetection(0); test.StepDetection(.1f);
                if(!test.IsArmed) throw new Exception("Detection triggered before hold");
                for(int sample=2;sample<=6;sample++) { test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",sample*.1f); test.StepDetection(sample*.1f); }
                if(!test.IsArmed || test.MotionPhase!="Idle") throw new Exception("Far picture did not remain pending");
                test.StepDetection(1); test.StepDetection(1.9f);
                if(!test.IsArmed) throw new Exception("Timeout/rearm failed");
                test.ResetPicks();
                test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",2);
                test.StepDetection(2);
                test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",2.6f);
                test.StepDetection(2.6f);
                if(!test.IsArmed) throw new Exception("Packet gap retained hold");
                test.ResetPicks();
                var travel=test.locomotion.travelRoot;
                var candy=test.candies.Last();
                if(test.gameObject.scene!=previewScene || test.trunk.Any(t=>t.gameObject.scene!=previewScene) || test.candies.Any(t=>t.gameObject.scene!=previewScene)) throw new Exception("Validation refs escaped preview scene");
                var candyPrefabRoot=PrefabUtility.GetOutermostPrefabInstanceRoot(candy.gameObject);
                if(candyPrefabRoot) PrefabUtility.UnpackPrefabInstance(candyPrefabRoot,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                var parent=candy.parent; var localPosition=candy.localPosition;
                var locals=test.trunk.Select(t=>t.localRotation).ToArray();
                var naturalPositions=test.trunk.Select(t=>t.localPosition).ToArray();
                var naturalTip=test.tip.localPosition;
                travel.rotation=Quaternion.identity;
                travel.position=new Vector3(100,travel.position.y,100);
                if(test.TryPick(3)) throw new Exception("Far candy picked");
                for(int sample=0;sample<=6;sample++) { test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",3+sample*.1f); test.StepDetection(3+sample*.1f); }
                if(!test.IsArmed || test.MotionPhase!="Idle") throw new Exception("Far held picture consumed trigger");
                var grip=candy.TransformPoint(test.gripLocal);
                var offset=test.trunk[0].position-travel.position;
                travel.position=new Vector3(grip.x-offset.x,travel.position.y,grip.z-offset.z);
                if(test.locomotion.stageRadius>0 && new Vector2(travel.position.x,travel.position.z).magnitude>test.locomotion.stageRadius)
                    throw new Exception("Near test would exceed the existing stage radius");
                test.candies=new[]{candy};
                // False+true packets may be drained in one Unity frame: absence
                // must break the hold even when StepDetection sees only the latest.
                test.ResetPicks();
                test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",2.2f); test.StepDetection(2.2f);
                test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":false,\"score\":0}",2.3f);
                test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",2.31f); test.StepDetection(2.31f);
                test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",2.5f); test.StepDetection(2.5f);
                test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",2.7f); test.StepDetection(2.7f);
                if(!test.IsArmed || test.MotionPhase!="Idle") throw new Exception("Drained absence retained old hold");
                test.ResetPicks();
                // Recreate the far-pending state after the local absence regression.
                travel.position=new Vector3(100,travel.position.y,100);
                for(int sample=0;sample<=6;sample++) { test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",3+sample*.1f); test.StepDetection(3+sample*.1f); }
                travel.position=new Vector3(grip.x-offset.x,travel.position.y,grip.z-offset.z);
                foreach(var renderer in previewScene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SkinnedMeshRenderer>(true))) {
                    renderer.forceMatrixRecalculationPerRender=true;
                    renderer.updateWhenOffscreen=true;
                }
                for(int sample=7;sample<=10;sample++) { test.AcceptPacket("{\"version\":1,\"label\":\"lollipop\",\"detected\":true,\"score\":0.9}",3+sample*.1f); test.StepDetection(3+sample*.1f); }
                if(test.IsArmed || test.MotionPhase!="Reach") throw new Exception("Pending far picture did not trigger after walking into reach");
                Capture(previewScene,test,candy,"before");
                bool capturedGrip=false;
                bool capturedEating=false;
                for(int frame=0;frame<=720;frame++) {
                    for(int i=0;i<test.trunk.Length;i++) test.trunk[i].localRotation=locals[i];
                    test.StepMotion(4+frame/60f);
                    if(!capturedGrip && test.MotionPhase=="Mouth") {
                        if(Vector3.Distance(test.tip.position,candy.TransformPoint(test.gripLocal))>.001f) throw new Exception("Stick grip attachment drifted");
                        if(test.TryPick(4+frame/60f)) throw new Exception("Second candy allowed during consumption");
                        Capture(previewScene,test,candy,"grip"); capturedGrip=true;
                    }
                    if(!capturedEating && test.MotionPhase=="Eat" && candy.localScale.magnitude<1.0f && candy.localScale.magnitude>.3f) { Capture(previewScene,test,candy,"eating"); capturedEating=true; }
                    if(frame==600) Capture(previewScene,test,candy,"birthday-hat");
                }
                if(test.PickedCount!=1 || candy.parent!=test.tip) throw new Exception("CCD failed: status="+test.Status+", phase="+test.MotionPhase+", preCancelError="+test.LastReachError.ToString("F4")+", grip="+candy.TransformPoint(test.gripLocal)+", tip="+test.tip.position+", distance="+Vector3.Distance(test.tip.position,candy.TransformPoint(test.gripLocal)).ToString("F4")+", base="+test.trunk[0].position+", travel="+travel.position+", chain="+test.ReachLength);
                if(!capturedEating || test.ConsumedCount!=1 || candy.gameObject.activeSelf) throw new Exception("Candy did not reach mouth and disappear: "+test.Status+", phase="+test.MotionPhase+", error="+test.LastReachError);
                if(test.LastMouthContactError>=.12f) throw new Exception("Consumed candy without actual trunk-tip mouth contact");
                if(!test.birthdayHat.gameObject.activeSelf || test.birthdayHat.localScale.sqrMagnitude<.9f) throw new Exception("Birthday hat did not grow");
                if(test.birthdayHat.parent.name!="elephant_Head_bone") throw new Exception("Hat is not head anchored");
                for(int i=0;i<locals.Length;i++) if(Quaternion.Angle(test.trunk[i].localRotation,locals[i])>.1f) throw new Exception("Return retained a trunk offset");
                test.ResetPicks();
                if(candy.parent!=parent || Vector3.Distance(candy.localPosition,localPosition)>.001f) throw new Exception("Reset failed to restore candy");
                if(!candy.gameObject.activeSelf || test.ConsumedCount!=0 || test.birthdayHat.gameObject.activeSelf) throw new Exception("Reset did not restore candy visibility and hide hat");
                candy.position+=Vector3.up*10;
                if(test.TryPick(8)) throw new Exception("Vertically unreachable candy picked");
                candy.position+=test.trunk[0].position+new Vector3(0,1.3f,Mathf.Sqrt(4.1f*4.1f-1.3f*1.3f))-candy.TransformPoint(test.gripLocal);
                if(Vector3.Distance(test.trunk[0].position,candy.TransformPoint(test.gripLocal))<test.ReachLength) throw new Exception("Extended test is inside natural reach");
                if(!test.TryPick(9)) throw new Exception("4.1m target not selectable with bounded extension");
                bool capturedExtendedGrip=false;
                for(int frame=0;frame<=720;frame++) {
                    for(int i=0;i<test.trunk.Length;i++) test.trunk[i].localRotation=locals[i];
                    test.StepMotion(9+frame/60f);
                    if(frame==50) Capture(previewScene,test,candy,"extended-reaching");
                    if(!capturedExtendedGrip && test.MotionPhase=="Mouth") { Capture(previewScene,test,candy,"extended-grip"); capturedExtendedGrip=true; }
                }
                if(test.PickedCount!=1 || candy.parent!=test.tip) throw new Exception("4.1m extended pickup failed: "+test.Status+", error="+test.LastReachError);
                if(test.ConsumedCount!=1 || candy.gameObject.activeSelf || !test.birthdayHat.gameObject.activeSelf) throw new Exception("Extended pickup did not consume and grow hat: "+test.Status);
                if(test.LastMouthContactError>=.12f) throw new Exception("Extended pickup missed mouth contact");
                for(int i=0;i<test.trunk.Length;i++) if(Vector3.Distance(test.trunk[i].localPosition,naturalPositions[i])>.0001f) throw new Exception("Return did not restore natural segment positions");
                if(Vector3.Distance(test.tip.localPosition,naturalTip)>.0001f) throw new Exception("Return did not restore natural tip position");
                test.ResetPicks();
                candy.position=test.trunk[0].position+Vector3.forward*(test.PickupReachLength+1)-candy.TransformVector(test.gripLocal);
                if(test.TryPick(12)) throw new Exception("Target beyond bounded extension accepted");
                candy.position+=test.trunk[0].position+Vector3.forward*4-candy.TransformPoint(test.gripLocal);
                if(!test.TryPick(13)) throw new Exception("Cancel test target not reachable");
                test.StepMotion(13.6f); test.ResetPicks();
                for(int i=0;i<test.trunk.Length;i++) if(Vector3.Distance(test.trunk[i].localPosition,naturalPositions[i])>.0001f) throw new Exception("Mid-reach reset retained extension");
                if(Vector3.Distance(test.tip.localPosition,naturalTip)>.0001f) throw new Exception("Mid-reach reset retained tip extension");
                float maximumFrameAngle=0;
                foreach(int fps in new[]{30,60}) {
                    test.ResetPicks();test.candies=allCandies.Take(3).ToArray();
                    for(int c=0;c<test.candies.Length;c++) {var candidate=test.candies[c];candidate.position+=test.trunk[0].position+new Vector3((c-1)*.7f,.7f+c*.15f,3.5f)-candidate.TransformPoint(test.gripLocal);}
                    for(int episode=0;episode<3;episode++) {
                        float start=30+episode*15;
                        if(episode==1) earCache.SetValue(test,Array.Empty<Vector3>());
                        if(!test.TryPick(start)) throw new Exception("Progression candy not selected");
                        var previous=test.trunk.Select(t=>t.localRotation).ToArray();
                        var previousTip=test.tip.rotation;
                        bool sawMouth=false,sawEat=false;
                        Transform carried=null;
                        for(int frame=0;frame<=fps*12;frame++) {
                            for(int i=0;i<test.trunk.Length;i++) test.trunk[i].localRotation=locals[i];
                            test.StepMotion(start+frame/(float)fps);
                            if(test.MotionPhase=="Mouth" || test.MotionPhase=="Eat") {
                                carried=test.candies.Single(c=>c.gameObject.activeSelf && c.parent==test.tip);
                                if(Vector3.Distance(carried.TransformPoint(test.gripLocal),test.tip.position)>.001f) throw new Exception("Candy detached from stick during "+test.MotionPhase+", bite="+(episode+1));
                                sawMouth|=test.MotionPhase=="Mouth";
                                sawEat|=test.MotionPhase=="Eat";
                                if(episode==1 && fps==60 && test.MotionPhase=="Eat" && frame%10==0) Capture(previewScene,test,carried,"second-eating");
                            }
                            float tipAngle=Quaternion.Angle(previousTip,test.tip.rotation);
                            if(tipAngle>test.tipDegreesPerSecond/fps+.15f) throw new Exception("Unsmoothed tip orientation "+tipAngle+"deg at "+fps+"fps, phase="+test.MotionPhase);
                            previousTip=test.tip.rotation;
                            for(int i=0;i<previous.Length;i++) {
                                float angle=Quaternion.Angle(previous[i],test.trunk[i].localRotation);maximumFrameAngle=Mathf.Max(maximumFrameAngle,angle);
                                if(angle>test.jointDegreesPerSecond/fps+.12f) throw new Exception("Unsmoothed trunk rotation "+angle+"deg at "+fps+"fps, phase="+test.MotionPhase);
                                previous[i]=test.trunk[i].localRotation;
                            }
                        }
                        if(!sawMouth || !sawEat || test.ConsumedCount!=episode+1 || test.LastMouthContactError>=.12f) throw new Exception("Progression consumption failed at "+(episode+1)+", "+fps+"fps: "+test.Status);
                        if(episode==0 && test.largeEars.Any(e=>Vector3.Distance(e.localScale,Vector3.one)>.01f)) throw new Exception("Ears grew early");
                        if(episode>=1 && test.largeEars.Any(e=>Vector3.Distance(e.localScale,Vector3.one*2.3f)>.01f)) throw new Exception("Second candy did not grow ears");
                        if(test.flight.Unlocked!=(episode>=1)) throw new Exception("Flight milestone incorrect");
                    }
                    Capture(previewScene,test,test.candies[2],"birthday-ears");
                    test.ResetPicks();
                    if(test.flight.Unlocked || test.largeEars.Any(e=>Vector3.Distance(e.localScale,Vector3.one)>.01f) || test.birthdayHat.gameObject.activeSelf || test.candies.Any(c=>!c.gameObject.activeSelf)) throw new Exception("Progression reset failed");
                }
                // A mouth miss must release the candy, not block every later bite.
                test.ResetPicks();test.candies=new[]{allCandies[0]};
                var retryCandy=test.candies[0];
                retryCandy.position+=test.trunk[0].position+new Vector3(0,.8f,3.4f)-retryCandy.TransformPoint(test.gripLocal);
                var retryParent=retryCandy.parent;var retryPosition=retryCandy.localPosition;var mouthPosition=test.mouth.position;
                if(!test.TryPick(100)) throw new Exception("Retry fixture not reachable");
                bool forcedMiss=false;
                for(int frame=0;frame<=900;frame++) {
                    for(int i=0;i<test.trunk.Length;i++) test.trunk[i].localRotation=locals[i];
                    test.StepMotion(100+frame/60f);
                    if(!forcedMiss && test.MotionPhase=="Mouth") {test.mouth.position+=Vector3.up*20;forcedMiss=true;}
                }
                test.mouth.position=mouthPosition;
                if(!forcedMiss || test.PickedCount!=0 || test.ConsumedCount!=0 || retryCandy.parent!=retryParent || Vector3.Distance(retryCandy.localPosition,retryPosition)>.001f || test.MotionPhase!="Idle") throw new Exception("Failed mouth pickup blocked retry: "+test.Status);
                if(!test.TryPick(120)) throw new Exception("Restored candy cannot be retried");
                for(int frame=0;frame<=900;frame++) {for(int i=0;i<test.trunk.Length;i++) test.trunk[i].localRotation=locals[i];test.StepMotion(120+frame/60f);}
                if(test.ConsumedCount!=1) throw new Exception("Retry did not consume: "+test.Status);
                test.ResetPicks();
                Debug.Log("LOLLIPOP_PICKUP_VALIDATION_OK: near/4.1m pickup, continuous candy grip, contacts, reset, failed-mouth retry, pending detection; three candies at30/60fps grow hat/ears, second unlocks flap flight, third adds no reward; max perframe angle="+maximumFrameAngle.ToString("F3")+"deg. Natural="+source.ReachLength.ToString("F2")+"m, maxPickup="+source.PickupReachLength.ToString("F2")+"m");
            } finally { EditorSceneManager.ClosePreviewScene(previewScene); }
        }
        static void Capture(Scene scene,LollipopPickup pickup,Transform candy,string stage)
        {
            var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
            if(!camera || SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            camera.scene=scene;
            var aim=(pickup.trunk[0].position+candy.TransformPoint(pickup.gripLocal))*.5f;
            if(stage.Contains("eating") || stage.Contains("birthday")) {
                aim=(pickup.mouth.position+pickup.birthdayHat.position)*.5f+Vector3.up*.2f;
                camera.transform.position=aim+pickup.locomotion.travelRoot.forward*5+pickup.locomotion.travelRoot.right*5+Vector3.up*2;
            } else camera.transform.position=aim+new Vector3(5,1.8f,-5);
            camera.transform.LookAt(aim); camera.rect=new Rect(0,0,1,1);
            var rt=new RenderTexture(960,720,24); var image=new Texture2D(960,720,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try {
                camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,960,720),0,0); image.Apply();
                string folder=Path.Combine(Path.GetTempPath(),"lollipop-pickup-validation"); Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,stage+".png"),image.EncodeToPNG());
            } finally { camera.targetTexture=null; RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); }
        }
    }
}
