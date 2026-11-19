using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GordonMann
{
    internal static class MenuFocus
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Subscribe()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
#if ENABLE_INPUT_SYSTEM
            if (EventSystem.current != null && EventSystem.current.TryGetComponent(out InputSystemUIInputModule input))
                input.deselectOnBackgroundClick = false;
#endif
            if (scene.name == "MainMenu") Title();
        }

        static Button Named(string name) => GameObject.Find(name)?.GetComponent<Button>();

        internal static void Title(Button preferred = null)
        {
            var play = Named("PlayButton");
            var quit = Named("QuitButton");
            var outcomes = Named("Outcomes");
            var sound = Named("Sound");
            var credits = Named("Credits");
            if (play == null || quit == null) return;
            var firstExtra = outcomes ?? sound ?? quit;
            Link(play, up: quit, down: firstExtra);
            Link(quit, up: credits ?? outcomes ?? sound ?? play, down: play);
            if (outcomes != null) Link(outcomes, right: sound, up: play, down: quit);
            if (sound != null) Link(sound, left: outcomes, right: credits, up: play, down: quit);
            if (credits != null) Link(credits, left: sound ?? outcomes, up: play, down: quit);
            Select(preferred != null ? preferred : play);
        }

        internal static void Row(Button first, Button exit, params Button[] candidates)
        {
            var available = candidates.Where(button => button != null && button.isActiveAndEnabled && button.IsInteractable()).ToArray();
            int count = available.Length;
            if (count == 0) return;
            for (int i = 0; i < count; i++)
            {
                var previous = count > 1 ? available[(i + count - 1) % count] : null;
                var next = count > 1 ? available[(i + 1) % count] : null;
                Link(available[i], previous, next, exit, exit);
            }
            var target = available.Contains(first) ? first : available[0];
            if (exit != null) Link(exit, down: target, up: target, left: target);
            Select(target);
        }

        internal static void Panel(GameObject panel)
        {
            var buttons = panel.GetComponentsInChildren<Button>();
            Row(buttons.FirstOrDefault(), null, buttons);
        }

        internal static void SoundPanel(Slider volume, Button mute, Button back)
        {
            Link(volume, up: back, down: mute);
            Link(mute, right: back, up: volume, down: volume);
            Link(back, left: mute, up: volume, down: volume);
            Select(volume);
        }

        static void Link(Selectable button, Selectable left = null, Selectable right = null, Selectable up = null, Selectable down = null)
        {
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = left,
                selectOnRight = right,
                selectOnUp = up,
                selectOnDown = down
            };
            if (!button.TryGetComponent<MenuFocusRing>(out _)) button.gameObject.AddComponent<MenuFocusRing>();
        }

        static void Select(Selectable button)
        {
            var system = EventSystem.current;
            if (system != null && !system.alreadySelecting)
                system.SetSelectedGameObject(button != null ? button.gameObject : null);
        }

        internal static void Clear() => Select(null);

    }
}
