using System;
using UnityEngine;

namespace BaitSwitch.Net
{
    /// <summary>
    /// Client: snapshot ring keyed by server tick (fish + hooks + boats), a smoothed render clock
    /// ~interpDelay behind the newest tick, and interpolation. Preallocated; zero allocations per frame.
    /// </summary>
    public sealed class SeaClient : MonoBehaviour
    {
        GameConfig _c;
        LocalPredictor _predictor;
        View.SeaView _view;
        View.HookView _hookView;

        public int FishCount { get; private set; }
        public int OwnIndex { get; private set; } = -1;
        public Role Role { get; private set; } = Role.Spectator;
        public int BoatId { get; private set; } = -1;
        public int Seats { get; private set; }
        public bool HasData => _any;

        // match info for the HUD
        public MatchStateMsg Match { get; private set; }
        public float MatchStateRecvTime { get; private set; }
        public RoundStartMsg RoundInfo { get; private set; }
        public bool HasRound { get; private set; }
        public LedgerMsg Ledger { get; private set; }
        public bool HasLedger { get; private set; }
        public TugMsg Tug { get; private set; }
        public float TugRecvTime { get; private set; } = -10f;
        public event System.Action<EventMsg> EventReceived;

        /// <summary>Seconds into the current match state, advanced locally between MatchStateMsgs.</summary>
        public float MatchTime => Match.time + (Time.unscaledTime - MatchStateRecvTime);

        uint[] _slotTick;
        bool[] _slotValid;
        FishSnap[][] _slot;
        HookSnap[][] _hooks;   // [slot][owner]
        bool[][] _hookOn;
        float[][] _boatX;
        HookSnap[] _hookScratch;
        BoatSnap[] _boatScratch;
        uint _latestTick;
        bool _any;
        double _latestRecvTime;
        double _renderTick;

        int _fromS, _toS;
        float _t;

        void Awake()
        {
            _predictor = GetComponent<LocalPredictor>();
            _view = GetComponent<View.SeaView>();
            _hookView = GetComponent<View.HookView>();
        }

        public void OnWelcome(WelcomeMsg m)
        {
            _c = GameConfig.I;
            OwnIndex = m.ownIndex == 0xFFFF ? -1 : m.ownIndex;
            Role = m.role;
            BoatId = m.boatId == 0xFF ? -1 : m.boatId;
            if (_slot == null || FishCount != m.fishCount)
            {
                FishCount = m.fishCount;
                Seats = _c.seats;
                int len = Mathf.Max(4, _c.snapshotBufferLen);
                _slotTick = new uint[len];
                _slotValid = new bool[len];
                _slot = new FishSnap[len][];
                _hooks = new HookSnap[len][];
                _hookOn = new bool[len][];
                _boatX = new float[len][];
                for (int i = 0; i < len; i++)
                {
                    _slot[i] = new FishSnap[FishCount];
                    _hooks[i] = new HookSnap[Seats];
                    _hookOn[i] = new bool[Seats];
                    _boatX[i] = new float[Seats];
                }
                _hookScratch = new HookSnap[256];
                _boatScratch = new BoatSnap[256];
                _any = false;
                if (_view != null) _view.Build(FishCount);
                if (_hookView != null) _hookView.Build(Seats);
            }
            if (_predictor != null) _predictor.Begin(OwnIndex, Role, BoatId);
        }

        public void Clear()
        {
            _any = false;
            _slot = null;
            FishCount = 0;
            OwnIndex = -1;
            BoatId = -1;
            Role = Role.Spectator;
            if (_predictor != null) _predictor.End();
            if (_view != null) _view.Clear();
            if (_hookView != null) _hookView.Clear();
        }

        public void OnEvent(EventMsg m)
        {
            if (_hookView != null) _hookView.OnEvent(m);
            if (_view != null) _view.OnEvent(m);
            EventReceived?.Invoke(m);
        }

        public void OnRoundStart(RoundStartMsg m)
        {
            RoundInfo = m;
            HasRound = true;
            if (_view != null) _view.Rebuild(m.appearanceTable);
            HasLedger = false;
            _any = false; // indices were reshuffled: never interpolate across rounds
        }

        public void OnMatchState(MatchStateMsg m)
        {
            Match = m;
            MatchStateRecvTime = Time.unscaledTime;
            if ((MatchState)m.state == MatchState.Waiting) { HasRound = false; HasLedger = false; }
        }

        public void OnLedger(LedgerMsg m) { Ledger = m; HasLedger = true; }

        public RosterMsg Roster { get; private set; }
        public bool HasRoster { get; private set; }
        public void OnRoster(RosterMsg m) { Roster = m; HasRoster = m.names != null; }

        public void OnTug(TugMsg m) { Tug = m; TugRecvTime = Time.unscaledTime; }

        public bool InTug => Time.unscaledTime - TugRecvTime < 0.4f;

        public void OnSnapshot(SnapshotMsg m)
        {
            if (_slot == null || m.first + m.count > FishCount) return;
            int len = _slotTick.Length;
            int s = (int)(m.tick % (uint)len);
            if (!_slotValid[s] || _slotTick[s] != m.tick)
            {
                if (_any && m.tick <= _latestTick) return; // late part for a slot we already reused
                // New tick: start from the newest data so a lost part holds the previous state.
                if (_any)
                {
                    int l = (int)(_latestTick % (uint)len);
                    Array.Copy(_slot[l], _slot[s], FishCount);
                    Array.Copy(_hooks[l], _hooks[s], Seats);
                    Array.Copy(_hookOn[l], _hookOn[s], Seats);
                    Array.Copy(_boatX[l], _boatX[s], Seats);
                }
                _slotTick[s] = m.tick;
                _slotValid[s] = true;
            }
            int r = SnapshotCodec.DecodeFish(m.data, 0, _slot[s], m.first, m.count);
            if (m.part == 0 && r < m.data.Count)
            {
                r += SnapshotCodec.DecodeHooks(m.data, r, _hookScratch, out int nh);
                SnapshotCodec.DecodeBoats(m.data, r, _boatScratch, out int nb);
                Array.Clear(_hookOn[s], 0, Seats);
                for (int i = 0; i < nh; i++)
                {
                    int o = _hookScratch[i].owner;
                    if (o >= Seats) continue;
                    _hooks[s][o] = _hookScratch[i];
                    _hookOn[s][o] = true;
                }
                for (int i = 0; i < nb; i++)
                {
                    int o = _boatScratch[i].owner;
                    if (o < Seats) _boatX[s][o] = _boatScratch[i].x;
                }
                if (_predictor != null && BoatId >= 0 && BoatId < Seats && _hookOn[s][BoatId] && m.tick >= _latestTick)
                    _predictor.OnServerHook(m.tick, m.ackSeq, _hooks[s][BoatId].pos, _hooks[s][BoatId].state, _boatX[s][BoatId]);
            }

            if (!_any || m.tick > _latestTick)
            {
                _latestTick = m.tick;
                _latestRecvTime = Time.unscaledTimeAsDouble;
                if (!_any) { _renderTick = m.tick - _c.interpDelay * _c.simHz; _any = true; }
            }

            if (_predictor != null && OwnIndex >= m.first && OwnIndex < m.first + m.count)
                _predictor.OnServerState(m.tick, m.ackSeq, _slot[s][OwnIndex],
                    new Vector2(Quant.Unpack(m.ownVx), Quant.Unpack(m.ownVy)));
        }

        void Update()
        {
            if (!_any) return;
            double hz = _c.simHz;
            double now = Time.unscaledTimeAsDouble;
            double target = _latestTick + (now - _latestRecvTime) * hz - _c.interpDelay * hz;
            double err = target - _renderTick;
            if (Math.Abs(err) > _c.clockResetSeconds * hz) _renderTick = target;
            else
            {
                double rate = 1.0 + Math.Max(-_c.clockNudge, Math.Min(_c.clockNudge, err / hz * 2.0));
                _renderTick += Time.unscaledDeltaTime * hz * rate;
            }
            FindWindow();
        }

        void FindWindow()
        {
            int len = _slotTick.Length;
            double rt = Math.Min(_renderTick, _latestTick);
            _fromS = -1; _toS = -1;
            uint fromT = 0, toT = 0;
            for (uint back = 0; back < len && back <= _latestTick; back++)
            {
                uint k = _latestTick - back;
                int s = (int)(k % (uint)len);
                if (!_slotValid[s] || _slotTick[s] != k) continue;
                if (k <= rt) { _fromS = s; fromT = k; break; }
                _toS = s; toT = k;
            }
            if (_fromS < 0) { _fromS = _toS; _t = 0f; }
            else if (_toS < 0) { _toS = _fromS; _t = 0f; }
            else _t = (float)((rt - fromT) / (toT - fromT));
        }

        /// <summary>Interpolated pose of fish i at the current render time. Call after Update.</summary>
        public void Sample(int i, out Vector2 pos, out float heading, out FishFlags flags)
        {
            ref FishSnap a = ref _slot[_fromS][i];
            ref FishSnap b = ref _slot[_toS][i];
            pos = Vector2.LerpUnclamped(a.pos, b.pos, _t);
            heading = Mathf.LerpAngle(a.heading, b.heading, _t);
            flags = b.flags;
            // never slide a fish across the sea when it respawns
            if (((a.flags ^ b.flags) & FishFlags.Dead) != 0) pos = b.pos;
        }

        /// <summary>Interpolated hook + boat of seat h. False if that seat has no Angler.</summary>
        public bool SampleHook(int h, out Vector2 hookPos, out HookState state, out float boatX)
        {
            hookPos = default; state = HookState.Hidden; boatX = 0f;
            if (!_any || h < 0 || h >= Seats || !_hookOn[_toS][h]) return false;
            bool fromOn = _hookOn[_fromS][h];
            ref HookSnap a = ref _hooks[fromOn ? _fromS : _toS][h];
            ref HookSnap b = ref _hooks[_toS][h];
            hookPos = Vector2.LerpUnclamped(a.pos, b.pos, _t);
            state = b.state;
            boatX = Mathf.LerpUnclamped(_boatX[fromOn ? _fromS : _toS][h], _boatX[_toS][h], _t);
            return true;
        }

        public byte LatestSize(int i) => _any ? _slot[(int)(_latestTick % (uint)_slotTick.Length)][i].sizeStep : (byte)0;

        public FishFlags LatestFlags(int i) => _any ? _slot[(int)(_latestTick % (uint)_slotTick.Length)][i].flags : FishFlags.None;
    }
}
