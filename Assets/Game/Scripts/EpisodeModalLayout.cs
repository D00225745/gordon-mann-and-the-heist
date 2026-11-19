using UnityEngine;

namespace GordonMann
{
    public sealed class EpisodeModalLayout : MonoBehaviour
    {
        RectTransform panel, canvas;
        Vector3 authoredScale;
        Vector2 lastViewport;

        void Awake()
        {
            panel = (RectTransform)transform;
            canvas = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
            authoredScale = panel.localScale;
        }

        void OnEnable() => Fit();

        void LateUpdate()
        {
            if (canvas.rect.size != lastViewport) Fit();
        }

        void Fit()
        {
            lastViewport = canvas.rect.size;
            bool portrait = lastViewport.x > 0f && lastViewport.x < lastViewport.y * 1.35f;
            Vector2 size = panel.rect.size;
            float scale = portrait ? Mathf.Clamp(Mathf.Min((lastViewport.x - 72f) / size.x,
                (lastViewport.y - 72f) / size.y), .05f, 2.1f) : 1f;
            panel.localScale = authoredScale * scale;
        }
    }
}
