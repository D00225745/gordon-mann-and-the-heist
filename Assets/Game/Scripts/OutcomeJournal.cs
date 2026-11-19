using System.Collections.Generic;
using UnityEngine;

namespace GordonMann
{
    public static class OutcomeJournal
    {
        public readonly struct Entry
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string Origin;

            public Entry(string id, string title, string origin = null)
            {
                Id = id;
                Title = title;
                Origin = origin;
            }
        }

        public static IReadOnlyList<Entry> Vault { get; } = new[]
        {
            new Entry("Fail_Pogo", "Pogo Stick", "Choice_1"),
            new Entry("Fail_Pizza", "Pizza Guy", "Choice_1"),
            new Entry("Fail_Cannon", "Cannonball", "Choice_1"),
            new Entry("Fail_Statue", "Statue", "Choice_2"),
            new Entry("Fail_Chair", "Rolling Chair", "Choice_2"),
            new Entry("Fail_Alarm", "Fire Alarm", "Choice_2"),
            new Entry("Fail_PIN", "Guess the PIN", "Choice_3"),
            new Entry("Fail_Knock", "Knock Politely", "Choice_3"),
            new Entry("Fail_Cart", "Mail Cart", "Choice_4"),
            new Entry("Fail_Unplug", "Unplug It", "Choice_4"),
            new Entry("Fail_Selfie", "Selfie First", "Choice_4"),
            new Entry("Fail_Frozen", "Out of Time", "Choice_4"),
            new Entry("Fail_Skates", "Rocket Skates", "Choice_5"),
            new Entry("Victory", "Vault Job complete", "Choice_5")
        };

        public static string PreferenceKey(string id) => "GordonMann.Outcomes.v1." + id;

        public static bool Found(string id) => PlayerPrefs.GetInt(PreferenceKey(id), 0) != 0;

        internal static void MarkVaultResult(string scene, string origin)
        {
            foreach (var entry in Vault)
            {
                if (entry.Id != scene || entry.Origin != origin) continue;
                Mark(entry.Id);
                break;
            }
        }

        internal static void Mark(string id)
        {
            if (Found(id)) return;
            PlayerPrefs.SetInt(PreferenceKey(id), 1);
            PlayerPrefs.Save();
        }
    }
}
