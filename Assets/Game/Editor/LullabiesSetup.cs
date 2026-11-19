using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GordonMann.EditorTools
{
    public static class LullabiesSetup
    {
        const string Art = "Assets/Game/Blockout/Lullabies";
        const string Lobby = "Assets/Game/Blockout/Scenes/Choice_2.unity";

        public static void Install()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Gordon Mann v005 Lullabies.fbx");
            if (model == null || !model.GetComponentsInChildren<SkinnedMeshRenderer>()
                    .Any(r => r.sharedMesh.blendShapeCount == 8))
                throw new InvalidOperationException("The eight Lullabies pose targets were not imported.");
            var scene = EditorSceneManager.OpenScene(Lobby);
            var roots = scene.GetRootGameObjects();
            var director = roots.Select(g => g.GetComponent<SceneDirector>()).Single(d => d != null);
            var arrival = director.lobbyArrival;
            var player = roots.Single(g => g.name == "Vault Job Blockout").transform.Find("Props");
            var old = roots.SingleOrDefault(g => g.name == "Lullabies Sequence");
            if (old != null) Object.DestroyImmediate(old);
            var beat = new GameObject("Lullabies Sequence").AddComponent<LullabiesSequence>();
            beat.heading = arrival.heading;
            beat.narration = arrival.narration;
            beat.gordon = PortalJarSetup.CreateActor(model, arrival.gordon.gameObject);
            beat.gordon.SetParent(beat.transform, true);
            beat.gordon.gameObject.SetActive(false);

            beat.desk = RigGuard(player.Find("Reception Guard"));
            beat.patrol = RigGuard(player.Find("Patrol Guard"));
            beat.patrol.torso.parent.localRotation = Quaternion.Euler(0, -18, 0);
            beat.radioLight = MakeStatusLight(player);
            var radio = beat.radioLight.parent.gameObject;
            if (!radio.TryGetComponent<AudioSource>(out var source)) source = radio.AddComponent<AudioSource>();
            beat.radioAudio = source;
            beat.radioAudio.playOnAwake = false;
            beat.radioAudio.spatialBlend = 0;
            beat.radioAudio.volume = .53f;
            beat.radioAudio.loop = true;
            beat.effects = beat.gameObject.AddComponent<AudioSource>();
            beat.effects.playOnAwake = false;
            beat.buttonClick = RequireAudio("Player Click");
            beat.lullaby = RequireAudio("Music Box Lullaby");
            beat.cloth = RequireAudio("Guard Slump");
            beat.footfall = RequireAudio("Soft Footstep");
            beat.radioShot = Shot(beat.transform, "Radio Shot", new Vector3(2.2f, 3.9f, -7.4f), new Vector3(-2.4f, 1.12f, .02f));
            beat.guardsShot = Shot(beat.transform, "Guard Reaction Shot", new Vector3(3.25f, 4.6f, -6.1f), new Vector3(-1.58f, 1.37f, 4.25f));
            beat.routeShot = Shot(beat.transform, "Sneak Route Wide", new Vector3(7.9f, 5.2f, -10.5f), new Vector3(-.96f, 1.38f, 2.7f));
            beat.doorMark = Marker(beat.transform, "Security Hall Threshold", new Vector3(-.08f, 0, 5.89f));
            BuildExit(player);
            beat.portalArrival = arrival;
            director.choicePanel.lullabies = beat;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the Lullabies lobby.");
            var security = EditorSceneManager.OpenScene("Assets/Game/Blockout/Scenes/Choice_3.unity");
            var securityRoots = security.GetRootGameObjects();
            var placeholder = securityRoots.Single(g => g.name == "Gordon");
            var previousActor = securityRoots.SingleOrDefault(g => g.name == "Gordon Mann v005");
            if (previousActor != null) Object.DestroyImmediate(previousActor);
            var actor = PortalJarSetup.CreateActor(model, placeholder);
            actor.rotation = Quaternion.Euler(0, 32, 0);
            placeholder.SetActive(false);
            EditorSceneManager.MarkSceneDirty(security);
            if (!EditorSceneManager.SaveScene(security)) throw new InvalidOperationException("Could not save Gordon's security-door staging.");
            AssetDatabase.SaveAssets();
            Debug.Log("Lullabies installed in the blockout lobby.");
        }

        static AudioClip RequireAudio(string name) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Art}/{name}.wav") ??
            throw new InvalidOperationException("Missing Lullabies cue " + name);

        internal static LullabiesSequence.Guard RigGuard(Transform original)
        {
            var torso = original.Find("Lullabies Torso Pivot");
            if (torso != null)
                return new LullabiesSequence.Guard
                {
                    torso = torso, head = torso.Find("Head Pivot"),
                    openEyes = torso.Find("Head Pivot/Open Eyes").gameObject,
                    shutEyes = torso.Find("Head Pivot/Closed Eyes").gameObject
                };
            var body = original.Find("Body");
            var head = original.Find("Head");
            var cap = original.Find("Cap");
            torso = Marker(original, "Lullabies Torso Pivot", new Vector3(0, .69f, 0));
            body.SetParent(torso, true);
            var swivel = Marker(torso, "Head Pivot", new Vector3(0, .72f, 0));
            head.SetParent(swivel, true);
            cap.SetParent(swivel, true);
            var open = Marker(swivel, "Open Eyes", new Vector3(0, .172f, -.182f));
            var shut = Marker(swivel, "Closed Eyes", new Vector3(0, .172f, -.182f));
            var ink = Material("Guard Eyes", new Color(.024f, .031f, .048f));
            foreach (float x in new[] { -.059f, .059f })
            {
                Block(open, "Eye", new Vector3(x, 0, 0), new Vector3(.036f, .046f, .024f), ink);
                Block(shut, "Lid", new Vector3(x, 0, 0), new Vector3(.057f, .012f, .024f), ink);
            }
            shut.gameObject.SetActive(false);
            return new LullabiesSequence.Guard { torso = torso, head = swivel, openEyes = open.gameObject, shutEyes = shut.gameObject };
        }

        static Transform MakeStatusLight(Transform props)
        {
            var player = props.Find("Lullaby Player");
            var grille = props.Find("Speaker Grille");
            player.localPosition = new Vector3(-2.01f, .82f, -.12f);
            grille.localPosition = new Vector3(-2.01f, .82f, -.26f);
            var stand = props.Find("Lullaby Player Stand");
            if (stand == null)
                Block(props, "Lullaby Player Stand", new Vector3(-2.01f, .29f, -.12f),
                    new Vector3(.59f, .58f, .38f), Material("Player Stand", new Color(.16f, .25f, .31f)));
            var light = player.Find("Playback Light");
            if (light == null)
                light = Block(player, "Playback Light", new Vector3(.30f, .25f, -.52f),
                    new Vector3(.14f, .10f, .11f), Material("Player Lamp", new Color(.23f, 1f, .67f)));
            light.gameObject.SetActive(false);
            return light;
        }

        static void BuildExit(Transform props)
        {
            if (props.Find("Security Hall Entrance") != null) return;
            var arch = Marker(props, "Security Hall Entrance", new Vector3(.05f, 0, 6.19f));
            var trim = Material("Security Trim", new Color(.26f, .38f, .46f));
            Block(arch, "Recess", new Vector3(0, 1.48f, .13f), new Vector3(2.13f, 2.96f, .12f),
                Material("Security Recess", new Color(.055f, .085f, .11f)));
            foreach (float side in new[] { -1f, 1f })
                Block(arch, "Jamb", new Vector3(side * 1.06f, 1.5f, .06f), new Vector3(.12f, 3, .35f), trim);
            Block(arch, "Lintel", new Vector3(0, 2.97f, .06f), new Vector3(2.18f, .16f, .35f), trim);
        }

        static Transform Block(Transform parent, string label, Vector3 where, Vector3 size, Material material)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = label;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = where;
            item.transform.localScale = size;
            item.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(item.GetComponent<Collider>());
            return item.transform;
        }

        static Material Material(string name, Color color)
        {
            var path = $"{Art}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color, name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Transform Marker(Transform group, string label, Vector3 where)
        {
            var mark = new GameObject(label).transform;
            mark.SetParent(group, false);
            mark.localPosition = where;
            return mark;
        }

        static Transform Shot(Transform group, string label, Vector3 eye, Vector3 at)
        {
            var camera = Marker(group, label, eye);
            camera.LookAt(at);
            return camera;
        }
    }
}
