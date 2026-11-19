using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GordonMann.EditorTools
{
    public static class LaserFailureSetup
    {
        const string Art = "Assets/Game/Blockout/Laser Failures";
        const string Scenes = "Assets/Game/Blockout/Scenes";
        static readonly Color Ink = Tint(0x222E41), Slate = Tint(0x63778C), Gold = Tint(0xC88539);
        static readonly Color Pink = Tint(0xFF4C74), Mint = Tint(0x84CAC5), Cream = Tint(0xF5DEB2);

        public static void Install()
        {
            var standing = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Gordon Mann v005 Laser Failures.fbx");
            var seated = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Blockout/Lobby Failures/Gordon Mann v005 Lobby Failures.fbx");
            if (standing == null || seated == null)
                throw new InvalidOperationException("Missing the seated or standing pose export from Gordon v005.");
            var cart = StartScene(LaserFailureSequence.Gag.Cart, seated, "Mail Cart Rattle", "Cart Jolt");
            if (cart != null) InstallCart(cart);
            var unplug = StartScene(LaserFailureSequence.Gag.Unplug, standing, "Panel Reach", "Lights Off");
            if (unplug != null) InstallUnplug(unplug);
            var selfie = StartScene(LaserFailureSequence.Gag.Selfie, standing, "Phone Ready", "Camera Flash");
            if (selfie != null) InstallSelfie(selfie);
            var frozen = StartScene(LaserFailureSequence.Gag.Frozen, standing, "Time Expired", "Laser Sweep");
            if (frozen != null) InstallFrozen(frozen);
            AssetDatabase.SaveAssets();
        }

        static LaserFailureSequence StartScene(LaserFailureSequence.Gag kind, GameObject model, string first, string hit)
        {
            var scene = EditorSceneManager.OpenScene($"{Scenes}/Fail_{kind}.unity");
            var director = Root(scene, "SceneDirector").GetComponent<SceneDirector>();
            if (director.laserFailure != null) return null;
            var beat = new GameObject($"{kind} Sequence").AddComponent<LaserFailureSequence>();
            director.laserFailure = beat;
            beat.gag = kind;
            var old = Root(scene, "Gordon");
            beat.gordon = PortalJarSetup.CreateActor(model, old);
            beat.gordon.SetParent(beat.transform, true);
            old.SetActive(false);
            beat.speaker = beat.gameObject.AddComponent<AudioSource>();
            beat.speaker.playOnAwake = false;
            beat.startCue = Cue(first);
            beat.eventCue = Cue(hit);
            beat.alarmCue = Cue("Laser Warning");
            var props = Props(beat);
            beat.beams = props.Cast<Transform>().Where(obj => obj.name == "Laser Beam").OrderBy(obj => obj.position.z).ToArray();
            if (beat.beams.Length != 5) throw new InvalidOperationException("The hallway needs all five beams.");
            foreach (var beam in beat.beams)
            {
                var renderer = beam.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Material("Beam Core", Pink, true);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            beat.warning = Warning(props);
            return beat;
        }

        static void InstallCart(LaserFailureSequence beat)
        {
            var props = Props(beat);
            props.Find("Mail Cart").gameObject.SetActive(false);
            beat.prop = Place(Prefab("Mail Trolley", CreateCart), props, V(-.04f, 0, -3.71f));
            beat.wheels = beat.prop.Find("Wheels").Cast<Transform>().ToArray();
            beat.actionMark = Mark(beat.transform, "First beam stop", V(-.04f, 0, -1.18f));
            beat.gordon.SetParent(beat.prop, false);
            beat.gordon.SetLocalPositionAndRotation(V(0, .078f, .032f), Quaternion.identity);
            beat.closeShot = Shot(beat.transform, "Trolley approach", V(6.64f, 4.24f, -8.0f), V(-.25f, 1.09f, -.77f));
            Save(beat);
        }

        static void InstallUnplug(LaserFailureSequence beat)
        {
            var props = Props(beat);
            props.Find("Lighting Power Box").gameObject.SetActive(false);
            props.Find("Disconnected Plug").gameObject.SetActive(false);
            var panel = Place(Prefab("Hall Lighting Panel", CreatePanel), props, V(-3.26f, .707f, -1.724f));
            beat.prop = panel.Find("Lamp plug");
            beat.actionMark = Mark(beat.transform, "Unseated lamp plug", V(-2.84f, .71f, -1.724f));
            beat.actionMark.rotation = Quaternion.Euler(0, 0, -8);
            beat.cableAnchor = Mark(panel, "Cable guide", V(.085f, -.40f, -.28f));
            beat.cable = Mark(props, "Lamp cable", Vector3.zero).gameObject.AddComponent<LineRenderer>();
            beat.cable.useWorldSpace = true;
            beat.cable.positionCount = 13;
            beat.cable.widthMultiplier = .036f;
            beat.cable.numCapVertices = 4;
            beat.cable.sharedMaterial = Material("Rubber Cable", Ink, true);

            var normalLights = beat.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Light>()).Where(light => light.enabled && light.type == LightType.Directional);
            beat.roomLights = normalLights.ToArray();
            if (beat.roomLights.Length == 0) throw new InvalidOperationException("Missing hallway work lights.");
            var strips = Mark(props, "Work light fixtures", Vector3.zero);
            for (int i = 0; i < 4; i++)
                Shape(strips, "Work light strip", PrimitiveType.Cube, V(-3.305f, 2.95f, -2.14f + i * 2.30f),
                    V(.10f, .104f, 1.23f), Material("Work Lights", Cream, true));
            beat.workLights = strips.gameObject;
            AddLight(props, "Emergency floor fill", V(-2.02f, 2.0f, -2.08f), Mint, 4.1f, 1.36f);
            beat.gordon.SetPositionAndRotation(V(-2.62f, 0, -2.071f), Quaternion.Euler(0, -90, 0));
            beat.closeShot = Shot(beat.transform, "At the lighting panel", V(5.27f, 4.97f, .847f), V(-2.80f, 1.14f, -1.81f));
            beat.wideShot = Shot(beat.transform, "The beams stay on", V(5.31f, 3.66f, -6.72f), V(-.87f, 1.05f, .58f));
            Save(beat);
        }

        static void InstallSelfie(LaserFailureSequence beat)
        {
            var props = Props(beat);
            props.Find("Phone").gameObject.SetActive(false);
            props.Find("Flash").gameObject.SetActive(false);
            beat.gordon.SetPositionAndRotation(V(-.70f, 0, -2.05f), Quaternion.Euler(0, 163, 0));
            beat.prop = Place(Prefab("Pocket Camera Phone", CreatePhone), beat.gordon, V(.375f, 1.057f, .347f));
            beat.prop.localRotation = Quaternion.Euler(15, 0, -13);
            beat.actionMark = Mark(beat.gordon, "Raised phone", V(.355f, 1.600f, .458f));
            beat.actionMark.localRotation = Quaternion.Euler(4, -11, -11);
            beat.flash = AddLight(beat.prop, "Brief phone flash", V(0, .11f, -.18f), Tint(0xF1F7FF), 2.9f, 4.9f);
            beat.flash.enabled = false;
            beat.closeShot = Shot(beat.transform, "Selfie framing", V(3.35f, 2.90f, -7.56f), V(-.58f, 1.25f, -.94f));
            Save(beat);
        }

        static void InstallFrozen(LaserFailureSequence beat)
        {
            var props = Props(beat);
            props.Find("Timeout Indicator").gameObject.SetActive(false);
            var indicator = Mark(props, "Expired timer", V(.03f, 2.41f, -.62f));
            Shape(indicator, "Timer housing", PrimitiveType.Cube, Vector3.zero, V(1.30f, .37f, .20f), Material("Timer Housing", Ink));
            Text(indicator, "00:00", V(0, 0, -.105f), .046f, Pink);
            beat.gordon.SetPositionAndRotation(V(-.64f, 0, -2.86f), Quaternion.Euler(0, 111, 0));
            var beam = beat.beams[0];
            beat.actionMark = Mark(beat.transform, "Sweep stops at Gordon", V(beam.position.x, beam.position.y, -2.25f));
            beat.closeShot = Shot(beat.transform, "The clock ran out", V(4.48f, 3.1f, -6.46f), V(-.52f, 1.18f, -1.58f));
            MountScanner(beat);
            Save(beat);
        }

        static void MountScanner(LaserFailureSequence beat)
        {
            var props = Props(beat);
            var leadingBeam = beat.beams[0];
            var heads = props.Cast<Transform>().Where(head =>
                (head.name is "Laser Emitter" or "Laser Receiver") && Mathf.Abs(head.position.z - leadingBeam.position.z) < .01f).ToArray();
            if (heads.Length != 2) throw new InvalidOperationException("The scanning beam must have two heads.");
            beat.scannerRig = Mark(props, "Traveling laser carriage", leadingBeam.position);
            leadingBeam.SetParent(beat.scannerRig, true);
            foreach (var head in heads) head.SetParent(beat.scannerRig, true);
            var trim = Material("Scanning rail", Slate);
            foreach (float x in new[] { -3.37f, 3.37f })
            {
                Shape(props, "Scanning travel rail", PrimitiveType.Cube, V(x, leadingBeam.position.y, -1.45f), V(.108f, .131f, 1.92f), trim);
                foreach (float z in new[] { -.59f, -2.32f })
                    Shape(props, "Rail support", PrimitiveType.Cube, V(x, .48f, z), V(.105f, .96f, .114f), trim);
            }
        }

        static GameObject Warning(Transform props)
        {
            var group = Mark(props, "Alarm lights", Vector3.zero);
            var baseMat = Material("Siren Mount", Ink);
            var lens = Material("Siren Lens", Pink, true);
            foreach (float x in new[] { -3.06f, 3.06f })
            {
                Shape(props, "Siren mount", PrimitiveType.Cube, V(x, 2.42f, -.72f), V(.34f, .26f, .15f), baseMat);
                Shape(group, "Siren lens", PrimitiveType.Sphere, V(x, 2.42f, -.83f), V(.217f, .145f, .09f), lens);
                AddLight(group, "Siren wash", V(x, 2.44f, -1.04f), Pink, 3.5f, 1.32f);
            }
            group.gameObject.SetActive(false);
            return group.gameObject;
        }

        static void CreateCart(Transform root)
        {
            var frame = Material("Trolley Frame", Slate);
            var orange = Material("Trolley Orange", Gold);
            var tire = Material("Trolley Tires", Ink);
            Shape(root, "Tray deck", PrimitiveType.Cube, V(0, .406f, .004f), V(1.02f, .11f, 1.13f), orange);
            Shape(root, "Seat cushion", PrimitiveType.Cube, V(0, .477f, -.034f), V(.60f, .035f, .55f), tire);
            for (int i = 0; i < 4; i++)
            {
                float sideX = i % 2 == 0 ? -.44f : .44f;
                float z = i < 2 ? -.43f : .42f;
                Shape(root, "Basket corner", PrimitiveType.Cube, V(sideX, .656f, z), V(.046f, .465f, .047f), frame);
            }
            foreach (float x in new[] { -.45f, .45f })
                Shape(root, "Basket side rail", PrimitiveType.Cube, V(x, .78f, 0), V(.044f, .047f, .90f), frame);
            Shape(root, "Push rail", PrimitiveType.Cube, V(0, .89f, -.44f), V(.960f, .057f, .059f), frame);
            Shape(root, "Mail sack", PrimitiveType.Sphere, V(.32f, .695f, -.346f), V(.31f, .47f, .28f), Material("Sack Canvas", Cream));
            var wheels = Mark(root, "Wheels", Vector3.zero);
            foreach (float x in new[] { -.42f, .42f })
                foreach (float z in new[] { -.385f, .385f })
                {
                    var pivot = Mark(wheels, "Axle", V(x, .13f, z));
                    Shape(pivot, "Caster", PrimitiveType.Cylinder, Vector3.zero, V(.26f, .065f, .26f), tire).Rotate(0, 0, 90);
                    Shape(pivot, "Hubcap", PrimitiveType.Cylinder, Vector3.zero, V(.114f, .069f, .114f), frame).Rotate(0, 0, 90);
                }
        }

        static void CreatePanel(Transform root)
        {
            var gray = Material("Lighting Panel Steel", Slate);
            var ink = Material("Panel Seal", Ink);
            Shape(root, "Enclosure", PrimitiveType.Cube, Vector3.zero, V(.29f, .59f, .55f), gray);
            Shape(root, "Seal", PrimitiveType.Cube, V(.15f, 0, 0), V(.022f, .52f, .49f), ink);
            Shape(root, "Faceplate", PrimitiveType.Cube, V(.161f, 0, 0), V(.030f, .442f, .422f), gray);
            var name = Text(root, "LIGHTS", V(.179f, .157f, 0), .030f, Cream);
            name.rotation = Quaternion.Euler(0, -90, 0);
            var plug = Mark(root, "Lamp plug", V(.237f, -.003f, 0));
            Shape(plug, "Sleeve", PrimitiveType.Cylinder, V(-.054f, 0, 0), V(.205f, .085f, .205f), ink).Rotate(0, 0, 90);
            Shape(plug, "Molded grip", PrimitiveType.Cube, V(.043f, 0, 0), V(.23f, .152f, .196f), Material("Lamp Plug", Gold));
            for (int i = 0; i < 3; i++)
                Shape(plug, "Grip rib", PrimitiveType.Cube, V(.02f + i * .049f, .081f, 0), V(.023f, .020f, .158f), ink);
        }

        static void CreatePhone(Transform root)
        {
            var dark = Material("Phone Glass", Ink);
            var caseMat = Material("Phone Case", Tint(0x4F8E88));
            Shape(root, "Case", PrimitiveType.Cube, Vector3.zero, V(.226f, .37f, .068f), caseMat);
            Shape(root, "Screen", PrimitiveType.Cube, V(0, 0, -.037f), V(.194f, .327f, .012f), dark);
            Shape(root, "Preview", PrimitiveType.Cube, V(0, .016f, -.045f), V(.163f, .228f, .005f),
                Material("Phone Preview", Tint(0x70849F), true));
            Shape(root, "Shutter button", PrimitiveType.Cylinder, V(0, -.128f, -.047f), V(.045f, .003f, .045f),
                Material("Phone Shutter", Cream, true)).Rotate(90, 0, 0);
            Shape(root, "Lens housing", PrimitiveType.Cube, V(-.062f, .116f, .038f), V(.075f, .105f, .023f), dark);
            Shape(root, "Lens", PrimitiveType.Cylinder, V(-.061f, .133f, .052f), V(.045f, .006f, .045f),
                Material("Phone Lens", Tint(0x405D78))).Rotate(90, 0, 0);
        }

        static Light AddLight(Transform parent, string title, Vector3 at, Color color, float range, float strength)
        {
            var light = Mark(parent, title, at).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = strength;
            return light;
        }

        static Transform Text(Transform parent, string label, Vector3 at, float size, Color tint)
        {
            var text = Mark(parent, label, at).gameObject.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 48;
            text.characterSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = tint;
            text.text = label;
            return text.transform;
        }

        static GameObject Prefab(string title, Action<Transform> build)
        {
            string path = $"{Art}/{title}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject(title);
            build(root.transform);
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return asset != null ? asset : throw new IOException("Could not save " + path);
        }

        static Transform Place(GameObject asset, Transform parent, Vector3 at)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.transform.localPosition = at;
            return instance.transform;
        }

        static Transform Shape(Transform parent, string name, PrimitiveType type, Vector3 at, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = at;
            part.transform.localScale = size;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (material.shader.name.EndsWith("/Unlit", StringComparison.Ordinal))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part.transform;
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

        static Transform Shot(Transform parent, string title, Vector3 eye, Vector3 look)
        {
            var mark = Mark(parent, title, eye);
            mark.LookAt(look);
            return mark;
        }

        static Transform Mark(Transform parent, string title, Vector3 at)
        {
            var mark = new GameObject(title).transform;
            mark.SetParent(parent, false);
            mark.localPosition = at;
            return mark;
        }

        static AudioClip Cue(string title) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Art}/{title}.wav") ?? throw new FileNotFoundException("Missing cue " + title);

        static void Save(LaserFailureSequence beat)
        {
            var scene = beat.gameObject.scene;
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path);
        }

        static GameObject Root(Scene scene, string title) => scene.GetRootGameObjects().Single(obj => obj.name == title);
        static Transform Props(LaserFailureSequence beat) => Root(beat.gameObject.scene, "Vault Job Blockout").transform.Find("Props");
        static Color Tint(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    }
}
