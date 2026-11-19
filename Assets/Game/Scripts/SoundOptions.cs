using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GordonMann
{
    public sealed class SoundOptions : MonoBehaviour
    {
        public GameObject parentPanel;
        public Button open, mute, back;
        public Slider volume;
        public Text percentage, muteLabel;
        internal bool IsOpen => gameObject.activeInHierarchy;
        bool changed;
#if ENABLE_INPUT_SYSTEM
        InputAction cancel;
#endif

        public void Show()
        {
            if (ScreenFader.IsBusy || IsOpen || !parentPanel.activeInHierarchy) return;
            parentPanel.SetActive(false);
            gameObject.SetActive(true);
            MenuFocus.SoundPanel(volume, mute, back);
        }

        void OnEnable()
        {
            volume.SetValueWithoutNotify(GameAudio.Volume);
            Refresh();
#if ENABLE_INPUT_SYSTEM
            cancel = new InputAction(type: InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");
            cancel.Enable();
#endif
        }

        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            cancel?.Dispose();
            cancel = null;
#endif
            if (changed) PlayerPrefs.Save();
            changed = false;
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (cancel?.WasPressedThisFrame() == true) Back();
#else
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)) Back();
#endif
        }

        public void Back()
        {
            if (!IsOpen || ScreenFader.IsBusy) return;
            Dismiss();
            MenuFocus.Title(open);
        }

        internal void Dismiss()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            parentPanel.SetActive(true);
        }

        public void Adjust(float level)
        {
            GameAudio.SetVolume(Mathf.RoundToInt(level));
            changed = true;
            Refresh();
        }

        public void Mute()
        {
            GameAudio.ToggleMute();
            changed = true;
            Refresh();
        }

        void Refresh()
        {
            int percent = GameAudio.Volume * 100 / GameAudio.VolumeSteps;
            percentage.text = GameAudio.Muted ? $"{percent}% · muted" : $"{percent}%";
            muteLabel.text = GameAudio.Muted ? "Unmute" : "Mute";
        }
    }
}
