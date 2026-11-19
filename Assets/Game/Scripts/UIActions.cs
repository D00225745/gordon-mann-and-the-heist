using UnityEngine;

namespace GordonMann
{
    // Button hooks for the menus (wired via OnClick in the Inspector).
    public class UIActions : MonoBehaviour
    {
        public void Retry() => GameFlow.RetryLastChoice();

        public void Restart() => GameFlow.Restart();

        public void LoadScene(string sceneName) => GameFlow.Load(sceneName);

        public void Quit()
        {
            if (!ScreenFader.IsBusy) Application.Quit();
        }

    }
}
