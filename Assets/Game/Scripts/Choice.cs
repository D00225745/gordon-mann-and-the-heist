using System;
using UnityEngine;

namespace GordonMann
{
    // One pickable option in a choice menu.
    [Serializable]
    public class Choice
    {
        public string label;
        public Sprite icon;
        public string targetScene;
    }
}
