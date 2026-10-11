using BaitSwitch.Net;
using UnityEngine;

namespace BaitSwitch.Sim
{
    /// <summary>One fish body in the sim. Plain struct: no physics, no NetworkTransform.</summary>
    public struct FishBody
    {
        public Vector2 pos, vel;
        public float heading; // degrees, 0 = +X
        public byte flags, size;
        public float dashT, dashCd;
        public bool dashHeld;
    }

    /// <summary>
    /// Movement shared by the server sim and the client's own-fish prediction, so replaying the
    /// same inputs gives the same result on both sides.
    /// </summary>
    public static class FishMotor
    {
        /// <summary>Player swim: momentum from a -1..1 stick, Space dash, then integrate.</summary>
        public static void Step(ref FishBody b, Vector2 move, Buttons buttons, float dt, GameConfig c)
        {
            if (move.sqrMagnitude > 1f) move.Normalize();
            bool dash = (buttons & Buttons.Dash) != 0;
            if (b.dashCd > 0f) b.dashCd -= dt;
            if (dash && !b.dashHeld && b.dashCd <= 0f && (b.flags & (byte)FishFlags.Leviathan) == 0)
            {
                Vector2 dir = move.sqrMagnitude > 0.01f ? move.normalized : Dir(b.heading);
                b.vel = dir * c.dashSpeed;
                b.dashT = c.dashDuration;
                b.dashCd = c.dashCooldown;
            }
            b.dashHeld = dash;

            if (b.dashT > 0f)
            {
                b.dashT -= dt;
                b.flags |= (byte)FishFlags.Dashing;
            }
            else
            {
                b.flags &= unchecked((byte)~FishFlags.Dashing);
                b.vel += move * (c.fishAccel * dt);
                b.vel -= b.vel * Mathf.Min(1f, c.waterDrag * dt);
                float max = MaxSpeed(b, c);
                if (b.vel.sqrMagnitude > max * max) b.vel = b.vel.normalized * max;
            }
            Integrate(ref b, dt, c);
        }

        /// <summary>Move by velocity, turn the heading toward it at the shared turn rate, keep in the sea.</summary>
        public static void Integrate(ref FishBody b, float dt, GameConfig c)
        {
            b.pos += b.vel * dt;
            if (b.vel.sqrMagnitude > 0.01f)
            {
                float target = Mathf.Atan2(b.vel.y, b.vel.x) * Mathf.Rad2Deg;
                b.heading = Mathf.MoveTowardsAngle(b.heading, target, c.fishTurnRate * dt);
            }
            Clamp(ref b, c);
        }

        public static float MaxSpeed(in FishBody b, GameConfig c) =>
            c.fishMaxSpeed * Mathf.Max(0.2f, 1f + c.speedPerGrowth * b.size);

        public static float Scale(in FishBody b, GameConfig c) =>
            (b.flags & (byte)FishFlags.Leviathan) != 0 ? c.leviathanScale : 1f + c.sizePerGrowth * b.size;

        public static void Clamp(ref FishBody b, GameConfig c)
        {
            float r = c.fishHitRadius * Scale(b, c);
            float hx = c.seaSize.x * 0.5f - r;
            float minY = c.seabedY + r, maxY = c.surfaceY - r;
            if (b.pos.x < -hx) { b.pos.x = -hx; if (b.vel.x < 0f) b.vel.x = 0f; }
            else if (b.pos.x > hx) { b.pos.x = hx; if (b.vel.x > 0f) b.vel.x = 0f; }
            if (b.pos.y < minY) { b.pos.y = minY; if (b.vel.y < 0f) b.vel.y = 0f; }
            else if (b.pos.y > maxY) { b.pos.y = maxY; if (b.vel.y > 0f) b.vel.y = 0f; }
            Terrain.PushOut(ref b.pos, ref b.vel, c.fishHitRadius, c.rocks);
        }

        public static Vector2 Dir(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }
    }
}
