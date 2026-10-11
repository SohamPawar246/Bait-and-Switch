using System.Collections.Generic;
using BaitSwitch.Net;
using UnityEngine;

namespace BaitSwitch.Sim
{
    /// <summary>A seat in the match: a human or a bot, playing a fish or an Angler. Always 6 per round.</summary>
    public sealed class Seat
    {
        public int index;
        public bool bot = true;
        public int peer = -1;             // connectionId when human
        public string humanName = "";
        public string botFishName, botAnglerName;
        public Role role = Role.Spectator;
        public int fish = -1, hook = -1;
        public byte[] appearance;
        public bool startedAsAngler;      // this match
        public float convertT = -1f;      // > 0: landed, becoming an Angler
        public float biteT;               // Leviathan bite progress
        public bool dirty;                // body/role changed: SeaServer sends WelcomeMsg

        // match stats
        public int points, catches, escapes, playerCatches;
        public float maxGrowth;
        public bool dragged;

        // inputs for this tick (from the network or a bot brain)
        public Vector2 move, cursor;
        public Buttons buttons;

        public FishBot fishBot;
        public AnglerBot anglerBot;

        public string Name => bot ? (role == Role.Angler ? botAnglerName : botFishName) : humanName;
        public bool Swimming => role == Role.Fish && fish >= 0 && convertT < 0f;
    }

    /// <summary>
    /// Server game state (docs/04, 07, 10, 11): match of 3 rounds (intro → day/dusk/night → ledger), seats and roles,
    /// conversions, tugs, growth, Leviathan, sonar, marks, scoring, the Deep's Appetite, bots, join/leave takeover.
    /// Owns the per-round SeaSim / HookSim / TugSystem. Pure C#; SeaServer does the networking.
    /// </summary>
    public sealed class GameState : IHookRules, ITugRules
    {
        public readonly GameConfig Config;
        public readonly EventQueue Events = new EventQueue();
        public readonly Seat[] Seats;
        public SeaSim Sea { get; private set; }
        public HookSim Hooks { get; private set; }
        public TugSystem Tugs { get; private set; }

        public MatchState State { get; private set; } = MatchState.Waiting;
        public int Round { get; private set; }
        public float StateTime { get; private set; }
        public Phase Phase { get; private set; }
        public int FishLeft { get; private set; }
        public byte AppetiteSlot { get; private set; }
        public byte AppetitePart { get; private set; }
        public byte[] AppearanceTable { get; private set; }
        public bool StateDirty, RoundStartPending, LedgerPending;
        public bool LastRoundFishWon { get; private set; }
        public int RoundStartAngler { get; private set; } = -1;

        readonly System.Random _rng;
        float[] _markTime;
        int[] _markBy;
        float _matchClock;

        public GameState(GameConfig c, int seed)
        {
            Config = c;
            _rng = new System.Random(seed);
            Seats = new Seat[c.seats];
            var used = new HashSet<int>();
            for (int i = 0; i < Seats.Length; i++)
            {
                int n;
                do n = _rng.Next(10, 99); while (!used.Add(n));
                Seats[i] = new Seat
                {
                    index = i,
                    botFishName = string.Format(Copy.BotFish, n),
                    botAnglerName = Copy.BotAnglers[i % Copy.BotAnglers.Length],
                };
            }
            NewSea();
        }

        public int HumanCount
        {
            get { int n = 0; foreach (var s in Seats) if (!s.bot) n++; return n; }
        }

        public Seat SeatOfPeer(int peer)
        {
            foreach (var s in Seats) if (!s.bot && s.peer == peer) return s;
            return null;
        }

        // ------------------------------------------------------------------ joins and leaves

        /// <summary>A human arrives. Takes a bot seat (fish first) and its body, invisibly. Null if all 6 seats are human.</summary>
        public Seat AddHuman(int peer, string name, byte[] appearance)
        {
            Seat pick = null;
            foreach (var s in Seats) if (s.bot && s.role == Role.Fish && s.Swimming) { pick = s; break; }
            if (pick == null) foreach (var s in Seats) if (s.bot) { pick = s; break; }
            if (pick == null) return null;
            pick.bot = false;
            pick.peer = peer;
            pick.humanName = string.IsNullOrWhiteSpace(name) ? Copy.NameSuggestions[_rng.Next(Copy.NameSuggestions.Length)] : name;
            if (appearance != null && appearance.Length == PartCatalog.SlotCount) pick.appearance = appearance;
            if (pick.fish >= 0) Sea.AutoPilot[pick.fish] = false;
            pick.dirty = true;
            Events.Push(EventKind.PlayerJoined, pick.index, 0, Vector2.zero);
            if (State == MatchState.Waiting) StartRound(1);
            return pick;
        }

        /// <summary>A human leaves: a bot takes over the body so the round stays balanced.</summary>
        public void RemoveHuman(int peer)
        {
            var s = SeatOfPeer(peer);
            if (s == null) return;
            s.bot = true;
            s.peer = -1;
            Events.Push(EventKind.PlayerLeft, s.index, 0, Vector2.zero);
            if (HumanCount == 0) ResetToWaiting();
        }

        // ------------------------------------------------------------------ round flow

        void NewSea()
        {
            Sea = new SeaSim(Config, _rng.Next(), Events);
            Hooks = new HookSim(Config, Sea, Events) { Rules = this, StrikesEnabled = false };
            Tugs = new TugSystem(Config, Sea, Hooks, Events) { Rules = this };
            _markTime = new float[Sea.Count];
            _markBy = new int[Sea.Count];
            for (int i = 0; i < _markBy.Length; i++) _markBy[i] = -1;
        }

        void ResetToWaiting()
        {
            State = MatchState.Waiting;
            Round = 0;
            StateTime = 0f;
            foreach (var s in Seats) ClearSeatBody(s);
            NewSea();
            StateDirty = true;
        }

        void StartRound(int round)
        {
            var c = Config;
            Round = round;
            if (round == 1) foreach (var s in Seats) { s.points = s.catches = s.escapes = s.playerCatches = 0; s.maxGrowth = 0f; s.dragged = false; s.startedAsAngler = false; }
            foreach (var s in Seats) { s.fish = -1; s.hook = -1; s.convertT = -1f; s.biteT = 0f; s.role = Role.Spectator; }
            NewSea();

            // Appearances: every fish (school included) from the same kit; IDs are shuffled each round by construction.
            var cat = PartCatalog.I;
            AppearanceTable = new byte[Sea.Count * PartCatalog.SlotCount];
            for (int i = 0; i < Sea.Count; i++)
            {
                byte[] a = cat != null ? cat.RandomAppearance(_rng) : new byte[PartCatalog.SlotCount];
                System.Buffer.BlockCopy(a, 0, AppearanceTable, i * PartCatalog.SlotCount, PartCatalog.SlotCount);
            }

            // Starting Angler: a random human who hasn't started as Angler yet; else a bot.
            // One human: fish in round 1, Angler in round 2, fish in round 3.
            int starter = -1;
            var humans = new List<Seat>();
            foreach (var s in Seats) if (!s.bot && !s.startedAsAngler) humans.Add(s);
            if (HumanCount == 1) { if (round == 2) starter = humans.Count > 0 ? humans[0].index : -1; }
            else if (humans.Count > 0) starter = humans[_rng.Next(humans.Count)].index;
            if (starter < 0) foreach (var s in Seats) if (s.bot && !s.startedAsAngler) { starter = s.index; break; }
            if (starter < 0) foreach (var s in Seats) if (s.bot) { starter = s.index; break; }
            RoundStartAngler = starter;

            foreach (var s in Seats)
            {
                if (s.index == starter) { s.startedAsAngler = true; MakeAngler(s); }
                else MakeFish(s, growth: 0f);
                s.fishBot = new FishBot(_rng, c);
                s.anglerBot = new AnglerBot();
            }

            // The Deep's Appetite: one fish gameplay part this round.
            AppetiteSlot = (byte)_rng.Next(0, (int)Slot.Special + 1);
            int count = cat != null ? cat.CountOf((Slot)AppetiteSlot) : 1;
            AppetitePart = (byte)_rng.Next(0, Mathf.Max(1, count));

            State = MatchState.Intro;
            StateTime = 0f;
            Phase = Phase.Day;
            Hooks.StrikesEnabled = false;
            RoundStartPending = true;
            StateDirty = true;
            CountFish();
        }

        void MakeFish(Seat s, float growth)
        {
            ClearSeatBody(s);
            s.role = Role.Fish;
            s.fish = Sea.Claim(s.index);
            if (s.fish >= 0)
            {
                Sea.SetGrowth(s.fish, growth);
                Sea.AutoPilot[s.fish] = s.bot;
                if (s.appearance != null && AppearanceTable != null)
                    System.Buffer.BlockCopy(s.appearance, 0, AppearanceTable, s.fish * PartCatalog.SlotCount, PartCatalog.SlotCount);
            }
            s.dirty = true;
        }

        void MakeAngler(Seat s)
        {
            ClearSeatBody(s);
            s.role = Role.Angler;
            s.hook = Hooks.Claim();
            if (s.hook < 0) { MakeFish(s, 0f); return; }
            s.dirty = true;
        }

        void ClearSeatBody(Seat s)
        {
            if (s.fish >= 0 && Sea != null && Sea.Owner[s.fish] == s.index) Sea.Release(s.fish);
            if (s.hook >= 0 && Hooks != null) Hooks.Release(s.hook);
            s.fish = -1; s.hook = -1;
            s.convertT = -1f;
        }

        // ------------------------------------------------------------------ tick

        /// <summary>One 30 Hz tick. Seat inputs must already hold this tick's human inputs.</summary>
        public void Tick(float dt)
        {
            var c = Config;
            _matchClock += dt;
            StateTime += dt;

            // Bots think, then every seat's input goes into the sim.
            bool live = State == MatchState.Intro || State == MatchState.Playing;
            foreach (var s in Seats)
            {
                if (s.bot && live)
                {
                    if (s.role == Role.Fish) s.fishBot?.Think(this, s, dt, _rng);
                    else if (s.role == Role.Angler) s.anglerBot?.Think(this, s, dt, _rng);
                }
                if (!live) { s.move = Vector2.zero; s.buttons = Buttons.None; }
                if (s.Swimming)
                {
                    Sea.Move[s.fish] = s.move;
                    Sea.Held[s.fish] = s.buttons;
                }
                else if (s.role == Role.Angler && s.hook >= 0) Hooks.SetInput(s.hook, s.move.x, s.cursor, s.buttons);
            }

            if (State == MatchState.Playing) AnglerAbilities();
            Hooks.Step(dt);
            Tugs.Step(dt);
            Sea.Step(dt);
            if (State == MatchState.Playing) Bites(dt);
            Conversions(dt);
            foreach (var s in Seats) if (s.Swimming) s.maxGrowth = Mathf.Max(s.maxGrowth, Sea.Growth[s.fish]);

            switch (State)
            {
                case MatchState.Waiting:
                    break;
                case MatchState.Intro:
                    if (StateTime >= c.introSeconds) { State = MatchState.Playing; StateTime = 0f; Hooks.StrikesEnabled = true; StateDirty = true; }
                    break;
                case MatchState.Playing:
                {
                    var ph = c.PhaseAt(StateTime);
                    if (ph != Phase) { Phase = ph; Events.Push(EventKind.PhaseChanged, (int)ph, 0, Vector2.zero); StateDirty = true; }
                    CountFish();
                    if (FishLeft == 0) EndRound(fishWon: false);
                    else if (StateTime >= c.roundSeconds) EndRound(fishWon: true);
                    break;
                }
                case MatchState.Ledger:
                    if (StateTime >= c.ledgerSeconds)
                    {
                        if (Round < c.roundsPerMatch) StartRound(Round + 1);
                        else { State = MatchState.Final; StateTime = 0f; LedgerPending = true; StateDirty = true; Events.Push(EventKind.MatchEnd, 0, 0, Vector2.zero); }
                    }
                    break;
                case MatchState.Final:
                    if (StateTime >= c.ledgerSeconds)
                    {
                        if (HumanCount > 0) StartRound(1);
                        else ResetToWaiting();
                    }
                    break;
            }
        }

        void CountFish()
        {
            int n = 0;
            foreach (var s in Seats) if (s.Swimming) n++;
            if (n != FishLeft) StateDirty = true;
            FishLeft = n;
        }

        void EndRound(bool fishWon)
        {
            var c = Config;
            Tugs.CancelAll();
            Hooks.StrikesEnabled = false;
            LastRoundFishWon = fishWon;
            if (fishWon)
            {
                int survivors = 0;
                foreach (var s in Seats) if (s.Swimming) survivors++;
                foreach (var s in Seats)
                {
                    if (!s.Swimming) continue;
                    Award(s, c.ptsSurvive, EventKind.RoundEnd);
                    if (survivors == 1) Award(s, c.ptsLastFish, EventKind.RoundEnd);
                    if (WearsAppetite(s.fish)) Award(s, c.ptsAppetite, EventKind.RoundEnd);
                }
            }
            else if (RoundStartAngler >= 0) Award(Seats[RoundStartAngler], c.ptsCleanSweep, EventKind.RoundEnd);

            Events.Push(EventKind.RoundEnd, fishWon ? 1 : 0, Round, Vector2.zero);
            State = MatchState.Ledger;
            StateTime = 0f;
            LedgerPending = true;
            StateDirty = true;
        }

        // ------------------------------------------------------------------ abilities

        void AnglerAbilities()
        {
            var c = Config;
            foreach (var s in Seats)
            {
                if (s.role != Role.Angler || s.hook < 0) continue;
                ref Hook k = ref Hooks.Hooks[s.hook];
                Vector2 boat = HookPhysics.RodTip(k.boatX, c);

                if (k.Pressed(Buttons.Sonar) && k.sonarCd <= 0f)
                {
                    k.sonarCd = c.sonarCooldown;
                    Events.Push(EventKind.SonarPing, s.hook, 0, boat);
                    float r2 = c.sonarRadius * c.sonarRadius;
                    foreach (var f in Seats)
                        if (f.Swimming && (Sea.Fish[f.fish].pos - boat).sqrMagnitude <= r2)
                            Events.Push(EventKind.SonarOutline, f.fish, 0, Sea.Fish[f.fish].pos, s.index);
                }

                if (k.Pressed(Buttons.Mark) && k.markCd <= 0f)
                {
                    int f = Sea.Closest(s.cursor, 1.5f);
                    if (f >= 0)
                    {
                        k.markCd = c.markCooldown;
                        _markBy[f] = s.index;
                        _markTime[f] = _matchClock;
                        foreach (var a in Seats)
                            if (a.role == Role.Angler) Events.Push(EventKind.Mark, f, s.index, Sea.Fish[f].pos, a.index);
                    }
                }
            }
        }

        void Bites(float dt)
        {
            var c = Config;
            foreach (var s in Seats)
            {
                if (!s.Swimming || !Sea.IsLeviathan(s.fish) || Sea.IsHookedOrDead(s.fish)) { s.biteT = 0f; continue; }
                bool biting = (s.buttons & (Buttons.Bite | Buttons.Nibble)) != 0;
                int boat = -1;
                Vector2 p = Sea.Fish[s.fish].pos;
                for (int h = 0; h < Hooks.Hooks.Length; h++)
                {
                    if (!Hooks.Hooks[h].active) continue;
                    if ((new Vector2(Hooks.Hooks[h].boatX, c.surfaceY) - p).magnitude <= c.leviathanBiteRange) { boat = h; break; }
                }
                if (!biting || boat < 0) { s.biteT = 0f; continue; }
                s.biteT += dt;
                if (s.biteT < c.leviathanBiteTime) continue;

                // The boat is dragged under: that Angler becomes a tiny fish; the Leviathan shrinks back to growth 3.
                s.biteT = 0f;
                Seat victim = null;
                foreach (var a in Seats) if (a.role == Role.Angler && a.hook == boat) { victim = a; break; }
                Events.Push(EventKind.BoatDragged, boat, s.index, new Vector2(Hooks.Hooks[boat].boatX, c.surfaceY));
                Sea.SetGrowth(s.fish, c.leviathanGrowthAfterDrag);
                Award(s, c.ptsBoatDrag, EventKind.BoatDragged);
                s.dragged = true;
                if (victim != null)
                {
                    int tug = Hooks.Hooks[boat].tug;
                    if (tug >= 0) Tugs.CancelAll();
                    MakeFish(victim, 0f);
                    Events.Push(EventKind.Switched, victim.index, 0, Vector2.zero, victim.index);
                }
            }
        }

        void Conversions(float dt)
        {
            foreach (var s in Seats)
            {
                if (s.convertT < 0f) continue;
                s.convertT -= dt;
                if (s.convertT <= 0f)
                {
                    s.convertT = -1f;
                    MakeAngler(s);
                    Events.Push(EventKind.SomethingNewSwims, s.index, 0, Vector2.zero);
                }
            }
        }

        // ------------------------------------------------------------------ IHookRules

        public void OnBaitEaten(int hook, int fish, bool school)
        {
            if (school) return;
            var s = SeatOfFish(fish);
            if (s == null) return;
            Award(s, Config.ptsEatBait, EventKind.Eaten);
            Sea.AddGrowth(fish, Config.GrowthForPhase(Phase));
            s.maxGrowth = Mathf.Max(s.maxGrowth, Sea.Growth[fish]);
        }

        public void OnSchoolLanded(int hook, int fish)
        {
            var a = SeatOfHook(hook);
            if (a == null) return;
            a.catches++;
            Award(a, Config.ptsLandSchool, EventKind.Landed);
        }

        public bool OnStrikeCatch(int hook, int fish)
        {
            if (SeatOfFish(fish) == null && !Sea.IsLeviathan(fish)) return false; // plain school fish: auto-land
            return Tugs.Start(hook, fish) >= 0 || Tugs.FindByFish(fish) >= 0;
        }

        // ------------------------------------------------------------------ ITugRules

        public void OnTugLanded(int tug)
        {
            var c = Config;
            ref Tug g = ref Tugs.Tugs[tug];
            var fishSeat = SeatOfFish(g.fish);
            bool appetite = fishSeat != null && WearsAppetite(g.fish);
            Vector2 p = Sea.Fish[g.fish].pos;
            Events.Push(EventKind.Landed, g.hookA, g.fish, p);

            for (int k = 0; k < 2; k++)
            {
                int h = k == 0 ? g.hookA : g.hookB;
                if (h < 0) continue;
                var a = SeatOfHook(h);
                if (a == null) continue;
                a.catches++;
                a.playerCatches++;
                Award(a, g.leviathan ? c.ptsLandLeviathan : c.ptsLandPlayer, EventKind.Landed);
                if (appetite && k == 0) Award(a, c.ptsAppetite, EventKind.Landed);
                Hooks.Hooks[h].baited = false;
            }
            int marker = _markBy[g.fish];
            if (marker >= 0 && _matchClock - _markTime[g.fish] <= 10f) Award(Seats[marker], c.ptsMarkAssist, EventKind.Mark);

            Sea.Remove(g.fish);
            if (fishSeat != null)
            {
                fishSeat.fish = -1;
                fishSeat.convertT = 3f; // flop on deck, the robing, then their own dinghy
                fishSeat.dirty = true;
                Events.Push(EventKind.Switched, fishSeat.index, 0, p, fishSeat.index);
                var angler = g.hookA >= 0 ? SeatOfHook(g.hookA) : null;
                Events.Push(EventKind.Reeled, fishSeat.index, angler != null ? angler.index : 0xFF, p);
            }
            CountFish();
        }

        public void OnTugEscaped(int tug, bool snapped)
        {
            ref Tug g = ref Tugs.Tugs[tug];
            var fishSeat = SeatOfFish(g.fish);
            if (fishSeat != null)
            {
                fishSeat.escapes++;
                if (snapped) Award(fishSeat, Config.ptsEscape, EventKind.Snapped);
                Events.Push(EventKind.Escaped, snapped ? Config.ptsEscape : 0, 0, Sea.Fish[g.fish].pos, fishSeat.index);
            }
            if (snapped) Hooks.Tangle(g.hookA, 3f);
            else Hooks.Hooks[g.hookA].baited = false;
            if (g.hookB >= 0) Hooks.Hooks[g.hookB].baited = false;
        }

        // ------------------------------------------------------------------ helpers

        public Seat SeatOfFish(int fish)
        {
            if (fish < 0) return null;
            foreach (var s in Seats) if (s.fish == fish && s.role == Role.Fish) return s;
            return null;
        }

        public Seat SeatOfHook(int hook)
        {
            if (hook < 0) return null;
            foreach (var s in Seats) if (s.hook == hook && s.role == Role.Angler) return s;
            return null;
        }

        bool WearsAppetite(int fish) =>
            AppearanceTable != null && fish >= 0 &&
            AppearanceTable[fish * PartCatalog.SlotCount + AppetiteSlot] == AppetitePart;

        void Award(Seat s, int pts, EventKind reason)
        {
            if (pts == 0) return;
            s.points += pts;
            Events.Push(EventKind.Points, pts, (int)reason, Vector2.zero, s.index);
        }

        public string AppetiteName()
        {
            var p = PartCatalog.I != null ? PartCatalog.I.Find((Slot)AppetiteSlot, AppetitePart) : null;
            return p != null ? p.displayName : ((Slot)AppetiteSlot).ToString();
        }

        /// <summary>Builds the ledger for the round just ended (titles only after the final round).</summary>
        public LedgerMsg BuildLedger()
        {
            int n = Seats.Length;
            var m = new LedgerMsg
            {
                round = (byte)Round,
                fishWon = LastRoundFishWon,
                names = new string[n], points = new short[n], catches = new byte[n], escapes = new byte[n],
                titles = new string[n],
            };
            for (int i = 0; i < n; i++)
            {
                var s = Seats[i];
                m.names[i] = s.Name + (s.bot ? " (bot)" : "");
                m.points[i] = (short)s.points;
                m.catches[i] = (byte)Mathf.Min(255, s.catches);
                m.escapes[i] = (byte)Mathf.Min(255, s.escapes);
                m.titles[i] = "";
            }
            if (State == MatchState.Final)
            {
                Title(m, Copy.TitleBiggest, s => s.maxGrowth);
                Title(m, Copy.TitleHook, s => s.playerCatches);
                Title(m, Copy.TitleSlippery, s => s.escapes);
                for (int i = 0; i < n; i++) if (Seats[i].dragged) Append(m, i, Copy.TitleLeviathan);
            }
            return m;
        }

        void Title(LedgerMsg m, string title, System.Func<Seat, float> score)
        {
            int best = -1; float bestV = 0f;
            for (int i = 0; i < Seats.Length; i++) { float v = score(Seats[i]); if (v > bestV) { bestV = v; best = i; } }
            if (best >= 0) Append(m, best, title);
        }

        static void Append(LedgerMsg m, int i, string t) => m.titles[i] = string.IsNullOrEmpty(m.titles[i]) ? t : m.titles[i] + ", " + t;
    }
}
