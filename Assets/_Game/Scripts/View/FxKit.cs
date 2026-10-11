using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// Client-side juice, all procedural (no texture assets): bubbles, splashes, ink clouds, glow and ring quads.
    /// One pooled ParticleSystem per effect; everything is emitted with Emit() so nothing allocates per frame.
    /// </summary>
    public static class FxKit
    {
        static Texture2D _soft, _ring, _bubble, _puff;
        static ParticleSystem _bubbles, _splash, _ink, _sparks;
        static Transform _root;

        public static bool Ready => _root != null;

        // ------------------------------------------------------------------ textures + materials

        public static Texture2D Soft => _soft != null ? _soft : (_soft = MakeTex(64, (x, y) => { float r = Mathf.Sqrt(x * x + y * y); float a = Mathf.Clamp01(1 - r); return a * a; }));
        public static Texture2D Ring => _ring != null ? _ring : (_ring = MakeTex(128, (x, y) => { float r = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01(1 - Mathf.Abs(r - 0.82f) / 0.07f) + Mathf.Clamp01(1 - r) * 0.12f; }));
        public static Texture2D Bubble => _bubble != null ? _bubble : (_bubble = MakeTex(64, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float rim = Mathf.Clamp01(1 - Mathf.Abs(r - 0.82f) / 0.16f);
            float hl = Mathf.Clamp01(1 - Vector2.Distance(new Vector2(x, y), new Vector2(-0.32f, 0.34f)) / 0.22f);
            return r > 1 ? 0 : Mathf.Max(rim * 0.85f + 0.12f, hl);
        }));
        public static Texture2D Puff => _puff != null ? _puff : (_puff = MakeTex(64, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float n = Mathf.PerlinNoise(x * 2.3f + 5f, y * 2.3f + 9f) * 0.6f + Mathf.PerlinNoise(x * 5f + 1f, y * 5f + 3f) * 0.4f;
            float a = Mathf.Clamp01(1 - r) * (0.55f + 0.7f * n);
            return Mathf.Clamp01(a * 1.3f);
        }));

        static Texture2D MakeTex(int size, System.Func<float, float, float> alpha)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float x = (i + 0.5f) / size * 2 - 1, y = (j + 0.5f) / size * 2 - 1;
                    px[j * size + i] = new Color(1, 1, 1, Mathf.Clamp01(alpha(x, y)));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        /// <summary>An FX material: additive glows, alpha-blended bubbles/ink.</summary>
        public static Material Mat(Texture tex, Color tint, bool additive)
        {
            var lib = ArtLibrary.I;
            var m = lib != null && lib.fx != null ? new Material(lib.fx) : new Material(Shader.Find("Sprites/Default"));
            m.mainTexture = tex;
            if (m.HasProperty("_Tint")) m.SetColor("_Tint", tint); else m.color = tint;
            if (m.HasProperty("_SrcBlend"))
            {
                m.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            m.renderQueue = 3000;
            return m;
        }

        /// <summary>A camera-facing quad (the camera always looks down +Z) with an FX material.</summary>
        public static Transform Quad(string name, Transform parent, Texture tex, Color tint, bool additive)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Mat(tex, tint, additive);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        // ------------------------------------------------------------------ particle systems

        public static void Init()
        {
            if (_root != null) return;
            _root = new GameObject("Fx").transform;
            Object.DontDestroyOnLoad(_root.gameObject);

            _bubbles = System("Bubbles", Mat(Bubble, new Color(0.85f, 0.97f, 1f, 0.85f), false), 700);
            var m = _bubbles.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            m.gravityModifier = -0.09f;
            var n = _bubbles.noise; n.enabled = true; n.strength = 0.35f; n.frequency = 1.3f; n.scrollSpeed = 0.5f;
            Fade(_bubbles, 0.1f, 0.8f);

            _splash = System("Splash", Mat(Soft, new Color(0.92f, 0.97f, 0.95f, 0.9f), false), 400);
            m = _splash.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            m.gravityModifier = 0.85f;
            Fade(_splash, 0.05f, 0.7f);

            _ink = System("Ink", Mat(Puff, new Color(0.09f, 0.05f, 0.14f, 0.92f), false), 300);
            m = _ink.main;
            m.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var lv = _ink.limitVelocityOverLifetime; lv.enabled = true; lv.dampen = 0.12f; lv.limit = 0.2f;
            var sz = _ink.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.55f, 1f, 1.5f));
            var rot = _ink.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            Fade(_ink, 0.08f, 0.75f);

            _sparks = System("Sparks", Mat(Soft, Color.white, true), 300);
            m = _sparks.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            var lv2 = _sparks.limitVelocityOverLifetime; lv2.enabled = true; lv2.dampen = 0.15f; lv2.limit = 0.5f;
            Fade(_sparks, 0.02f, 0.5f);
        }

        static ParticleSystem System(string name, Material mat, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startSpeed = 0f;
            main.playOnAwake = false;
            main.loop = true;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            var sh = ps.shape; sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortMode = ParticleSystemSortMode.None;
            ps.Play();
            return ps;
        }

        static void Fade(ParticleSystem ps, float inAt, float outAt)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, inAt), new GradientAlphaKey(1f, outAt), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        static ParticleSystem.EmitParams _ep;

        // ------------------------------------------------------------------ emitters

        public static void Bubbles(Vector3 pos, int count, float spread, Vector2 drift)
        {
            if (_bubbles == null) return;
            for (int i = 0; i < count; i++)
            {
                _ep = default;
                _ep.position = pos + (Vector3)(Random.insideUnitCircle * spread) + new Vector3(0f, 0f, -0.2f);
                _ep.velocity = new Vector3(drift.x + Random.Range(-0.25f, 0.25f), drift.y + Random.Range(0.2f, 0.7f), 0f);
                _ep.applyShapeToPosition = false;
                _bubbles.Emit(_ep, 1);
            }
        }

        public static void Splash(Vector2 pos, float size)
        {
            if (_splash == null) return;
            int n = Mathf.RoundToInt(10 + 14 * size);
            for (int i = 0; i < n; i++)
            {
                _ep = default;
                _ep.position = new Vector3(pos.x + Random.Range(-0.25f, 0.25f) * size, pos.y + 0.05f, -0.3f);
                float a = Random.Range(55f, 125f) * Mathf.Deg2Rad;
                float s = Random.Range(2.2f, 4.8f) * Mathf.Sqrt(size);
                _ep.velocity = new Vector3(Mathf.Cos(a) * s, Mathf.Sin(a) * s, 0f);
                _ep.startSize = Random.Range(0.07f, 0.2f) * Mathf.Sqrt(size);
                _splash.Emit(_ep, 1);
            }
            Bubbles(new Vector3(pos.x, pos.y - 0.4f, 0f), Mathf.RoundToInt(6 * size), 0.35f * size, Vector2.zero);
        }

        public static void Wake(Vector2 pos, float back)
        {
            if (_splash == null) return;
            for (int i = 0; i < 2; i++)
            {
                _ep = default;
                _ep.position = new Vector3(pos.x + Random.Range(-0.15f, 0.15f), pos.y + 0.03f, -0.3f);
                _ep.velocity = new Vector3(back * Random.Range(0.4f, 1.2f), Random.Range(0.6f, 1.6f), 0f);
                _ep.startSize = Random.Range(0.05f, 0.12f);
                _splash.Emit(_ep, 1);
            }
        }

        public static void Ink(Vector2 pos, float radius, float life)
        {
            if (_ink == null) return;
            for (int i = 0; i < 30; i++)
            {
                _ep = default;
                Vector2 d = Random.insideUnitCircle;
                _ep.position = new Vector3(pos.x + d.x * radius * 0.25f, pos.y + d.y * radius * 0.25f, -0.6f - Random.value * 0.3f);
                _ep.velocity = (Vector3)(d.normalized * Random.Range(0.6f, 1.6f) * radius * 0.6f);
                _ep.startLifetime = life * Random.Range(0.85f, 1.1f);
                _ep.startSize = radius * Random.Range(0.45f, 0.8f);
                _ink.Emit(_ep, 1);
            }
        }

        public static void Sparks(Vector2 pos, Color color, int count, float speed)
        {
            if (_sparks == null) return;
            for (int i = 0; i < count; i++)
            {
                _ep = default;
                _ep.position = new Vector3(pos.x, pos.y, -0.4f);
                _ep.velocity = (Vector3)(Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * speed);
                _ep.startColor = color;
                _sparks.Emit(_ep, 1);
            }
        }
    }
}
