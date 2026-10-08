using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigiPhant.Editor
{
    public static class GoldEarringsSetup
    {
        const string Folder = "Assets/DigiPhant/Accessories";
        [MenuItem("DigiPhant/Add Gold Earrings")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/DigiPhant/Scenes/DigiPhant.unity");
            var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            var left = all.Single(x => x.name == "elephant_l_Ear1_bone");
            var right = all.Single(x => x.name == "elephant_r_Ear1_bone");
            var skin = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .First(x => x.bones.Contains(left));
            var baked = new Mesh();
            skin.BakeMesh(baked);
            var vertices = baked.vertices.Select(v => skin.transform.TransformPoint(v)).ToArray();
            var bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (var v in vertices) bounds.Encapsulate(v);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            string meshPath = Folder + "/GoldHoop.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (!mesh) { mesh = MakeHoop(); AssetDatabase.CreateAsset(mesh, meshPath); }
            string matPath = Folder + "/GoldEarrings.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!mat)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) throw new Exception("URP Lit shader unavailable");
                mat = new Material(shader) { name = "Gold Earrings" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetColor("_BaseColor", new Color(1f, .64f, .12f));
            mat.SetFloat("_Metallic", .72f);
            mat.SetFloat("_Smoothness", .72f);
            EditorUtility.SetDirty(mat);
            float radius = bounds.size.y * .052f;
            var weights = skin.sharedMesh.boneWeights;
            var axis = (left.position - right.position).normalized;
            var normal = Vector3.Cross(axis, Vector3.up).normalized;
            foreach (var ear in new[] { left, right })
            {
                var indices = new HashSet<int>(Enumerable.Range(0, skin.bones.Length)
                    .Where(i => skin.bones[i] == ear || skin.bones[i].IsChildOf(ear)));
                var points = new List<Vector3>();
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = weights[i];
                    float influence = (indices.Contains(w.boneIndex0) ? w.weight0 : 0)
                        + (indices.Contains(w.boneIndex1) ? w.weight1 : 0)
                        + (indices.Contains(w.boneIndex2) ? w.weight2 : 0)
                        + (indices.Contains(w.boneIndex3) ? w.weight3 : 0);
                    if (influence > .45f) points.Add(vertices[i]);
                }
                if (points.Count == 0) throw new Exception("No weighted ear vertices: " + ear.name);
                var outward = ear == left ? axis : -axis;
                float mid = points.Select(p => Vector3.Dot(p - ear.position, outward)).OrderBy(x => x).ElementAt(points.Count / 2);
                var anchor = points.Where(p => Vector3.Dot(p - ear.position, outward) >= mid).OrderBy(p => p.y).First();
                // Suspend the hoop from its upper edge, slightly embedded in the ear lobe.
                var center = anchor - Vector3.up * radius * .86f;
                var existing = ear.Find("Gold Hoop Earring");
                var obj = existing ? existing.gameObject : new GameObject("Gold Hoop Earring");
                obj.transform.SetParent(ear, false);
                obj.transform.position = center;
                obj.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
                var parentScale = ear.lossyScale;
                obj.transform.localScale = new Vector3(radius / Mathf.Abs(parentScale.x), radius / Mathf.Abs(parentScale.y), radius / Mathf.Abs(parentScale.z));
                var filter = obj.GetComponent<MeshFilter>(); if (!filter) filter = obj.AddComponent<MeshFilter>();
                var renderer = obj.GetComponent<MeshRenderer>(); if (!renderer) renderer = obj.AddComponent<MeshRenderer>();
                filter.sharedMesh = mesh;
                renderer.sharedMaterial = mat;
                Debug.Log("GOLD_EARRING: " + ear.name + " anchor=" + anchor + " radius=" + radius + " earVertices=" + points.Count);
            }
            UnityEngine.Object.DestroyImmediate(baked);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            var camera = Camera.main;
            if (camera && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var oldRect = camera.rect;
                var oldTarget = camera.targetTexture;
                var oldActive = RenderTexture.active;
                var rt = new RenderTexture(1280, 900, 24);
                var image = new Texture2D(1280, 900, TextureFormat.RGB24, false);
                try
                {
                    camera.rect = new Rect(0, 0, 1, 1);
                    camera.targetTexture = rt;
                    camera.Render();
                    RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0);
                    image.Apply();
                    Directory.CreateDirectory("Logs");
                    File.WriteAllBytes("Logs/GoldEarringsPreview.png", image.EncodeToPNG());
                }
                finally
                {
                    camera.rect = oldRect;
                    camera.targetTexture = oldTarget;
                    RenderTexture.active = oldActive;
                    UnityEngine.Object.DestroyImmediate(image);
                    UnityEngine.Object.DestroyImmediate(rt);
                }
            }
            Debug.Log("GOLD_EARRINGS_SETUP_OK");
        }

        static Mesh MakeHoop()
        {
            const int major = 64, minor = 16;
            var vertices = new Vector3[(major + 1) * (minor + 1)];
            var normals = new Vector3[vertices.Length];
            var triangles = new List<int>();
            for (int i = 0; i <= major; i++)
            for (int j = 0; j <= minor; j++)
            {
                float a = 2 * Mathf.PI * i / major, b = 2 * Mathf.PI * j / minor;
                var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                int k = i * (minor + 1) + j;
                normals[k] = radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                vertices[k] = radial + normals[k] * .13f;
                if (i < major && j < minor)
                {
                    int n = k + minor + 1;
                    triangles.AddRange(new[] { k, n, k + 1, k + 1, n, n + 1 });
                }
            }
            var mesh = new Mesh { name = "Gold Hoop", vertices = vertices, normals = normals, triangles = triangles.ToArray() };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

