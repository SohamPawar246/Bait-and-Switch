using System.Collections.Generic;
using BaitSwitch.Sim;
using Mirror;
using Unity.Profiling;
using UnityEngine;

namespace BaitSwitch.Net
{
    /// <summary>
    /// Server networking around GameState: per-connection input queues (one input consumed per tick) feeding seats,
    /// the fixed 30 Hz tick, the 15 Hz snapshot writer, events (public or private), WelcomeMsg on body changes,
    /// RoundStartMsg, MatchStateMsg, TugMsg (10 Hz, participants only) and LedgerMsg.
    /// </summary>
    public sealed class SeaServer : MonoBehaviour
    {
        struct InputFrame { public sbyte mx, my; public Buttons b; }

        sealed class Peer
        {
            public NetworkConnectionToClient conn;
            public Seat seat;
            public bool joined;
            public uint lastSeq, newestSeq;
            public uint[] ringSeq;
            public InputFrame[] ring;
            public InputFrame last;
            public Vector2 cursor;
        }

        GameConfig _c;
        GameState _gs;
        readonly List<Peer> _peers = new List<Peer>(8);
        double _acc;
        int _ticksPerSnap, _ticksPerTug;
        uint _tick;
        float _stateTimer;
        FishSnap[] _snaps;
        HookSnap[] _hookSnaps;
        BoatSnap[] _boatSnaps;
        byte[][] _partBuf;
        int[] _partLen, _partFirst, _partCount;

        static readonly ProfilerMarker k_Snap = new ProfilerMarker("SeaServer.Snapshot");
        static readonly ProfilerMarker k_Game = new ProfilerMarker("GameState.Tick");

        public GameState Game => _gs;
        public SeaSim Sim => _gs?.Sea;
        public HookSim Hooks => _gs?.Hooks;

        public void Begin()
        {
            _c = GameConfig.I;
            _gs = new GameState(_c, System.Environment.TickCount);
            _acc = 0;
            _tick = 0;
            _ticksPerSnap = Mathf.Max(1, _c.simHz / Mathf.Max(1, _c.snapshotHz));
            _ticksPerTug = Mathf.Max(1, _c.simHz / Mathf.Max(1, _c.tugHz));
            int count = _gs.Sea.Count;
            _snaps = new FishSnap[count];
            _hookSnaps = new HookSnap[_c.seats];
            _boatSnaps = new BoatSnap[_c.seats];

            int parts = Mathf.Max(1, _c.snapshotParts);
            _partBuf = new byte[parts][];
            _partLen = new int[parts];
            _partFirst = new int[parts];
            _partCount = new int[parts];
            int per = (count + parts - 1) / parts;
            for (int p = 0; p < parts; p++)
            {
                _partFirst[p] = Mathf.Min(count, p * per);
                _partCount[p] = Mathf.Min(per, count - _partFirst[p]);
                _partBuf[p] = new byte[_partCount[p] * SnapshotCodec.FishBytes + 2 +
                                       _c.seats * (SnapshotCodec.HookBytes + SnapshotCodec.BoatBytes)];
            }
        }

        public void End()
        {
            _peers.Clear();
            _gs = null;
        }

        // ---------- connections ----------

        public void AddPeer(NetworkConnectionToClient conn)
        {
            if (Find(conn) != null) return;
            _peers.Add(new Peer { conn = conn, ringSeq = new uint[_c.inputBufferLen], ring = new InputFrame[_c.inputBufferLen] });
        }

        public void RemovePeer(NetworkConnectionToClient conn)
        {
            var p = Find(conn);
            if (p == null) return;
            if (p.seat != null) _gs.RemoveHuman(conn.connectionId);
            _peers.Remove(p);
            if (_gs != null) Debug.Log($"[Sea] leave {conn.connectionId} humans={_gs.HumanCount}");
        }

        /// <summary>Names come from clients: trim, cap at 18 chars, no rich-text tags, never empty.</summary>
        static string CleanName(string n)
        {
            if (string.IsNullOrWhiteSpace(n)) return Copy.NameSuggestions[0];
            n = n.Replace("<", "").Replace(">", "").Trim();
            if (n.Length > 18) n = n.Substring(0, 18);
            return n.Length == 0 ? Copy.NameSuggestions[0] : n;
        }

        public void OnJoin(NetworkConnectionToClient conn, JoinMsg msg)
        {
            if (_gs == null) return;
            var p = Find(conn);
            if (p == null) { AddPeer(conn); p = Find(conn); }
            if (p.joined) return;
            p.joined = true;
            p.seat = _gs.AddHuman(conn.connectionId, CleanName(msg.name), msg.appearance);
            Debug.Log($"[Sea] join {conn.connectionId} '{CleanName(msg.name)}' seat={(p.seat != null ? p.seat.index : -1)} humans={_gs.HumanCount}");
            p.lastSeq = p.newestSeq;
            SendWelcome(p);
            if (_gs.State != MatchState.Waiting && !_gs.RoundStartPending) SendRoundStart(p);
            SendState(p);
        }

        public void OnRoleRequest(NetworkConnectionToClient conn, RoleRequestMsg msg) { } // roles come from GameState now

        // ---------- input ----------

        public void OnInput(NetworkConnectionToClient conn, InputMsg m)
        {
            var p = Find(conn);
            if (p == null || p.seat == null) return;
            if (m.seq > p.newestSeq) p.cursor = new Vector2(Quant.Unpack(m.cursorX), Quant.Unpack(m.cursorY));
            Store(p, m.seq, m.moveX, m.moveY, m.buttons);
            if (m.seq > 1) Store(p, m.seq - 1, m.prevX1, m.prevY1, m.prevB1);
            if (m.seq > 2) Store(p, m.seq - 2, m.prevX2, m.prevY2, m.prevB2);
            if (m.seq > p.newestSeq) p.newestSeq = m.seq;
        }

        void Store(Peer p, uint seq, sbyte mx, sbyte my, Buttons b)
        {
            if (seq <= p.lastSeq) return;
            int i = (int)(seq % (uint)p.ring.Length);
            p.ringSeq[i] = seq;
            p.ring[i] = new InputFrame { mx = mx, my = my, b = b };
        }

        void ConsumeInput(Peer p)
        {
            if (p.seat == null || p.seat.bot) return;
            if (p.newestSeq > p.lastSeq + (uint)_c.maxInputQueue) p.lastSeq = p.newestSeq - 2; // client ran ahead: skip stale
            uint next = p.lastSeq + 1;
            int i = (int)(next % (uint)p.ring.Length);
            if (p.ringSeq[i] == next) { p.last = p.ring[i]; p.lastSeq = next; }
            else if (p.newestSeq > next) p.lastSeq = next; // lost for good: repeat the last frame
            // else starved: repeat the last frame without advancing
            p.seat.move = new Vector2(p.last.mx / 127f, p.last.my / 127f);
            p.seat.cursor = p.cursor;
            p.seat.buttons = p.last.b;
        }

        // ---------- tick ----------

        void Update()
        {
            if (_gs == null || !NetworkServer.active) return;
            float dt = 1f / _c.simHz;
            _acc += Time.unscaledDeltaTime;
            int steps = 0;
            while (_acc >= dt && steps < _c.maxSimStepsPerFrame)
            {
                _acc -= dt;
                steps++;
                TickOnce(dt);
            }
            if (steps == _c.maxSimStepsPerFrame) _acc = 0; // hitch: drop time rather than spiral
        }

        void TickOnce(float dt)
        {
            for (int i = 0; i < _peers.Count; i++) ConsumeInput(_peers[i]);
            using (k_Game.Auto()) _gs.Tick(dt);
            _tick++;
            var agent = EdgegapAgent.I;
            if (agent != null)
            {
                agent.SetHumanCount(_gs.HumanCount);
                // rotation: once the deployment is near its free-tier cap, finish the match, then move everyone on
                if (!agent.AcceptingNewMatches && _gs.State == MatchState.Final && _gs.StateTime > 8f && !agent.Stopping)
                {
                    BroadcastText(EventKind.Rotation);
                    agent.StopServer("rotation");
                }
            }

            if (_gs.RoundStartPending)
            {
                _gs.RoundStartPending = false;
                for (int i = 0; i < _peers.Count; i++) if (_peers[i].joined) SendRoundStart(_peers[i]);
            }
            foreach (var s in _gs.Seats)
            {
                if (!s.dirty) continue;
                s.dirty = false;
                var p = PeerOfSeat(s);
                if (p != null) SendWelcome(p);
            }
            BroadcastEvents();
            _gs.Events.Clear();

            _stateTimer -= dt;
            if (_gs.StateDirty || _stateTimer <= 0f)
            {
                _gs.StateDirty = false;
                _stateTimer = 1f;
                for (int i = 0; i < _peers.Count; i++) if (_peers[i].joined) { SendState(_peers[i]); SendRoster(_peers[i]); }
            }
            if (_gs.LedgerPending)
            {
                _gs.LedgerPending = false;
                var ledger = _gs.BuildLedger();
                for (int i = 0; i < _peers.Count; i++) if (_peers[i].joined) _peers[i].conn.Send(ledger);
            }
            if (_tick % (uint)_ticksPerTug == 0) SendTugs();
            if (_tick % (uint)_ticksPerSnap == 0) SendSnapshot();
        }

        void SendRoster(Peer p)
        {
            var seats = _gs.Seats;
            int n = seats.Length;
            var m = new RosterMsg
            {
                you = (byte)(p.seat != null ? p.seat.index : 255),
                names = new string[n], roles = new byte[n], human = new bool[n],
                fish = new short[n], hook = new short[n], points = new short[n],
            };
            bool viewerFish = p.seat != null && p.seat.role == Role.Fish;
            for (int i = 0; i < n; i++)
            {
                var s = seats[i];
                m.names[i] = s.Name;
                m.roles[i] = (byte)s.role;
                m.human[i] = !s.bot;
                m.fish[i] = (short)(viewerFish && s.role == Role.Fish && s.Swimming ? s.fish : -1);
                m.hook[i] = (short)(s.role == Role.Angler ? s.hook : -1);
                m.points[i] = (short)Mathf.Clamp(s.points, short.MinValue, short.MaxValue);
            }
            p.conn.Send(m);
        }

        void BroadcastEvents()
        {
            var q = _gs.Events;
            for (int e = 0; e < q.Count; e++)
            {
                var ev = q.Items[e];
                var msg = new EventMsg { kind = ev.kind, a = ev.a, b = ev.b, x = Quant.Pack(ev.pos.x), y = Quant.Pack(ev.pos.y) };
                for (int k = 0; k < _peers.Count; k++)
                {
                    var p = _peers[k];
                    if (!p.joined) continue;
                    if (ev.toSeat >= 0 && (p.seat == null || p.seat.index != ev.toSeat)) continue;
                    p.conn.Send(msg);
                }
            }
        }

        void BroadcastText(EventKind kind)
        {
            var msg = new EventMsg { kind = kind };
            for (int i = 0; i < _peers.Count; i++) if (_peers[i].joined) _peers[i].conn.Send(msg);
        }

        void SendWelcome(Peer p)
        {
            var s = p.seat;
            p.conn.Send(new WelcomeMsg
            {
                ownIndex = s != null && s.Swimming ? (ushort)s.fish : (ushort)0xFFFF,
                fishCount = (ushort)_gs.Sea.Count,
                tick = _tick,
                role = s != null ? (s.convertT >= 0f ? Role.Spectator : s.role) : Role.Spectator,
                boatId = s != null && s.role == Role.Angler && s.hook >= 0 ? (byte)s.hook : (byte)0xFF,
            });
        }

        void SendRoundStart(Peer p)
        {
            var s = p.seat;
            var names = new string[_gs.Seats.Length];
            for (int i = 0; i < names.Length; i++) names[i] = _gs.Seats[i].Name;
            p.conn.Send(new RoundStartMsg
            {
                round = (byte)_gs.Round,
                role = s != null ? s.role : Role.Spectator,
                yourFishId = s != null && s.Swimming ? (ushort)s.fish : (ushort)0xFFFF,
                yourBoatId = s != null && s.hook >= 0 ? (byte)s.hook : (byte)0xFF,
                appetiteSlot = _gs.AppetiteSlot,
                appetitePart = _gs.AppetitePart,
                fishCount = (ushort)_gs.Sea.Count,
                appearanceTable = _gs.AppearanceTable,
                seatNames = names,
            });
        }

        void SendState(Peer p)
        {
            p.conn.Send(new MatchStateMsg
            {
                state = (byte)_gs.State,
                round = (byte)_gs.Round,
                time = _gs.StateTime,
                phase = (byte)_gs.Phase,
                fishLeft = (byte)_gs.FishLeft,
            });
        }

        void SendTugs()
        {
            var tugs = _gs.Tugs.Tugs;
            for (int t = 0; t < tugs.Length; t++)
            {
                ref Tug g = ref tugs[t];
                if (!g.active) continue;
                float thr = _gs.Tugs.Threshold(g);
                var msg = new TugMsg
                {
                    tugId = (byte)t,
                    threshold = (byte)Mathf.Clamp(thr, 0f, 255f),
                    stamina = (byte)Mathf.Clamp(g.stamina / Mathf.Max(1f, g.maxStamina) * 255f, 0f, 255f),
                    distanceCm = (ushort)Mathf.Clamp(g.distance * 100f, 0f, 65535f),
                    exhausted = g.Exhausted,
                };
                var fishSeat = _gs.SeatOfFish(g.fish);
                SendTugTo(fishSeat, msg, Mathf.Max(g.tensionA, g.tensionB));
                SendTugTo(_gs.SeatOfHook(g.hookA), msg, g.tensionA);
                if (g.hookB >= 0) SendTugTo(_gs.SeatOfHook(g.hookB), msg, g.tensionB);
            }
        }

        void SendTugTo(Seat s, TugMsg msg, float tension)
        {
            var p = PeerOfSeat(s);
            if (p == null) return;
            msg.tension = (byte)Mathf.Clamp(tension, 0f, 255f);
            p.conn.Send(msg, Channels.Unreliable);
        }

        void SendSnapshot()
        {
            using (k_Snap.Auto())
            {
                var sea = _gs.Sea;
                var fish = sea.Fish;
                const byte publicFlags = (byte)~FishFlags.Scattered; // only school fish scatter: never reveal it
                for (int i = 0; i < sea.Count; i++)
                {
                    _snaps[i].pos = fish[i].pos;
                    _snaps[i].heading = fish[i].heading;
                    _snaps[i].flags = (FishFlags)(fish[i].flags & publicFlags);
                    _snaps[i].sizeStep = fish[i].size;
                }
                int hookCount = 0;
                var hooks = _gs.Hooks.Hooks;
                for (int h = 0; h < hooks.Length; h++)
                {
                    if (!hooks[h].active) continue;
                    _hookSnaps[hookCount] = new HookSnap { owner = (byte)h, pos = hooks[h].pos, state = _gs.Hooks.StateOf(hooks[h]) };
                    _boatSnaps[hookCount] = new BoatSnap { owner = (byte)h, x = hooks[h].boatX };
                    hookCount++;
                }

                int parts = _partBuf.Length;
                for (int p = 0; p < parts; p++)
                {
                    int w = SnapshotCodec.EncodeFish(_snaps, _partFirst[p], _partCount[p], _partBuf[p], 0);
                    if (p == 0)
                    {
                        w += SnapshotCodec.EncodeHooks(_hookSnaps, hookCount, _partBuf[p], w);
                        w += SnapshotCodec.EncodeBoats(_boatSnaps, hookCount, _partBuf[p], w);
                    }
                    _partLen[p] = w;
                }

                for (int k = 0; k < _peers.Count; k++)
                {
                    var peer = _peers[k];
                    if (!peer.joined) continue;
                    short vx = 0, vy = 0;
                    if (peer.seat != null && peer.seat.Swimming)
                    {
                        vx = Quant.Pack(fish[peer.seat.fish].vel.x);
                        vy = Quant.Pack(fish[peer.seat.fish].vel.y);
                    }
                    for (int p = 0; p < parts; p++)
                    {
                        peer.conn.Send(new SnapshotMsg
                        {
                            tick = _tick,
                            ackSeq = peer.lastSeq,
                            ownVx = vx,
                            ownVy = vy,
                            part = (byte)p,
                            partCount = (byte)parts,
                            first = (ushort)_partFirst[p],
                            count = (ushort)_partCount[p],
                            data = new System.ArraySegment<byte>(_partBuf[p], 0, _partLen[p]),
                        }, Channels.Unreliable);
                    }
                }
            }
        }

        Peer Find(NetworkConnectionToClient conn)
        {
            for (int i = 0; i < _peers.Count; i++) if (_peers[i].conn == conn) return _peers[i];
            return null;
        }

        Peer PeerOfSeat(Seat s)
        {
            if (s == null || s.bot) return null;
            for (int i = 0; i < _peers.Count; i++) if (_peers[i].seat == s) return _peers[i];
            return null;
        }
    }
}
