using UnityEngine;

namespace BaitSwitch.UI
{
    /// <summary>
    /// Hardware cursors drawn in code (zero lag, exact hotspots): a small fish hook for menus whose click point is the
    /// top of the shank, like an arrow tip, and an aim ring for Anglers / mouse-swimming fish with the hotspot dead centre.
    /// Cream with an ink outline so they read on the pale parchment and the dark sea alike.
    /// </summary>
    public static class CursorKit
    {
        public enum Mode { System, Hidden, Hook, RingGold, RingMint }

        const int Size = 32;
        static Texture2D _hook, _gold, _mint;
        static Mode _current = (Mode)(-1);

        static readonly Color Ink = new Color(0.12f, 0.08f, 0.16f, 1f);
        static readonly Color Cream = new Color(0.96f, 0.91f, 0.85f, 1f);

        public static void Set(Mode mode)
        {
            if (mode == _current) return;
            _current = mode;
            switch (mode)
            {
                case Mode.System: Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); Cursor.visible = true; break;
                case Mode.Hidden: Cursor.visible = false; break;
                case Mode.Hook: Cursor.SetCursor(Hook(), new Vector2(7, 1), CursorMode.Auto); Cursor.visible = true; break;
                case Mode.RingGold: Cursor.SetCursor(Ring(ref _gold, new Color(0.95f, 0.76f, 0.31f)), new Vector2(Size / 2, Size / 2), CursorMode.Auto); Cursor.visible = true; break;
                case Mode.RingMint: Cursor.SetCursor(Ring(ref _mint, new Color(0.6f, 0.96f, 0.77f)), new Vector2(Size / 2, Size / 2), CursorMode.Auto); Cursor.visible = true; break;
            }
        }

        // distance from p to segment ab
        static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>Signed-ish distance field of the hook, in top-left pixel coordinates.</summary>
        static float HookDist(Vector2 p)
        {
            float d = Seg(p, new Vector2(7, 3), new Vector2(7, 19));                       // shank
            // the bend: lower half circle centred (13, 19), radius 6
            Vector2 c = new Vector2(13, 19);
            Vector2 q = p - c;
            float arc = Mathf.Abs(q.magnitude - 6f);
            if (q.y < 0f) arc = Mathf.Min(Vector2.Distance(p, new Vector2(7, 19)), Vector2.Distance(p, new Vector2(19, 19)));
            d = Mathf.Min(d, arc);
            d = Mathf.Min(d, Seg(p, new Vector2(19, 19), new Vector2(19, 13)));            // up to the point
            d = Mathf.Min(d, Seg(p, new Vector2(19, 13), new Vector2(16, 16)));            // barb
            d = Mathf.Min(d, Mathf.Abs(Vector2.Distance(p, new Vector2(7, 2.5f)) - 1.6f)); // the eye
            return d;
        }

        static Texture2D Hook()
        {
            if (_hook != null) return _hook;
            _hook = Draw(p =>
            {
                float d = HookDist(p);
                return (d < 1.25f ? 1f : 0f, d < 2.4f ? 1f : 0f);   // (fill, outline)
            }, Cream);
            return _hook;
        }

        static Texture2D Ring(ref Texture2D cache, Color fill)
        {
            if (cache != null) return cache;
            cache = Draw(p =>
            {
                Vector2 c = new Vector2(Size / 2f, Size / 2f);
                float r = Vector2.Distance(p, c);
                float ring = Mathf.Abs(r - 10.5f);
                float dot = r;
                float tick = Mathf.Min(Mathf.Min(Seg(p, c + new Vector2(0, -15), c + new Vector2(0, -12)), Seg(p, c + new Vector2(0, 12), c + new Vector2(0, 15))),
                                       Mathf.Min(Seg(p, c + new Vector2(-15, 0), c + new Vector2(-12, 0)), Seg(p, c + new Vector2(12, 0), c + new Vector2(15, 0))));
                float d = Mathf.Min(Mathf.Min(ring, tick), dot - 0.6f);
                return (d < 1.2f ? 1f : 0f, d < 2.3f ? 1f : 0f);
            }, fill);
            return cache;
        }

        /// <summary>4x supersampled: fill colour inside, ink outline around it, transparent elsewhere.</summary>
        static Texture2D Draw(System.Func<Vector2, (float fill, float line)> shape, Color fill)
        {
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float f = 0f, l = 0f;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                        {
                            var s = shape(new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f));
                            f += s.fill; l += s.line;
                        }
                    f /= 16f; l /= 16f;
                    Color c = Color.Lerp(Ink, fill, f / Mathf.Max(1e-4f, l));
                    c.a = l;
                    px[(Size - 1 - y) * Size + x] = c;   // texture rows run bottom-up; our coordinates are top-down
                }
            t.SetPixels(px);
            t.Apply(false, false);
            return t;
        }
    }
}
