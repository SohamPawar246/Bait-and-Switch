using UnityEngine;

namespace BaitSwitch
{
    /// <summary>
    /// References to shared art: shader materials, fonts and Blender-kit prefabs that are not PartDefs.
    /// Built by Tools > Bait & Switch > Build Greybox into Resources/ArtLibrary. Load with ArtLibrary.I.
    /// </summary>
    public class ArtLibrary : ScriptableObject
    {
        static ArtLibrary _i;
        public static ArtLibrary I => _i != null ? _i : (_i = Resources.Load<ArtLibrary>("ArtLibrary"));

        [Header("Materials (shader templates)")]
        public Material toon;
        public Material seaBackdrop;
        public Material menuWater;
        public Material ditherImage;
        public Material ditherFade;
        public Material fx;

        [Header("Fonts (OFL)")]
        public Font display;   // Pirata One
        public Font body;      // Crimson Text
        public Font bodyBold;

        [Header("Kit prefabs")]
        public GameObject boat;
        public GameObject gull;
        public GameObject raincoat;
        public GameObject lantern;
        public GameObject[] hats;
        public GameObject[] rods;
        public GameObject[] hooks;
        public GameObject[] baits;
        public GameObject[] rocks;
        public GameObject[] kelp;
        public GameObject[] coral;
        public GameObject arch;
        public GameObject shrine, candle, crate, skeleton, eggSac;
    }
}
