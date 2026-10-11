using BaitSwitch.Net;
using Unity.Profiling;
using UnityEngine;

namespace BaitSwitch.Sim
{
    /// <summary>
    /// Server-side sea: every fish (school, bots, players) in one struct array with stable indices.
    /// Unowned fish run boids (flock / cruise / idle / nibble / scatter); owned fish run FishMotor
    /// from their player's input. Zero allocations per tick.
    /// </summary>
    public sealed class SeaSim
    {
        public readonly int Count;
        public readonly FishBody[] Fish;
        public readonly int[] Owner;    // -1 = school/bot, else connectionId
        public readonly Vector2[] Move; // latest stick per owned fish
        public readonly Buttons[] Held; // latest buttons per owned fish
        public readonly bool[] AutoPilot; // owned fish swimming as a boid (bot "blend" state)
        public readonly float[] Growth;
        public uint Tick;
        public HookSim Hooks;           // set by HookSim's constructor
        public readonly EventQueue Events;

        const byte ModeFlock = 0, ModeCruise = 1, ModeIdle = 2, ModeNibble = 3, ModeScatter = 4;

        readonly GameConfig _c;
        readonly BoidsGrid _grid;
        readonly Vector2[] _newVel, _target;
        readonly float[] _wander, _speedMul, _modeTime, _respawnT;
        readonly byte[] _mode;
        readonly int[] _nibbleHook;
        readonly float[] _inkT, _inkCd, _levScatterT;
        readonly bool[] _inkHeld;
        uint _rng;

        static readonly ProfilerMarker k_Tick = new ProfilerMarker("SeaSim.Tick");

        public SeaSim(GameConfig c, int seed, EventQueue events)
        {
            _c = c;
            Events = events;
            Count = c.FishCount;
            Fish = new FishBody[Count];
            Owner = new int[Count];
            Move = new Vector2[Count];
            Held = new Buttons[Count];
            AutoPilot = new bool[Count];
            Growth = new float[Count];
            _inkT = new float[Count];
            _inkCd = new float[Count];
            _levScatterT = new float[Count];
            _inkHeld = new bool[Count];
            _newVel = new Vector2[Count];
            _target = new Vector2[Count];
            _wander = new float[Count];
            _speedMul = new float[Count];
            _modeTime = new float[Count];
            _respawnT = new float[Count];
            _mode = new byte[Count];
            _nibbleHook = new int[Count];
            _rng = (uint)seed | 1u;

            float hx = c.seaSize.x * 0.5f;
            _grid = new BoidsGrid(new Vector2(-hx, c.seabedY), new Vector2(hx, c.surfaceY), c.neighbourRadius, Count);

            float depth = c.surfaceY - c.seabedY;
            for (int i = 0; i < Count; i++)
            {
                Owner[i] = -1;
                _nibbleHook[i] = -1;
                float ang = Rand() * 360f;
                _wander[i] = ang * Mathf.Deg2Rad;
                _speedMul[i] = 1f + (Rand() * 2f - 1f) * c.schoolSpeedJitter;
                Fish[i].pos = new Vector2((Rand() * 2f - 1f) * (hx - c.boundsMargin),
                                          c.seabedY + depth * (0.25f + 0.5f * Rand()));
                Fish[i].heading = ang;
                Fish[i].vel = FishMotor.Dir(ang) * c.schoolCruise;
                FishMotor.Clamp(ref Fish[i], c);
            }
        }

        // ---------- ownership ----------

        /// <summary>Give a connection a random unowned, swimming fish (no pop-in). -1 if none.</summary>
        public int Claim(int connId)
        {
            int free = 0;
            for (int i = 0; i < Count; i++) if (Owner[i] < 0 && !IsHookedOrDead(i)) free++;
            if (free == 0) return -1;
            int pick = (int)(Rand() * free);
            for (int i = 0; i < Count; i++)
            {
                if (Owner[i] >= 0 || IsHookedOrDead(i)) continue;
                if (pick-- == 0)
                {
                    LeaveNibble(i);
                    Owner[i] = connId; Move[i] = Vector2.zero; Held[i] = Buttons.None;
                    AutoPilot[i] = false; Growth[i] = 0f; Fish[i].size = 0;
                    _inkT[i] = 0f; _inkCd[i] = 0f;
                    _mode[i] = ModeFlock;
                    return i;
                }
            }
            return -1;
        }

        public void Release(int idx)
        {
            if (idx < 0 || idx >= Count) return;
            Owner[idx] = -1;
            Move[idx] = Vector2.zero;
            Held[idx] = Buttons.None;
            AutoPilot[idx] = false;
        }

        /// <summary>Change owner id in place (seat handover to/from a bot); the body keeps swimming.</summary>
        public void Transfer(int idx, int newOwner) { if (idx >= 0) Owner[idx] = newOwner; }

        /// <summary>Landed player-seat fish: gone for the rest of the round.</summary>
        public void Remove(int i)
        {
            LeaveNibble(i);
            Fish[i].flags = (byte)FishFlags.Dead;
            _respawnT[i] = float.MaxValue;
            Owner[i] = -1;
            AutoPilot[i] = false;
        }

        public bool IsLeviathan(int i) => (Fish[i].flags & (byte)FishFlags.Leviathan) != 0;

        /// <summary>Adds growth; returns true if this made the fish a Leviathan.</summary>
        public bool AddGrowth(int i, float g)
        {
            Growth[i] = Mathf.Max(0f, Growth[i] + g);
            Fish[i].size = (byte)Mathf.Min(255, Mathf.FloorToInt(Growth[i]));
            if (!IsLeviathan(i) && Growth[i] >= _c.leviathanThreshold)
            {
                Fish[i].flags |= (byte)FishFlags.Leviathan;
                Events?.Push(EventKind.LeviathanRises, i, 0, Fish[i].pos);
                return true;
            }
            return false;
        }

        public void SetGrowth(int i, float g)
        {
            Growth[i] = g;
            Fish[i].size = (byte)Mathf.FloorToInt(g);
            if (g < _c.leviathanThreshold) Fish[i].flags &= unchecked((byte)~FishFlags.Leviathan);
        }

        // ---------- queries and hooks API (used by HookSim) ----------

        public bool IsHookedOrDead(int i) => (Fish[i].flags & (byte)(FishFlags.Hooked | FishFlags.Dead)) != 0;
        public bool IsNibbleHeld(int i) => Owner[i] >= 0 && !IsLeviathan(i) && (Held[i] & Buttons.Nibble) != 0;

        public int Closest(Vector2 p, float radius)
        {
            int best = -1;
            float bestD = radius * radius;
            for (int i = 0; i < Count; i++)
            {
                if (IsHookedOrDead(i)) continue;
                float d = (Fish[i].pos - p).sqrMagnitude;
                if (d <= bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>Closest Leviathan (hooked or not) within radius: a second hook may join a team tug.</summary>
        public int ClosestLeviathanTo(Vector2 p, float radius)
        {
            int best = -1;
            float bestD = radius * radius;
            for (int i = 0; i < Count; i++)
            {
                if (!IsLeviathan(i) || (Fish[i].flags & (byte)FishFlags.Dead) != 0) continue;
                float d = (Fish[i].pos - p).sqrMagnitude;
                if (d <= bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>Closest player fish holding Nibble within range of p, or -1.</summary>
        public int PlayerNibbling(Vector2 p, float range)
        {
            int best = -1;
            float bestD = range * range;
            for (int i = 0; i < Count; i++)
            {
                if (!IsNibbleHeld(i) || IsHookedOrDead(i)) continue;
                float d = (Fish[i].pos - p).sqrMagnitude;
                if (d <= bestD) { bestD = d; best = i; }
            }
            return best;
        }

        public void SetNibbling(int i, bool on)
        {
            if (on) Fish[i].flags |= (byte)FishFlags.Nibbling;
            else Fish[i].flags &= unchecked((byte)~FishFlags.Nibbling);
        }

        public void EndSchoolNibble(int i)
        {
            _nibbleHook[i] = -1;
            if (_mode[i] == ModeNibble) { _mode[i] = ModeFlock; _modeTime[i] = Mathf.Lerp(_c.flockTime.x, _c.flockTime.y, Rand()); }
        }

        public void Hook(int i)
        {
            LeaveNibble(i);
            Fish[i].flags |= (byte)FishFlags.Hooked;
            Fish[i].vel = Vector2.zero;
        }

        public void Unhook(int i) => Fish[i].flags &= unchecked((byte)~FishFlags.Hooked);

        public void HoldAt(int i, Vector2 p) { Fish[i].pos = p; Fish[i].vel = Vector2.zero; }

        /// <summary>Landed: hide the fish and respawn it from the side of the sea later.</summary>
        public void Land(int i)
        {
            LeaveNibble(i);
            Fish[i].flags = (byte)FishFlags.Dead;
            Fish[i].size = 0;
            _respawnT[i] = _c.schoolRespawn;
        }

        public void Scatter(Vector2 from, float radius, float time)
        {
            float r2 = radius * radius;
            for (int i = 0; i < Count; i++)
            {
                if ((Owner[i] >= 0 && !AutoPilot[i]) || IsHookedOrDead(i)) continue;
                if ((Fish[i].pos - from).sqrMagnitude > r2) continue;
                LeaveNibble(i);
                _mode[i] = ModeScatter;
                _modeTime[i] = time;
                _target[i] = from;
                Fish[i].flags |= (byte)FishFlags.Scattered;
            }
        }

        // ---------- tick ----------

        public void Step(float dt)
        {
            using (k_Tick.Auto())
            {
                _grid.Build(Fish, Count);
                for (int i = 0; i < Count; i++)
                    if ((Owner[i] < 0 || AutoPilot[i]) && !IsHookedOrDead(i)) _newVel[i] = Boid(i, dt);

                for (int i = 0; i < Count; i++)
                {
                    if ((Fish[i].flags & (byte)FishFlags.Dead) != 0) { StepRespawn(i, dt); continue; }
                    if ((Fish[i].flags & (byte)FishFlags.Hooked) != 0) continue;
                    if (Owner[i] >= 0 && !AutoPilot[i])
                    {
                        bool wasDashing = Fish[i].dashT > 0f;
                        FishMotor.Step(ref Fish[i], Move[i], Held[i], dt, _c);
                        if (!wasDashing && Fish[i].dashT > 0f) Scatter(Fish[i].pos, _c.missScatterRadius, _c.scatterTime);
                        StepInk(i, dt);
                    }
                    else { Fish[i].vel = _newVel[i]; FishMotor.Integrate(ref Fish[i], dt, _c); }
                    if (IsLeviathan(i))
                    {
                        _levScatterT[i] -= dt;
                        if (_levScatterT[i] <= 0f) { _levScatterT[i] = 0.5f; Scatter(Fish[i].pos, 4f, _c.scatterTime); }
                    }
                }
                Tick++;
            }
        }

        void StepInk(int i, float dt)
        {
            var c = _c;
            if (_inkCd[i] > 0f) _inkCd[i] -= dt;
            bool held = (Held[i] & Buttons.Ink) != 0;
            if (held && !_inkHeld[i] && _inkCd[i] <= 0f && !IsLeviathan(i))
            {
                _inkT[i] = c.inkDuration;
                _inkCd[i] = c.inkCooldown;
                Fish[i].flags |= (byte)FishFlags.Inked;
                Scatter(Fish[i].pos, c.inkRadius, c.scatterTime);
                Events?.Push(EventKind.InkCloud, 0, 0, Fish[i].pos);
            }
            _inkHeld[i] = held;
            if (_inkT[i] > 0f)
            {
                _inkT[i] -= dt;
                if (_inkT[i] <= 0f) Fish[i].flags &= unchecked((byte)~FishFlags.Inked);
            }
        }

        public float DashCooldown(int i) => Fish[i].dashCd;
        public float InkCooldown(int i) => _inkCd[i];

        void StepRespawn(int i, float dt)
        {
            _respawnT[i] -= dt;
            if (_respawnT[i] > 0f) return;
            var c = _c;
            float side = Rand() < 0.5f ? -1f : 1f;
            float hx = c.seaSize.x * 0.5f - c.fishHitRadius - 0.1f;
            float depth = c.surfaceY - c.seabedY;
            Fish[i].pos = new Vector2(side * hx, c.seabedY + depth * (0.3f + 0.4f * Rand()));
            Fish[i].heading = side > 0f ? 180f : 0f;
            Fish[i].vel = FishMotor.Dir(Fish[i].heading) * c.schoolCruise;
            Fish[i].flags = 0;
            _mode[i] = ModeCruise;
            _modeTime[i] = Mathf.Lerp(c.cruiseTime.x, c.cruiseTime.y, Rand());
            _target[i] = new Vector2(-side * hx * 0.3f, Fish[i].pos.y);
            FishMotor.Clamp(ref Fish[i], c);
        }

        Vector2 Boid(int i, float dt)
        {
            var c = _c;
            Vector2 pos = Fish[i].pos, vel = Fish[i].vel;
            Vector2 sep = Vector2.zero, ali = Vector2.zero, coh = Vector2.zero;
            int n = 0;
            float nr2 = c.neighbourRadius * c.neighbourRadius, sr2 = c.separationRadius * c.separationRadius;

            int cx = _grid.CellX(pos.x), cy = _grid.CellY(pos.y);
            for (int gy = cy - 1; gy <= cy + 1; gy++)
            {
                if (gy < 0 || gy >= _grid.H) continue;
                for (int gx = cx - 1; gx <= cx + 1; gx++)
                {
                    if (gx < 0 || gx >= _grid.W) continue;
                    int cell = gy * _grid.W + gx;
                    for (int k = _grid.Start[cell]; k < _grid.Start[cell + 1]; k++)
                    {
                        int j = _grid.Sorted[k];
                        if (j == i || IsHookedOrDead(j)) continue;
                        Vector2 d = Fish[j].pos - pos;
                        float d2 = d.sqrMagnitude;
                        if (d2 > nr2) continue;
                        n++;
                        ali += Fish[j].vel;
                        coh += Fish[j].pos;
                        if (d2 < sr2 && d2 > 1e-6f) sep -= d / d2;
                    }
                }
            }

            float hx = c.seaSize.x * 0.5f, m = c.boundsMargin;
            _modeTime[i] -= dt;
            if (_mode[i] == ModeCruise && (_target[i] - pos).sqrMagnitude < 2.25f) _modeTime[i] = 0f;
            if (_modeTime[i] <= 0f)
            {
                if (_mode[i] == ModeNibble) LeaveNibble(i);
                if (_mode[i] == ModeScatter) Fish[i].flags &= unchecked((byte)~FishFlags.Scattered);
                PickMode(i, hx, m);
            }
            if (Owner[i] < 0 && (_mode[i] == ModeFlock || _mode[i] == ModeIdle)) TryCuriosity(i, pos, dt);

            byte mode = _mode[i];
            float flock = mode == ModeCruise || mode == ModeNibble ? c.cruiseFlockScale : mode == ModeScatter ? 0f : 1f;
            Vector2 steer = Vector2.zero;
            if (n > 0)
            {
                ali /= n;
                if (ali.sqrMagnitude > 1e-4f) steer += ali.normalized * (c.wAlign * flock);
                steer += (coh / n - pos) * (c.wCohesion * flock);
                steer += sep * c.wSeparate;
            }

            float speedMul = 1f;
            if (mode == ModeCruise) { steer += (_target[i] - pos).normalized * c.wTarget; speedMul = c.cruiseSpeedMul; }
            else if (mode == ModeIdle) speedMul = c.idleSpeedMul;
            else if (mode == ModeScatter) { steer += (pos - _target[i]).normalized * (c.wTarget * 2f); speedMul = c.scatterSpeedMul; }
            else if (mode == ModeNibble && _nibbleHook[i] >= 0)
            {
                Vector2 hp = Hooks.Hooks[_nibbleHook[i]].pos;
                Vector2 to = hp - pos;
                steer += to.normalized * (c.wTarget * 2f);
                float dist = to.magnitude;
                speedMul = dist < c.nibbleRange ? 0.05f : Mathf.Clamp01(dist / 2f) + 0.2f;
            }

            if (mode != ModeNibble)
            {
                _wander[i] += (Rand() * 2f - 1f) * c.wanderJitter * dt;
                steer += new Vector2(Mathf.Cos(_wander[i]), Mathf.Sin(_wander[i])) * c.wWander;
                steer += AvoidHooks(pos);
            }

            if (pos.x < -hx + m) steer.x += c.wBounds * (-hx + m - pos.x) / m;
            else if (pos.x > hx - m) steer.x -= c.wBounds * (pos.x - (hx - m)) / m;
            if (pos.y < c.seabedY + m) steer.y += c.wBounds * (c.seabedY + m - pos.y) / m;
            else if (pos.y > c.surfaceY - m) steer.y -= c.wBounds * (pos.y - (c.surfaceY - m)) / m;

            float mid = (c.surfaceY + c.seabedY) * 0.5f, half = (c.surfaceY - c.seabedY) * 0.5f;
            steer.y += (mid - pos.y) / half * c.wDepth;

            Vector2 cur = vel.sqrMagnitude > 1e-4f ? vel.normalized : FishMotor.Dir(Fish[i].heading);
            Vector2 want = steer.sqrMagnitude > 1e-4f ? steer.normalized : cur;
            float maxTurn = c.schoolTurnRate * dt * (mode == ModeScatter ? 2f : 1f);
            float turn = Mathf.Clamp(Vector2.SignedAngle(cur, want), -maxTurn, maxTurn) * Mathf.Deg2Rad;
            float cs = Mathf.Cos(turn), sn = Mathf.Sin(turn);
            Vector2 dir = new Vector2(cur.x * cs - cur.y * sn, cur.x * sn + cur.y * cs);

            float accel = mode == ModeScatter ? c.schoolAccel * 3f : c.schoolAccel;
            float speed = Mathf.MoveTowards(vel.magnitude, c.schoolCruise * _speedMul[i] * speedMul, accel * dt);
            return dir * speed;
        }

        Vector2 AvoidHooks(Vector2 pos)
        {
            if (Hooks == null) return Vector2.zero;
            var c = _c;
            Vector2 push = Vector2.zero;
            float r2 = c.avoidHookRadius * c.avoidHookRadius;
            var hooks = Hooks.Hooks;
            for (int h = 0; h < hooks.Length; h++)
            {
                if (!hooks[h].active || hooks[h].vel.sqrMagnitude < c.avoidHookMinSpeed * c.avoidHookMinSpeed) continue;
                Vector2 d = pos - hooks[h].pos;
                float d2 = d.sqrMagnitude;
                if (d2 < r2 && d2 > 1e-6f) push += d.normalized * (c.wAvoidHook * (1f - d2 / r2));
            }
            return push;
        }

        void TryCuriosity(int i, Vector2 pos, float dt)
        {
            if (Hooks == null || Rand() > _c.curiosityPerSecond * dt) return;
            var c = _c;
            var hooks = Hooks.Hooks;
            float r2 = c.curiosityRange * c.curiosityRange;
            for (int h = 0; h < hooks.Length; h++)
            {
                if ((hooks[h].pos - pos).sqrMagnitude > r2) continue;
                float dur = Mathf.Lerp(c.schoolNibble.x, c.schoolNibble.y, Rand());
                if (!Hooks.TryClaimNibble(h, i, dur, Rand() < c.schoolEatChance)) continue;
                _mode[i] = ModeNibble;
                _nibbleHook[i] = h;
                _modeTime[i] = c.nibbleApproachTimeout + dur;
                return;
            }
        }

        void LeaveNibble(int i)
        {
            int h = _nibbleHook[i];
            _nibbleHook[i] = -1;
            if (h >= 0 && Hooks != null) Hooks.CancelNibble(h, i);
            SetNibbling(i, false);
            if (_mode[i] == ModeNibble) _mode[i] = ModeFlock;
        }

        void PickMode(int i, float hx, float margin)
        {
            var c = _c;
            float r = Rand();
            if (r < c.cruiseShare)
            {
                _mode[i] = ModeCruise;
                _modeTime[i] = Mathf.Lerp(c.cruiseTime.x, c.cruiseTime.y, Rand());
                _target[i] = new Vector2((Rand() * 2f - 1f) * (hx - margin),
                                         Mathf.Lerp(c.seabedY + margin, c.surfaceY - margin, Rand()));
            }
            else if (r < c.cruiseShare + c.idleShare)
            {
                _mode[i] = ModeIdle;
                _modeTime[i] = Mathf.Lerp(c.idleTime.x, c.idleTime.y, Rand());
            }
            else
            {
                _mode[i] = ModeFlock;
                _modeTime[i] = Mathf.Lerp(c.flockTime.x, c.flockTime.y, Rand());
            }
        }

        float Rand()
        {
            _rng ^= _rng << 13; _rng ^= _rng >> 17; _rng ^= _rng << 5;
            return (_rng & 0xFFFFFF) / 16777216f;
        }
    }
}
