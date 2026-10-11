using BaitSwitch.Net;
using Unity.Profiling;
using UnityEngine;

namespace BaitSwitch.Sim
{
    /// <summary>One Angler's dinghy + hook. Plain struct in the server sim.</summary>
    public struct Hook
    {
        public bool active;
        public float boatX;
        public Vector2 pos, vel, cursor;
        public float row;            // -1..1 from A/D
        public Buttons buttons, prevButtons;
        public bool baited;
        public float strikeT, cooldown, rebaitT, tangleT;
        public int caught;           // school fish being landed, -1 none
        public float landT;
        public int tug;              // tug index while in a tug-of-war, -1 none
        public int nibbler;          // fish index nibbling, -1 none
        public float nibbleT, nibbleDur, twitchT;
        public bool nibblerIsSchool, schoolEats;
        public float sonarCd, markCd;

        public bool Pressed(Buttons b) => (buttons & b) != 0 && (prevButtons & b) == 0;
        public bool Held(Buttons b) => (buttons & b) != 0;
    }

    /// <summary>Game rules the hook sim defers to (implemented by GameState).</summary>
    public interface IHookRules
    {
        void OnBaitEaten(int hook, int fish, bool school);
        void OnSchoolLanded(int hook, int fish);
        /// <summary>A strike caught this fish. Return true if the rules took it (tug-of-war); false = auto-land as school.</summary>
        bool OnStrikeCatch(int hook, int fish);
    }

    /// <summary>Spring/drag/gravity hook on a rope, shared by the server and the client's own-hook prediction.</summary>
    public static class HookPhysics
    {
        public static Vector2 RodTip(float boatX, GameConfig c) => new Vector2(boatX, c.surfaceY + c.rodHeight);

        public static float StepBoat(float boatX, float row, float dt, GameConfig c)
        {
            float hx = c.seaSize.x * 0.5f - 1f;
            return Mathf.Clamp(boatX + Mathf.Clamp(row, -1f, 1f) * c.dinghySpeed * dt, -hx, hx);
        }

        /// <summary>Free swing toward the cursor (not reeling, not landing).</summary>
        public static void StepFree(ref Vector2 pos, ref Vector2 vel, Vector2 cursor, float boatX, float dt, GameConfig c)
        {
            Vector2 target = new Vector2(cursor.x, Mathf.Min(cursor.y, c.surfaceY - 0.2f));
            Vector2 acc = (target - pos) * c.hookSpring - vel * c.hookDrag;
            acc.y -= c.hookGravity;
            vel += acc * dt;
            if (Terrain.InAny(pos, c.kelp)) vel *= Mathf.Pow(c.kelpHookSlow, dt * 10f);
            pos += vel * dt;
            Constrain(ref pos, ref vel, boatX, c);
        }

        public static void Constrain(ref Vector2 pos, ref Vector2 vel, float boatX, GameConfig c)
        {
            Vector2 tip = RodTip(boatX, c);
            Vector2 d = pos - tip;
            if (d.sqrMagnitude > c.maxLineLength * c.maxLineLength)
            {
                Vector2 n = d.normalized;
                pos = tip + n * c.maxLineLength;
                float outward = Vector2.Dot(vel, n);
                if (outward > 0f) vel -= n * outward;
            }
            Terrain.PushOut(ref pos, ref vel, c.hookRadius, c.rocks);
            float hx = c.seaSize.x * 0.5f;
            pos.x = Mathf.Clamp(pos.x, -hx, hx);
            if (pos.y < c.seabedY + c.hookRadius) { pos.y = c.seabedY + c.hookRadius; if (vel.y < 0f) vel.y = 0f; }
            if (pos.y > c.surfaceY) { pos.y = c.surfaceY; if (vel.y > 0f) vel.y = 0f; }
        }

        public static void StepReel(ref Vector2 pos, ref Vector2 vel, float boatX, float dt, GameConfig c)
        {
            pos = Vector2.MoveTowards(pos, RodTip(boatX, c), c.reelUpSpeed * dt);
            vel = Vector2.zero;
        }
    }

    /// <summary>
    /// Server: hooks, boats, nibbles, twitches, strikes, school landings, rebait, tangles. One slot per seat.
    /// Tug-of-war bodies are moved by TugSystem; rules (points, tugs) live in GameState via IHookRules.
    /// </summary>
    public sealed class HookSim
    {
        public readonly Hook[] Hooks;
        public bool StrikesEnabled = true;
        public IHookRules Rules;

        readonly GameConfig _c;
        readonly SeaSim _sea;
        readonly EventQueue _events;
        static readonly ProfilerMarker k_Tick = new ProfilerMarker("HookSim.Tick");

        public HookSim(GameConfig c, SeaSim sea, EventQueue events)
        {
            _c = c;
            _sea = sea;
            _events = events;
            Hooks = new Hook[c.seats];
            for (int i = 0; i < Hooks.Length; i++) { Hooks[i].caught = -1; Hooks[i].nibbler = -1; Hooks[i].tug = -1; }
            sea.Hooks = this;
        }

        public int Claim()
        {
            for (int i = 0; i < Hooks.Length; i++)
                if (!Hooks[i].active) { Activate(i); return i; }
            return -1;
        }

        void Activate(int i)
        {
            float hx = _c.seaSize.x * 0.5f - 2f;
            float x = Mathf.Lerp(-hx, hx, (i + 0.5f) / Hooks.Length);
            Hooks[i] = new Hook
            {
                active = true, boatX = x, baited = true, caught = -1, nibbler = -1, tug = -1,
                pos = new Vector2(x, _c.surfaceY - 2f), cursor = new Vector2(x, _c.surfaceY - 4f),
            };
        }

        public void Release(int h)
        {
            if (h < 0 || h >= Hooks.Length) return;
            ref Hook k = ref Hooks[h];
            if (k.caught >= 0) { _sea.Unhook(k.caught); k.caught = -1; }
            if (k.nibbler >= 0) EndNibble(ref k);
            k.active = false;
            k.tug = -1;
        }

        /// <summary>Call once per tick for every active hook (previous buttons become the edge reference).</summary>
        public void SetInput(int h, float row, Vector2 cursor, Buttons b)
        {
            ref Hook k = ref Hooks[h];
            k.prevButtons = k.buttons;
            k.row = row;
            k.cursor = cursor;
            k.buttons = b;
        }

        public bool IsStillAndBaited(int h)
        {
            ref Hook k = ref Hooks[h];
            return k.active && k.baited && k.caught < 0 && k.tug < 0 && k.strikeT <= 0f && k.tangleT <= 0f
                   && k.vel.magnitude < _c.avoidHookMinSpeed && (k.buttons & Buttons.Rebait) == 0;
        }

        public void CancelNibble(int h, int fish)
        {
            if (h < 0 || h >= Hooks.Length) return;
            ref Hook k = ref Hooks[h];
            if (k.nibbler == fish) EndNibble(ref k);
        }

        /// <summary>A school fish starts a curiosity nibble. False if the hook is busy.</summary>
        public bool TryClaimNibble(int h, int fish, float duration, bool eats)
        {
            ref Hook k = ref Hooks[h];
            if (!IsStillAndBaited(h) || k.nibbler >= 0) return false;
            k.nibbler = fish; k.nibbleT = 0f; k.nibbleDur = duration; k.twitchT = 0f;
            k.nibblerIsSchool = true; k.schoolEats = eats;
            return true;
        }

        /// <summary>After a snap: no bait, no strikes for a while.</summary>
        public void Tangle(int h, float seconds)
        {
            ref Hook k = ref Hooks[h];
            k.tug = -1;
            k.baited = false;
            k.tangleT = seconds;
            k.vel = Vector2.zero;
            _events.Push(EventKind.Tangled, h, 0, k.pos);
        }

        public void Step(float dt)
        {
            using (k_Tick.Auto())
            {
                for (int h = 0; h < Hooks.Length; h++)
                {
                    ref Hook k = ref Hooks[h];
                    if (!k.active) continue;
                    StepHook(h, ref k, dt);
                }
            }
        }

        void StepHook(int h, ref Hook k, float dt)
        {
            var c = _c;
            k.boatX = HookPhysics.StepBoat(k.boatX, k.row, dt, c);
            if (k.cooldown > 0f) k.cooldown -= dt;
            if (k.strikeT > 0f) k.strikeT -= dt;
            if (k.sonarCd > 0f) k.sonarCd -= dt;
            if (k.markCd > 0f) k.markCd -= dt;
            if (k.tug >= 0) return; // TugSystem owns the hook

            // Landing a caught school fish: pull it to the boat.
            if (k.caught >= 0)
            {
                k.landT -= dt;
                HookPhysics.StepReel(ref k.pos, ref k.vel, k.boatX, dt, c);
                _sea.HoldAt(k.caught, k.pos);
                if (k.landT <= 0f || (k.pos - HookPhysics.RodTip(k.boatX, c)).sqrMagnitude < c.reelTipDistance * c.reelTipDistance)
                {
                    int f = k.caught;
                    k.caught = -1;
                    _events.Push(EventKind.Landed, h, f, k.pos);
                    _sea.Land(f);
                    Rules?.OnSchoolLanded(h, f);
                }
                return;
            }

            if (k.tangleT > 0f)
            {
                k.tangleT -= dt;
                HookPhysics.StepFree(ref k.pos, ref k.vel, k.pos + Vector2.down, k.boatX, dt, c); // limp line
                return;
            }

            bool rebait = (k.buttons & Buttons.Rebait) != 0;
            if (rebait && !k.baited)
            {
                if (k.nibbler >= 0) EndNibble(ref k);
                HookPhysics.StepReel(ref k.pos, ref k.vel, k.boatX, dt, c);
                if ((k.pos - HookPhysics.RodTip(k.boatX, c)).sqrMagnitude < c.reelTipDistance * c.reelTipDistance)
                {
                    k.rebaitT += dt;
                    if (k.rebaitT >= c.rebaitTime) { k.baited = true; k.rebaitT = 0f; }
                }
                return;
            }
            k.rebaitT = 0f;

            HookPhysics.StepFree(ref k.pos, ref k.vel, k.cursor, k.boatX, dt, c);
            if (StrikesEnabled && k.Pressed(Buttons.Strike) && k.cooldown <= 0f) Strike(h, ref k);
            StepNibble(h, ref k, dt);
        }

        void Strike(int h, ref Hook k)
        {
            var c = _c;
            k.vel.y += c.strikeJerk / c.strikeJerkTime;
            k.strikeT = c.strikeJerkTime;
            // The nibbling fish is the one at the bait: it is hooked first if it is inside the catch radius.
            int fish = -1;
            if (k.nibbler >= 0 && !_sea.IsHookedOrDead(k.nibbler) &&
                (_sea.Fish[k.nibbler].pos - k.pos).sqrMagnitude <= c.catchRadius * c.catchRadius) fish = k.nibbler;
            if (fish < 0) fish = _sea.Closest(k.pos, c.catchRadius);
            if (fish < 0) fish = _sea.ClosestLeviathanTo(k.pos, c.catchRadius * c.leviathanScale);
            if (fish >= 0)
            {
                if (k.nibbler >= 0) EndNibble(ref k);
                k.baited = false;
                _sea.Hook(fish);
                _events.Push(EventKind.Hooked, h, fish, k.pos);
                if (Rules != null && Rules.OnStrikeCatch(h, fish)) return; // tug-of-war
                k.caught = fish;
                k.landT = c.schoolLandTime;
            }
            else
            {
                k.cooldown = c.strikeCooldown;
                _sea.Scatter(k.pos, c.missScatterRadius, c.missScatterTime);
                _events.Push(EventKind.MissScatter, h, 0, k.pos);
            }
        }

        void StepNibble(int h, ref Hook k, float dt)
        {
            var c = _c;
            // A player fish holding Nibble next to a free, baited hook starts nibbling.
            if (k.nibbler < 0 && k.baited)
            {
                int p = _sea.PlayerNibbling(k.pos, c.nibbleRange);
                if (p >= 0) { k.nibbler = p; k.nibbleT = 0f; k.nibbleDur = c.nibbleTime; k.twitchT = 0f; k.nibblerIsSchool = false; }
            }
            if (k.nibbler < 0) return;

            bool inRange = (_sea.Fish[k.nibbler].pos - k.pos).sqrMagnitude <= c.nibbleRange * c.nibbleRange;
            bool fail = !k.baited || _sea.IsHookedOrDead(k.nibbler)
                        || (!k.nibblerIsSchool && (!inRange || !_sea.IsNibbleHeld(k.nibbler)))
                        || (k.nibblerIsSchool && k.vel.magnitude > c.avoidHookMinSpeed);
            if (fail) { EndNibble(ref k); return; }
            if (!inRange) { _sea.SetNibbling(k.nibbler, false); return; } // school fish still swimming over

            _sea.SetNibbling(k.nibbler, true);
            k.nibbleT += dt;
            k.twitchT -= dt;
            if (k.twitchT <= 0f) { k.twitchT = c.nibbleTwitchInterval; _events.Push(EventKind.Nibble, h, 0, k.pos); }

            if (k.nibbleT >= k.nibbleDur)
            {
                bool school = k.nibblerIsSchool;
                bool eats = !school || k.schoolEats;
                int f = k.nibbler;
                EndNibble(ref k);
                if (eats)
                {
                    k.baited = false;
                    _events.Push(EventKind.Eaten, h, 0, k.pos);
                    Rules?.OnBaitEaten(h, f, school);
                }
            }
        }

        void EndNibble(ref Hook k)
        {
            if (k.nibbler >= 0)
            {
                _sea.SetNibbling(k.nibbler, false);
                if (k.nibblerIsSchool) _sea.EndSchoolNibble(k.nibbler);
            }
            k.nibbler = -1;
        }

        /// <summary>Seconds the current nibble on this hook has lasted (what a twitch-watching Angler can infer), or -1.</summary>
        public float NibbleAge(int h) => Hooks[h].nibbler >= 0 ? Hooks[h].nibbleT : -1f;

        public HookState StateOf(in Hook k)
        {
            if (k.tug >= 0) return HookState.Tugging;
            if (k.caught >= 0) return HookState.ReelingUp;
            if ((k.buttons & Buttons.Rebait) != 0 && !k.baited && k.tangleT <= 0f) return HookState.ReelingUp;
            if (k.strikeT > 0f) return HookState.Striking;
            return k.baited ? HookState.Baited : HookState.Empty;
        }
    }
}
