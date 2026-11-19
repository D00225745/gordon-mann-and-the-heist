using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GordonMann.EditorTools
{
    // Builds the game's scenes from code so the flow stays easy to restructure.
    // Tools > Scene Builder > Rebuild All Scenes regenerates everything —
    // careful: that overwrites manual edits inside those scenes.
    public static class SceneBuilder
    {
        const string Root = "Assets/Game";
        const string ScenesDir = Root + "/Scenes";
        const string PrefabsDir = Root + "/Prefabs";
        const string ButtonPrefabPath = PrefabsDir + "/ChoiceButton.prefab";
        const string GordonModelPath = Root + "/Models/Character_UVs_v3.fbx";

        static readonly Color FailBg = new Color(0.24f, 0.10f, 0.10f);

        static Font cachedFont;
        static Font DefaultFont =>
            cachedFont != null ? cachedFont : cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Sprite UISprite =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ------------------------------------------------------------------ menu items

        [MenuItem("Tools/Scene Builder/Rebuild All Scenes")]
        public static void RebuildAll()
        {
            EnsureFolders();
            var button = LoadOrBuildButtonPrefab();
            var paths = new List<string>();

            // MainMenu first: build index 0 is where Restart()/MAIN MENU lands.
            paths.Add(BuildMainMenu());

            paths.Add(BuildChoiceScene("Choice_1", new Color(0.13f, 0.17f, 0.26f),
                "THE BREAK-IN",
                "The Blackwell Bank. 2 AM. Gordon needs a way in.",
                new[]
                {
                    new Choice { label = "Pogo Stick", targetScene = "Fail_Pogo" },
                    new Choice { label = "Pizza Guy",  targetScene = "Fail_Pizza" },
                    new Choice { label = "Portal Jar", targetScene = "Choice_2" },
                    new Choice { label = "Cannonball", targetScene = "Fail_Cannon" },
                }, button));

            paths.Add(BuildChoiceScene("Choice_2", new Color(0.10f, 0.20f, 0.18f),
                "THE LOBBY",
                "He's in — but the night guards are everywhere.",
                new[]
                {
                    new Choice { label = "Statue",        targetScene = "Fail_Statue" },
                    new Choice { label = "Lullabies",     targetScene = "Choice_3" },
                    new Choice { label = "Rolling Chair", targetScene = "Fail_Chair" },
                    new Choice { label = "Fire Alarm",    targetScene = "Fail_Alarm" },
                }, button));

            paths.Add(BuildChoiceScene("Choice_3", new Color(0.20f, 0.16f, 0.10f),
                "THE SECURITY DOOR",
                "A big steel door. A tiny red light. One shot at this.",
                new[]
                {
                    new Choice { label = "Guess the PIN",  targetScene = "Fail_PIN" },
                    new Choice { label = "Knock Politely", targetScene = "Fail_Knock" },
                    new Choice { label = "Library Card",   targetScene = "Choice_4" },
                }, button));

            paths.Add(BuildChoiceScene("Choice_4", new Color(0.16f, 0.10f, 0.22f),
                "THE LASER HALLWAY",
                "Lasers. Why is it always lasers? Eight seconds — go!",
                new[]
                {
                    new Choice { label = "Mail Cart",    targetScene = "Fail_Cart" },
                    new Choice { label = "Unplug It",    targetScene = "Fail_Unplug" },
                    new Choice { label = "Floor Slide",  targetScene = "Choice_5" },
                    new Choice { label = "Selfie First", targetScene = "Fail_Selfie" },
                }, button, timeLimit: 8f, timeoutScene: "Fail_Frozen"));

            paths.Add(BuildChoiceScene("Choice_5", new Color(0.08f, 0.12f, 0.20f),
                "THE GETAWAY",
                "Alarms blaring. Pockets full. Time to vanish.",
                new[]
                {
                    new Choice { label = "Rocket Skates", targetScene = "Fail_Skates" },
                    new Choice { label = "Night Bus",     targetScene = "Victory" },
                }, button));

            paths.Add(BuildFailScene("Fail_Pogo", "Great height. No steering.", button));
            paths.Add(BuildFailScene("Fail_Pizza", "Nobody orders a pepperoni at 2 AM. Suspicious.", button));
            paths.Add(BuildFailScene("Fail_Cannon", "Cleared the bank. Cleared the block, actually.", button));
            paths.Add(BuildFailScene("Fail_Statue", "He held the pose. The sneeze didn't.", button));
            paths.Add(BuildFailScene("Fail_Chair", "Silent. Graceful. Straight into the fountain.", button));
            paths.Add(BuildFailScene("Fail_Alarm", "The fire department showed up. All of them.", button));
            paths.Add(BuildFailScene("Fail_PIN", "It wasn't 1-2-3-4. Somehow.", button));
            paths.Add(BuildFailScene("Fail_Knock", "A guard opened it. Awkward for everyone.", button));
            paths.Add(BuildFailScene("Fail_Cart", "The squeaky wheel got the laser.", button));
            paths.Add(BuildFailScene("Fail_Unplug", "That was the lights. The lasers run on something else.", button));
            paths.Add(BuildFailScene("Fail_Selfie", "The flash. Every sensor saw it.", button));
            paths.Add(BuildFailScene("Fail_Frozen", "He thought about it too long. The lasers didn't.", button));
            paths.Add(BuildFailScene("Fail_Skates", "The skates worked. The stopping didn't.", button));

            paths.Add(BuildBeat("Victory", new Color(0.22f, 0.18f, 0.08f), null, null, button,
                (dir, panel) => dir.outcome = SceneOutcome.Win,
                winSub: "The 2:45 night bus. Exact change. Flawless.\nThe Vault Job: complete."));

            EditorBuildSettings.scenes =
                paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();

            EditorSceneManager.OpenScene(ScenesDir + "/MainMenu.unity");
            Debug.Log("Scene Builder: rebuilt " + paths.Count + " scenes.");
        }

        [MenuItem("Tools/Scene Builder/New Scene...")]
        public static void NewScene()
        {
            EnsureFolders();
            string path = EditorUtility.SaveFilePanelInProject(
                "New Scene", "Choice_New", "unity",
                "Pick a name and location for the new story scene.", ScenesDir);
            if (string.IsNullOrEmpty(path)) return;

            var button = LoadOrBuildButtonPrefab();
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            BuildBeatAt(path, new Color(0.15f, 0.17f, 0.21f), sceneName, null, button, (dir, panel) => { });
            AddToBuildSettings(path);
            Debug.Log("Scene Builder: created " + path);
        }

        // ------------------------------------------------------------------ scene kinds

        static string BuildMainMenu()
        {
            string path = ScenesDir + "/MainMenu.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupCamera(new Color(0.09f, 0.10f, 0.16f));
            SpawnGordon();

            // Off to the side on the menu so the buttons don't cover him.
            var gordon = GameObject.Find("Gordon");
            if (gordon != null)
            {
                gordon.transform.position += new Vector3(1.7f, 0f, 0f);
                gordon.transform.rotation = Quaternion.Euler(0f, 155f, 0f);
            }

            var canvasT = MakeCanvas().transform;
            var actions = canvasT.gameObject.AddComponent<UIActions>();

            MakeText(canvasT, "Title", "GORDON MANN", 110, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1700f, 170f), FontStyle.Bold);
            MakeText(canvasT, "Tagline", "in: The Vault Job", 34, new Color(1f, 1f, 1f, 0.7f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1500f, 60f), FontStyle.Italic);

            var play = MakeButton(canvasT, "PlayButton", "PLAY", new Vector2(0f, -70f), new Vector2(420f, 110f));
            UnityEventTools.AddStringPersistentListener(
                play.onClick, new UnityAction<string>(actions.LoadScene), "Choice_1");

            var quit = MakeButton(canvasT, "QuitButton", "QUIT", new Vector2(0f, -210f), new Vector2(420f, 110f));
            UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(actions.Quit));

            MakeEventSystem();
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static string BuildChoiceScene(string sceneName, Color bg, string heading, string narration,
            Choice[] choices, ChoiceButton button, float timeLimit = 0f, string timeoutScene = null)
        {
            return BuildBeat(sceneName, bg, heading, narration, button, (dir, panel) =>
            {
                dir.outcome = SceneOutcome.ShowChoices;
                dir.choices = new List<Choice>(choices);
                panel.timeLimit = timeLimit;
                panel.timeoutScene = timeoutScene;
            });
        }

        static string BuildFailScene(string sceneName, string quip, ChoiceButton button)
        {
            return BuildBeat(sceneName, FailBg, null, null, button,
                (dir, panel) => dir.outcome = SceneOutcome.Fail,
                failQuip: quip);
        }

        static string BuildBeat(string sceneName, Color bg, string heading, string narration,
            ChoiceButton button, System.Action<SceneDirector, ChoicePanel> configure,
            string failQuip = null, string winSub = null)
        {
            string path = ScenesDir + "/" + sceneName + ".unity";
            BuildBeatAt(path, bg, heading, narration, button, configure, failQuip, winSub);
            return path;
        }

        static void BuildBeatAt(string path, Color bg, string heading, string narration,
            ChoiceButton button, System.Action<SceneDirector, ChoicePanel> configure,
            string failQuip = null, string winSub = null)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupCamera(bg);
            SpawnGordon();

            var canvasT = MakeCanvas().transform;

            if (!string.IsNullOrEmpty(heading))
                MakeText(canvasT, "Heading", heading, 54, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(1800f, 90f), FontStyle.Bold);
            if (!string.IsNullOrEmpty(narration))
                MakeText(canvasT, "Narration", narration, 28, new Color(1f, 1f, 1f, 0.8f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1700f, 80f), FontStyle.Italic);

            // Choice panel ----------------------------------------------------
            var choicePanelGO = NewUI("ChoicePanel", canvasT);
            Stretch(choicePanelGO);
            var choicePanel = choicePanelGO.AddComponent<ChoicePanel>();
            choicePanel.buttonPrefab = button;

            var buttonRow = NewUI("Buttons", choicePanelGO.transform);
            var rowRect = buttonRow.GetComponent<RectTransform>();
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.anchoredPosition = new Vector2(0f, 150f);
            rowRect.sizeDelta = new Vector2(1700f, 320f);
            var layout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            choicePanel.buttonContainer = buttonRow.transform;

            var timerBar = NewUI("TimerBar", choicePanelGO.transform);
            var timerRect = timerBar.GetComponent<RectTransform>();
            timerRect.anchorMin = timerRect.anchorMax = new Vector2(0.5f, 0f);
            timerRect.pivot = new Vector2(0.5f, 0f);
            timerRect.anchoredPosition = new Vector2(0f, 90f);
            timerRect.sizeDelta = new Vector2(600f, 26f);
            var timerBg = timerBar.AddComponent<Image>();
            timerBg.sprite = UISprite;
            timerBg.type = Image.Type.Sliced;
            timerBg.color = new Color(0f, 0f, 0f, 0.6f);

            var fill = NewUI("Fill", timerBar.transform);
            var fillRect = Stretch(fill);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = UISprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.color = new Color(1f, 0.76f, 0.2f);
            choicePanel.timerFill = fillImage;

            // Fail / win panels ------------------------------------------------
            var failPanel = MakeEndPanel(canvasT, "FailPanel", new Color(0.30f, 0.02f, 0.02f, 0.94f),
                "YOU FAILED.", failQuip, "RETRY", out var retryButton);
            var failActions = failPanel.AddComponent<UIActions>();
            UnityEventTools.AddPersistentListener(retryButton.onClick, new UnityAction(failActions.Retry));

            var winPanel = MakeEndPanel(canvasT, "WinPanel", new Color(0.03f, 0.22f, 0.10f, 0.94f),
                "CONGRATULATIONS!", winSub, "MAIN MENU", out var menuButton);
            var winActions = winPanel.AddComponent<UIActions>();
            UnityEventTools.AddPersistentListener(menuButton.onClick, new UnityAction(winActions.Restart));

            // Director ---------------------------------------------------------
            var director = new GameObject("SceneDirector").AddComponent<SceneDirector>();
            director.choicePanel = choicePanel;
            director.failPanel = failPanel;
            director.winPanel = winPanel;

            configure(director, choicePanel);

            choicePanelGO.SetActive(false);
            failPanel.SetActive(false);
            winPanel.SetActive(false);

            MakeEventSystem();
            EditorSceneManager.SaveScene(scene, path);
        }

        // ------------------------------------------------------------------ stage

        static void SetupCamera(Color bg)
        {
            var camGO = GameObject.Find("Main Camera");
            if (camGO == null) return;
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            camGO.transform.position = new Vector3(0f, 1.2f, -4f);
            camGO.transform.rotation = Quaternion.identity;
        }

        static void SpawnGordon()
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GordonModelPath);
            if (modelAsset == null)
            {
                var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prop.name = "Prop";
                prop.transform.position = new Vector3(0f, 0.5f, 0f);
                return;
            }

            var gordon = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            gordon.name = "Gordon";

            // Face the camera, stand ~2 units tall, feet at the origin —
            // regardless of the FBX's export scale and pivot.
            gordon.transform.position = Vector3.zero;
            gordon.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var renderers = gordon.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            if (b.size.y > 0.0001f)
                gordon.transform.localScale *= 2f / b.size.y;

            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            gordon.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
        }

        // ------------------------------------------------------------------ prefab

        static ChoiceButton LoadOrBuildButtonPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
            if (existing != null) return existing.GetComponent<ChoiceButton>();

            var root = NewUI("ChoiceButton", null);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 300f);

            var bgImage = root.AddComponent<Image>();
            bgImage.sprite = UISprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.93f, 0.87f, 0.70f);

            var buttonComp = root.AddComponent<Button>();
            buttonComp.targetGraphic = bgImage;
            var choiceButton = root.AddComponent<ChoiceButton>();

            var iconGO = NewUI("Icon", root.transform);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -25f);
            iconRect.sizeDelta = new Vector2(180f, 180f);
            var icon = iconGO.AddComponent<Image>();
            icon.preserveAspect = true;

            var label = MakeText(root.transform, "Label", "Item", 32, new Color(0.15f, 0.13f, 0.10f),
                new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(220f, 70f), FontStyle.Bold);

            choiceButton.icon = icon;
            choiceButton.label = label;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ChoiceButton>();
        }

        // ------------------------------------------------------------------ UI helpers

        static GameObject MakeCanvas()
        {
            var canvasGO = NewUI("Canvas", null);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            return canvasGO;
        }

        static void MakeEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        static RectTransform Stretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Text MakeText(Transform parent, string name, string content, int fontSize, Color color,
            Vector2 anchor, Vector2 anchoredPos, Vector2 size, FontStyle style = FontStyle.Normal)
        {
            var go = NewUI(name, parent);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = DefaultFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return text;
        }

        static Button MakeButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
        {
            var go = NewUI(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = UISprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.92f, 0.92f, 0.92f);
            var buttonComp = go.AddComponent<Button>();
            buttonComp.targetGraphic = image;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var text = MakeText(go.transform, "Label", label, 36, new Color(0.12f, 0.12f, 0.12f),
                new Vector2(0.5f, 0.5f), Vector2.zero, size, FontStyle.Bold);
            Stretch(text.gameObject);
            return buttonComp;
        }

        static GameObject MakeEndPanel(Transform canvas, string name, Color bg,
            string message, string sub, string buttonLabel, out Button button)
        {
            var panel = NewUI(name, canvas);
            Stretch(panel);
            panel.AddComponent<Image>().color = bg;

            MakeText(panel.transform, "Message", message, 96, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(1600f, 160f), FontStyle.Bold);
            if (!string.IsNullOrEmpty(sub))
                MakeText(panel.transform, "Sub", sub, 30, new Color(1f, 1f, 1f, 0.85f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 15f), new Vector2(1500f, 90f), FontStyle.Italic);
            button = MakeButton(panel.transform, buttonLabel + "Button", buttonLabel,
                new Vector2(0f, -130f), new Vector2(380f, 100f));
            return panel;
        }

        // ------------------------------------------------------------------ misc

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "Game");
            if (!AssetDatabase.IsValidFolder(ScenesDir)) AssetDatabase.CreateFolder(Root, "Scenes");
            if (!AssetDatabase.IsValidFolder(PrefabsDir)) AssetDatabase.CreateFolder(Root, "Prefabs");
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
