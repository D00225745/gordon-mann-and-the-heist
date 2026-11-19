using UnityEngine.SceneManagement;

namespace GordonMann
{
    // Central scene switching. Static so it survives loads without any setup.
    public static class GameFlow
    {
        // Where Retry sends you back to.
        public static string LastChoiceScene { get; private set; }

        public static void PickOutcome(string targetScene)
        {
            if (ScreenFader.IsBusy) return;
            LastChoiceScene = SceneManager.GetActiveScene().name;
            Load(targetScene);
        }

        public static void RetryLastChoice()
        {
            if (!string.IsNullOrEmpty(LastChoiceScene))
                Load(LastChoiceScene);
            else
                Restart();
        }

        public static void Restart()
        {
            if (ScreenFader.IsBusy) return;
            LastChoiceScene = null;
            ScreenFader.LoadScene(0);
        }

        public static void Load(string sceneName) => ScreenFader.LoadScene(sceneName);
    }
}
