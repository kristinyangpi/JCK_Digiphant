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
    public static class BananaPeelSetup
    {
        const string Folder = "Assets/StudentWork/BananaPeel";
        const string Name = "Banana Peel beside Pond";

        [MenuItem("DigiPhant/Student Props/Add Banana Peel Beside Pond")]
        public static void Add()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before adding the banana peel.");
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save the scene first.");
            var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var existing = objects.Where(t => t.name == Name).ToArray();
            if (existing.Length > 0)
            {
                Selection.activeGameObject = existing[0].gameObject;
                Debug.Log("BANANA_PEEL_ALREADY_PRESENT: existing prop left unchanged.");
                return;
            }
            var ponds = objects.Where(t => t.name == "02 Water basin").ToArray();
            if (ponds.Length != 1) throw new InvalidOperationException("Expected exactly one 02 Water basin in the active scene.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (scene.isDirty) throw new InvalidOperationException("Save all scene changes before adding this prop.");
            var backup = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path, null) + "_BeforeBananaPeel.unity");
            if (!AssetDatabase.CopyAsset(scene.path, backup)) throw new IOException("Could not create scene backup.");
            string prefabPath = Folder + "/BananaPeel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab)
            {
                if (Directory.Exists(Folder)) throw new InvalidOperationException("BananaPeel asset folder exists without its prefab; reconcile existing assets first.");
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
                var yellow = Material("Golden peel", new Color(1f, .69f, .055f));
                var cream = Material("Cream inner peel", new Color(1f, .91f, .59f));
                var brown = Material("Brown stem and tips", new Color(.24f, .105f, .035f));
                var model = new GameObject(Name);
                try
                {
                    for (int petal = 0; petal < 4; petal++)
                    {
                        var mesh = Flap(petal);
                        AssetDatabase.CreateAsset(mesh, Folder + "/Curl" + (petal + 1) + ".asset");
                        var part = new GameObject("Curled peel " + (petal + 1));
                        part.transform.SetParent(model.transform, false);
                        part.AddComponent<MeshFilter>().sharedMesh = mesh;
                        part.AddComponent<MeshRenderer>().sharedMaterials = new[] { yellow, cream, brown };
                    }
                    // A short, angled stalk makes the four curling flaps read as one peel.
                    var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stem.name = "Dry banana stem";
                    UnityEngine.Object.DestroyImmediate(stem.GetComponent<Collider>());
                    stem.transform.SetParent(model.transform, false);
                    stem.transform.localPosition = new Vector3(0, .19f, 0);
                    stem.transform.localRotation = Quaternion.Euler(0, 0, -16);
                    stem.transform.localScale = new Vector3(.09f, .085f, .09f);
                    stem.GetComponent<MeshRenderer>().sharedMaterial = brown;
                    prefab = PrefabUtility.SaveAsPrefabAsset(model, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(model); }
                AssetDatabase.SaveAssets();
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Add banana peel beside pond");
            // Independent root: course regeneration will leave this student prop intact.
            instance.name = Name;
            instance.transform.position = ponds[0].position + new Vector3(2.242841f, .03485f, .6f);
            instance.transform.rotation = Quaternion.Euler(0, 24, 0);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = instance;
            Debug.Log("BANANA_PEEL_OK: " + scene.path + " | backup: " + backup);
        }

        static Material Material(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader is required.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .22f);
            AssetDatabase.CreateAsset(material, Folder + "/" + name + ".mat");
            return material;
        }

        static Mesh Flap(int petal)
        {
            // Raised central base, low outer belly and an upward curling narrow tip.
            float[] radius = { .035f, .16f, .31f, .46f, .54f, .56f };
            float[] height = { .19f, .13f, .055f, .026f, .075f, .17f };
            float[] width = { .052f, .125f, .15f, .115f, .06f, .009f };
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
            Quaternion turn = Quaternion.Euler(0, petal * 90 + 8, 0);
            for (int i = 0; i < radius.Length; i++)
            {
                for (int layer = 0; layer < 2; layer++)
                    for (int side = 0; side < 3; side++)
                    {
                        float x = (side - 1) * width[i];
                        float y = height[i] + (side == 1 ? .016f : 0) - layer * .022f;
                        vertices.Add(turn * new Vector3(x, y, radius[i]));
                        uv.Add(new Vector2(side / 2f, i / 5f));
                    }
            }
            void Quad(int material, int a, int b, int c, int d)
            {
                triangles[material].AddRange(new[] { a, b, c, a, c, d });
            }
            for (int i = 0; i < radius.Length - 1; i++)
            {
                int a = i * 6, b = (i + 1) * 6;
                int inside = i == radius.Length - 2 ? 2 : 1;
                for (int s = 0; s < 2; s++)
                {
                    Quad(inside, a + s, b + s, b + s + 1, a + s + 1);
                    Quad(0, a + 3 + s, a + 4 + s, b + 4 + s, b + 3 + s);
                }
                Quad(0, a, a + 3, b + 3, b);
                Quad(0, a + 2, b + 2, b + 5, a + 5);
            }
            Quad(0, 0, 1, 4, 3); Quad(0, 1, 2, 5, 4);
            int end = (radius.Length - 1) * 6;
            Quad(2, end, end + 3, end + 4, end + 1);
            Quad(2, end + 1, end + 4, end + 5, end + 2);
            var mesh = new Mesh { name = "Tapered banana curl " + (petal + 1) };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.subMeshCount = 3;
            for (int i = 0; i < 3; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
