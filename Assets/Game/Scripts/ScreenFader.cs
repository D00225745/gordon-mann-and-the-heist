using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GordonMann
{
    // Fade to black, load, fade back in. Creates itself on demand and blocks
    // clicks mid-transition so a double click can't load two scenes.
    public class ScreenFader : MonoBehaviour
    {
        const float FadeDuration = 0.35f;

        static ScreenFader instance;
        static bool busy;

        CanvasGroup group;

        public static bool IsBusy => busy;
        internal static float Opacity => instance != null ? instance.group.alpha : 0f;

        public static void LoadScene(string sceneName)
        {
            Get().Begin(() => SceneManager.LoadScene(sceneName));
        }

        public static void LoadScene(int buildIndex)
        {
            Get().Begin(() => SceneManager.LoadScene(buildIndex));
        }

        static ScreenFader Get()
        {
            if (instance == null)
            {
                var go = new GameObject("ScreenFader");
                DontDestroyOnLoad(go);

                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;

                instance = go.AddComponent<ScreenFader>();
                instance.group = go.AddComponent<CanvasGroup>();
                instance.group.alpha = 0f;
                instance.group.blocksRaycasts = false;

                var black = new GameObject("Black", typeof(Image));
                black.transform.SetParent(go.transform, false);
                black.GetComponent<Image>().color = Color.black;
                var rect = black.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            return instance;
        }

        void Begin(System.Action load)
        {
            if (busy) return;
            busy = true;
            MenuFocus.Clear();
            StartCoroutine(FadeAndLoad(load));
        }

        IEnumerator FadeAndLoad(System.Action load)
        {
            group.blocksRaycasts = true;
            yield return Fade(0f, 1f);
            load();
            yield return null;
            yield return Fade(1f, 0f);
            group.blocksRaycasts = false;
            busy = false;
        }

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < FadeDuration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / FadeDuration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
