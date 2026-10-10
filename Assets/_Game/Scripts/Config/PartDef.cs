using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaitSwitch
{
    public enum Faction : byte { Fish, Angler }

    public enum Slot : byte
    {
        // Fish gameplay
        Body, Eyes, Fins, Tail, Mouth, Special,
        // Fish cosmetic
        SkinColor, Pattern, GlowColor, EyeColor,
        // Angler gameplay
        Hook, Bait, Line, Rod, Relic,
        // Angler cosmetic
        CoatColor, Hat, FlameColor, BoatPaint, RodFinish, HoodEyes,
    }

    /// <summary>Numeric stats a part can change. Mul stats multiply (base 1), Add stats add, Set stats replace.</summary>
    public enum Stat : byte
    {
        // Fish
        TopSpeedMul, AccelMul, TurnMul, StaminaMul, StaminaRegenMul, HitboxMul,
        NightVisionSet, DashDistanceMul, DashCooldownAdd, NibbleTimeMul, NibbleRangeAdd,
        TwitchMul, TugPullTensionMul, DashTensionSet, SonarOutlineMul, InkCooldownSet, InkRadiusMul,
        LeviathanBiteSet, LightRadiusSet,
        // Angler
        CatchRadiusSet, SnapThresholdSet, SnapThresholdAdd, SinkSpeedMul, StrikeCooldownSet,
        ReelMul, MaxLineSet, HookFollowMul, StrikeJerkSet, HookSpringMul, DinghySpeedMul,
        SonarCooldownSet, SonarRadiusSet, MarkDurationSet, MissScatterMul, RebaitTimeMul,
        CuriosityMul, CuriosityRangeSet, ExtraGrowthAdd, SchoolPointAdd,
    }

    /// <summary>Behaviours that aren't just a number. Gameplay code checks these flags.</summary>
    [Flags]
    public enum Ability : int
    {
        None = 0,
        PuffWhenHooked = 1 << 0,
        FlatfishHide = 1 << 1,      // still on the seabed: invisible to sonar
        PlatedBounce = 1 << 2,      // first catch per round bounces off
        SinkWhenIdle = 1 << 3,
        CyclopsRings = 1 << 4,      // sees hook catch radii
        TeethTell = 1 << 5,         // teeth flash while nibbling
        HammerKnock = 1 << 6,       // dash into a hook knocks it 2 u away
        LampGlow = 1 << 7,          // lights surroundings at night, and glows
        MimicSkin = 1 << 8,         // colour shifts to nearest school fish every 10 s
        ElectricShock = 1 << 9,     // once per tug: Angler can't reel for 1 s
        SpikySnag = 1 << 10,        // hook snags any fish touched while moving fast
        CircleSelfHook = 1 << 11,   // a finished nibble hooks the fish
        GlowBait = 1 << 12,         // lights 3 u around the hook at night
        SpinnerBait = 1 << 13,      // school ignores it; flashes by day
        GhostLine = 1 << 14,        // line invisible underwater
        NeverSnaps = 1 << 15,
        VigilLantern = 1 << 16,     // marks show through ink; lantern cone visible at night
        ChumBucket = 1 << 17,       // E: school gathers for 6 s, 20 s cooldown
        NoSonar = 1 << 18,
    }

    [Serializable]
    public struct StatMod
    {
        public Stat stat;
        public float value;
        public StatMod(Stat s, float v) { stat = s; value = v; }
    }

    /// <summary>One customization option. Built by Tools > Bait & Switch > Build Part Catalog.</summary>
    [CreateAssetMenu(menuName = "Bait & Switch/Part", fileName = "Part")]
    public class PartDef : ScriptableObject
    {
        public byte id;                 // unique per slot; 0 = baseline
        public Slot slot;
        public string displayName;
        [TextArea] public string plus;  // shown green on the stat card
        [TextArea] public string minus; // shown red
        public bool cosmetic;
        public Ability abilities;
        public List<StatMod> mods = new List<StatMod>();
        public Color tint = Color.white; // for colour slots
        public GameObject prefab;        // mesh part, attached at the slot's socket
        public Sprite thumbnail;

        public static Faction FactionOf(Slot s) => s <= Slot.EyeColor ? Faction.Fish : Faction.Angler;
    }

    /// <summary>The resolved numbers for one creature after applying all its parts.</summary>
    public class StatBlock
    {
        readonly Dictionary<Stat, float> _v = new Dictionary<Stat, float>();
        public Ability abilities;

        public void Clear() { _v.Clear(); abilities = Ability.None; }

        public void Apply(PartDef p)
        {
            if (p == null) return;
            abilities |= p.abilities;
            foreach (var m in p.mods)
            {
                string n = m.stat.ToString();
                if (n.EndsWith("Mul")) _v[m.stat] = Get(m.stat, 1f) * m.value;
                else if (n.EndsWith("Add")) _v[m.stat] = Get(m.stat, 0f) + m.value;
                else _v[m.stat] = m.value; // Set
            }
        }

        public float Get(Stat s, float fallback) => _v.TryGetValue(s, out var v) ? v : fallback;
        public float Mul(Stat s) => Get(s, 1f);
        public float Add(Stat s) => Get(s, 0f);
        public bool Has(Ability a) => (abilities & a) != 0;

        /// <summary>Speed caps from docs/09-customization.md: total -25% / +20%.</summary>
        public float TopSpeedMulCapped => Mathf.Clamp(Mul(Stat.TopSpeedMul), 0.75f, 1.2f);
        public float NibbleRangeBonusCapped => Mathf.Min(Add(Stat.NibbleRangeAdd), 0.6f);
    }
}
