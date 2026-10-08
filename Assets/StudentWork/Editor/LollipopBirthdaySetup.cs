using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StudentWork.Editor {
    public static class LollipopBirthdaySetup {
        const string Folder="Assets/StudentWork/BirthdayHat";
        [MenuItem("DigiPhant/Student Props/Enable Lollipop Eating and Birthday Hat")]
        public static void Enable() {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first");
            var scene=SceneManager.GetActiveScene();
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var pickup=all.Select(t=>t.GetComponent<LollipopPickup>()).Where(p=>p).Single();
            if(pickup.mouth && pickup.birthdayHat) { Debug.Log("LOLLIPOP_BIRTHDAY_ALREADY_ENABLED"); return; }
            var head=all.Single(t=>t.name=="elephant_Head_bone");
            var jaw=all.Single(t=>t.name=="elephant_Jaw_bone");
            if(string.IsNullOrEmpty(scene.path) || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
            var backup=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path,null)+"_BeforeBirthdayHat.unity");
            if(!AssetDatabase.CopyAsset(scene.path,backup)) throw new IOException("Scene backup failed");
            Undo.RecordObject(pickup,"Configure eating and birthday hat");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/BirthdayHat.prefab");
            if(!prefab) {
                if(Directory.Exists(Folder)) throw new InvalidOperationException("Existing hat folder needs reconciliation");
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                var pink=Material("Party pink",new Color(1,.19f,.57f));
                var yellow=Material("Golden dots",new Color(1,.88f,.14f));
                var purple=Material("Purple pom",new Color(.54f,.13f,.88f));
                var root=new GameObject("Growing Birthday Hat");
                try {
                    var mesh=new Mesh {name="Party cone"}; var vertices=new List<Vector3>();var triangles=new List<int>();
                    for(int i=0;i<32;i++) {
                        float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;int n=vertices.Count;
                        vertices.Add(new Vector3(Mathf.Cos(a)*.38f,0,Mathf.Sin(a)*.38f));
                        vertices.Add(new Vector3(0,.85f,0));vertices.Add(new Vector3(Mathf.Cos(b)*.38f,0,Mathf.Sin(b)*.38f));
                        triangles.AddRange(new[]{n,n+1,n+2});
                    }
                    mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh,Folder+"/PartyCone.asset");
                    var cone=new GameObject("Pink party cone");cone.transform.SetParent(root.transform,false);cone.AddComponent<MeshFilter>().sharedMesh=mesh;cone.AddComponent<MeshRenderer>().sharedMaterial=pink;
                    for(int row=0;row<3;row++) for(int i=0;i<8;i++) {
                        float y=.12f+row*.22f,r=.38f*(1-y/.85f),angle=(i*45+row*22.5f)*Mathf.Deg2Rad;
                        var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="Yellow party dot";UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());
                        dot.transform.SetParent(root.transform,false);dot.transform.localPosition=new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r);
                        dot.transform.localScale=Vector3.one*.075f;dot.GetComponent<Renderer>().sharedMaterial=yellow;
                    }
                    var pom=GameObject.CreatePrimitive(PrimitiveType.Sphere);pom.name="Purple birthday pom";UnityEngine.Object.DestroyImmediate(pom.GetComponent<Collider>());
                    pom.transform.SetParent(root.transform,false);pom.transform.localPosition=new Vector3(0,.85f,0);pom.transform.localScale=Vector3.one*.18f;pom.GetComponent<Renderer>().sharedMaterial=purple;
                    prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/BirthdayHat.prefab");AssetDatabase.SaveAssets();
                } finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            if(!pickup.mouth) {
                var mouth=new GameObject("Lollipop Mouth Target");Undo.RegisterCreatedObjectUndo(mouth,"Add mouth target");mouth.transform.SetParent(jaw,false);
                mouth.transform.position=jaw.position+pickup.locomotion.travelRoot.forward*.30f+Vector3.up*.10f;
                pickup.mouth=mouth.transform;
            }
            if(!pickup.birthdayHat) {
                var hat=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);Undo.RegisterCreatedObjectUndo(hat,"Add birthday hat");
                hat.transform.position=head.position+Vector3.up*.65f;hat.transform.rotation=Quaternion.identity;hat.transform.SetParent(head,true);hat.SetActive(false);pickup.birthdayHat=hat.transform;
            }
            EditorUtility.SetDirty(pickup);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("LOLLIPOP_BIRTHDAY_SETUP_OK: mouth="+pickup.mouth.position+", hat="+pickup.birthdayHat.position+" | backup: "+backup);
        }
        static Material Material(string name,Color color) {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.25f);
            AssetDatabase.CreateAsset(material,Folder+"/"+name+".mat");return material;
        }
    }
}
