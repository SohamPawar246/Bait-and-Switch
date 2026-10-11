using UnityEngine;

namespace BaitSwitch.Sim
{
    /// <summary>Rocks block bodies; kelp slows hooks. Rects come from GameConfig. Shared by server and client.</summary>
    public static class Terrain
    {
        /// <summary>Pushes a circle out of every rock it overlaps and removes velocity into the rock.</summary>
        public static void PushOut(ref Vector2 pos, ref Vector2 vel, float radius, Rect[] rocks)
        {
            if (rocks == null) return;
            for (int i = 0; i < rocks.Length; i++)
            {
                Rect r = rocks[i];
                float minX = r.xMin - radius, maxX = r.xMax + radius, minY = r.yMin - radius, maxY = r.yMax + radius;
                if (pos.x <= minX || pos.x >= maxX || pos.y <= minY || pos.y >= maxY) continue;
                float dl = pos.x - minX, dr = maxX - pos.x, db = pos.y - minY, dt = maxY - pos.y;
                float m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
                if (m == dl) { pos.x = minX; if (vel.x > 0f) vel.x = 0f; }
                else if (m == dr) { pos.x = maxX; if (vel.x < 0f) vel.x = 0f; }
                else if (m == db) { pos.y = minY; if (vel.y > 0f) vel.y = 0f; }
                else { pos.y = maxY; if (vel.y < 0f) vel.y = 0f; }
            }
        }

        public static bool InAny(Vector2 p, Rect[] rects)
        {
            if (rects == null) return false;
            for (int i = 0; i < rects.Length; i++) if (rects[i].Contains(p)) return true;
            return false;
        }
    }
}
