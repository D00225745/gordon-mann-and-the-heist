using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GordonMann.EditorTools
{
    public static class PortalJarSetup
    {
        const string Art = "Assets/Game/Blockout/Portal Jar";
        const string Scenes = "Assets/Game/Blockout/Scenes";

        public static void Install()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Gordon Mann v005 Portal.fbx");
            var impact = Sound("Jar Impact");
            var opening = Sound("Portal Open");
            var slide = Sound("Portal Slide");
            if (model == null || impact == null || opening == null || slide == null)
                throw new InvalidOperationException("The v005 export and Portal Jar audio must import before setup.");
            var poses = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (!poses.Any(r => r.sharedMesh.blendShapeCount >= 3))
                throw new InvalidOperationException("The exported rig poses were missing.");
            foreach (string sceneName in new[] { "Choice_1", "Choice_2" })
            {
                var scene = EditorSceneManager.OpenScene($"{Scenes}/{sceneName}.unity", OpenSceneMode.Single);
                bool outside = sceneName == "Choice_1";
                var blockout = Root(scene, "Vault Job Blockout").transform;
                var prior = scene.GetRootGameObjects().FirstOrDefault(o => o.name == "Portal Jar Sequence");
                if (prior != null) Object.DestroyImmediate(prior);
                var sequence = new GameObject("Portal Jar Sequence").AddComponent<PortalJarSequence>();
                var stand = Root(scene, "Gordon");
                sequence.gordon = CreateActor(model, stand);
                if (stand.activeSelf) stand.SetActive(false);
                var director = Root(scene, "SceneDirector").GetComponent<SceneDirector>();
                var canvas = Root(scene, "Canvas").transform;
                sequence.heading = canvas.Find("Heading").gameObject;
                sequence.narration = canvas.Find("Narration").gameObject;
                sequence.speaker = sequence.gameObject.AddComponent<AudioSource>();
                sequence.speaker.playOnAwake = false;
                sequence.speaker.spatialBlend = 0;
                sequence.jarHit = impact;
                sequence.portalOpen = opening;
                sequence.slide = slide;

                if (outside)
                {
                    sequence.jar = blockout.Find("Props/Portal Jar");
                    var preview = blockout.Find("Props/Portal Preview (enable for shot)");
                    if (preview != null) preview.gameObject.SetActive(false);
                    var center = new Vector3(.53f, 1.08f, .88f);
                    sequence.portal = MakePortal(sequence.transform, center, -38);
                    sequence.slideEnd = Marker(sequence.transform, "Exterior Portal Center", new Vector3(.52f, 0, .90f));
                    sequence.cutCamera = Marker(sequence.transform, "Portal Jar Close Shot", new Vector3(2, 3.05f, -7.8f));
                    sequence.cutCamera.LookAt(new Vector3(-.76f, 1.31f, .5f));
                    director.choicePanel.portalJar = sequence;
                }
                else
                {
                    var center = new Vector3(-4.65f, 1.06f, .14f);
                    sequence.portal = MakePortal(sequence.transform, center, -65);
                    sequence.slideEnd = Marker(sequence.transform, "Lobby Portal Exit", new Vector3(center.x, 0, center.z));
                    director.lobbyArrival = sequence;
                }
                sequence.gordon.SetParent(sequence.transform, true);
                sequence.portal.gameObject.SetActive(false);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Portal Jar installed with v005 poses, departure and arrival audio, and lobby handoff.");
        }

        static GameObject Root(Scene scene, string name) => scene.GetRootGameObjects().Single(o => o.name == name);
        static AudioClip Sound(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Art}/{name}.wav");

        internal static Transform CreateActor(GameObject model, GameObject placeholder)
        {
            var actor = new GameObject("Gordon Mann v005");
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(model);
            copy.transform.SetParent(actor.transform, false);
            var renderers = copy.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            if (!(bounds.size.y > 0.001f)) throw new InvalidOperationException("v005 had no character height.");
            copy.transform.localScale *= 2f / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            copy.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            actor.transform.SetPositionAndRotation(new Vector3(placeholder.transform.position.x, 0, placeholder.transform.position.z),
                placeholder.transform.rotation);
            var palette = new (string name, Color32 color)[]
            {
                ("polo_shirt", new Color32(202, 221, 220, 255)),
                ("sweater_vest", new Color32(25, 74, 78, 255)),
                ("shorts", new Color32(49, 77, 117, 255)),
                ("socks", new Color32(153, 179, 189, 255)),
                ("shoes", new Color32(87, 48, 33, 255)),
                ("hair", new Color32(83, 48, 33, 255)),
                ("eyebrows", new Color32(83, 48, 33, 255)),
                ("belt", new Color32(87, 55, 41, 255)),
                ("rolex", new Color32(82, 114, 119, 255)),
                ("eyedot", new Color32(35, 28, 26, 255)),
                ("eye", new Color32(239, 230, 201, 255))
            };
            foreach (var renderer in renderers)
            {
                var entry = palette.FirstOrDefault(p => renderer.name.StartsWith(p.name, StringComparison.OrdinalIgnoreCase));
                var material = Matte(entry.name ?? "Skin", entry.name != null ? entry.color : new Color32(195, 122, 90, 255));
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
            }
            return actor.transform;
        }

        static Transform MakePortal(Transform parent, Vector3 center, float yaw)
        {
            var portal = Marker(parent, "Portal Aperture", center);
            portal.localRotation = Quaternion.Euler(0, yaw, 0);
            Disc(portal, "Outer Glow", Vector3.zero, 2.27f, new Color(.06f, .8f, .62f));
            Disc(portal, "Bright Rim", new Vector3(0, 0, -.022f), 2.07f, new Color(.45f, 1f, .89f));
            Disc(portal, "Deep Center", new Vector3(0, 0, -.043f), 1.82f, new Color(.065f, .39f, .46f));
            Disc(portal, "Inner Current", new Vector3(.09f, .08f, -.064f), 1.41f, new Color(.065f, .24f, .37f));
            return portal;
        }

        static void Disc(Transform parent, string name, Vector3 position, float diameter, Color color)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = position;
            disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
            disc.transform.localScale = new Vector3(diameter, .008f, diameter);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.sharedMaterial = Matte(name, color, true);
        }

        static Material Matte(string name, Color tint, bool glowing = false)
        {
            var path = $"{Art}/Portal {name}.mat";
            var exists = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (exists != null) return exists;
            var shader = Shader.Find(glowing ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            var material = new Material(shader) { color = tint };
            if (!glowing) material.SetFloat("_Smoothness", .24f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Transform Marker(Transform parent, string name, Vector3 at)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.localPosition = at;
            return marker;
        }
    }
}
