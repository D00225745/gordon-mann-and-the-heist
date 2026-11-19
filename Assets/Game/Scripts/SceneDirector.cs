using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace GordonMann
{
    public enum SceneOutcome { ShowChoices, Fail, Win, AutoAdvance }

    // Runs one story beat: play the intro cutscene, then hand over to the outcome.
    public class SceneDirector : MonoBehaviour
    {
        [Header("Intro")]
        [Tooltip("Timeline for this beat's cutscene. Empty = wait introDelay seconds instead.")]
        public PlayableDirector introTimeline;
        [Tooltip("Fallback delay when no timeline is set. 0 skips straight to the outcome.")]
        public float introDelay = 0f;
        public PortalJarSequence lobbyArrival;
        public BankFailureSequence bankFailure;
        public LobbyFailureSequence lobbyFailure;
        public SecurityFailureSequence securityFailure;
        public LaserFailureSequence laserFailure;
        public RocketSkatesSequence rocketSkates;
        public UnityEvent onIntroFinished;

        [Header("Outcome")]
        public SceneOutcome outcome = SceneOutcome.ShowChoices;
        public List<Choice> choices = new List<Choice>();
        [Tooltip("Only used with AutoAdvance.")]
        public string nextScene;

        [Header("UI")]
        public ChoicePanel choicePanel;
        public GameObject failPanel;
        public GameObject winPanel;

        bool introDone;

        IEnumerator Start()
        {
            if (choicePanel != null) choicePanel.gameObject.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);

            if (lobbyArrival != null && lobbyArrival.TryBeginArrival())
            {
                while (lobbyArrival.Playing)
                    yield return null;
            }
            else if (bankFailure != null)
            {
                yield return bankFailure.Play();
            }
            else if (lobbyFailure != null)
            {
                yield return lobbyFailure.Play();
            }
            else if (securityFailure != null)
            {
                yield return securityFailure.Play();
            }
            else if (laserFailure != null)
            {
                yield return laserFailure.Play();
            }
            else if (rocketSkates != null)
            {
                yield return rocketSkates.Play();
            }
            else if (introTimeline != null)
            {
                introTimeline.Play();
                while (introTimeline.state == PlayState.Playing)
                    yield return null;
            }
            else if (introDelay > 0f)
            {
                yield return new WaitForSeconds(introDelay);
            }

            FinishIntro();
        }

        // Also callable from an animation event or a skip button.
        public void FinishIntro()
        {
            if (introDone) return;
            introDone = true;

            onIntroFinished.Invoke();

            switch (outcome)
            {
                case SceneOutcome.ShowChoices:
                    choicePanel.Show(choices);
                    break;
                case SceneOutcome.Fail:
                    OutcomeJournal.MarkVaultResult(SceneManager.GetActiveScene().name, GameFlow.LastChoiceScene);
                    failPanel.SetActive(true);
                    MenuFocus.Panel(failPanel);
                    break;
                case SceneOutcome.Win:
                    OutcomeJournal.MarkVaultResult(SceneManager.GetActiveScene().name, GameFlow.LastChoiceScene);
                    winPanel.SetActive(true);
                    MenuFocus.Panel(winPanel);
                    break;
                case SceneOutcome.AutoAdvance:
                    GameFlow.Load(nextScene);
                    break;
            }
        }
    }
}
