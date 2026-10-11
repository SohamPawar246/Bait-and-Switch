using System.Collections.Generic;
using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>How a Fill surface is painted (body gradient, pattern, fin ribs, gloss).</summary>
    public struct ToonLook
    {
        public float gradient;     // 0..1 belly-to-back painting
        public int pattern;        // 0 none, 1 stripes, 2 spots, 3 freckles, 4 scars
        public Color patternColor;
        public float ribs;         // 0..1 fin/tail ribs
        public float gloss;        // 0..1
        public float outline;
        public float scales;       // 0..1 fine body scales
        public float sway;         // world units of current sway at the top (kelp)

        public static ToonLook Plain(float outline = 0.016f) => new ToonLook { gloss = 0.25f, outline = outline, patternColor = new Color(0.12f, 0.08f, 0.16f) };
        public static ToonLook Body(int pattern, Color patternColor) => new ToonLook { gradient = 1f, pattern = pattern, patternColor = patternColor, gloss = 0.5f, outline = 0.018f, scales = 1f };
        public static ToonLook Fin() => new ToonLook { ribs = 1f, gloss = 0.2f, outline = 0.012f, patternColor = new Color(0.12f, 0.08f, 0.16f) };
    }

    /// <summary>
    /// Cached BaitSwitch/Toon materials (one per colour + look, so the SRP batcher keeps batching).
    /// Maps the Blender kit's material names (Fill, EyeWhite, Pupil, Void, HoodEyes, Detail_RRGGBB…) to toon colours;
    /// Fill slots take the tint and look passed in (fish skin, raincoat, boat paint).
    /// </summary>
    public static class ToonLibrary
    {
        struct Key
        {
            public Color32 b, e, pc;
            public byte grad, pattern, ribs, gloss, scales, sway;
            public ushort outline;
        }

        static readonly Dictionary<Key, Material> _cache = new Dictionary<Key, Material>();

        static readonly Dictionary<string, (string hex, float glow)> Named = new Dictionary<string, (string, float)>
        {
            { "EyeWhite", ("#F7EFE2", 0f) }, { "Pupil", ("#120C18", 0f) }, { "Highlight", ("#FFFFFF", 1.6f) },
            { "Iris", ("#C77DFF", 0f) }, { "Lid", ("#1E1428", 0f) },
            { "Teeth", ("#F7EFE2", 0f) }, { "MouthInk", ("#2A1530", 0f) }, { "Glow", ("#9BF6C4", 2.2f) },
            { "Spark", ("#7FD8FF", 2.0f) }, { "InkSac", ("#1B2440", 0f) }, { "Void", ("#05030A", 0f) },
            { "HoodEyes", ("#F2C14E", 3.5f) }, { "Iron", ("#2B2B30", 0f) }, { "Flame", ("#F2C14E", 3.5f) },
            { "CandleFlame", ("#F2C14E", 3.5f) }, { "Wax", ("#F4E9D8", 0f) }, { "Stone", ("#6E6A64", 0f) },
            { "Wood", ("#6A4A33", 0f) }, { "WoodDark", ("#4A3222", 0f) }, { "Plank", ("#4A3220", 0f) }, { "Trim", ("#3B2C22", 0f) },
            { "Sigil", ("#1E1428", 0f) }, { "SigilGlow", ("#C77DFF", 2.6f) }, { "Rock", ("#4A5868", 0f) },
            { "Kelp", ("#3E8A55", 0f) }, { "KelpStem", ("#24563B", 0f) }, { "EggSac", ("#C9E8D8", 0.25f) },
            { "Glass", ("#9FC7C2", 0.1f) }, { "Metal", ("#8A8F96", 0f) }, { "GullWing", ("#8E97A3", 0f) },
            { "GullInk", ("#1E1428", 0f) }, { "GullBeak", ("#F2C14E", 0f) }, { "HookRed", ("#D64545", 0f) },
            { "Steel", ("#C9CED6", 0f) }, { "GlowBait", ("#9BF6C4", 3f) }, { "Brass", ("#C9A86A", 0f) },
            { "Chrome", ("#E8E2D0", 0.6f) }, { "Lure", ("#D64545", 0f) }, { "LureEye", ("#F4E9D8", 0f) },
            { "Stink", ("#7A8C3A", 0f) }, { "Worm", ("#E07A8A", 0f) }, { "Fill_Gull", ("#F4F2EC", 0f) },
            { "Ink", ("#1E1428", 0f) }, { "Rope", ("#B9A27A", 0f) }, { "Water", ("#2E5A5A", 0f) },
        };

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h.StartsWith("#") ? h : "#" + h, out var c);
            return c;
        }

        public static Material Get(Color baseColor, Color emission, float outline = 0.016f) =>
            Get(baseColor, emission, ToonLook.Plain(outline));

        public static Material Get(Color baseColor, Color emission, ToonLook look)
        {
            var key = new Key
            {
                b = baseColor,
                e = new Color(Mathf.Clamp01(emission.r / 4f), Mathf.Clamp01(emission.g / 4f), Mathf.Clamp01(emission.b / 4f)),
                pc = look.patternColor,
                grad = (byte)Mathf.RoundToInt(look.gradient * 255f),
                pattern = (byte)look.pattern,
                ribs = (byte)Mathf.RoundToInt(look.ribs * 255f),
                gloss = (byte)Mathf.RoundToInt(look.gloss * 255f),
                scales = (byte)Mathf.RoundToInt(look.scales * 255f),
                sway = (byte)Mathf.Clamp(Mathf.RoundToInt(look.sway * 100f), 0, 255),
                outline = (ushort)Mathf.Clamp(Mathf.RoundToInt(look.outline * 4000f), 0, 65535),
            };
            if (_cache.TryGetValue(key, out var m) && m != null) return m;
            var lib = ArtLibrary.I;
            m = lib != null && lib.toon != null ? new Material(lib.toon) : new Material(Shader.Find("BaitSwitch/Toon"));
            m.name = "Toon_" + ColorUtility.ToHtmlStringRGB(baseColor);
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_OutlineWidth", look.outline);
            m.SetFloat("_Gradient", look.gradient);
            m.SetFloat("_Pattern", look.pattern);
            m.SetColor("_PatternColor", look.patternColor);
            m.SetFloat("_Ribs", look.ribs);
            m.SetFloat("_Gloss", look.gloss);
            m.SetFloat("_Scales", look.scales);
            m.SetFloat("_Sway", look.sway);
            m.enableInstancing = true;
            _cache[key] = m;
            return m;
        }

        static string Canon(string n)
        {
            int dot = n.IndexOf('.');
            if (dot >= 0) n = n.Substring(0, dot);
            return n.Replace(" (Instance)", "");
        }

        /// <summary>The toon material for one of the kit's source materials. fill/look paint the Fill slots.</summary>
        public static Material Map(Material src, Color fill, Color glowTint, ToonLook look, Color eyeTint = default)
        {
            string n = src != null ? Canon(src.name) : "Fill";
            if (n.StartsWith("Fill") && n != "Fill_Gull") return Get(fill, Color.black, look);
            var plain = ToonLook.Plain(look.outline);
            plain.sway = look.sway;
            if (n == "EyeWhite") return Get(eyeTint.a > 0f ? Color.Lerp(Hex("#F7EFE2"), eyeTint, 0.35f) : Hex("#F7EFE2"), Color.black, new ToonLook { gloss = 0.9f, outline = look.outline * 0.7f });
            if (n == "Iris") return Get(eyeTint.a > 0f ? eyeTint : Hex("#8A5A9E"), Color.black, new ToonLook { gloss = 0.6f, outline = 0f });
            if (n == "Pupil") return Get(Hex("#120C18"), Color.black, new ToonLook { gloss = 1f, outline = 0f });
            if (n == "Glow" || n == "Spark" || n == "HoodEyes") return Get(glowTint, glowTint * 3f, plain);
            if (n == "Flame" || n == "CandleFlame") return Get(glowTint, glowTint * 3.5f, new ToonLook { outline = 0f });
            if (Named.TryGetValue(n, out var v))
            {
                var c = Hex(v.hex);
                return Get(c, v.glow > 0 ? c * v.glow : Color.black, plain);
            }
            int us = n.LastIndexOf('_');
            if (us >= 0 && n.Length - us - 1 == 6 && ColorUtility.TryParseHtmlString("#" + n.Substring(us + 1), out var hc))
                return Get(hc, Color.black, plain);
            return Get(new Color(0.72f, 0.7f, 0.66f), Color.black, plain);
        }

        public static Material Map(Material src, Color fill, Color glowTint, float outline = 0.016f, Color eyeTint = default) =>
            Map(src, fill, glowTint, ToonLook.Plain(outline), eyeTint);

        /// <summary>Swap every renderer's materials under root to toon materials.</summary>
        public static void Apply(GameObject root, Color fill, Color glowTint, float outline = 0.016f, Color eyeTint = default) =>
            Apply(root, fill, glowTint, ToonLook.Plain(outline), eyeTint);

        public static void Apply(GameObject root, Color fill, Color glowTint, ToonLook look, Color eyeTint = default)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer) continue;
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++) dst[i] = Map(src[i], fill, glowTint, look, eyeTint);
                r.sharedMaterials = dst;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }
}
