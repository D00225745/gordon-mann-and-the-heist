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
    public static class SecurityFailureSetup
    {
        const string Art = "Assets/Game/Blockout/Security Failures";
        const string Scenes = "Assets/Game/Blockout/Scenes";
        static readonly Color Ink = Tint(0x1A2939), Metal = Tint(0x708995), Pale = Tint(0xF4E3BA);
        static readonly Color Amber = Tint(0xFFCF64), Scarlet = Tint(0xFA4464), Mint = Tint(0x62DEAF);

        public static void Install()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Gordon Mann v005 Security Failures.fbx");
            if (model == null || !model.GetComponentsInChildren<SkinnedMeshRenderer>().Any(mesh => mesh.sharedMesh.blendShapeCount == 6))
                throw new InvalidOperationException("Missing the six security poses from Gordon Mann v005.");
            var pin = StartScene(SecurityFailureSequence.Gag.PIN, model, "Key Press", "PIN Rejected");
            if (pin != null) StagePIN(pin);
            var knock = StartScene(SecurityFailureSequence.Gag.Knock, model, "Steel Knock", "Security Hinge");
            if (knock != null) StageKnock(knock);
            AssetDatabase.SaveAssets();
        }

        static SecurityFailureSequence StartScene(SecurityFailureSequence.Gag kind, GameObject model, string tap, string answer)
        {
            var scene = EditorSceneManager.OpenScene($"{Scenes}/Fail_{kind}.unity");
            var director = Root(scene, "SceneDirector").GetComponent<SceneDirector>();
            if (director.securityFailure != null) return null;
            var beat = new GameObject($"{kind} Sequence").AddComponent<SecurityFailureSequence>();
            director.securityFailure = beat;
            var placeholder = Root(scene, "Gordon");
            beat.gordon = PortalJarSetup.CreateActor(model, placeholder);
            beat.gordon.SetParent(beat.transform, true);
            placeholder.SetActive(false);
            beat.gag = kind;
            beat.speaker = beat.gameObject.AddComponent<AudioSource>();
            beat.speaker.playOnAwake = false;
            beat.tap = Cue(tap);
            beat.answer = Cue(answer);
            return beat;
        }

        static void StagePIN(SecurityFailureSequence beat)
        {
            var scene = beat.gameObject.scene;
            var props = Root(scene, "Vault Job Blockout").transform.Find("Props");
            beat.keypad = ReplaceKeypad(props);
            beat.closeShot = Shot(beat.transform, "PIN close shot", V(1.37f, 2.79f, .91f), V(-2.55f, 1.38f, 3.61f));
            beat.gordon.SetPositionAndRotation(V(-2.710f, 0, 3.366f), Quaternion.identity);
            Save(scene);
        }

        static void StageKnock(SecurityFailureSequence beat)
        {
            var scene = beat.gameObject.scene;
            var props = Root(scene, "Vault Job Blockout").transform.Find("Props");
            ReplaceKeypad(props);
            beat.door = props.Find("Security Door Pivot");
            beat.door.localRotation = Quaternion.identity;
            var guard = props.Find("Door Guard");
            guard.SetPositionAndRotation(V(.44f, 0, 4.64f), Quaternion.Euler(0, 11, 0));
            var dressed = LullabiesSetup.RigGuard(guard);
            beat.guardHead = dressed.head;
            var bronze = Prefab("Guard Coffee Mug", MakeMug);
            beat.mug = Mark(dressed.torso, "Guard cup grip", V(.134f, .57f, -.35f));
            Place(bronze, beat.mug, Vector3.zero);
            beat.guardHand = Shape(beat.mug, "Glove", PrimitiveType.Sphere, V(.084f, -.024f, .066f),
                V(.156f, .155f, .143f), Material("Guard Glove", Ink));
            var sleeve = Material("Guard Sleeve", Tint(0x454F64));
            Shape(dressed.torso, "Upper sleeve", PrimitiveType.Cube, V(.313f, .435f, -.044f), V(.168f, .32f, .175f), sleeve)
                .Rotate(-12, 0, 5);
            var elbow = V(.313f, .273f, -.076f);
            beat.guardElbow = Mark(dressed.torso, "Guard elbow", elbow);
            Vector3 fromElbow = dressed.torso.InverseTransformPoint(beat.guardHand.position) - elbow;
            beat.guardForearm = Shape(dressed.torso, "Forearm sleeve", PrimitiveType.Cube,
                elbow + .5f * fromElbow, V(.155f, fromElbow.magnitude + .10f, .18f), sleeve);
            beat.guardForearm.localRotation = Quaternion.FromToRotation(Vector3.up, fromElbow);
            var recess = Material("Security Recess", Ink);
            Shape(props, "Doorway return", PrimitiveType.Cube, V(0, 1.65f, 6.05f), V(3.24f, 3.30f, .14f), recess);
            Shape(props, "Doorway threshold", PrimitiveType.Cube, V(0, -.043f, 4.90f), V(3.14f, .09f, 1.76f), Material("Door Threshold", Metal));
            var light = Mark(props, "Guard room fill", V(.54f, 2.65f, 4.96f)).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Tint(0xFFD18D);
            light.range = 2.9f;
            light.intensity = 1.4f;
            beat.gordon.SetPositionAndRotation(V(.27f, 0, 3.427f), Quaternion.identity);
            beat.recoilMark = Mark(beat.transform, "Awkward step back", V(.37f, 0, 2.95f));
            beat.recoilMark.rotation = Quaternion.Euler(0, 27, 0);
            beat.closeShot = Shot(beat.transform, "Knock close shot", V(3.94f, 3.05f, -.41f), V(.33f, 1.51f, 3.60f));
            beat.outcomeShot = Shot(beat.transform, "Guard reveal shot", V(2.83f, 2.75f, -.04f), V(.03f, 1.37f, 3.98f));
            beat.reaction = Cue("Guard Sip");
            Save(scene);
        }

        static SecurityKeypad ReplaceKeypad(Transform props)
        {
            if (props.Find("Security Keypad") is Transform existing)
                return existing.GetComponent<SecurityKeypad>();
            foreach (Transform child in props)
                if (child.name is "Keypad Button" or "Keypad Housing" or "Rejected PIN Stroke" or "Access Light")
                    child.gameObject.SetActive(false);
            var keypad = Prefab("Security Keypad", MakeKeypad);
            return Place(keypad, props, V(-2.40f, 1.393f, 3.98f)).GetComponent<SecurityKeypad>();
        }

        static void MakeKeypad(Transform root)
        {
            var dark = Material("Keypad Frame", Ink);
            var steel = Material("Keypad Trim", Metal);
            var keys = Material("Numbered Key", Tint(0xBEC7C9));
            Shape(root, "Cast housing", PrimitiveType.Cube, Vector3.zero, V(.66f, .91f, .174f), dark);
            Shape(root, "Faceplate", PrimitiveType.Cube, V(0, 0, -.091f), V(.603f, .855f, .031f), steel);
            Shape(root, "Display glass", PrimitiveType.Cube, V(-.023f, .315f, -.114f), V(.488f, .141f, .036f), dark);
            var display = Text(root, "Readout", "—", V(-.023f, .315f, -.134f), .0275f, Mint);
            var ready = Shape(root, "Ready LED", PrimitiveType.Sphere, V(.253f, .314f, -.122f),
                V(.040f, .040f, .029f), Material("Ready LED", Amber, true));
            var rejected = Shape(root, "Denied LED", PrimitiveType.Sphere, V(.253f, .314f, -.123f),
                V(.040f, .040f, .029f), Material("Denied LED", Scarlet, true));
            rejected.gameObject.SetActive(false);
            var keyAnchors = new Transform[12];
            var labels = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "×", "0", "✓" };
            for (int i = 0; i < labels.Length; i++)
            {
                var key = Mark(root, "Key " + labels[i], V((i % 3 - 1) * .1645f, .117f - (i / 3) * .151f, -.126f));
                var baseColor = i == 9 ? Material("Clear Key", Scarlet) : keys;
                Shape(key, "Keycap", PrimitiveType.Cube, Vector3.zero, V(.136f, .114f, .064f), baseColor);
                Text(key, "Legend", labels[i], V(0, 0, -.035f), .0205f, Ink);
                keyAnchors[i] = key;
            }
            var component = root.gameObject.AddComponent<SecurityKeypad>();
            var data = new SerializedObject(component);
            var array = data.FindProperty("entryKeys");
            array.arraySize = 4;
            for (int i = 0; i < array.arraySize; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = keyAnchors[i];
            data.FindProperty("display").objectReferenceValue = display;
            data.FindProperty("readyLamp").objectReferenceValue = ready.gameObject;
            data.FindProperty("rejectLamp").objectReferenceValue = rejected.gameObject;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void MakeMug(Transform root)
        {
            var gold = Material("Mug Glaze", Tint(0xC97D50));
            var clay = Material("Mug Rim", Pale);
            var drink = Material("Coffee", Tint(0x41291E));
            Shape(root, "Mug wall", PrimitiveType.Cylinder, Vector3.zero, V(.151f, .079f, .151f), gold);
            Shape(root, "Rim ceramic", PrimitiveType.Cylinder, V(0, .078f, 0), V(.153f, .011f, .153f), clay);
            Shape(root, "Coffee surface", PrimitiveType.Cylinder, V(0, .089f, 0), V(.122f, .0017f, .122f), drink);
            for (int i = 0; i < 7; i++)
            {
                float angle = (-88 + i * 29f) * Mathf.Deg2Rad;
                Shape(root, "Handle segment", PrimitiveType.Sphere, V(.071f + .048f * Mathf.Cos(angle), .038f * Mathf.Sin(angle), 0),
                    V(.039f, .034f, .035f), gold);
            }
        }

        static TextMesh Text(Transform parent, string name, string label, Vector3 position, float size, Color tint)
        {
            var text = Mark(parent, name, position).gameObject.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 44;
            text.fontStyle = FontStyle.Bold;
            text.characterSize = size;
            text.color = tint;
            text.text = label;
            return text;
        }

        static GameObject Prefab(string title, Action<Transform> build)
        {
            string file = $"{Art}/{title}.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            if (asset != null) return asset;
            var root = new GameObject(title);
            build(root.transform);
            asset = PrefabUtility.SaveAsPrefabAsset(root, file);
            Object.DestroyImmediate(root);
            return asset != null ? asset : throw new IOException("Could not save " + file);
        }

        static Transform Place(GameObject prefab, Transform parent, Vector3 at)
        {
            var transform = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)).transform;
            transform.localPosition = at;
            return transform;
        }

        static Transform Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(item.GetComponent<Collider>());
            return item.transform;
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

        static Transform Shot(Transform parent, string label, Vector3 position, Vector3 target)
        {
            var mark = Mark(parent, label, position);
            mark.LookAt(target);
            return mark;
        }

        static Transform Mark(Transform parent, string label, Vector3 position)
        {
            var mark = new GameObject(label).transform;
            mark.SetParent(parent, false);
            mark.localPosition = position;
            return mark;
        }

        static AudioClip Cue(string title) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Art}/{title}.wav") ?? throw new FileNotFoundException("Missing cue " + title);

        static void Save(Scene scene)
        {
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path);
        }

        static GameObject Root(Scene scene, string title) => scene.GetRootGameObjects().Single(obj => obj.name == title);
        static Color Tint(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    }
}
