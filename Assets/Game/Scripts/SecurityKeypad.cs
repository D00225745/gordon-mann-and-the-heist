using UnityEngine;

namespace GordonMann
{
    public sealed class SecurityKeypad : MonoBehaviour
    {
        [SerializeField] Transform[] entryKeys;
        [SerializeField] TextMesh display;
        [SerializeField] GameObject readyLamp;
        [SerializeField] GameObject rejectLamp;
        Vector3[] rest;

        public string Display => display.text;
        public bool Rejected => rejectLamp.activeSelf;

        void Awake()
        {
            rest = new Vector3[entryKeys.Length];
            for (int i = 0; i < rest.Length; i++) rest[i] = entryKeys[i].localPosition;
        }

        public void Depress(int index, float amount) =>
            entryKeys[index].localPosition = rest[index] + Vector3.forward * (.024f * amount);

        public void ShowEntry(int count) => display.text = new string('•', count);

        public void Reject()
        {
            display.text = "DENIED";
            display.color = new Color32(255, 103, 112, 255);
            readyLamp.SetActive(false);
            rejectLamp.SetActive(true);
        }
    }
}
