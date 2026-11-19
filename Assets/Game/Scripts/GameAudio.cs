using UnityEngine;

namespace GordonMann
{
    public static class GameAudio
    {
        public const int VolumeSteps = 20;
        internal const string VolumeKey = "GordonMann.Sound.Volume.v1";
        internal const string MuteKey = "GordonMann.Sound.Muted.v1";
        internal static int Volume { get; private set; } = VolumeSteps;
        internal static bool Muted { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            Volume = Mathf.Clamp(PlayerPrefs.GetInt(VolumeKey, VolumeSteps), 0, VolumeSteps);
            Muted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
            Apply();
        }

        internal static void SetVolume(int level)
        {
            Volume = Mathf.Clamp(level, 0, VolumeSteps);
            PlayerPrefs.SetInt(VolumeKey, Volume);
            Apply();
        }

        internal static void ToggleMute()
        {
            Muted = !Muted;
            PlayerPrefs.SetInt(MuteKey, Muted ? 1 : 0);
            Apply();
        }

        static void Apply() => AudioListener.volume = Muted ? 0f : Volume / (float)VolumeSteps;
    }
}
