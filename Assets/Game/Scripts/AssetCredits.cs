using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GordonMann
{
    public sealed class AssetCredits : MonoBehaviour
    {
        public GameObject titleControls;
        public Button open, back;
#if ENABLE_INPUT_SYSTEM
        InputAction cancel;
#endif
        public void Show()
        {
            if (ScreenFader.IsBusy || gameObject.activeSelf || !titleControls.activeSelf) return;
            titleControls.SetActive(false);
            gameObject.SetActive(true);
            MenuFocus.Row(back, null, back);
        }
        public void Close()
        {
            if (ScreenFader.IsBusy || !gameObject.activeSelf) return;
            gameObject.SetActive(false);
            titleControls.SetActive(true);
            MenuFocus.Title(open);
        }
        void OnEnable()
        {
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
        }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (cancel?.WasPressedThisFrame() == true) Close();
#endif
        }
    }
}
