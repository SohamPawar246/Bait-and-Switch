using UnityEngine;

namespace BaitSwitch.UI
{
    /// <summary>Player settings and identity, saved in PlayerPrefs. Every read is cheap (cached).</summary>
    public static class GameSettings
    {
        const string KMaster = "bs.master", KMusic = "bs.music", KSfx = "bs.sfx", KMouse = "bs.swimMouse", KFull = "bs.fullscreen",
                     KCursor = "bs.systemCursor", KName = "bs.name", KAppearance = "bs.appearance", KIntro = "bs.introSeen", KRules = "bs.rulesSeen", KTipsF = "bs.tipsFish", KTipsA = "bs.tipsAngler";

        public static float Master { get => PlayerPrefs.GetFloat(KMaster, 0.8f); set { PlayerPrefs.SetFloat(KMaster, value); AudioListener.volume = value; } }
        public static float Music { get => PlayerPrefs.GetFloat(KMusic, 0.7f); set => PlayerPrefs.SetFloat(KMusic, value); }
        public static float Sfx { get => PlayerPrefs.GetFloat(KSfx, 0.8f); set => PlayerPrefs.SetFloat(KSfx, value); }
        public static bool SwimWithMouse { get => PlayerPrefs.GetInt(KMouse, 0) == 1; set => PlayerPrefs.SetInt(KMouse, value ? 1 : 0); }
        public static bool SystemCursor { get => PlayerPrefs.GetInt(KCursor, 0) == 1; set => PlayerPrefs.SetInt(KCursor, value ? 1 : 0); }
        public static bool IntroSeen { get => PlayerPrefs.GetInt(KIntro, 0) == 1; set => PlayerPrefs.SetInt(KIntro, value ? 1 : 0); }
        public static bool RulesSeen { get => PlayerPrefs.GetInt(KRules, 0) == 1; set => PlayerPrefs.SetInt(KRules, value ? 1 : 0); }
        public static bool TipsFishSeen { get => PlayerPrefs.GetInt(KTipsF, 0) == 1; set => PlayerPrefs.SetInt(KTipsF, value ? 1 : 0); }
        public static bool TipsAnglerSeen { get => PlayerPrefs.GetInt(KTipsA, 0) == 1; set => PlayerPrefs.SetInt(KTipsA, value ? 1 : 0); }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(KFull, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(KFull, value ? 1 : 0);
                Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            }
        }

        public static string Name
        {
            get
            {
                string n = PlayerPrefs.GetString(KName, "");
                if (string.IsNullOrWhiteSpace(n)) { n = Copy.NameSuggestions[Random.Range(0, Copy.NameSuggestions.Length)]; PlayerPrefs.SetString(KName, n); }
                return n;
            }
            set => PlayerPrefs.SetString(KName, string.IsNullOrWhiteSpace(value) ? "" : value.Trim());
        }

        /// <summary>The 21 appearance bytes (PartCatalog.SlotCount), random the first time.</summary>
        public static byte[] Appearance
        {
            get
            {
                string s = PlayerPrefs.GetString(KAppearance, "");
                byte[] a = null;
                try { if (s.Length > 0) a = System.Convert.FromBase64String(s); } catch { a = null; }
                if (a == null || a.Length != PartCatalog.SlotCount)
                {
                    a = PartCatalog.I != null ? PartCatalog.I.RandomAppearance(new System.Random()) : new byte[PartCatalog.SlotCount];
                    Appearance = a;
                }
                return a;
            }
            set => PlayerPrefs.SetString(KAppearance, System.Convert.ToBase64String(value));
        }

        public static void ApplyAll()
        {
            AudioListener.volume = Master;
#if !UNITY_WEBGL
            if (!Application.isEditor) Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
        }
    }
}
