using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StudentWork.Editor {
    public static class LollipopWingsSetup {
        const string Folder="Assets/StudentWork/BirthdayWings";
        [MenuItem("DigiPhant/Student Props/Enable Lollipop Wings Progression")]
        public static void Enable() {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first");
            var scene=SceneManager.GetActiveScene();
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var pickup=all.Select(t=>t.GetComponent<LollipopPickup>()).Where(p=>p).Single();
            var spine=all.Single(t=>t.name=="elephant_Spine2_bone");
            var flight=pickup.GetComponent<StudentFlight>();
            if(pickup.birthdayWings) { Debug.Log("LOLLIPOP_WINGS_ALREADY_ENABLED"); return; }
            if(string.IsNullOrEmpty(scene.path) || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
            var backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeWings.unity");
            if(!AssetDatabase.CopyAsset(scene.path,backup)) throw new IOException("Backup failed");
            if(!flight) flight=Undo.AddComponent<StudentFlight>(pickup.gameObject);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/BirthdayWings.prefab");
            if(!prefab) {
                if(Directory.Exists(Folder)) throw new InvalidOperationException("Existing wings assets require reconciliation");
                Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
                var white=Material("Cream feathers",new Color(1,.94f,.97f));
                var pink=Material("Pink feather accents",new Color(1,.46f,.72f));
                var mesh=Feather();AssetDatabase.CreateAsset(mesh,Folder+"/Feather.asset");
                var root=new GameObject("Growing Birthday Wings");
                try {
                    for(int side=0;side<2;side++) {
                        var pivot=new GameObject(side==0 ? "Left birthday wing" : "Right birthday wing");pivot.transform.SetParent(root.transform,false);
                        pivot.transform.localPosition=new Vector3(side==0 ? -.55f:.55f,0,0);
                        pivot.transform.localRotation=Quaternion.Euler(0,side==0 ? 180:0,0);
                        for(int feather=0;feather<8;feather++) {
                            var part=new GameObject("Layered feather "+(feather+1));part.transform.SetParent(pivot.transform,false);
                            part.transform.localPosition=new Vector3(feather*.055f,feather*.016f,-.6f+feather*.18f);
                            part.transform.localRotation=Quaternion.Euler(0,-28+feather*8,0);
                            part.transform.localScale=new Vector3(1-feather*.035f,1,1);
                            part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=feather%3==0 ? pink:white;
                        }
                    }
                    prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/BirthdayWings.prefab");AssetDatabase.SaveAssets();
                } finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            Undo.RecordObject(pickup,"Enable lollipop progression");
            var wings=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);Undo.RegisterCreatedObjectUndo(wings,"Add birthday wings");
            wings.transform.position=spine.position+Vector3.up*.15f;wings.transform.rotation=pickup.locomotion.travelRoot.rotation;wings.transform.SetParent(spine,true);wings.SetActive(false);
            pickup.birthdayWings=wings.transform;pickup.wingPivots=new[]{wings.transform.GetChild(0),wings.transform.GetChild(1)};pickup.flight=flight;
            EditorUtility.SetDirty(pickup);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("LOLLIPOP_WINGS_SETUP_OK: spine-anchored wings; second candy grows wings, third unlocks flight after eating | backup: "+backup);
        }
        static Material Material(string name,Color color) {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(material,Folder+"/"+name+".mat");return material;
        }
        static Mesh Feather() {
            var outline=new[]{new Vector3(0,0,0),new Vector3(.35f,0,-.19f),new Vector3(1.1f,0,-.22f),new Vector3(1.75f,0,0),new Vector3(1.1f,0,.22f),new Vector3(.35f,0,.19f)};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int layer=0;layer<2;layer++) {
                int start=vertices.Count;vertices.Add(new Vector3(.8f,layer==0 ? .09f:-.03f,0));
                foreach(var p in outline) vertices.Add(p+Vector3.up*(layer==0 ? .045f:-.03f));
                for(int i=0;i<6;i++) { int a=start+i+1,b=start+(i+1)%6+1;triangles.AddRange(layer==0 ? new[]{start,b,a}:new[]{start,a,b}); }
            }
            for(int i=0;i<6;i++) {int a=i+1,b=(i+1)%6+1;triangles.AddRange(new[]{a,b,b+7,a,b+7,a+7});}
            var mesh=new Mesh{name="Tapered party feather"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
