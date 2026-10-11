using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>Put on editor-placed kit prefabs: converts their imported materials to toon materials at runtime.</summary>
    public sealed class ToonDressing : MonoBehaviour
    {
        public Color fill = new Color(0.9f, 0.88f, 0.82f);
        public Color glow = new Color(0.6f, 0.96f, 0.77f);
        public float outline = 0.012f;
        public float sway;   // kelp: world units of current sway at the top
        void Awake()
        {
            var look = ToonLook.Plain(outline);
            look.sway = sway;
            ToonLibrary.Apply(gameObject, fill, glow, look);
        }
    }
}
