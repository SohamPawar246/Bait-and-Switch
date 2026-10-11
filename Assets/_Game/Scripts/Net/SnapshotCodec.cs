using System;
using UnityEngine;

namespace BaitSwitch.Net
{
    [Flags]
    public enum FishFlags : byte
    {
        None = 0, Dashing = 1, Inked = 2, Nibbling = 4, Hooked = 8, Dead = 16, Leviathan = 32, Glowing = 64, Scattered = 128,
    }

    public enum HookState : byte { Hidden, Sinking, Baited, Empty, Striking, Tugging, ReelingUp }

    /// <summary>One fish as sent to clients. School, players and bots are all the same record.
    /// The fish id is its index in the snapshot (stable for the round, shuffled per round).</summary>
    public struct FishSnap
    {
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
    /// Allocation-free packing into caller-owned buffers (little endian).
    /// Fish: i16 x, i16 y, u8 heading, u8 flags, u8 size        (7 bytes, index implicit)
    /// Hooks: [u8 count] then u8 owner, i16 x, i16 y, u8 state (6 bytes each)
    /// Boats: [u8 count] then u8 owner, i16 x, i8 tilt, u8 sinking (5 bytes each)
    /// </summary>
    public static class SnapshotCodec
    {
        public const int FishBytes = 7, HookBytes = 6, BoatBytes = 5;

        public static int EncodeFish(FishSnap[] src, int first, int count, byte[] dst, int off)
        {
            int w = off;
            for (int i = first; i < first + count; i++)
            {
                ref FishSnap f = ref src[i];
                I16(dst, ref w, Quant.Pack(f.pos.x));
                I16(dst, ref w, Quant.Pack(f.pos.y));
                dst[w++] = (byte)(Mathf.RoundToInt(Mathf.Repeat(f.heading, 360f) / 360f * 256f) & 0xFF);
                dst[w++] = (byte)f.flags;
                dst[w++] = f.sizeStep;
            }
            return w - off;
        }

        /// <summary>Decodes `count` fish into dst[first..]. Returns bytes read.</summary>
        public static int DecodeFish(ArraySegment<byte> src, int off, FishSnap[] dst, int first, int count)
        {
            byte[] d = src.Array;
            int r = src.Offset + off;
            for (int i = first; i < first + count; i++)
            {
                ref FishSnap f = ref dst[i];
                f.pos.x = Quant.Unpack(RI16(d, ref r));
                f.pos.y = Quant.Unpack(RI16(d, ref r));
                f.heading = d[r++] / 256f * 360f;
                f.flags = (FishFlags)d[r++];
                f.sizeStep = d[r++];
            }
            return r - src.Offset - off;
        }

        public static int EncodeHooks(HookSnap[] src, int count, byte[] dst, int off)
        {
            int w = off;
            dst[w++] = (byte)count;
            for (int i = 0; i < count; i++)
            {
                dst[w++] = src[i].owner;
                I16(dst, ref w, Quant.Pack(src[i].pos.x));
                I16(dst, ref w, Quant.Pack(src[i].pos.y));
                dst[w++] = (byte)src[i].state;
            }
            return w - off;
        }

        public static int DecodeHooks(ArraySegment<byte> src, int off, HookSnap[] dst, out int count)
        {
            byte[] d = src.Array;
            int r = src.Offset + off;
            count = d[r++];
            for (int i = 0; i < count; i++)
            {
                dst[i].owner = d[r++];
                dst[i].pos.x = Quant.Unpack(RI16(d, ref r));
                dst[i].pos.y = Quant.Unpack(RI16(d, ref r));
                dst[i].state = (HookState)d[r++];
            }
            return r - src.Offset - off;
        }

        public static int EncodeBoats(BoatSnap[] src, int count, byte[] dst, int off)
        {
            int w = off;
            dst[w++] = (byte)count;
            for (int i = 0; i < count; i++)
            {
                dst[w++] = src[i].owner;
                I16(dst, ref w, Quant.Pack(src[i].x));
                dst[w++] = (byte)(sbyte)Mathf.Clamp(Mathf.RoundToInt(src[i].tilt), -127, 127);
                dst[w++] = src[i].sinking ? (byte)1 : (byte)0;
            }
            return w - off;
        }

        public static int DecodeBoats(ArraySegment<byte> src, int off, BoatSnap[] dst, out int count)
        {
            byte[] d = src.Array;
            int r = src.Offset + off;
            count = d[r++];
            for (int i = 0; i < count; i++)
            {
                dst[i].owner = d[r++];
                dst[i].x = Quant.Unpack(RI16(d, ref r));
                dst[i].tilt = (sbyte)d[r++];
                dst[i].sinking = d[r++] != 0;
            }
            return r - src.Offset - off;
        }

        static void I16(byte[] d, ref int w, short v) { d[w++] = (byte)v; d[w++] = (byte)((ushort)v >> 8); }
        static short RI16(byte[] d, ref int r) { short v = (short)(d[r] | (d[r + 1] << 8)); r += 2; return v; }
    }
}
