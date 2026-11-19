using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GordonMann
{
    // A shared non-spatial channel lets short interface cues finish across scene fades.
    [RequireComponent(typeof(Button))]
    public sealed class InterfaceFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public AudioClip focus, activate;
        static AudioSource channel;
        static float lastFocus = -1;
        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Click);
        }

        static AudioSource Channel()
        {
            if (channel != null) return channel;
            var owner = new GameObject("Interface audio");
            DontDestroyOnLoad(owner);
            channel = owner.AddComponent<AudioSource>();
            channel.playOnAwake = false;
            channel.spatialBlend = 0;
            return channel;
        }

        public void OnPointerEnter(PointerEventData data) => Focus();
        public void OnSelect(BaseEventData data) => Focus();

        void Focus()
        {
            if (focus == null || button == null || !button.IsInteractable() || ScreenFader.IsBusy ||
                Time.timeSinceLevelLoad < .15f || Time.unscaledTime - lastFocus < .09f) return;
            lastFocus = Time.unscaledTime;
            Channel().PlayOneShot(focus, .12f);
        }

        void Click()
        {
            if (activate != null && button.IsInteractable()) Channel().PlayOneShot(activate, .22f);
        }
    }
}
