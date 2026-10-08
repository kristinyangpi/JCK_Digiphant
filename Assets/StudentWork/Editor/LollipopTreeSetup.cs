using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StudentWork.Editor
{
    public static class LollipopTreeSetup
    {
        const string Folder = "Assets/StudentWork/LollipopTree";
        const string RootName = "Pink Swirl Lollipop Tree";

        [MenuItem("DigiPhant/Student Props/Add Pink Swirl Lollipop Tree")]
        public static void Add()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save the active scene first.");
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var existing = all.FirstOrDefault(t => t.name == RootName);
            if (existing) { Selection.activeGameObject = existing.gameObject; Debug.Log("LOLLIPOP_TREE_ALREADY_PRESENT"); return; }
            var finish = all.Single(t => t.name == "04 Finish marker");
            var tree = all.Where(t => t.name.StartsWith("Acacia ") && t.name.Length == 9)
                .OrderBy(t => (t.position - finish.position).sqrMagnitude).First();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (scene.isDirty) throw new InvalidOperationException("Save scene changes before adding lollipops.");
            string backup = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path, null) + "_BeforeLollipops.unity");
            if (!AssetDatabase.CopyAsset(scene.path, backup)) throw new IOException("Could not create backup.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/PinkSwirlLollipops.prefab");
            if (!prefab)
            {
                if (Directory.Exists(Folder)) throw new InvalidOperationException("Existing asset folder needs reconciliation; nothing overwritten.");
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                var pink = Material("Raspberry pink candy", new Color(1f, .20f, .51f));
                var white = Material("Strawberry cream swirl", new Color(1f, .94f, .97f));
                var rim = Material("Dark pink candy edge", new Color(.78f, .075f, .32f));
                var disc = Disc(); AssetDatabase.CreateAsset(disc, Folder + "/CandyDisc.asset");
                var spiral = Spiral(); AssetDatabase.CreateAsset(spiral, Folder + "/CreamSpiral.asset");
                var root = new GameObject(RootName);
                try
                {
                    // Spread seven candies around the canopy edge on short twig-like sticks.
                    for (int i = 0; i < 7; i++)
                    {
                        float angle = (i * 360f / 7 + 18) * Mathf.Deg2Rad;
                        var candy = new GameObject("Growing pink lollipop " + (i + 1));
                        candy.transform.SetParent(root.transform, false);
                        candy.transform.localPosition = new Vector3(Mathf.Sin(angle) * 1.3f, 4.8f + (i % 3) * .25f, Mathf.Cos(angle) * 1.3f);
                        candy.transform.localRotation = Quaternion.Euler(0, i * 360f / 7 + 18, i % 2 == 0 ? -12 : 12);
                        MeshPart("Pink candy disc", candy.transform, disc, new[] { pink, rim });
                        MeshPart("Raised cream spiral on both faces", candy.transform, spiral, new[] { white });
                        for (int s = 0; s < 6; s++)
                        {
                            var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            stick.name = "Striped stick " + (s + 1);
                            UnityEngine.Object.DestroyImmediate(stick.GetComponent<Collider>());
                            stick.transform.SetParent(candy.transform, false);
                            stick.transform.localPosition = new Vector3(0, -.39f - s * .13f, 0);
                            stick.transform.localScale = new Vector3(.065f, .065f, .065f);
                            stick.GetComponent<Renderer>().sharedMaterial = s % 2 == 0 ? white : pink;
                        }
                    }
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/PinkSwirlLollipops.prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                AssetDatabase.SaveAssets();
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Grow pink lollipops");
            instance.name = RootName;
            instance.transform.SetPositionAndRotation(tree.position, tree.rotation);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = instance;
            Debug.Log("LOLLIPOP_TREE_OK: " + tree.name + " | backup: " + backup);
        }

        static Material Material(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit required.");
            var result = new Material(shader) { name = name };
            result.SetColor("_BaseColor", color); result.SetFloat("_Smoothness", .5f);
            AssetDatabase.CreateAsset(result, Folder + "/" + name + ".mat"); return result;
        }
        static void MeshPart(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var part = new GameObject(name); part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }
        static Mesh Disc()
        {
            const int segments = 48;
            var v = new List<Vector3>(); var faces = new List<int>(); var edge = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? .055f : -.055f;
                int center = v.Count; v.Add(new Vector3(0, 0, z));
                for (int i = 0; i <= segments; i++)
                {
                    float a = i * Mathf.PI * 2 / segments;
                    v.Add(new Vector3(Mathf.Cos(a) * .42f, Mathf.Sin(a) * .42f, z));
                }
                for (int i = 0; i < segments; i++)
                    faces.AddRange(side == 0 ? new[] { center, center + i + 1, center + i + 2 } : new[] { center, center + i + 2, center + i + 1 });
            }
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
                int n = v.Count;
                v.Add(new Vector3(Mathf.Cos(a) * .42f, Mathf.Sin(a) * .42f, .055f));
                v.Add(new Vector3(Mathf.Cos(a) * .42f, Mathf.Sin(a) * .42f, -.055f));
                v.Add(new Vector3(Mathf.Cos(b) * .42f, Mathf.Sin(b) * .42f, -.055f));
                v.Add(new Vector3(Mathf.Cos(b) * .42f, Mathf.Sin(b) * .42f, .055f));
                edge.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
            var mesh = new Mesh { name = "Round pink candy" }; mesh.SetVertices(v); mesh.subMeshCount = 2;
            mesh.SetTriangles(faces, 0); mesh.SetTriangles(edge, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Mesh Spiral()
        {
            var v = new List<Vector3>(); var t = new List<int>(); const int steps = 180;
            for (int side = 0; side < 2; side++)
            {
                int start = v.Count;
                for (int i = 0; i <= steps; i++)
                {
                    float a = i / (float)steps * Mathf.PI * 6.2f;
                    float r = .025f + i / (float)steps * .35f;
                    for (int edge = 0; edge < 2; edge++)
                    {
                        float radius = r + (edge == 0 ? -.020f : .020f);
                        v.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, side == 0 ? .061f : -.061f));
                    }
                }
                for (int i = 0; i < steps; i++)
                {
                    int n = start + i * 2;
                    t.AddRange(side == 0 ? new[] { n, n + 1, n + 3, n, n + 3, n + 2 } : new[] { n, n + 3, n + 1, n, n + 2, n + 3 });
                }
            }
            var mesh = new Mesh { name = "Cream spiral front and back" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
