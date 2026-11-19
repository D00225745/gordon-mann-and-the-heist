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
    public static class VaultJobBlockout
    {
        const string Art = "Assets/Game/Blockout";
        const string OriginalScenes = "Assets/Game/Scenes";
        internal const string BlockoutScenes = Art + "/Scenes";
        const string RootName = "Vault Job Blockout";
        static readonly Color32 Slate = new Color32(55, 72, 94, 255);
        static readonly Color32 Stone = new Color32(127, 143, 158, 255);
        static readonly Color32 Dark = new Color32(24, 35, 53, 255);
        static readonly Color32 Teal = new Color32(46, 184, 173, 255);
        static readonly Color32 Gold = new Color32(247, 184, 88, 255);
        static readonly Color32 Pink = new Color32(252, 62, 105, 255);
        static readonly Color32 Glass = new Color32(75, 135, 181, 255);

        [MenuItem("Tools/Scene Builder/Create Vault Job Blockout Scenes")]
        public static void ApplyAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            EnsureArtFolder();
            if (!AssetDatabase.IsValidFolder(BlockoutScenes)) AssetDatabase.CreateFolder(Art, "Scenes");
            int count = 0;
            foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var name = Path.GetFileNameWithoutExtension(entry.path);
                if (Location(name) == 0) continue;
                var target = $"{BlockoutScenes}/{name}.unity";
                if (File.Exists(target))
                    continue;
                var source = $"{OriginalScenes}/{name}.unity";
                if (!AssetDatabase.CopyAsset(source, target)) throw new IOException("Could not copy " + source);
                var scene = EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
                Apply(scene, name);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save " + entry.path);
                count++;
            }
            AssetDatabase.SaveAssets();
            if (setup.Length != 0 && setup.All(entry => !string.IsNullOrEmpty(entry.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            Debug.Log($"Created {count} Vault Job blockout scenes.");
        }

        [MenuItem("Tools/Scene Builder/Open Vault Job Blockout")]
        public static void UseBlockout()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SetActiveScenes(BlockoutScenes);
            EditorSceneManager.OpenScene($"{BlockoutScenes}/MainMenu.unity", OpenSceneMode.Single);
        }

        [MenuItem("Tools/Scene Builder/Use Original Vault Job Scenes")]
        public static void UseOriginal() => SetActiveScenes(OriginalScenes);

        internal static EditorBuildSettingsScene[] VaultSceneList(string directory)
        {
            var entries = EditorBuildSettings.scenes;
            return entries.Where(entry => Path.GetDirectoryName(entry.path)?.Replace('\\', '/') is OriginalScenes or BlockoutScenes).Select(entry =>
            {
                string name = Path.GetFileNameWithoutExtension(entry.path);
                string path = $"{directory}/{name}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new FileNotFoundException("Create the Vault Job blockout scenes first.", path);
                return new EditorBuildSettingsScene(path, entry.enabled);
            }).ToArray();
        }

        static void SetActiveScenes(string directory) => EditorBuildSettings.scenes = VaultSceneList(directory);

        static int Location(string name) => name switch
        {
            "MainMenu" or "Choice_1" or "Fail_Pogo" or "Fail_Pizza" or "Fail_Cannon" => 1,
            "Choice_2" or "Fail_Statue" or "Fail_Chair" or "Fail_Alarm" => 2,
            "Choice_3" or "Fail_PIN" or "Fail_Knock" => 3,
            "Choice_4" or "Fail_Cart" or "Fail_Unplug" or "Fail_Selfie" or "Fail_Frozen" => 4,
            "Choice_5" or "Fail_Skates" or "Victory" => 5,
            _ => 0
        };

        public static void Apply(Scene scene, string name)
        {
            int location = Location(name);
            if (location == 0) return;
            var roots = scene.GetRootGameObjects();
            var existing = roots.FirstOrDefault(go => go.name == RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            var root = Group(RootName);
            var architecture = Group("Architecture", root);
            var props = Group("Props", root);
            var markers = Group("Staging Markers", root);
            Vector3 actor = new Vector3(-2.8f, 0, -.7f);
            Vector3 target = new Vector3(0, 1.55f, 2.6f);
            Vector3 eye = new Vector3(7.6f, 4.9f, -11.8f);
            float framing = 4.6f;

            switch (location)
            {
                case 1:
                    Exterior(architecture, props, markers, name);
                    target.y = 2.05f;
                    framing = 5.15f;
                    break;
                case 2:
                    Lobby(architecture, props, markers, name);
                    eye = new Vector3(8.5f, 5.5f, -12);
                    target.z = 2;
                    break;
                case 3:
                    Security(architecture, props, markers, name);
                    eye = new Vector3(5.3f, 4, -10.6f);
                    target.z = 1.3f;
                    framing = 3.7f;
                    break;
                case 4:
                    Lasers(architecture, props, markers, name);
                    actor = new Vector3(-2.2f, 0, -3.5f);
                    eye = new Vector3(7.7f, 5.7f, -11);
                    target.z = 2.9f;
                    break;
                case 5:
                    Getaway(architecture, props, markers, name);
                    eye = new Vector3(5.4f, 4.9f, -13);
                    target.z = 1.5f;
                    actor.x = -3.7f;
                    break;
            }
            if (name == "MainMenu") actor.x = 4.5f;
            if (name == "Fail_Statue") actor = new Vector3(3.9f, .36f, .8f);

            Mark(markers, "Gordon Start", actor.x, actor.y, actor.z);
            Group("Establishing Camera Position", markers, eye).LookAt(target);
            var camera = roots.Select(go => go.GetComponent<Camera>()).FirstOrDefault(cam => cam != null);
            if (camera != null)
            {
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
                camera.orthographic = true;
                camera.orthographicSize = framing;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Dark;
            }
            var gordon = roots.FirstOrDefault(go => go.name == "Gordon");
            if (gordon != null) PlaceGordon(gordon, actor, eye);
            ReframePanels(roots);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void PlaceGordon(GameObject gordon, Vector3 mark, Vector3 camera)
        {
            var visible = gordon.GetComponentsInChildren<Renderer>();
            if (visible.Length == 0) return;
            var face = camera - mark;
            face.y = 0;
            gordon.transform.SetPositionAndRotation(mark, Quaternion.LookRotation(face));
            var bounds = visible[0].bounds;
            foreach (var item in visible) bounds.Encapsulate(item.bounds);
            gordon.transform.position += mark - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        static void ReframePanels(GameObject[] roots)
        {
            var canvas = roots.FirstOrDefault(go => go.name == "Canvas");
            if (canvas == null) return;
            var buttons = canvas.transform.Find("ChoicePanel/Buttons") as RectTransform;
            if (buttons != null)
            {
                buttons.anchoredPosition = new Vector2(0, 56);
                buttons.sizeDelta = new Vector2(1700, 118);
                var choicePanel = buttons.GetComponentInParent<ChoicePanel>(true);
                if (choicePanel != null) choicePanel.compactButtons = true;
            }
            var timer = canvas.transform.Find("ChoicePanel/TimerBar") as RectTransform;
            if (timer != null) timer.anchoredPosition = new Vector2(0, 22);
            foreach (string name in new[] { "FailPanel", "WinPanel" })
            {
                var panel = canvas.transform.Find(name) as RectTransform;
                if (panel == null) continue;
                panel.anchorMin = new Vector2(0, 0);
                panel.anchorMax = new Vector2(1, 0);
                panel.pivot = new Vector2(.5f, 0);
                panel.anchoredPosition = Vector2.zero;
                panel.sizeDelta = new Vector2(0, 230);
                foreach (RectTransform child in panel)
                {
                    if (child.name == "Message") SetPanelItem(child, 175, 57, 49);
                    else if (child.name == "Sub") SetPanelItem(child, 119, 58, 27);
                    else SetPanelItem(child, 44, 64, 29);
                }
            }
        }

        static void SetPanelItem(RectTransform rect, float y, float height, int font)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            var text = rect.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null) text.fontSize = font;
        }

        static void Exterior(Transform a, Transform p, Transform m, string beat)
        {
            Box(a, "Bank Street", 0, -.26f, 2, 23, .4f, 21, Dark);
            Box(a, "Sidewalk", 0, -.10f, 2, 20, .2f, 9, Slate);
            Box(a, "Blackwell Bank Mass", 0, 2.65f, 6.35f, 12, 5.3f, 3.4f, Slate);
            Triangle(a, "Bank Pediment", 0, 5.14f, 4.53f, 12.5f, 1.6f, .55f, Stone);
            Box(a, "Cornice", 0, 4.76f, 4.36f, 12.7f, .22f, .7f, Stone);
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 3.05f;
                Tube(a, "Facade Column", x, 2.26f, 4.1f, .52f, 4.52f, Stone);
            }
            Box(a, "Bank Entry", 0, 1.65f, 4.46f, 2.6f, 3.3f, .28f, Dark);
            Box(a, "Entry Transom", 0, 3.47f, 4.25f, 2.65f, .48f, .11f, Glass);
            foreach (int side in new[] { -1, 1 })
            {
                Box(a, "Bank Window", side * 3.05f, 2.4f, 4.55f, 1.7f, 1.85f, .35f, Glass);
                Lamp(a, side * 6.4f, .6f);
            }
            for (int i = 0; i < 3; i++)
                Box(a, "Entry Step", 0, .08f + i * .08f, 3.04f + .24f * i, 3.2f, .16f + .16f * i, 1.3f, Stone);
            Mark(m, "Portal Landing", 0, 0, 3.1f);
            Mark(m, "Roof Overshoot", 5.4f, 7, 5);
            if (beat == "Choice_1")
            {
                var jar = Group("Portal Jar", p, new Vector3(-1.9f, 0, .2f));
                Tube(jar, "Glass Jar", 0, .26f, 0, .42f, .52f, Teal);
                Tube(jar, "Lid", 0, .56f, 0, .46f, .12f, Gold);
                var portal = Group("Portal Preview (enable for shot)", p, new Vector3(1.25f, 1.08f, 1));
                Tube(portal, "Portal Disc", 0, 0, 0, 2.15f, .12f, Teal).localRotation = Quaternion.Euler(90, 0, 0);
                portal.gameObject.SetActive(false);
            }
            if (beat == "Fail_Pogo")
            {
                var pogo = Group("Pogo Stick", p, new Vector3(-1.75f, 0, .15f));
                Tube(pogo, "Shaft", 0, .83f, 0, .15f, 1.62f, Teal);
                Box(pogo, "Handle", 0, 1.61f, 0, .65f, .12f, .12f, Gold);
                Box(pogo, "Footrests", 0, .26f, 0, .75f, .13f, .22f, Stone);
            }
            if (beat == "Fail_Pizza")
            {
                Box(p, "Pizza Box", -1.72f, .89f, .05f, .76f, .12f, .75f, Gold);
                Box(p, "Pizza Delivery Stand", -1.72f, .4f, .05f, .39f, .8f, .45f, Slate);
                Box(p, "Intercom", 1.66f, 1.37f, 4.1f, .24f, .43f, .17f, Dark);
            }
            if (beat == "Fail_Cannon")
            {
                var rig = Group("Cartoon Cannon", p, new Vector3(-1.2f, 0, -.8f));
                Box(rig, "Base", 0, .28f, 0, 1.26f, .56f, 1, Stone);
                Tube(rig, "Barrel", 0, .9f, 0, .53f, 1.65f, Slate).localRotation = Quaternion.Euler(26, 0, -38);
                Ball(rig, "Round Pivot", 0, .67f, 0, .72f, Gold);
            }
        }

        static void Lobby(Transform a, Transform p, Transform m, string beat)
        {
            Interior(a, 14, 13, 6.4f, Slate);
            Box(a, "Entry Runner", 0, .013f, 1.25f, 3, .026f, 9.1f, Teal);
            Box(a, "Reception Counter", -4.2f, .63f, 4.2f, 3.5f, 1.26f, 1.1f, Stone);
            Box(a, "Counter Top", -4.2f, 1.29f, 4.2f, 3.7f, .16f, 1.3f, Dark);
            Tube(a, "Fountain Rim", 2.4f, .25f, 2.5f, 2.8f, .5f, Stone);
            Tube(a, "Fountain Water", 2.4f, .506f, 2.5f, 2.43f, .04f, Glass);
            Ball(a, "Fountain Ornament", 2.4f, .91f, 2.5f, .54f, Slate);
            Tube(a, "Statue Plinth", 3.9f, .18f, .8f, 1.36f, .36f, Stone);
            Guard(p, "Reception Guard", -4.4f, 0, 5.35f);
            Guard(p, "Patrol Guard", 1.12f, 0, 4.8f);
            Box(p, "Fire Alarm", -2.8f, 1.7f, 6.20f, .33f, .37f, .18f, Pink);
            Mark(m, "Lullaby Player Position", -1.65f, 0, 1.2f);
            Mark(m, "Guard Fall Mark", 1.35f, 0, 5.55f);
            Mark(m, "Fountain Splash", 2.4f, .6f, 2.5f);
            Mark(m, "To Security Door", 0, 0, 5.85f);
            if (beat == "Choice_2")
            {
                Box(p, "Lullaby Player", -1.7f, .22f, 1.1f, .62f, .44f, .26f, Gold);
                Ball(p, "Speaker Grille", -1.7f, .23f, .96f, .25f, Dark);
            }
            if (beat == "Fail_Chair")
            {
                var chair = Cart(p, "Rolling Chair", -1.35f, .03f, -.1f, 1, Stone);
                Box(chair, "Backrest", 0, .82f, .30f, .66f, .77f, .13f, Slate);
            }
            if (beat == "Fail_Alarm")
                Ball(p, "Alarm Light", -2.8f, 2.66f, 6.14f, .42f, Pink);
        }

        static void Security(Transform a, Transform p, Transform m, string beat)
        {
            Interior(a, 10, 11, 4.48f, Slate, 3.2f);
            foreach (float x in new[] { -1.73f, 1.73f })
                Box(a, "Security Door Jamb", x, 1.7f, 4.26f, .31f, 3.4f, .56f, Dark);
            Box(a, "Security Door Header", 0, 3.42f, 4.26f, 3.77f, .36f, .56f, Dark);
            var door = Group("Security Door Pivot", p, new Vector3(-1.55f, 0, 4.05f));
            Box(door, "Steel Slab", 1.55f, 1.62f, 0, 3.1f, 3.24f, .34f, Stone);
            Box(door, "Steel Center Seam", 1.55f, 1.62f, -.18f, .052f, 3.20f, .04f, Dark);
            Box(p, "Keypad Housing", -2.38f, 1.35f, 4.05f, .57f, .68f, .34f, Dark);
            for (int i = 0; i < 9; i++)
                Box(p, "Keypad Button", -2.55f + .17f * (i % 3), 1.19f + .14f * (i / 3), 3.86f,
                    .11f, .09f, .04f, Stone);
            Ball(p, "Access Light", -2.38f, 1.86f, 3.89f, .21f, Pink);
            Mark(m, "At Keypad", -2.1f, 0, 3.2f);
            Mark(m, "From Lobby", -2.8f, 0, -.7f);
            Mark(m, "To Laser Hallway", 0, 0, 4.9f);
            if (beat == "Choice_3")
                Box(p, "Library Card", -1.95f, 1.1f, .2f, .56f, .35f, .06f, Gold);
            if (beat == "Fail_Knock")
            {
                door.localRotation = Quaternion.Euler(0, -64, 0);
                Guard(p, "Door Guard", .87f, 0, 3.85f);
            }
            if (beat == "Fail_PIN")
            {
                Box(p, "Rejected PIN Stroke", -2.38f, 2.3f, 3.85f, .58f, .09f, .07f, Pink).Rotate(0, 0, 45);
                Box(p, "Rejected PIN Stroke", -2.38f, 2.3f, 3.84f, .58f, .09f, .07f, Pink).Rotate(0, 0, -45);
            }
        }

        static void Lasers(Transform a, Transform p, Transform m, string beat)
        {
            Interior(a, 7, 15, 8.5f, beat == "Fail_Unplug" ? Dark : Slate);
            Box(a, "Threshold", 0, .025f, -2, 6.6f, .05f, .35f, Gold);
            Box(a, "Slide Lane", 0, .014f, 2.6f, 2.5f, .028f, 10.2f, Dark);
            Box(a, "Vault Frame", 0, 1.75f, 8.26f, 3.6f, 3.5f, .65f, Stone);
            Tube(a, "Circular Vault Door", 0, 1.66f, 7.84f, 3, .25f, Dark).Rotate(90, 0, 0);
            Tube(a, "Vault Hub", 0, 1.66f, 7.67f, .82f, .12f, Gold).Rotate(90, 0, 0);
            for (int i = 0; i < 5; i++)
            {
                float z = -.65f + 1.45f * i;
                float y = 1.02f + i % 3 * .38f;
                Box(p, "Laser Beam", 0, y, z, 6.7f, .065f, .065f, Pink);
                Box(p, "Laser Emitter", -3.22f, y, z, .35f, .26f, .27f, Dark);
                Box(p, "Laser Receiver", 3.22f, y, z, .35f, .26f, .27f, Dark);
            }
            Mark(m, "Slide Start", 0, 0, -2.7f);
            Mark(m, "Slide End", 0, 0, 7.2f);
            Mark(m, "Low Clearance Centerline", 0, .75f, 2.7f);
            Mark(m, "Alarm Camera Look Target", 0, 1.5f, -.5f);
            if (beat == "Fail_Cart") Cart(p, "Mail Cart", .15f, 0, -1.1f, 1.4f, Gold);
            if (beat == "Fail_Selfie")
            {
                Box(p, "Phone", -1.42f, 1.42f, -2.8f, .27f, .48f, .09f, Gold);
                Ball(p, "Flash", -1.42f, 1.57f, -2.87f, .15f, Color.white);
            }
            if (beat == "Fail_Unplug")
            {
                Box(p, "Lighting Power Box", -3.22f, .52f, -1.75f, .4f, .62f, .55f, Stone);
                Box(p, "Disconnected Plug", -2.57f, .28f, -1.37f, .25f, .22f, .32f, Gold);
            }
            if (beat == "Fail_Frozen")
                Box(p, "Timeout Indicator", 0, 2.48f, -.67f, 1.2f, .24f, .20f, Pink);
        }

        static void Getaway(Transform a, Transform p, Transform m, string beat)
        {
            Box(a, "Night Street", 0, -.29f, 2, 23, .4f, 18, Dark);
            Box(a, "Curb and Sidewalk", 0, -.1f, -.55f, 20, .2f, 3.35f, Slate);
            Box(a, "Service Wall", 0, 2.3f, 6, 16, 4.6f, 1.2f, Slate);
            Box(a, "Bank Service Exit", -5.25f, 1.21f, 5.3f, 1.54f, 2.42f, .31f, Dark);
            for (int i = -4; i <= 4; i++)
                Box(a, "Road Marking", i * 2.2f, -.080f, 4.53f, 1.24f, .025f, .13f, Gold);
            Lamp(a, -6.5f, .12f);
            Tube(a, "Bus Stop Post", -2.45f, 1.17f, .78f, .1f, 2.34f, Stone);
            Box(a, "Bus Stop Sign", -2.45f, 2.24f, .78f, .53f, .4f, .1f, Teal);
            var bus = Group("Night Bus", p, new Vector3(beat == "Victory" ? 2.8f : 3.4f, -.06f, 2.95f));
            Box(bus, "Body", 0, 1.19f, 0, 6.9f, 1.7f, 1.85f, Teal);
            Box(bus, "Roof", 0, 2.12f, 0, 6.92f, .22f, 1.93f, Dark);
            for (int i = 0; i < 4; i++)
                Box(bus, "Night Bus Window", -2.55f + i * 1.32f, 1.7f, -.94f, 1.12f, .58f, .06f, Glass);
            Box(bus, "Boarding Door", 2.64f, 1.19f, -.95f, .89f, 1.53f, .09f, Dark);
            Box(bus, "Route Display", 2.64f, 1.69f, -1.015f, .63f, .28f, .035f, Gold);
            foreach (float x in new[] { -2.32f, 2.02f })
                foreach (float z in new[] { -.88f, .88f })
                    Tube(bus, "Bus Wheel", x, .4f, z, .8f, .23f, Dark).Rotate(90, 0, 0);
            Mark(m, "Bus Arrival", -9, 0, 2.95f);
            Mark(m, "Boarding Step", bus.position.x + 2.64f, 0, 1.46f);
            Mark(m, "Bus Departure", 12, 0, 2.95f);
            if (beat == "Fail_Skates")
            {
                for (int i = 0; i < 2; i++)
                {
                    var skate = Cart(p, "Rocket Skate", -2.1f + .45f * i, 0, -.22f, .47f, Gold);
                    Ball(skate, "Rocket Exhaust", -.3f, .40f, 0, .28f, Pink);
                }
                Box(p, "Crash Barrier", -6.84f, .51f, 2, .56f, 1.02f, 1.7f, Gold);
            }
        }

        static void Interior(Transform set, float width, float length, float back, Color wall, float opening = 0)
        {
            float center = back - length * .5f;
            Box(set, "Floor Slab", 0, -.12f, center, width, .24f, length, Dark);
            if (opening == 0)
                Box(set, "Back Wall", 0, 1.9f, back + .19f, width, 3.8f, .38f, wall);
            else
            {
                float segment = (width - opening) * .5f;
                foreach (float side in new[] { -1f, 1f })
                    Box(set, "Back Wall Segment", side * (opening + segment) * .5f,
                        1.9f, back + .19f, segment, 3.8f, .38f, wall);
            }
            Box(set, "Left Wall", -width * .5f, 1.9f, center, .34f, 3.8f, length, wall);
            Box(set, "Right Wall Footprint", width * .5f, .09f, center, .32f, .18f, length, Stone);
            for (int i = 0; i < Mathf.FloorToInt(length); i++)
                Box(set, "Floor Joint", 0, .002f, back - i - .4f, width - .35f, .004f, .025f, Slate);
            Box(set, "Back Wall Accent", 0, 3.6f, back - .02f, width - .35f, .10f, .06f, Glass);
        }

        static void Guard(Transform p, string name, float x, float y, float z)
        {
            var guard = Group(name, p, new Vector3(x, y, z));
            Box(guard, "Body", 0, .98f, 0, .55f, .89f, .36f, Slate);
            foreach (float leg in new[] { -.17f, .17f })
                Box(guard, "Leg", leg, .31f, 0, .22f, .62f, .24f, Dark);
            Ball(guard, "Head", 0, 1.55f, 0, .38f, Stone);
            Tube(guard, "Cap", 0, 1.73f, 0, .42f, .14f, Gold);
        }

        static Transform Cart(Transform p, string name, float x, float y, float z, float scale, Color color)
        {
            var cart = Group(name, p, new Vector3(x, y, z));
            cart.localScale = Vector3.one * scale;
            Box(cart, "Platform", 0, .34f, 0, .89f, .14f, .68f, color);
            foreach (float wx in new[] { -.29f, .29f })
                foreach (float wz in new[] { -.25f, .25f })
                    Tube(cart, "Wheel", wx, .14f, wz, .27f, .10f, Dark).Rotate(90, 0, 0);
            return cart;
        }

        static void Lamp(Transform p, float x, float z)
        {
            Tube(p, "Streetlamp Post", x, 1.38f, z, .13f, 2.76f, Dark);
            Box(p, "Streetlamp Housing", x, 2.77f, z, .52f, .23f, .43f, Slate);
            Box(p, "Streetlamp Lens", x, 2.66f, z, .39f, .10f, .33f, Gold);
        }

        static Transform Group(string name, Transform parent = null, Vector3 position = default)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = position;
            return node;
        }

        static void Mark(Transform p, string name, float x, float y, float z) => Group(name, p, new Vector3(x, y, z));

        static Transform Box(Transform p, string name, float x, float y, float z, float w, float h, float d, Color c)
            => Primitive(PrimitiveType.Cube, p, name, new Vector3(x, y, z), new Vector3(w, h, d), c);

        static Transform Tube(Transform p, string name, float x, float y, float z, float diameter, float height, Color c)
            => Primitive(PrimitiveType.Cylinder, p, name, new Vector3(x, y, z), new Vector3(diameter, height / 2, diameter), c);

        static Transform Ball(Transform p, string name, float x, float y, float z, float diameter, Color c)
            => Primitive(PrimitiveType.Sphere, p, name, new Vector3(x, y, z), Vector3.one * diameter, c);

        static Transform Primitive(PrimitiveType type, Transform p, string name, Vector3 pos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            return go.transform;
        }

        static void Triangle(Transform p, string name, float x, float y, float z, float w, float h, float d, Color c)
        {
            EnsureArtFolder();
            const string asset = Art + "/Triangle Prism.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(asset);
            if (mesh == null)
            {
                var a = new Vector3(-.5f, -.5f, -.5f);
                var b = new Vector3(0, .5f, -.5f);
                var bottomRight = new Vector3(.5f, -.5f, -.5f);
                var e = a + Vector3.forward;
                var f = b + Vector3.forward;
                var g = bottomRight + Vector3.forward;
                mesh = new Mesh { name = "Triangle Prism" };
                mesh.vertices = new[] { a,b,bottomRight, e,g,f, a,e,f,b, b,f,g,bottomRight, a,bottomRight,g,e };
                mesh.triangles = new[] { 0,1,2, 3,4,5, 6,7,8,6,8,9, 10,11,12,10,12,13, 14,15,16,14,16,17 };
                mesh.RecalculateNormals();
                AssetDatabase.CreateAsset(mesh, asset);
            }
            var node = Group(name, p, new Vector3(x, y, z));
            node.localScale = new Vector3(w, h, d);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.gameObject.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(c);
        }

        static Material MaterialFor(Color color)
        {
            EnsureArtFolder();
            string key = ColorUtility.ToHtmlStringRGB(color);
            string path = $"{Art}/Blockout {key}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("The URP Lit shader was unavailable.");
            material = new Material(shader) { color = color, name = "Blockout " + key };
            material.SetFloat("_Smoothness", .23f);
            if (key == ColorUtility.ToHtmlStringRGB(Pink))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * .7f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EnsureArtFolder()
        {
            if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Game", "Blockout");
        }
    }
}

