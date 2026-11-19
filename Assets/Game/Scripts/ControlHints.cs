using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;
#endif

namespace GordonMann
{
    public sealed class ControlHints : MonoBehaviour
    {
        public Image navigate, confirm;
        public Sprite arrows, enter, dpad, buttonSouth;
        public Text instruction;
        bool controller;

        void OnEnable() => Display(false);
        void Display(bool gamepad)
        {
            controller = gamepad;
            navigate.sprite = gamepad ? dpad : arrows;
            confirm.sprite = gamepad ? buttonSouth : enter;
            navigate.gameObject.SetActive(navigate.sprite != null);
            confirm.gameObject.SetActive(confirm.sprite != null);
            instruction.text = gamepad ? "Choose" : "Choose / click a choice";
        }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            // A-labelled prompts are only shown for Xbox-compatible controllers.
            if (!controller && pad is XInputController &&
                (pad.dpad.ReadValue().sqrMagnitude > .2f || pad.leftStick.ReadValue().sqrMagnitude > .2f || pad.buttonSouth.wasPressedThisFrame))
                Display(true);
            if (controller && (pad is not XInputController || Keyboard.current?.anyKey.wasPressedThisFrame == true ||
                Mouse.current?.leftButton.wasPressedThisFrame == true || Mouse.current?.delta.ReadValue().sqrMagnitude > 4)) Display(false);
#endif
        }
    }
}
