using System.Collections.Generic;
using UnityEngine;
using S = BaitSwitch.Stat;
using A = BaitSwitch.Ability;

namespace BaitSwitch
{
    /// <summary>
    /// All customization parts. The asset lives at Resources/PartCatalog and is generated from Specs()
    /// by Tools > Bait & Switch > Build Part Catalog (docs/09-customization.md is the source of truth).
    /// An appearance is one byte per slot (the part id), so it packs into 21 bytes.
    /// </summary>
    [CreateAssetMenu(menuName = "Bait & Switch/Part Catalog", fileName = "PartCatalog")]
    public class PartCatalog : ScriptableObject
    {
        static PartCatalog _i;
        public static PartCatalog I => _i != null ? _i : (_i = Resources.Load<PartCatalog>("PartCatalog"));

        public List<PartDef> parts = new List<PartDef>();

        Dictionary<int, PartDef> _map;
        static int Key(Slot s, byte id) => ((int)s << 8) | id;

        public PartDef Find(Slot s, byte id)
        {
            if (_map == null)
            {
                _map = new Dictionary<int, PartDef>();
                foreach (var p in parts) if (p != null) _map[Key(p.slot, p.id)] = p;
            }
            return _map.TryGetValue(Key(s, id), out var d) ? d : null;
        }

        public List<PartDef> OfSlot(Slot s)
        {
            var list = new List<PartDef>();
            foreach (var p in parts) if (p != null && p.slot == s) list.Add(p);
            list.Sort((a, b) => a.id.CompareTo(b.id));
            return list;
        }

        public int CountOf(Slot s) { int n = 0; foreach (var p in parts) if (p != null && p.slot == s) n++; return n; }

        /// <summary>Resolve the stats for one faction from an appearance (byte per Slot).</summary>
        public void Resolve(byte[] appearance, Faction f, StatBlock into)
        {
            into.Clear();
            for (int i = 0; i < appearance.Length && i < SlotCount; i++)
            {
                var slot = (Slot)i;
                if (PartDef.FactionOf(slot) != f) continue;
                into.Apply(Find(slot, appearance[i]));
            }
        }

        public const int SlotCount = (int)Slot.HoodEyes + 1; // 21

        /// <summary>A random appearance using every option in equal measure (for bots and the school).</summary>
        public byte[] RandomAppearance(System.Random rng)
        {
            var a = new byte[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                int n = CountOf((Slot)i);
                a[i] = (byte)(n > 0 ? rng.Next(n) : 0);
            }
            return a;
        }

        // ------------------------------------------------------------------ data

        public struct Spec
        {
            public Slot slot; public byte id; public string name, plus, minus;
            public A abilities; public StatMod[] mods; public bool cosmetic; public Color tint;
        }

        static Spec P(Slot slot, byte id, string name, string plus, string minus, A ab = A.None, params StatMod[] mods)
            => new Spec { slot = slot, id = id, name = name, plus = plus, minus = minus, abilities = ab, mods = mods };

        static Spec C(Slot slot, byte id, string name, Color tint = default)
            => new Spec { slot = slot, id = id, name = name, plus = "", minus = "", cosmetic = true, tint = tint, mods = new StatMod[0] };

        static StatMod M(S s, float v) => new StatMod(s, v);

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        public static List<Spec> Specs()
        {
            var L = new List<Spec>
            {
                // ---------------- Fish: Body
                P(Slot.Body, 0, "Round", "Baseline", ""),
                P(Slot.Body, 1, "Eel", "+12% top speed", "-20% stamina in tugs", A.None, M(S.TopSpeedMul, 1.12f), M(S.StaminaMul, 0.8f)),
                P(Slot.Body, 2, "Puffer", "+25% stamina; puffs up when hooked", "-10% top speed", A.PuffWhenHooked, M(S.StaminaMul, 1.25f), M(S.TopSpeedMul, 0.9f)),
                P(Slot.Body, 3, "Flatfish", "Still on the seabed: hidden from sonar", "-15% acceleration", A.FlatfishHide, M(S.AccelMul, 0.85f)),
                P(Slot.Body, 4, "Plated", "The first hook each round bounces off", "-15% acceleration; sinks when idle", A.PlatedBounce | A.SinkWhenIdle, M(S.AccelMul, 0.85f)),

                // ---------------- Fish: Eyes (night vision = how far you see hooks in the dark)
                P(Slot.Eyes, 0, "Two", "See hooks in the dark within 2 units", "", A.None, M(S.NightVisionSet, 2f)),
                P(Slot.Eyes, 1, "Three", "See hooks in the dark within 4 units", "5% bigger hitbox", A.None, M(S.NightVisionSet, 4f), M(S.HitboxMul, 1.05f)),
                P(Slot.Eyes, 2, "Seven", "See hooks in the dark within 6 units", "10% bigger hitbox", A.None, M(S.NightVisionSet, 6f), M(S.HitboxMul, 1.1f)),
                P(Slot.Eyes, 3, "Cyclops", "See every hook's catch radius", "Night vision drops to 1 unit", A.CyclopsRings, M(S.NightVisionSet, 1f)),

                // ---------------- Fish: Fins
                P(Slot.Fins, 0, "Stubby", "Baseline", ""),
                P(Slot.Fins, 1, "Big fins", "Dash goes 30% further", "Dash cooldown +0.6 s", A.None, M(S.DashDistanceMul, 1.3f), M(S.DashCooldownAdd, 0.6f)),
                P(Slot.Fins, 2, "Wing fins", "Turn 25% faster", "-10% acceleration", A.None, M(S.TurnMul, 1.25f), M(S.AccelMul, 0.9f)),

                // ---------------- Fish: Tail
                P(Slot.Tail, 0, "Fan", "Baseline", ""),
                P(Slot.Tail, 1, "Fork", "+12% acceleration", "-15% stamina regeneration", A.None, M(S.AccelMul, 1.12f), M(S.StaminaRegenMul, 0.85f)),
                P(Slot.Tail, 2, "Curl", "+20% stamina regeneration", "-10% acceleration", A.None, M(S.StaminaRegenMul, 1.2f), M(S.AccelMul, 0.9f)),
                P(Slot.Tail, 3, "Thrasher", "A dash in a tug adds +35 tension", "Dash cooldown +0.4 s", A.None, M(S.DashTensionSet, 35f), M(S.DashCooldownAdd, 0.4f)),

                // ---------------- Fish: Mouth
                P(Slot.Mouth, 0, "Small", "Baseline", ""),
                P(Slot.Mouth, 1, "Needle teeth", "Nibble 20% faster", "Teeth flash while you nibble", A.TeethTell, M(S.NibbleTimeMul, 0.8f)),
                P(Slot.Mouth, 2, "Big jaw", "Nibble in 0.4 s", "Every nibble twitches the line twice as hard", A.None, M(S.NibbleTimeMul, 0.6667f), M(S.TwitchMul, 2f)),
                P(Slot.Mouth, 3, "Sucker", "Nibble from 0.3 units further", "Nibble 15% slower", A.None, M(S.NibbleRangeAdd, 0.3f), M(S.NibbleTimeMul, 1.15f)),

                // ---------------- Fish: Special
                P(Slot.Special, 0, "None", "Travel light: +5% top speed", "", A.None, M(S.TopSpeedMul, 1.05f)),
                P(Slot.Special, 1, "Hammerhead", "Leviathan bite 1.4 s; dash knocks hooks away", "Turn 15% slower", A.HammerKnock, M(S.LeviathanBiteSet, 1.4f), M(S.TurnMul, 0.85f)),
                P(Slot.Special, 2, "Tentacles", "Nibble from 0.4 units further", "-8% top speed", A.None, M(S.NibbleRangeAdd, 0.4f), M(S.TopSpeedMul, 0.92f)),
                P(Slot.Special, 3, "Spines", "+15% tension on every pull", "Sonar outlines you twice as long", A.None, M(S.TugPullTensionMul, 1.15f), M(S.SonarOutlineMul, 2f)),
                P(Slot.Special, 4, "Angler lamp", "Lights your surroundings at night", "You glow at night", A.LampGlow, M(S.LightRadiusSet, 5f)),
                P(Slot.Special, 5, "Mimic skin", "Your colour shifts to match the school", "-15% stamina", A.MimicSkin, M(S.StaminaMul, 0.85f)),
                P(Slot.Special, 6, "Ink gland", "Ink cooldown 8 s, 30% bigger cloud", "-10% stamina", A.None, M(S.InkCooldownSet, 8f), M(S.InkRadiusMul, 1.3f), M(S.StaminaMul, 0.9f)),
                P(Slot.Special, 7, "Electric organ", "Once per tug, shock: no reeling for 1 s", "-10% top speed; sparks give you away", A.ElectricShock, M(S.TopSpeedMul, 0.9f)),

                // ---------------- Angler: Hook
                P(Slot.Hook, 0, "J hook", "Baseline", "", A.None, M(S.CatchRadiusSet, 0.6f), M(S.SnapThresholdSet, 100f)),
                P(Slot.Hook, 1, "Spiky", "Snags any fish it touches while moving fast", "Snaps at 80", A.SpikySnag, M(S.SnapThresholdSet, 80f)),
                P(Slot.Hook, 2, "Barbed", "Snaps at 120", "Catch radius 0.45", A.None, M(S.SnapThresholdSet, 120f), M(S.CatchRadiusSet, 0.45f)),
                P(Slot.Hook, 3, "Big", "Catch radius 0.8", "Sinks 30% slower; strike cooldown 1.3 s", A.None, M(S.CatchRadiusSet, 0.8f), M(S.SinkSpeedMul, 0.7f), M(S.StrikeCooldownSet, 1.3f)),
                P(Slot.Hook, 4, "Circle", "A full nibble hooks the fish by itself", "Strike catch radius only 0.4", A.CircleSelfHook, M(S.CatchRadiusSet, 0.4f)),
                P(Slot.Hook, 5, "Treble", "Strike cooldown 0.7 s", "Snaps at 90", A.None, M(S.StrikeCooldownSet, 0.7f), M(S.SnapThresholdSet, 90f)),

                // ---------------- Angler: Bait
                P(Slot.Bait, 0, "Worm", "Baseline", ""),
                P(Slot.Bait, 1, "Glow orb", "Lights 3 units at night; school more curious", "Fish see your hook from far away", A.GlowBait, M(S.CuriosityMul, 1.5f)),
                P(Slot.Bait, 2, "Chunky lure", "School twice as curious", "+1 extra growth for whoever eats it", A.None, M(S.CuriosityMul, 2f), M(S.ExtraGrowthAdd, 1f)),
                P(Slot.Bait, 3, "Spinner", "School ignores it: every nibble is a player", "Flashes by day; +2 growth if eaten", A.SpinnerBait, M(S.CuriosityMul, 0f), M(S.ExtraGrowthAdd, 2f)),
                P(Slot.Bait, 4, "Stink bait", "Draws school fish from 10 units", "Constant decoy twitches", A.None, M(S.CuriosityRangeSet, 10f), M(S.CuriosityMul, 1.5f)),

                // ---------------- Angler: Line
                P(Slot.Line, 0, "Mono", "Baseline", ""),
                P(Slot.Line, 1, "Steel", "Never snaps", "Reel x0.7; heavier swing", A.NeverSnaps, M(S.ReelMul, 0.7f), M(S.HookSpringMul, 0.85f)),
                P(Slot.Line, 2, "Silk", "Reel x1.3", "Snap threshold -15", A.None, M(S.ReelMul, 1.3f), M(S.SnapThresholdAdd, -15f)),
                P(Slot.Line, 3, "Ghost line", "Invisible underwater", "Snap threshold -10", A.GhostLine, M(S.SnapThresholdAdd, -10f)),

                // ---------------- Angler: Rod
                P(Slot.Rod, 0, "Short", "Baseline (20 unit line)", "", A.None, M(S.MaxLineSet, 20f)),
                P(Slot.Rod, 1, "Long", "Max line 26 units", "Hook follows 20% slower", A.None, M(S.MaxLineSet, 26f), M(S.HookFollowMul, 0.8f)),
                P(Slot.Rod, 2, "Whip", "Bigger, faster strike", "Reel -10%", A.None, M(S.StrikeJerkSet, 1.6f), M(S.ReelMul, 0.9f)),
                P(Slot.Rod, 3, "Heavy", "Reel +20%", "Sluggish aim", A.None, M(S.ReelMul, 1.2f), M(S.HookSpringMul, 0.75f)),

                // ---------------- Angler: Relic
                P(Slot.Relic, 0, "None", "Dinghy +15% speed", "", A.None, M(S.DinghySpeedMul, 1.15f)),
                P(Slot.Relic, 1, "Sonar bell", "Sonar cooldown 10 s", "Sonar radius 9 units", A.None, M(S.SonarCooldownSet, 10f), M(S.SonarRadiusSet, 9f)),
                P(Slot.Relic, 2, "Vigil lantern", "Marks last 6 s and show through ink", "Your lantern cone is visible at night", A.VigilLantern, M(S.MarkDurationSet, 6f)),
                P(Slot.Relic, 3, "Chum bucket", "E: the school gathers for 6 s", "No sonar", A.ChumBucket | A.NoSonar),
                P(Slot.Relic, 4, "Offering bowl", "+1 point per school fish landed", "Missed strikes scatter 50% wider", A.None, M(S.SchoolPointAdd, 1f), M(S.MissScatterMul, 1.5f)),
                P(Slot.Relic, 5, "Deep weight", "Hook sinks twice as fast", "Rebaiting is 30% slower", A.None, M(S.SinkSpeedMul, 2f), M(S.RebaitTimeMul, 1.3f)),
            };

            // ---------------- Cosmetics
            string[] skins = { "#FF8C7A", "#B79CED", "#B8D86B", "#3FA7A3", "#FFB3A7", "#D4C4F6", "#D8EBA6", "#86CFCB" };
            for (byte i = 0; i < skins.Length; i++) L.Add(C(Slot.SkinColor, i, "Skin " + (i + 1), Hex(skins[i])));
            string[] patterns = { "Plain", "Stripes", "Spots", "Scars", "Freckles" };
            for (byte i = 0; i < patterns.Length; i++) L.Add(C(Slot.Pattern, i, patterns[i]));
            string[] glows = { "#9BF6C4", "#C77DFF", "#F2C14E", "#7FD8FF" };
            for (byte i = 0; i < glows.Length; i++) L.Add(C(Slot.GlowColor, i, "Glow " + (i + 1), Hex(glows[i])));
            string[] eyes = { "#1E1428", "#D64545", "#F2C14E", "#9BF6C4" };
            for (byte i = 0; i < eyes.Length; i++) L.Add(C(Slot.EyeColor, i, "Eyes " + (i + 1), Hex(eyes[i])));

            string[] coats = { "#F2C14E", "#D64545", "#1F5F7A", "#B79CED", "#F4E9D8", "#1E1428" };
            for (byte i = 0; i < coats.Length; i++) L.Add(C(Slot.CoatColor, i, "Coat " + (i + 1), Hex(coats[i])));
            string[] hats = { "Sou'wester", "Bucket", "Pointed hood", "None" };
            for (byte i = 0; i < hats.Length; i++) L.Add(C(Slot.Hat, i, hats[i]));
            string[] flames = { "#F2C14E", "#9BF6C4", "#C77DFF", "#D64545" };
            for (byte i = 0; i < flames.Length; i++) L.Add(C(Slot.FlameColor, i, "Flame " + (i + 1), Hex(flames[i])));
            string[] paints = { "#F4E9D8", "#1F5F7A", "#D64545", "#1E1428" };
            for (byte i = 0; i < paints.Length; i++) L.Add(C(Slot.BoatPaint, i, "Paint " + (i + 1), Hex(paints[i])));
            string[] finishes = { "Driftwood", "Bone", "Black lacquer" };
            for (byte i = 0; i < finishes.Length; i++) L.Add(C(Slot.RodFinish, i, finishes[i]));
            string[] hoodEyes = { "#F2C14E", "#9BF6C4", "#D64545", "#F4E9D8" };
            for (byte i = 0; i < hoodEyes.Length; i++) L.Add(C(Slot.HoodEyes, i, "Gaze " + (i + 1), Hex(hoodEyes[i])));

            return L;
        }
    }
}
