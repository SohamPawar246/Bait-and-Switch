using BaitSwitch.Net;
using UnityEngine;

namespace BaitSwitch.Sim
{
    public struct Tug
    {
        public bool active;
        public int fish;
        public int hookA, hookB;      // hookB >= 0 only in a Leviathan team tug
        public float distance;
        public float tensionA, tensionB;
        public float stamina, maxStamina;
        public float t, exhaustT, singleT;
        public float fatigue;         // threshold lost so far
        public float angle;           // direction from hook A's rod tip to the fish (deg)
        public bool leviathan, dashHeld;
        public float pull;            // last pull 0..1 (for bots/HUD)

        public bool Exhausted => exhaustT > 0f;
    }

    public interface ITugRules
    {
        void OnTugLanded(int tug);
        /// <summary>Fish got away. snapped = a line broke (escape points); false = torn loose / single hook broke.</summary>
        void OnTugEscaped(int tug, bool snapped);
    }

    /// <summary>
    /// Server tug-of-war (docs/07-tug.md). Tension, distance, stamina per tick; dash spikes; exhaustion;
    /// line fatigue after 6 s and tear-loose at 12 s; Leviathan team tug with two hooks.
    /// </summary>
    public sealed class TugSystem
    {
        public readonly Tug[] Tugs;
        public ITugRules Rules;

        readonly GameConfig _c;
        readonly SeaSim _sea;
        readonly HookSim _hooks;
        readonly EventQueue _events;

        public TugSystem(GameConfig c, SeaSim sea, HookSim hooks, EventQueue events)
        {
            _c = c; _sea = sea; _hooks = hooks; _events = events;
            Tugs = new Tug[c.seats];
        }

        public int FindByFish(int fish)
        {
            for (int i = 0; i < Tugs.Length; i++) if (Tugs[i].active && Tugs[i].fish == fish) return i;
            return -1;
        }

        public float Threshold(in Tug g) => _c.snapThreshold - g.fatigue;

        /// <summary>Start a tug (or join a Leviathan's existing one as the second hook). Returns tug index or -1.</summary>
        public int Start(int hook, int fish)
        {
            var c = _c;
            int existing = FindByFish(fish);
            if (existing >= 0)
            {
                ref Tug e = ref Tugs[existing];
                if (!e.leviathan || e.hookB >= 0 || e.hookA == hook) return -1;
                e.hookB = hook;
                e.tensionB = 0f;
                e.singleT = 0f;
                _hooks.Hooks[hook].tug = existing;
                return existing;
            }
            for (int i = 0; i < Tugs.Length; i++)
            {
                if (Tugs[i].active) continue;
                Vector2 tip = HookPhysics.RodTip(_hooks.Hooks[hook].boatX, c);
                Vector2 d = _sea.Fish[fish].pos - tip;
                bool lev = _sea.IsLeviathan(fish);
                Tugs[i] = new Tug
                {
                    active = true, fish = fish, hookA = hook, hookB = -1,
                    distance = d.magnitude,
                    stamina = lev ? c.leviathanStamina : 100f,
                    maxStamina = lev ? c.leviathanStamina : 100f,
                    angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg,
                    leviathan = lev,
                };
                _hooks.Hooks[hook].tug = i;
                return i;
            }
            return -1;
        }

        public void Step(float dt)
        {
            for (int i = 0; i < Tugs.Length; i++)
                if (Tugs[i].active) StepTug(i, ref Tugs[i], dt);
        }

        void StepTug(int idx, ref Tug g, float dt)
        {
            var c = _c;
            ref FishBody fish = ref _sea.Fish[g.fish];
            ref Hook ha = ref _hooks.Hooks[g.hookA];
            Vector2 tip = HookPhysics.RodTip(ha.boatX, c);
            Vector2 away = FishMotor.Dir(g.angle);
            Vector2 move = _sea.Move[g.fish];
            Buttons held = _sea.Held[g.fish];

            // Pull: how hard the fish swims away from the boat (0 while exhausted).
            float pull = g.Exhausted ? 0f : Mathf.Clamp01(Vector2.Dot(move.sqrMagnitude > 1f ? move.normalized : move, away));
            g.pull = pull;
            bool pulling = pull > 0.05f;

            // Dash during a tug: tension spike (normal dash cooldown).
            bool dash = (held & Buttons.Dash) != 0;
            if (dash && !g.dashHeld && fish.dashCd <= 0f && !g.Exhausted)
            {
                fish.dashCd = c.dashCooldown;
                g.tensionA += c.tensionDash;
                if (g.hookB >= 0) g.tensionB += c.tensionDash;
            }
            g.dashHeld = dash;
            if (fish.dashCd > 0f) fish.dashCd -= dt;

            float reelSum = 0f;
            Line(ref g.tensionA, ha, pull, pulling, dt, ref reelSum);
            if (g.hookB >= 0) Line(ref g.tensionB, _hooks.Hooks[g.hookB], pull, pulling, dt, ref reelSum);

            if (reelSum > 0f) g.distance -= reelSum * dt;
            else if (pulling) g.distance += c.linePayOut * pull * dt;
            g.distance = Mathf.Clamp(g.distance, 0f, c.maxLineLength);

            if (pulling) g.stamina -= c.staminaDrain * pull * dt;
            else if (!g.Exhausted) g.stamina += c.staminaRegen * dt;
            g.stamina = Mathf.Min(g.stamina, g.maxStamina);
            if (g.exhaustT > 0f) g.exhaustT -= dt;
            else if (g.stamina <= 0f) { g.stamina = 0f; g.exhaustT = c.exhaustedTime; }

            // Sideways input swings the fish around the boat a little.
            Vector2 perp = new Vector2(-away.y, away.x);
            g.angle += Vector2.Dot(move, perp) * 40f * dt;
            g.angle = Mathf.Clamp(Mathf.DeltaAngle(0f, g.angle), -175f, -5f); // stay below the surface

            fish.pos = tip + FishMotor.Dir(g.angle) * g.distance;
            fish.pos.y = Mathf.Clamp(fish.pos.y, c.seabedY + 0.3f, c.surfaceY - 0.3f);
            fish.vel = Vector2.zero;
            fish.heading = g.angle;
            ha.pos = fish.pos; ha.vel = Vector2.zero;
            if (g.hookB >= 0) { _hooks.Hooks[g.hookB].pos = fish.pos; _hooks.Hooks[g.hookB].vel = Vector2.zero; }

            g.t += dt;
            if (g.t > c.fatigueStart) g.fatigue += c.fatigueRate * dt;

            // Outcomes.
            if (g.distance <= c.landDistance) { Finish(idx, ref g, landed: true, snapped: false); return; }
            float thr = Threshold(g);
            bool snapA = g.tensionA >= thr && !NeverSnaps(g.hookA);
            bool snapB = g.hookB >= 0 && g.tensionB >= thr;
            if (g.t >= c.tearLoose) { _events.Push(EventKind.TornLoose, g.hookA, g.fish, fish.pos); Finish(idx, ref g, false, false); return; }

            if (g.hookB >= 0 && (snapA || snapB))
            {
                // Team tug: the snapped line drops out; the remaining single hook breaks after 1 s.
                int snappedHook = snapA ? g.hookA : g.hookB;
                _events.Push(EventKind.Snapped, snappedHook, g.fish, fish.pos);
                _hooks.Tangle(snappedHook, 3f);
                if (snapA) { g.hookA = g.hookB; g.tensionA = g.tensionB; }
                g.hookB = -1;
                g.singleT = 0f;
                return;
            }
            if (snapA)
            {
                _events.Push(EventKind.Snapped, g.hookA, g.fish, fish.pos);
                Finish(idx, ref g, false, true);
                return;
            }
            if (g.leviathan && g.hookB < 0)
            {
                g.singleT += dt;
                if (g.singleT >= c.leviathanSingleHookBreak)
                {
                    _events.Push(EventKind.Snapped, g.hookA, g.fish, fish.pos);
                    Finish(idx, ref g, false, false);
                }
            }
        }

        bool NeverSnaps(int hook) => false; // gear (steel line) arrives with customization

        void Line(ref float tension, in Hook h, float pull, bool pulling, float dt, ref float reelSum)
        {
            var c = _c;
            bool reel = h.Held(Buttons.Reel);
            float d = reel
                ? c.tensionReel + (pulling ? c.tensionFishPull * pull : c.tensionFishRests)
                : (pulling ? c.tensionFishPull * pull + c.tensionEaseOff : c.tensionEaseOff + c.tensionFishRests);
            tension = Mathf.Max(0f, tension + d * dt);
            if (reel) reelSum += c.reelInSpeed * (1f - c.reelPullDamping * pull);
        }

        void Finish(int idx, ref Tug g, bool landed, bool snapped)
        {
            if (landed) Rules?.OnTugLanded(idx);
            else Rules?.OnTugEscaped(idx, snapped);

            // Release the hooks (the rules may already have tangled/rebaited them).
            if (g.hookA >= 0 && _hooks.Hooks[g.hookA].tug == idx) _hooks.Hooks[g.hookA].tug = -1;
            if (g.hookB >= 0 && _hooks.Hooks[g.hookB].tug == idx) _hooks.Hooks[g.hookB].tug = -1;
            if (!landed)
            {
                _sea.Unhook(g.fish);
                _sea.Fish[g.fish].vel = FishMotor.Dir(g.angle) * _c.dashSpeed * 0.5f; // swims off
            }
            g.active = false;
        }

        /// <summary>Round over: every running tug ends with the fish torn loose (it survives).</summary>
        public void CancelAll()
        {
            for (int i = 0; i < Tugs.Length; i++)
            {
                ref Tug g = ref Tugs[i];
                if (!g.active) continue;
                if (g.hookA >= 0) _hooks.Hooks[g.hookA].tug = -1;
                if (g.hookB >= 0) _hooks.Hooks[g.hookB].tug = -1;
                _sea.Unhook(g.fish);
                g.active = false;
            }
        }
    }
}
