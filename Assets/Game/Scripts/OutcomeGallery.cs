using UnityEngine;
using UnityEngine.UI;

namespace GordonMann
{
    public sealed class OutcomeGallery : MonoBehaviour
    {
        public GameObject episodeMenu, gallery;
        public Button openButton, back;
        public Text progress;
        public Text[] labels;
        public Graphic[] tiles;

        static readonly Color Paper = new Color32(249, 234, 211, 255);
        static readonly Color Muted = new Color32(169, 187, 191, 255);
        static readonly Color Revealed = new Color32(53, 72, 81, 255);
        static readonly Color Missing = new Color32(32, 47, 60, 255);

        public void Open()
        {
            if (ScreenFader.IsBusy || gallery.activeSelf) return;
            int found = 0;
            var entries = OutcomeJournal.Vault;
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                bool discovered = OutcomeJournal.Found(entry.Id);
                if (discovered) found++;
                labels[index].text = discovered ? "✓   " + entry.Title : "—   Undiscovered";
                labels[index].color = discovered ? Paper : Muted;
                tiles[index].color = discovered ? Revealed : Missing;
            }
            progress.text = $"{found} / {entries.Count} discovered";
            MenuFocus.Clear();
            episodeMenu.SetActive(false);
            gallery.SetActive(true);
            MenuFocus.Row(back, null, back);
        }

        public void Close()
        {
            if (!gallery.activeSelf) return;
            MenuFocus.Clear();
            gallery.SetActive(false);
            episodeMenu.SetActive(true);
            MenuFocus.Title(openButton);
        }
    }
}
