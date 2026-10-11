using BaitSwitch.Net;
using UnityEngine;

namespace BaitSwitch.Sim
{
    /// <summary>
    /// Fish bot (docs/11-bots.md): Blend (boid with the school) → Forage (greed roll near a still bait) →
    /// Flee (strike or fast hook nearby) → Tug (pull ~1 s, rest ~0.7 s, dash past 60 tension) → Leviathan (bite a boat).
    /// Produces the same inputs a human would.
    /// </summary>
    public sealed class FishBot
    {
        enum S { Blend, Forage, Flee, Leviathan }

        S _state;
        readonly float _greed;
        float _checkT, _stateT, _nibbleT, _cycleT;
        int _hook = -1;
        bool _abortEarly, _pulling, _dashHeld, _inkUsed;

        public FishBot(System.Random rng, GameConfig c)
        {
            _greed = Mathf.Lerp(c.fishBotGreed.x, c.fishBotGreed.y, (float)rng.NextDouble());
        }

        public void Think(GameState g, Seat s, float dt, System.Random rng)
        {
            s.move = Vector2.zero;
            s.buttons = Buttons.None;
            int i = s.fish;
            if (i < 0) return;
            var sea = g.Sea;
            var c = g.Config;
            if (sea.IsHookedOrDead(i))
            {
                sea.AutoPilot[i] = false;
                int t = g.Tugs.FindByFish(i);
                if (t >= 0) TugInput(g, s, ref g.Tugs.Tugs[t], dt);
                return;
            }
            Vector2 pos = sea.Fish[i].pos;
            float cruise = c.schoolCruise / c.fishMaxSpeed;

            if (sea.IsLeviathan(i)) _state = S.Leviathan;
            else if (_state != S.Flee && Threatened(g, pos, out _)) { _state = S.Flee; _stateT = 2f; _inkUsed = false; }

            switch (_state)
            {
                case S.Leviathan:
                {
                    sea.AutoPilot[i] = false;
                    int best = -1; float bestD = float.MaxValue;
                    var hooks = g.Hooks.Hooks;
                    for (int h = 0; h < hooks.Length; h++)
                    {
                        if (!hooks[h].active) continue;
                        float d = Mathf.Abs(hooks[h].boatX - pos.x);
                        if (d < bestD) { bestD = d; best = h; }
                    }
                    if (best < 0) { sea.AutoPilot[i] = true; break; }
                    Vector2 target = new Vector2(hooks[best].boatX, c.surfaceY - 1f);
                    Vector2 to = target - pos;
                    s.move = to.sqrMagnitude > 0.25f ? to.normalized : Vector2.zero;
                    if (to.magnitude < c.leviathanBiteRange) s.buttons |= Buttons.Bite | Buttons.Nibble;
                    if (!sea.IsLeviathan(i)) _state = S.Blend;
                    break;
                }
                case S.Flee:
                {
                    sea.AutoPilot[i] = false;
                    _stateT -= dt;
                    bool still = Threatened(g, pos, out Vector2 from);
                    Vector2 away = (pos - from).sqrMagnitude > 1e-4f ? (pos - from).normalized : FishMotor.Dir(sea.Fish[i].heading);
                    s.move = still ? away : away * cruise;
                    if (still && sea.Fish[i].dashCd <= 0f) Press(ref s.buttons, Buttons.Dash, ref _dashHeld);
                    else _dashHeld = false;
                    if (still && _stateT < 1f && !_inkUsed && sea.InkCooldown(i) <= 0f) { s.buttons |= Buttons.Ink; _inkUsed = true; }
                    if (_stateT <= 0f && !still) _state = S.Blend;
                    break;
                }
                case S.Forage:
                {
                    sea.AutoPilot[i] = false;
                    ref Hook k = ref g.Hooks.Hooks[_hook];
                    bool valid = k.active && k.baited && k.tug < 0 && k.caught < 0 && k.vel.magnitude < c.avoidHookMinSpeed;
                    if (!valid || (k.nibbler >= 0 && k.nibbler != i)) { _state = S.Blend; break; }
                    Vector2 to = k.pos - pos;
                    if (to.magnitude > c.nibbleRange * 0.7f) s.move = to.normalized * cruise;
                    else
                    {
                        s.buttons |= Buttons.Nibble;
                        _nibbleT += dt;
                        if (_abortEarly && _nibbleT > 0.4f) _state = S.Blend;
                    }
                    break;
                }
                default:
                {
                    sea.AutoPilot[i] = true;
                    _checkT -= dt;
                    if (_checkT > 0f) break;
                    _checkT = 0.5f;
                    int h = NearestStillBait(g, pos, 6f);
                    if (h >= 0 && rng.NextDouble() < _greed)
                    {
                        _state = S.Forage; _hook = h; _nibbleT = 0f; _abortEarly = rng.NextDouble() < 0.3;
                    }
                    break;
                }
            }
        }

        void TugInput(GameState g, Seat s, ref Tug t, float dt)
        {
            // Pull about 1 s, rest about 0.7 s; dash when tension passes 60 and dash is ready.
            _cycleT -= dt;
            if (_cycleT <= 0f) { _pulling = !_pulling; _cycleT = _pulling ? 1f : 0.7f; }
            Vector2 away = FishMotor.Dir(t.angle);
            s.move = _pulling ? away : Vector2.zero;
            float tension = Mathf.Max(t.tensionA, t.tensionB);
            if (tension > 60f && g.Sea.Fish[s.fish].dashCd <= 0f) Press(ref s.buttons, Buttons.Dash, ref _dashHeld);
            else _dashHeld = false;
        }

        static bool Threatened(GameState g, Vector2 pos, out Vector2 from)
        {
            from = pos;
            var hooks = g.Hooks.Hooks;
            for (int h = 0; h < hooks.Length; h++)
            {
                if (!hooks[h].active || hooks[h].tug >= 0) continue;
                bool fast = hooks[h].vel.magnitude > 1f || hooks[h].strikeT > 0f;
                if (fast && (hooks[h].pos - pos).sqrMagnitude < 4f) { from = hooks[h].pos; return true; }
            }
            return false;
        }

        static int NearestStillBait(GameState g, Vector2 pos, float range)
        {
            int best = -1; float bestD = range * range;
            for (int h = 0; h < g.Hooks.Hooks.Length; h++)
            {
                if (!g.Hooks.IsStillAndBaited(h) || g.Hooks.Hooks[h].nibbler >= 0) continue;
                float d = (g.Hooks.Hooks[h].pos - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }

        static void Press(ref Buttons b, Buttons key, ref bool held)
        {
            if (!held) b |= key; // one tick down, then released: a clean edge
            held = !held;
        }
    }

    /// <summary>
    /// Angler bot (docs/11-bots.md). Never cheats: it reads twitches, visible tells (dash, ink, size) and its own tug meters.
    /// Parks the hook in the densest school column 2–4 s, strikes 250–450 ms after a twitch (25% too late on purpose),
    /// sonar when ready, reel below 65 tension and release above 80, rebait when empty, drops everything for a Leviathan.
    /// </summary>
    public sealed class AnglerBot
    {
        float _parkT, _reactT = -1f;
        Vector2 _park;
        bool _strikeHeld, _sonarHeld, _reeling;
        readonly int[] _suspect = new int[4];
        readonly float[] _suspectT = new float[4];

        public void Think(GameState g, Seat s, float dt, System.Random rng)
        {
            s.move = Vector2.zero;
            s.buttons = Buttons.None;
            int h = s.hook;
            if (h < 0) return;
            var c = g.Config;
            ref Hook k = ref g.Hooks.Hooks[h];
            s.cursor = k.cursor;
            Remember(g, dt);

            if (k.tug >= 0)
            {
                ref Tug t = ref g.Tugs.Tugs[k.tug];
                float tension = t.hookA == h ? t.tensionA : t.tensionB;
                if (t.Exhausted || tension < c.anglerBotReelBelow) _reeling = true;
                if (tension > c.anglerBotReleaseAbove && !t.Exhausted) _reeling = false;
                if (_reeling) s.buttons |= Buttons.Reel;
                return;
            }
            if (k.caught >= 0) return;
            if (!k.baited && k.tangleT <= 0f) { s.buttons |= Buttons.Rebait; return; }

            int lev = -1;
            for (int i = 0; i < g.Sea.Count; i++)
                if (g.Sea.IsLeviathan(i) && (g.Sea.Fish[i].flags & (byte)FishFlags.Dead) == 0) { lev = i; break; }

            _parkT -= dt;
            if (lev >= 0) _park = g.Sea.Fish[lev].pos;
            else if (_parkT <= 0f && g.Hooks.NibbleAge(h) < 0f) PickPark(g, rng);

            float dx = _park.x - k.boatX;
            if (Mathf.Abs(dx) > 3f) s.move.x = Mathf.Clamp(dx / 3f, -1f, 1f);
            s.cursor = _park;

            // Strike timing: react to the twitch, strike if it lasted > 0.35 s or on a 30% gamble.
            float age = g.Hooks.NibbleAge(h);
            bool wantStrike = false;
            if (lev >= 0 && (k.pos - g.Sea.Fish[lev].pos).magnitude < c.catchRadius * c.leviathanScale) wantStrike = true;
            if (age >= 0f && _reactT < 0f)
                _reactT = Mathf.Lerp(c.anglerBotReaction.x, c.anglerBotReaction.y, (float)rng.NextDouble())
                          + (rng.NextDouble() < c.anglerBotMiss ? 0.5f : 0f);
            if (_reactT >= 0f)
            {
                _reactT -= dt;
                if (_reactT <= 0f)
                {
                    _reactT = -1f;
                    if (age > 0.35f || rng.NextDouble() < 0.3) wantStrike = true;
                }
            }
            if (age < 0f && _reactT > 0.6f) _reactT = -1f;
            if (wantStrike && !_strikeHeld) { s.buttons |= Buttons.Strike; _strikeHeld = true; }
            else _strikeHeld = false;

            if (k.sonarCd <= 0f && g.FishLeft >= 2 && !_sonarHeld) { s.buttons |= Buttons.Sonar; _sonarHeld = true; }
            else _sonarHeld = false;
        }

        void PickPark(GameState g, System.Random rng)
        {
            var c = g.Config;
            _parkT = Mathf.Lerp(2f, 4f, (float)rng.NextDouble());
            float hx = c.seaSize.x * 0.5f - 1f;
            // Prefer a remembered suspect; else the densest column of the school.
            for (int j = 0; j < _suspect.Length; j++)
            {
                if (_suspectT[j] <= 0f || rng.NextDouble() > 0.6) continue;
                int f = _suspect[j];
                if ((g.Sea.Fish[f].flags & (byte)FishFlags.Dead) != 0) continue;
                _park = g.Sea.Fish[f].pos + new Vector2(0f, 0.5f);
                return;
            }
            const int bins = 10;
            int bestBin = 0, bestN = -1;
            for (int b = 0; b < bins; b++)
            {
                float x0 = Mathf.Lerp(-hx, hx, b / (float)bins), x1 = Mathf.Lerp(-hx, hx, (b + 1) / (float)bins);
                int n = 0;
                for (int i = 0; i < g.Sea.Count; i++)
                {
                    var f = g.Sea.Fish[i];
                    if ((f.flags & (byte)FishFlags.Dead) == 0 && f.pos.x >= x0 && f.pos.x < x1) n++;
                }
                if (n > bestN) { bestN = n; bestBin = b; }
            }
            float x = Mathf.Lerp(-hx, hx, (bestBin + (float)rng.NextDouble()) / bins);
            float y = Mathf.Lerp(c.seabedY + 3f, c.surfaceY - 3f, (float)rng.NextDouble());
            _park = new Vector2(x, y);
        }

        void Remember(GameState g, float dt)
        {
            for (int j = 0; j < _suspectT.Length; j++) if (_suspectT[j] > 0f) _suspectT[j] -= dt;
            var sea = g.Sea;
            for (int i = 0; i < sea.Count; i++)
            {
                byte f = sea.Fish[i].flags;
                bool tell = (f & (byte)(FishFlags.Dashing | FishFlags.Inked)) != 0 || sea.Fish[i].size > 0;
                if (!tell || (f & (byte)FishFlags.Dead) != 0) continue;
                int slot = 0; float oldest = float.MaxValue;
                for (int j = 0; j < _suspect.Length; j++)
                {
                    if (_suspect[j] == i && _suspectT[j] > 0f) { slot = j; break; }
                    if (_suspectT[j] < oldest) { oldest = _suspectT[j]; slot = j; }
                }
                _suspect[slot] = i;
                _suspectT[slot] = 5f;
            }
        }
    }
}
