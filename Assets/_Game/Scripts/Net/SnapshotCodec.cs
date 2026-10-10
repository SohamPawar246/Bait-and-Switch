using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaitSwitch.Net
{
    [Flags]
    public enum FishFlags : byte
    {
        None = 0, Dashing = 1, Inked = 2, Nibbling = 4, Hooked = 8, Dead = 16, Leviathan = 32, Glowing = 64, Scattered = 128,
    }

    public enum HookState : byte { Hidden, Sinking, Baited, Empty, Striking, Tugging, ReelingUp }

    /// <summary>One fish as sent to clients. School, players and bots are all the same record.</summary>
    public struct FishSnap
    {
        public ushort id;
        public Vector2 pos;
        public float heading;   // degrees, 0 = facing +X
        public FishFlags flags;
        public byte sizeStep;   // growth steps, 0..
    }

    public struct HookSnap
    {
        public byte owner;      // boat/seat id
        public Vector2 pos;
        public HookState state;
    }

    public struct BoatSnap
    {
        public byte owner;
        public float x;
        public float tilt;      // degrees, -127..127
        public bool sinking;
    }

    /// <summary>
    /// Packs fish, hooks and boats into one byte[] (about 9 bytes per fish, ~1.4 KB for 156 fish).
    /// Layout: [u16 fishCount][fish...][u8 hookCount][hooks...][u8 boatCount][boats...]
    /// Fish: u16 id, i16 x, i16 y, u8 heading, u8 flags, u8 size  (9 bytes)
    /// Hook: u8 owner, i16 x, i16 y, u8 state                     (6 bytes)
    /// Boat: u8 owner, i16 x, i8 tilt, u8 sinking                 (5 bytes)
    /// </summary>
    public static class SnapshotCodec
    {
        static byte[] _buf = new byte[4096];
        static int _w;

        public static byte[] Encode(List<FishSnap> fish, List<HookSnap> hooks, List<BoatSnap> boats)
        {
            int need = 2 + fish.Count * 9 + 1 + hooks.Count * 6 + 1 + boats.Count * 5;
            if (_buf.Length < need) _buf = new byte[need * 2];
            _w = 0;

            U16((ushort)fish.Count);
            foreach (var f in fish)
            {
                U16(f.id); I16(Quant.Pack(f.pos.x)); I16(Quant.Pack(f.pos.y));
                U8((byte)Mathf.RoundToInt(Mathf.Repeat(f.heading, 360f) / 360f * 255f));
                U8((byte)f.flags); U8(f.sizeStep);
            }
            U8((byte)hooks.Count);
            foreach (var h in hooks) { U8(h.owner); I16(Quant.Pack(h.pos.x)); I16(Quant.Pack(h.pos.y)); U8((byte)h.state); }
            U8((byte)boats.Count);
            foreach (var b in boats)
            {
                U8(b.owner); I16(Quant.Pack(b.x));
                U8((byte)(sbyte)Mathf.Clamp(Mathf.RoundToInt(b.tilt), -127, 127)); U8(b.sinking ? (byte)1 : (byte)0);
            }

            var outArr = new byte[_w];
            Buffer.BlockCopy(_buf, 0, outArr, 0, _w);
            return outArr;
        }

        /// <summary>Decode into caller-owned lists (cleared first) to avoid allocations on the client.</summary>
        public static void Decode(byte[] d, List<FishSnap> fish, List<HookSnap> hooks, List<BoatSnap> boats)
        {
            fish.Clear(); hooks.Clear(); boats.Clear();
            int r = 0;
            int nf = RU16(d, ref r);
            for (int i = 0; i < nf; i++)
            {
                fish.Add(new FishSnap
                {
                    id = RU16(d, ref r),
                    pos = new Vector2(Quant.Unpack(RI16(d, ref r)), Quant.Unpack(RI16(d, ref r))),
                    heading = d[r++] / 255f * 360f,
                    flags = (FishFlags)d[r++],
                    sizeStep = d[r++],
                });
            }
            int nh = d[r++];
            for (int i = 0; i < nh; i++)
            {
                hooks.Add(new HookSnap
                {
                    owner = d[r++],
                    pos = new Vector2(Quant.Unpack(RI16(d, ref r)), Quant.Unpack(RI16(d, ref r))),
                    state = (HookState)d[r++],
                });
            }
            int nb = d[r++];
            for (int i = 0; i < nb; i++)
            {
                boats.Add(new BoatSnap
                {
                    owner = d[r++],
                    x = Quant.Unpack(RI16(d, ref r)),
                    tilt = (sbyte)d[r++],
                    sinking = d[r++] != 0,
                });
            }
        }

        static void U8(byte v) => _buf[_w++] = v;
        static void U16(ushort v) { _buf[_w++] = (byte)v; _buf[_w++] = (byte)(v >> 8); }
        static void I16(short v) => U16((ushort)v);
        static ushort RU16(byte[] d, ref int r) { ushort v = (ushort)(d[r] | (d[r + 1] << 8)); r += 2; return v; }
        static short RI16(byte[] d, ref int r) => (short)RU16(d, ref r);
    }
}
