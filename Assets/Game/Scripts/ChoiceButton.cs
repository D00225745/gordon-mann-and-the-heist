using UnityEngine;
using UnityEngine.UI;

namespace GordonMann
{
    // One choice box: item icon + label. Loads its target scene on click.
    public class ChoiceButton : MonoBehaviour
    {
        public Image icon;
        public Text label;

        public void Init(Choice choice, System.Action<Choice> pick = null)
        {
            label.text = choice.label;
            icon.sprite = choice.icon;
            icon.gameObject.SetActive(choice.icon != null);

            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (ScreenFader.IsBusy) return;
                if (pick != null) pick(choice);
                else GameFlow.PickOutcome(choice.targetScene);
            });
        }

        public void FitCompactRow()
        {
            var box = (RectTransform)transform;
            box.sizeDelta = new Vector2(310f, 94f);
            var text = label.rectTransform;
            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = new Vector2(icon.gameObject.activeSelf ? 84f : 8f, 5f);
            text.offsetMax = new Vector2(-8f, -5f);
            if (icon.gameObject.activeSelf)
            {
                var image = icon.rectTransform;
                image.anchorMin = image.anchorMax = new Vector2(0, .5f);
                image.pivot = new Vector2(0, .5f);
                image.anchoredPosition = new Vector2(12f, 0);
                image.sizeDelta = new Vector2(62f, 62f);
            }
        }
    }
}
