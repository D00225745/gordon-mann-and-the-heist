using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GordonMann
{
    // Spawns the row of choice boxes. Optional countdown: run out of time
    // and timeoutScene loads instead.
    public class ChoicePanel : MonoBehaviour
    {
        public ChoiceButton buttonPrefab;
        public Transform buttonContainer;
        public Image timerFill;

        [Header("Countdown (0 = none)")]
        public float timeLimit;
        public string timeoutScene;
        public bool compactButtons;
        public PortalJarSequence portalJar;
        public LullabiesSequence lullabies;
        public LibraryCardSequence libraryCard;
        public FloorSlideSequence floorSlide;
        public NightBusSequence nightBus;

        float timeLeft;
        bool counting;

        public void Show(List<Choice> choices)
        {
            gameObject.SetActive(true);

            foreach (Transform child in buttonContainer)
                Destroy(child.gameObject);
            var navigation = new List<Button>(choices.Count);
            foreach (var choice in choices)
            {
                var button = Instantiate(buttonPrefab, buttonContainer);
                button.Init(choice, Pick);
                if (compactButtons) button.FitCompactRow();
                navigation.Add(button.GetComponent<Button>());
            }
            MenuFocus.Row(null, null, navigation.ToArray());

            counting = timeLimit > 0f && !string.IsNullOrEmpty(timeoutScene);
            timeLeft = timeLimit;
            if (timerFill != null)
            {
                timerFill.fillAmount = 1f;
                timerFill.transform.parent.gameObject.SetActive(counting);
            }
        }

        void Pick(Choice choice)
        {
            if ((portalJar != null && portalJar.TryPlay(choice)) ||
                (lullabies != null && lullabies.TryPlay(choice)) ||
                (libraryCard != null && libraryCard.TryPlay(choice)) ||
                (floorSlide != null && floorSlide.TryPlay(choice)) ||
                (nightBus != null && nightBus.TryPlay(choice)))
            {
                counting = false;
                MenuFocus.Clear();
                gameObject.SetActive(false);
            }
            else
                GameFlow.PickOutcome(choice.targetScene);
        }

        void Update()
        {
            if (!counting || ScreenFader.IsBusy) return;

            timeLeft -= Time.deltaTime;
            if (timerFill != null)
                timerFill.fillAmount = Mathf.Clamp01(timeLeft / timeLimit);

            if (timeLeft <= 0f)
            {
                counting = false;
                GameFlow.PickOutcome(timeoutScene);
            }
        }
    }
}
