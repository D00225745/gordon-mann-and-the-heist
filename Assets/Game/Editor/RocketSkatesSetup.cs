using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GordonMann.EditorTools
{
    public static class RocketSkatesSetup
    {
        const string Art = "Assets/Game/Blockout/Rocket Skates";
        static readonly Color Ink = Tint(0x252D42), Metal = Tint(0x677C95), Gold = Tint(0xF2A742);
        static readonly Color Pale = Tint(0xF5DEB4), Mint = Tint(0x70F1D8), DeepMint = Tint(0x357A80);

        public static void Install()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Gordon Mann v005 Rocket Skates.fbx");
            if (model == null || !model.GetComponentsInChildren<SkinnedMeshRenderer>().Any(mesh => mesh.sharedMesh.blendShapeCount == 4))
                throw new InvalidOperationException("Missing Gordon's four skating poses.");
            var scene = EditorSceneManager.OpenScene("Assets/Game/Blockout/Scenes/Fail_Skates.unity");
            var roots = scene.GetRootGameObjects();
            var director = roots.Single(item => item.name == "SceneDirector").GetComponent<SceneDirector>();
            if (director.rocketSkates != null) return;
            var beat = new GameObject("Rocket Skates Sequence").AddComponent<RocketSkatesSequence>();
            director.rocketSkates = beat;
            var props = roots.Single(item => item.name == "Vault Job Blockout").transform.Find("Props");
            foreach (Transform prop in props)
                if (prop.name is "Rocket Skate" or "Crash Barrier")
                    prop.gameObject.SetActive(false);
            beat.rider = Mark(beat.transform, "Rider and skate rig", V(-1.42f, 0, -.74f));
            var placeholder = roots.Single(item => item.name == "Gordon");
            beat.gordon = PortalJarSetup.CreateActor(model, placeholder);
            placeholder.SetActive(false);
            beat.gordon.SetParent(beat.rider, false);
            beat.gordon.SetLocalPositionAndRotation(V(0, .135f, 0), Quaternion.identity);
            FitSkates(beat, Prefab("Rocket Roller Skate", MakeSkate));
            beat.rider.rotation = Quaternion.Euler(0, -90, 0);

            beat.stop = Mark(beat.transform, "Before the barricade", V(-6.79f, 0, -.73f));
            beat.barrier = Place(Prefab("Striped Road Barrier", MakeBarrier), props, V(-7.47f, .05f, -.72f));
            beat.barrier.localRotation = Quaternion.Euler(0, -90, 0);
            var cone = Prefab("Traffic Cone", MakeCone);
            beat.cones = new[] { Place(cone, props, V(-7.16f, 0, -1.58f)), Place(cone, props, V(-7.34f, 0, .12f)) };
            beat.dust = Mark(beat.transform, "Impact dust", V(-7.09f, .92f, -.84f));
            var cloud = Material("Impact Dust", Tint(0xA4ADBB));
            foreach (var (at, size) in new (Vector3, Vector3)[]
            {
                (V(0, -.24f, 0), V(1.04f, .87f, .86f)), (V(.15f, .20f, -.21f), V(.88f, .88f, .79f)),
                (V(-.34f, .18f, .08f), V(.68f, .67f, .61f)), (V(.05f, .58f, -.10f), V(.57f, .56f, .56f)),
                (V(.35f, -.38f, -.28f), V(.66f, .59f, .63f))
            })
            {
                var puff = Shape(beat.dust, "Puff", PrimitiveType.Sphere, at, size, cloud);
                puff.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            beat.dust.gameObject.SetActive(false);
            beat.closeShot = Shot(beat.transform, "Skate ignition close shot", V(-4.87f, 2.92f, -6.62f), V(-1.55f, 1.10f, -.67f));
            beat.runShot = Shot(beat.transform, "Sidewalk run", V(2.16f, 4.54f, -8.4f), V(-4.15f, 1.20f, -.55f));
            beat.impactShot = Shot(beat.transform, "Barricade impact", V(-3.92f, 3.58f, -7.51f), V(-6.94f, 1.09f, -.67f));
            beat.speaker = beat.gameObject.AddComponent<AudioSource>();
            beat.speaker.playOnAwake = false;
            beat.ignition = Cue("Skate Charge");
            beat.glide = Cue("Rocket Glide");
            beat.brake = Cue("Wheel Scrape");
            beat.crash = Cue("Barrier Clatter");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path);
            AssetDatabase.SaveAssets();
        }

        static void FitSkates(RocketSkatesSequence beat, GameObject prefab)
        {
            var skin = beat.gordon.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Single(part => part.name == "shoes_Portal" || part.name == "shoes_PortalShape");
            var bake = new Mesh();
            skin.BakeMesh(bake);
            var vertices = bake.vertices.Select(vertex => beat.gordon.InverseTransformPoint(skin.transform.TransformPoint(vertex))).ToArray();
            Object.DestroyImmediate(bake);
            beat.trails = new Transform[2];
            beat.wheels = new Transform[8];
            for (int foot = 0; foot < 2; foot++)
            {
                var points = vertices.Where(vertex => foot == 0 ? vertex.x < 0 : vertex.x >= 0).ToArray();
                if (points.Length == 0) throw new InvalidOperationException("Could not fit a skate to the v005 shoe.");
                var low = points.Aggregate(Vector3.Min);
                var high = points.Aggregate(Vector3.Max);
                var midpoint = (low + high) * .5f;
                var skate = Place(prefab, beat.rider, V(midpoint.x, low.y, midpoint.z));
                skate.name = foot == 0 ? "Skate left" : "Skate right";
                skate.localScale = V((high.x - low.x + .025f) / .26f, 1, (high.z - low.z + .11f) / .56f);
                beat.trails[foot] = skate.Find("Glow trail");
                var wheels = skate.Find("Wheel pivots");
                for (int wheel = 0; wheel < 4; wheel++) beat.wheels[foot * 4 + wheel] = wheels.GetChild(wheel);
            }
        }

        static void MakeSkate(Transform root)
        {
            var deck = Material("Skate Deck", Gold);
            var black = Material("Skate Rubber", Ink);
            var rim = Material("Skate Trim", Metal);
            var mint = Material("Skate Glow", Mint, true);
            var blue = Material("Skate Spark", DeepMint, true);
            Shape(root, "Sole plate", PrimitiveType.Cube, V(0, .111f, 0), V(.26f, .047f, .56f), deck);
            Shape(root, "Heel cushion", PrimitiveType.Cube, V(0, .093f, -.153f), V(.257f, .034f, .249f), black);
            Shape(root, "Glow heel", PrimitiveType.Sphere, V(0, .092f, -.328f), V(.17f, .115f, .16f), mint);
            Shape(root, "Heel ring", PrimitiveType.Cylinder, V(0, .092f, -.265f), V(.18f, .054f, .18f), rim).Rotate(90, 0, 0);
            var wheels = Mark(root, "Wheel pivots", Vector3.zero);
            foreach (float x in new[] { -.116f, .116f })
                foreach (float z in new[] { -.177f, .177f })
                {
                    var pivot = Mark(wheels, "Wheel", V(x, .054f, z));
                    Shape(pivot, "Wheel rubber", PrimitiveType.Cylinder, Vector3.zero, V(.109f, .029f, .109f), black).Rotate(0, 0, 90);
                    Shape(pivot, "Wheel hub", PrimitiveType.Cylinder, Vector3.zero, V(.053f, .032f, .053f), deck).Rotate(0, 0, 90);
                }
            var trail = Mark(root, "Glow trail", V(0, .093f, -.349f));
            Shape(trail, "Outer glow", PrimitiveType.Sphere, V(0, 0, -.205f), V(.19f, .126f, .55f), blue);
            Shape(trail, "Bright center", PrimitiveType.Sphere, V(0, 0, -.091f), V(.121f, .084f, .316f), mint);
            Shape(trail, "Trailing spark", PrimitiveType.Sphere, V(.015f, .012f, -.603f), V(.058f, .055f, .16f), mint);
            trail.gameObject.SetActive(false);
        }

        static void MakeBarrier(Transform root)
        {
            var frame = Material("Barrier Frame", Ink);
            var reflective = Material("Barrier Reflector", Gold);
            var offWhite = Material("Barrier Stripe", Pale);
            foreach (float x in new[] { -.61f, .61f })
            {
                Shape(root, "Foot", PrimitiveType.Cube, V(x, -.012f, 0), V(.29f, .08f, .67f), frame);
                Shape(root, "Upright", PrimitiveType.Cube, V(x, .385f, 0), V(.11f, .80f, .15f), frame);
            }
            Shape(root, "Warning board", PrimitiveType.Cube, V(0, .59f, 0), V(1.63f, .37f, .13f), reflective);
            for (int i = 0; i < 5; i++)
                Shape(root, "Reflective chevron", PrimitiveType.Cube, V(-.645f + i * .32f, .59f, -.070f),
                    V(.105f, .369f, .025f), offWhite).Rotate(0, 0, -28);
            Shape(root, "Lower brace", PrimitiveType.Cube, V(0, .265f, 0), V(1.34f, .114f, .10f), frame);
        }

        static void MakeCone(Transform root)
        {
            var orange = Material("Cone Orange", Tint(0xE7793C));
            var baseMat = Material("Cone Foot", Ink);
            var stripe = Material("Cone Reflector", Pale);
            Shape(root, "Base", PrimitiveType.Cube, V(0, .037f, 0), V(.376f, .075f, .376f), baseMat);
            var outline = new (float y, float radius)[] { (.061f, .149f), (.217f, .106f), (.305f, .084f), (.444f, .047f) };
            const int sides = 12;
            var points = outline.SelectMany(ring => Enumerable.Range(0, sides).Select(i =>
                V(ring.radius * Mathf.Cos(i * 2 * Mathf.PI / sides), ring.y, ring.radius * Mathf.Sin(i * 2 * Mathf.PI / sides)))).ToArray();
            var mesh = new Mesh { name = "Traffic Cone", vertices = points, subMeshCount = 2 };
            for (int surface = 0; surface < 2; surface++)
            {
                var faces = new System.Collections.Generic.List<int>();
                for (int r = 0; r < outline.Length - 1; r++)
                    if ((r == 1) == (surface == 1))
                        for (int i = 0; i < sides; i++)
                        {
                            int a = r * sides + i;
                            int b = r * sides + (i + 1) % sides;
                            faces.AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
                        }
                mesh.SetTriangles(faces, surface);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, $"{Art}/Traffic Cone Mesh.asset");
            var shell = Mark(root, "Cone shell", Vector3.zero);
            shell.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            shell.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { orange, stripe };
            Shape(root, "Cone top", PrimitiveType.Cylinder, V(0, .442f, 0), V(.094f, .009f, .094f), orange);
        }

        static GameObject Prefab(string name, Action<Transform> build)
        {
            string path = $"{Art}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject(name);
            build(root.transform);
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return asset != null ? asset : throw new IOException("Could not save " + path);
        }

        static Transform Place(GameObject prefab, Transform parent, Vector3 position)
        {
            var result = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            result.transform.localPosition = position;
            return result.transform;
        }

        static Transform Shape(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(shape);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (material.shader.name.EndsWith("/Unlit", StringComparison.Ordinal)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
        }

        static Material Material(string name, Color tint, bool unlit = false)
        {
            string path = $"{Art}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")) { color = tint };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Transform Shot(Transform parent, string name, Vector3 at, Vector3 toward)
        {
            var view = Mark(parent, name, at);
            view.LookAt(toward);
            return view;
        }

        static Transform Mark(Transform parent, string name, Vector3 at)
        {
            var mark = new GameObject(name).transform;
            mark.SetParent(parent, false);
            mark.localPosition = at;
            return mark;
        }

        static AudioClip Cue(string title) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Art}/{title}.wav") ?? throw new FileNotFoundException("Missing cue " + title);

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Color Tint(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
