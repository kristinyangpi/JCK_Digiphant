using System;
using System.Linq;
using DigiPhant;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace StudentWork.Editor {
    public static class WatermelonCollisionValidation {
        static void Check(bool ok,string message) { if(!ok) throw new Exception("Watermelon collision: "+message); }
        public static void RunAll() {
            Run(); P3Integration.RunAll(); Debug.Log("WATERMELON_ALL_CHECKS_PASSED");
        }
        public static void Run() {
            EditorSceneManager.OpenScene("Assets/DigiPhant/Scenes/DigiPhant.unity");
            var c=UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();
            var loco=c.GetComponent<DigiPhantLocomotion>();
            Watermelon.RemoveAll();var m=Watermelon.Spawn();Check(m,"Spawn failed");
            var trunk=c.controls.First(x=>x.label=="Trunk curl");trunk.current=0;
            var legs=c.controls.Where(x=>x.label.ToLower().Contains("leg")).SelectMany(x=>x.bones).ToArray();
            Check(legs.Length==4,"Four legs missing");
            foreach(var leg in legs) {
                var foot=leg.GetComponentsInChildren<Transform>().OrderBy(t=>t.position.y).First();
                var start=new Vector3(foot.position.x,m.home.y,foot.position.z);
                m.transform.position=start;m.Step(0);
                Check(Vector3.Distance(start,m.transform.position)>.15f,"Ground fruit stayed inside "+leg.name);
                Check(Mathf.Abs(m.transform.position.y-start.y)<.001f,"Ground fruit changed height");
                Check(Vector3.Distance(m.transform.position,m.SeparateFromElephant(m.transform.position,m.CollisionRadius))<.01f,"Separation did not converge");
            }
            Vector3 far=m.home+Vector3.right*20;m.transform.position=far;m.Step(0);Check(m.transform.position==far,"Distant fruit moved");
            var root=loco.travelRoot;var original=root.position;root.position+=Vector3.up*10;
            var low=new Vector3(legs[0].position.x,m.home.y,legs[0].position.z);m.transform.position=low;m.Step(0);Check(m.transform.position==low,"Airborne elephant pushed ground fruit");root.position=original;
            var tip=trunk.bones.Last();while(tip.childCount==1)tip=tip.GetChild(0);
            Check(Vector3.Distance(m.CarryPoint(m.CollisionRadius),tip.position)>=m.CollisionRadius,"Carry point intersects trunk endpoint");
            m.transform.position=m.ReachPoint;trunk.current=.5f;m.Step(.01f);Check(m.Held,"Reachable fruit failed to grab");m.Step(.3f);
            Check(Vector3.Distance(m.transform.position,tip.position)>=m.CollisionRadius,"Held fruit intersects trunk endpoint");
            trunk.current=0;m.Step(.01f);Check(!m.Held&&!ClipOverlay.Busy,"Drop failed to release");
            Check(Vector3.Distance(m.transform.position,m.SeparateFromElephant(m.transform.position,m.CollisionRadius))<.01f,"Drop inside body");
            Watermelon.RemoveAll(); Debug.Log("WATERMELON_COLLISION_VALIDATION_OK");
        }
    }
}
