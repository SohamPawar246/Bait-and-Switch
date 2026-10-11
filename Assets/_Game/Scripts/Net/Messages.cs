using System;
using Mirror;

namespace BaitSwitch.Net
{
    // All network messages (docs/13-netcode.md). Fish and hooks never use NetworkTransform:
    // the server sends one packed SnapshotMsg (see SnapshotCodec) so players look identical to the school.

    [Flags]
    public enum Buttons : ushort
    {
        None = 0,
        Dash = 1 << 0,
        Ink = 1 << 1,
        Nibble = 1 << 2,   // fish: hold near bait
        Bite = 1 << 3,     // Leviathan: hold near boat
        Strike = 1 << 4,   // angler: click
        Reel = 1 << 5,     // angler: hold during tug
        Rebait = 1 << 6,   // angler: hold right mouse
        Sonar = 1 << 7,
        Mark = 1 << 8,
        Chum = 1 << 9,
    }

    /// <summary>Client → server once after connecting.</summary>
    public struct JoinMsg : NetworkMessage
    {
        public string name;
        public byte[] appearance; // PartCatalog.SlotCount bytes
    }

    /// <summary>Client → server at 30 Hz, unreliable.</summary>
    public struct InputMsg : NetworkMessage
    {
        public uint seq;
        public sbyte moveX;   // -127..127
        public sbyte moveY;
        public short cursorX; // world units * 256 (angler hook target, or mouse-follow swim target)
        public short cursorY;
        public Buttons buttons;
        // The two previous inputs (seq-1, seq-2), resent so 2% loss never costs an input.
        public sbyte prevX1, prevY1, prevX2, prevY2;
        public Buttons prevB1, prevB2;
    }

    /// <summary>Server → one client after its JoinMsg, reliable. Private: only you learn your fish index.</summary>
    public struct WelcomeMsg : NetworkMessage
    {
        public ushort ownIndex;   // 0xFFFF = no fish (spectator / angler)
        public ushort fishCount;  // fish per snapshot, constant for the round
        public uint tick;
        public Role role;
        public byte boatId;       // 0xFF = no boat (fish)
    }

    /// <summary>Dev only until GameStateManager (step 4): client asks to switch role (F1).</summary>
    public struct RoleRequestMsg : NetworkMessage
    {
        public Role role;
    }

    /// <summary>Server → client once per round, reliable. Tells only YOU your role and fish id.</summary>
    public struct RoundStartMsg : NetworkMessage
    {
        public byte round;            // 1..3
        public Role role;
        public ushort yourFishId;     // 0xFFFF if angler
        public byte yourBoatId;       // 0xFF if fish
        public byte appetiteSlot;     // the Deep's Appetite: a Slot and part id
        public byte appetitePart;
        public ushort fishCount;
        public byte[] appearanceTable; // fishCount * SlotCount bytes, indexed by shuffled fish id
        public string[] seatNames;     // for the ledger and boats; bots carry their cult titles
    }

    /// <summary>
    /// Server → client at 15 Hz, unreliable. One tick is split into partCount parts so each fits the
    /// 1200-byte KCP MTU (docs/BRIEF.md). Fish bytes are identical for everyone; only ackSeq/ownV*
    /// differ per recipient and describe the recipient's own fish (zero if it has none).
    /// data = fish [first, first+count) packed by SnapshotCodec; part 0 also carries hooks + boats.
    /// ArraySegment: valid only inside the handler, decode immediately.
    /// </summary>
    public struct SnapshotMsg : NetworkMessage
    {
        public uint tick;
        public uint ackSeq;       // last InputMsg.seq the server applied to your fish
        public short ownVx, ownVy; // your fish velocity (Quant), for replaying inputs
        public byte part, partCount;
        public ushort first, count;
        public ArraySegment<byte> data;
    }

    public enum EventKind : byte
    {
        Hooked, Snapped, TornLoose, Landed, Switched, LeviathanRises, BoatDragged,
        SonarPing, Mark, PhaseChanged, RoundEnd, MatchEnd, PlayerJoined, PlayerLeft,
        SomethingNewSwims, Rotation, Nibble, Eaten, InkCloud, MissScatter, Shock,
        SonarOutline, // private to the pinging Angler: a = fish index outlined
        Tangled,      // a = hook
        Escaped,      // private to the fish: you got away (b = points)
        Points,       // private: a = points gained, b = reason (EventKind of the cause)
        Reeled,       // public: a = fish seat landed, b = angler seat (0xFF none) — the named catch feed
    }

    /// <summary>Server → all, reliable, on change and once per second. The match clock and state for the HUD.</summary>
    public struct MatchStateMsg : NetworkMessage
    {
        public byte state;     // MatchState
        public byte round;     // 1..3
        public float time;     // seconds into the current state
        public byte phase;     // Phase
        public byte fishLeft;  // fish seats still swimming
    }

    public enum MatchState : byte { Waiting, Intro, Playing, Ledger, Final }

    /// <summary>
    /// Server → each client, reliable, once a second: who is in the sea. Roles and names are public; a fish body index
    /// is only filled in for fellow fish (fish see fish nametags), boat indices for anglers (anglers see angler tags).
    /// </summary>
    public struct RosterMsg : NetworkMessage
    {
        public byte you;          // receiver's seat (255 none)
        public string[] names;
        public byte[] roles;      // Role per seat (Spectator mid-round = being robed)
        public bool[] human;
        public short[] fish;      // fish index, only for fellow fish, else -1
        public short[] hook;      // boat/hook index for anglers, else -1
        public short[] points;
    }

    /// <summary>Server → client, reliable. Generic event; meaning of a/b/x/y depends on kind.</summary>
    public struct EventMsg : NetworkMessage
    {
        public EventKind kind;
        public ushort a;    // usually a fish id or seat
        public ushort b;    // usually a boat/seat id
        public short x;     // world position * 256 for effects
        public short y;
        public string text; // optional line from Copy
    }

    /// <summary>Server → clients in a tug, 10 Hz, unreliable.</summary>
    public struct TugMsg : NetworkMessage
    {
        public byte tugId;
        public byte tension;     // 0..255 maps to 0..threshold
        public byte threshold;   // current snap threshold (fatigue lowers it)
        public byte stamina;     // 0..255 maps to 0..max
        public ushort distanceCm;
        public bool exhausted;
    }

    /// <summary>Server → client after each round, reliable.</summary>
    public struct LedgerMsg : NetworkMessage
    {
        public byte round;
        public bool fishWon;
        public string[] names;
        public short[] points;     // total so far
        public byte[] catches;
        public byte[] escapes;
        public string[] titles;    // filled after the final round, else empty
    }

    /// <summary>Server → client on connect: for the Edgegap HUD panel.</summary>
    public struct ServerInfoMsg : NetworkMessage
    {
        public string location;    // "Mumbai, India"
        public string deploymentId;
        public float uptimeSeconds;
    }

    public static class Quant
    {
        public const float Scale = 256f; // 1/256 unit resolution, ±128 unit range
        public static short Pack(float v) => (short)UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(v * Scale), short.MinValue, short.MaxValue);
        public static float Unpack(short v) => v / Scale;
    }
}
