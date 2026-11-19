using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GordonMann
{
    [RequireComponent(typeof(Selectable))]
    internal sealed class MenuFocusRing : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        GameObject ring;

        void Awake()
        {
            ring = new GameObject("Focus outline", typeof(RectTransform));
            var rect = ring.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            AddEdge(rect, Vector2.zero, Vector2.right, new Vector2(0, 4), new Vector2(0, 2));
            AddEdge(rect, Vector2.up, Vector2.one, new Vector2(0, 4), new Vector2(0, -2));
            AddEdge(rect, Vector2.zero, Vector2.up, new Vector2(4, 0), new Vector2(2, 0));
            AddEdge(rect, Vector2.right, Vector2.one, new Vector2(4, 0), new Vector2(-2, 0));
        }

        void OnEnable() => ring.SetActive(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);

        void OnDisable()
        {
            if (ring != null) ring.SetActive(false);
        }

        static void AddEdge(Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 offset)
        {
            var edge = new GameObject("Edge", typeof(RectTransform), typeof(Image), typeof(Outline));
            var rect = edge.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
            var image = edge.GetComponent<Image>();
            image.color = new Color32(255, 209, 113, 255);
            image.raycastTarget = false;
            var outline = edge.GetComponent<Outline>();
            outline.effectColor = new Color32(29, 45, 55, 255);
            outline.effectDistance = new Vector2(1, -1);
        }

        public void OnSelect(BaseEventData eventData) => ring.SetActive(true);
        public void OnDeselect(BaseEventData eventData) => ring.SetActive(false);
    }
}
