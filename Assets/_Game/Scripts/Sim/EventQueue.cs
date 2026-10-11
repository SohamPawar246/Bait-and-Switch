using BaitSwitch.Net;
using UnityEngine;

namespace BaitSwitch.Sim
{
    public struct SimEvent
    {
        public EventKind kind;
        public ushort a, b;
        public Vector2 pos;
        public int toSeat; // -1 = everyone, else only that seat
    }

    /// <summary>Fixed-size event buffer filled during a tick and drained by SeaServer. No allocations.</summary>
    public sealed class EventQueue
    {
        public readonly SimEvent[] Items = new SimEvent[128];
        public int Count;

        public void Clear() => Count = 0;

        public void Push(EventKind kind, int a, int b, Vector2 pos, int toSeat = -1)
        {
            if (Count >= Items.Length) return;
            Items[Count++] = new SimEvent { kind = kind, a = (ushort)a, b = (ushort)b, pos = pos, toSeat = toSeat };
        }
    }
}
