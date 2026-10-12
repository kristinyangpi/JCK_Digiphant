using System.Collections.Generic;
using UnityEngine;

namespace StudentWork
{
    // Flowers that grow where the elephant's sprayed water lands.
    // Each sown jet is followed along its arc (the same speed, direction and gravity as the visible droplets);
    // when it reaches the ground a flower sprouts there and grows to full size. Flowers stay until Play stops.
    // Everything is made in code while playing; nothing is saved in the scene.
    public class FlowerPatch : MonoBehaviour
    {
        [Tooltip("The oldest flowers are removed beyond this number.")]
        public int maxFlowers = 80;
        [Tooltip("Seconds a flower takes to grow to full size after the water lands.")]
        public float growSeconds = 1.2f;
        public Vector2 heightRange = new Vector2(.8f, 1.3f);
        public int Count => flowers.Count;
        public int Sown { get; private set; }
        public IReadOnlyList<Transform> Flowers => flowers;

        struct Seed { public float wait; public Vector3 at; }
        readonly List<Seed> seeds = new List<Seed>();
        readonly List<Transform> flowers = new List<Transform>();
        readonly List<float> ages = new List<float>();
        static readonly Color[] petalColours =
        {
            new Color(1, .45f, .7f), new Color(1, .85f, .2f), new Color(.98f, .98f, .95f), new Color(.95f, .25f, .25f), new Color(.7f, .45f, .95f)
        };
        Material stem, centre;
        Material[] petals;

        // Follows one jet of water from the trunk to the ground and plants a flower when it gets there.
        // Returns where it will land. keepClearOf: nothing grows within that radius of this point (the water basin).
        public Vector3 Sow(Vector3 from, Vector3 velocity, float gravity, float groundY, Vector3? keepClearOf = null, float clearRadius = 0)
        {
            // Intersect the descending arc with real, visible playable top triangles.
            // There is no infinite ground plane in the floating course.
            Vector3 at=from; float time=0; bool landed=false;
            for(float t=.04f;t<=5;t+=.04f) {
                Vector3 next=from+velocity*t+Vector3.down*(.5f*gravity*t*t);
                if(TrySurface(next, out float y) && at.y>=y && next.y<=y) { next.y=y;at=next;time=t;landed=true;break; }
                at=next;
            }
            if(!landed) return at;
            if (keepClearOf.HasValue)
            {
                Vector3 gap = at - keepClearOf.Value;
                gap.y = 0;
                if (gap.magnitude < clearRadius) return at;
            }
            seeds.Add(new Seed { wait = time, at = at });
            Sown++;
            return at;
        }

        struct Top { public Vector3 a,b,c; public Renderer renderer; }
        readonly List<Top> tops=new List<Top>();
        bool scanned;
        public bool TrySurface(Vector3 p,out float y) {
            if(!scanned){scanned=true;foreach(var f in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
                bool allowed=f.name=="Open path" || (f.name.StartsWith("Island ") && f.name.EndsWith(" icing"));
                var r=f.GetComponent<Renderer>();if(!allowed||!r||!r.enabled||!r.gameObject.activeInHierarchy||!f.sharedMesh)continue;
                var v=f.sharedMesh.vertices;var ids=f.sharedMesh.triangles;
                for(int i=0;i<ids.Length;i+=3){var a=f.transform.TransformPoint(v[ids[i]]);var b=f.transform.TransformPoint(v[ids[i+1]]);var c=f.transform.TransformPoint(v[ids[i+2]]);
                    if(Vector3.Cross(b-a,c-a).normalized.y>.7f)tops.Add(new Top{a=a,b=b,c=c,renderer=r});}
            }}
            y=float.NegativeInfinity;bool found=false;
            foreach(var t in tops){if(!t.renderer||!t.renderer.enabled||!t.renderer.gameObject.activeInHierarchy)continue;
                float d=(t.b.z-t.c.z)*(t.a.x-t.c.x)+(t.c.x-t.b.x)*(t.a.z-t.c.z);if(UnityEngine.Mathf.Abs(d)<.000001f)continue;
                float u=((t.b.z-t.c.z)*(p.x-t.c.x)+(t.c.x-t.b.x)*(p.z-t.c.z))/d;
                float v=((t.c.z-t.a.z)*(p.x-t.c.x)+(t.a.x-t.c.x)*(p.z-t.c.z))/d;float w=1-u-v;
                if(u<0||v<0||w<0)continue;float h=u*t.a.y+v*t.b.y+w*t.c.y;if(h>y){y=h;found=true;}
            }return found;
        }

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            for (int i = seeds.Count - 1; i >= 0; i--)
            {
                var seed = seeds[i];
                seed.wait -= dt;
                if (seed.wait > 0) { seeds[i] = seed; continue; }
                seeds.RemoveAt(i);
                Plant(seed.at);
            }
            for (int i = 0; i < flowers.Count; i++)
            {
                if (ages[i] >= growSeconds) continue;
                ages[i] += dt;
                // Eases out, with a slight overshoot so it looks like it springs up.
                float t = Mathf.Clamp01(ages[i] / Mathf.Max(.01f, growSeconds));
                float size = t >= 1 ? 1 : 1 - Mathf.Pow(1 - t, 3) + .12f * Mathf.Sin(t * Mathf.PI);
                flowers[i].localScale = Vector3.one * size;
            }
        }

        void Plant(Vector3 at)
        {
            if (stem == null) MakeMaterials();
            var flower = new GameObject("Flower").transform;
            flower.SetParent(transform, false);
            flower.SetPositionAndRotation(at, Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f)));
            float height = Random.Range(heightRange.x, heightRange.y);
            Part(flower, PrimitiveType.Cylinder, stem, new Vector3(0, height / 2, 0), Quaternion.identity, new Vector3(.05f, height / 2, .05f));
            var colour = petals[Random.Range(0, petals.Length)];
            const int petalCount = 6;
            for (int i = 0; i < petalCount; i++)
            {
                var turn = Quaternion.Euler(0, i * 360f / petalCount, 0);
                Part(flower, PrimitiveType.Sphere, colour, new Vector3(0, height, 0) + turn * new Vector3(0, 0, .17f), turn * Quaternion.Euler(-20, 0, 0), new Vector3(.17f, .05f, .26f));
            }
            Part(flower, PrimitiveType.Sphere, centre, new Vector3(0, height + .02f, 0), Quaternion.identity, new Vector3(.16f, .1f, .16f));
            flower.localScale = Vector3.zero;
            flowers.Add(flower);
            ages.Add(0);
            while (flowers.Count > Mathf.Max(1, maxFlowers))
            {
                Remove(flowers[0].gameObject);
                flowers.RemoveAt(0);
                ages.RemoveAt(0);
            }
        }

        static void Part(Transform parent, PrimitiveType shape, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var piece = GameObject.CreatePrimitive(shape);
            Remove(piece.GetComponent<Collider>());
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localRotation = rotation;
            piece.transform.localScale = scale;
            var renderer = piece.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // A few shared materials for all flowers, kept in memory only (see Watermelon for why they are marked to keep).
        void MakeMaterials()
        {
            Material Make(Color colour)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.HideAndDontSave };
                material.SetColor("_BaseColor", colour);
                material.SetFloat("_Smoothness", .2f);
                return material;
            }
            stem = Make(new Color(.2f, .55f, .2f));
            centre = Make(new Color(1, .75f, .1f));
            petals = System.Array.ConvertAll(petalColours, Make);
        }

        static void Remove(Object thing)
        {
            if (!thing) return;
            if (Application.isPlaying) Destroy(thing); else DestroyImmediate(thing);
        }
    }
}
