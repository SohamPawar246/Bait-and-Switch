using BaitSwitch.Net;
using BaitSwitch.Sim;
using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// Client: one pooled dinghy + hook + sagging verlet line per seat. Interpolated from snapshots,
    /// except your own boat and hook (predicted). Nibble events twitch the line for everyone.
    /// </summary>
    public sealed class HookView : MonoBehaviour
    {
        public GameObject boatPrefab;
        public GameObject hookPrefab;
        public Material lineMaterial;
        public int linePoints = 14;
        public float lineWidth = 0.035f;
        public float lineSlack = 1.08f;
        public float lineGravity = 4f;
        public float twitchImpulse = 0.35f;
        public float twitchTime = 0.15f;

        struct Seat
        {
            public Transform boat, hook, bait, glow;
            public float prevY, wakeT, lastBoatX, angle;
            public LineRenderer line;
            public Vector3[] pts, prev;
            public float twitchT;
            public bool on;
        }

        Seat[] _seats;
        static readonly Color[] CoatColors =
        {
            new Color(0.95f, 0.76f, 0.31f), new Color(0.84f, 0.27f, 0.27f), new Color(0.72f, 0.61f, 0.93f),
            new Color(0.25f, 0.65f, 0.64f), new Color(0.96f, 0.91f, 0.85f), new Color(0.72f, 0.85f, 0.42f),
        };
        MaterialPropertyBlock _mpb;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        Transform _root;
        SeaClient _client;
        LocalPredictor _predictor;
        GameConfig _c;

        void Awake()
        {
            _client = GetComponent<SeaClient>();
            _predictor = GetComponent<LocalPredictor>();
        }

        public void Build(int seats)
        {
            Clear();
            _c = GameConfig.I;
            if (!Application.isBatchMode) FxKit.Init();
            bool kit = ArtLibrary.I != null && ArtLibrary.I.boat != null;
            if (!kit && (boatPrefab == null || hookPrefab == null)) { Debug.LogError("[HookView] prefabs not set. Run Tools > Bait & Switch > Build Greybox."); return; }
            _root = new GameObject("Anglers").transform;
            _seats = new Seat[seats];
            for (int i = 0; i < seats; i++)
            {
                Transform boat, hook, baitT = null;
                if (kit)
                {
                    var coat = CoatColors[i % CoatColors.Length];
                    boat = KitBuilder.Boat(_root, i, coat, ToonLibrary.Hex("#E8E2D2")).transform;
                    hook = KitBuilder.Hook(_root, i, i, out var baitGo).transform;
                    if (baitGo != null) baitT = baitGo.transform;
                }
                else
                {
                    boat = Instantiate(boatPrefab, _root).transform;
                    hook = Instantiate(hookPrefab, _root).transform;
                    baitT = hook.Find("Bait");
                }
                Transform glow = null;
                if (ArtLibrary.I != null && ArtLibrary.I.fx != null)
                {
                    // the lure glows: it reads at depth and looks like something worth biting
                    glow = FxKit.Quad("LureGlow", hook, FxKit.Soft, new Color(1f, 0.8f, 0.45f, 0.75f), true);
                    glow.localPosition = new Vector3(0f, -0.24f, 0.06f);
                    glow.localScale = Vector3.one * 0.9f;
                }
                var lineGo = new GameObject("Line");
                lineGo.transform.SetParent(_root, false);
                var line = lineGo.AddComponent<LineRenderer>();
                line.sharedMaterial = lineMaterial;
                line.positionCount = linePoints;
                line.widthMultiplier = lineWidth;
                line.useWorldSpace = true;
                _seats[i] = new Seat
                {
                    boat = boat, hook = hook, bait = baitT, glow = glow, line = line,
                    pts = new Vector3[linePoints], prev = new Vector3[linePoints],
                };
                SetVisible(ref _seats[i], false);
            }
        }

        public void Clear()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = null;
            _seats = null;
        }

        public void OnEvent(EventMsg m)
        {
            if (_seats == null || m.a >= _seats.Length) return;
            if (m.kind == EventKind.Nibble) Twitch(m.a);
            else if (m.kind == EventKind.Snapped || m.kind == EventKind.Tangled) { Twitch(m.a); Twitch(m.a); }
        }

        void Twitch(int h)
        {
            ref Seat s = ref _seats[h];
            s.twitchT = twitchTime;
            for (int i = 2; i < s.pts.Length - 1; i++)
            {
                float side = (i & 1) == 0 ? 1f : -1f;
                s.prev[i] -= new Vector3(side * twitchImpulse, twitchImpulse * 0.5f, 0f) * 0.1f;
            }
        }

        void LateUpdate()
        {
            // Server-only editor: draw straight from the sim (debug; never in a headless build).
            var server = Mirror.NetworkServer.active && !Mirror.NetworkClient.active && !Application.isBatchMode
                ? GetComponent<SeaServer>() : null;
            var hooks = server != null ? server.Hooks : null;
            if (hooks != null && (_seats == null || _seats.Length != hooks.Hooks.Length)) Build(hooks.Hooks.Length);
            if (hooks == null && (_seats == null || _client == null || !_client.HasData)) return;
            if (_seats == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            for (int h = 0; h < _seats.Length; h++)
            {
                ref Seat s = ref _seats[h];
                bool on;
                Vector2 hookPos;
                HookState state;
                float boatX;
                if (hooks != null)
                {
                    var k = hooks.Hooks[h];
                    on = k.active; hookPos = k.pos; boatX = k.boatX; state = hooks.StateOf(k);
                }
                else on = _client.SampleHook(h, out hookPos, out state, out boatX);
                if (hooks == null && on && _predictor != null && _predictor.HookActive && _predictor.BoatId == h)
                {
                    hookPos = _predictor.VisualHookPos;
                    boatX = _predictor.VisualBoatX;
                }
                if (on != s.on) { SetVisible(ref s, on); if (on) ResetLine(ref s, boatX, hookPos); }
                if (!on) continue;

                Vector2 tip = HookPhysics.RodTip(boatX, _c);
                if (s.twitchT > 0f)
                {
                    s.twitchT -= dt;
                    hookPos += new Vector2(Mathf.Sin(Time.time * 90f) * 0.05f, 0f);
                }
                float bob = Mathf.Sin(Time.time * 1.4f + h * 1.7f);
                s.boat.SetPositionAndRotation(new Vector3(boatX, _c.surfaceY + bob * 0.035f, 0f),
                                              Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.1f + h) * 2.5f));
                s.hook.position = new Vector3(hookPos.x, hookPos.y, 0f);
                bool baited = state == HookState.Baited || state == HookState.Striking;
                if (s.bait != null && s.bait.gameObject.activeSelf != baited) s.bait.gameObject.SetActive(baited);
                if (s.glow != null)
                {
                    bool lit = baited && hookPos.y < _c.surfaceY;
                    if (s.glow.gameObject.activeSelf != lit) s.glow.gameObject.SetActive(lit);
                    if (lit) s.glow.localScale = Vector3.one * (0.85f + 0.15f * Mathf.Sin(Time.time * 3.1f + h));
                }
                StepLine(ref s, tip, hookPos, dt);

                // hang the hook along the last stretch of line, with a lazy swing
                int n = s.pts.Length;
                Vector3 dir = s.pts[n - 2] - s.pts[n - 1];
                float ang = Mathf.Atan2(dir.x, Mathf.Max(0.05f, dir.y)) * Mathf.Rad2Deg;
                s.angle = Mathf.LerpAngle(s.angle, -ang, 1f - Mathf.Exp(-10f * dt));
                s.hook.rotation = Quaternion.Euler(0f, 0f, s.angle + Mathf.Sin(Time.time * 2.1f + h) * 5f);

                // splashes where the hook crosses the surface, a wake behind a rowing boat
                float sy = _c.surfaceY;
                if (s.prevY > sy && hookPos.y <= sy) { FxKit.Splash(new Vector2(hookPos.x, sy), 0.7f); Audio.AudioDirector.At("splash_small", hookPos, 0.8f); }
                else if (s.prevY < sy - 0.05f && hookPos.y >= sy) { FxKit.Splash(new Vector2(hookPos.x, sy), 0.4f); Audio.AudioDirector.At("splash_small", hookPos, 0.5f); }
                s.prevY = hookPos.y;
                float bv = (boatX - s.lastBoatX) / Mathf.Max(1e-4f, dt);
                s.lastBoatX = boatX;
                s.wakeT -= dt;
                if (Mathf.Abs(bv) > 0.8f && s.wakeT <= 0f) { s.wakeT = 0.07f; FxKit.Wake(new Vector2(boatX - Mathf.Sign(bv) * 0.95f, sy), -Mathf.Sign(bv)); }
                Color col = new Color(0.86f, 0.9f, 0.94f);
                bool mine = _predictor != null && _predictor.BoatId == h;
                if (mine && _client.InTug)
                {
                    var tug = _client.Tug;
                    float k = Mathf.Clamp01(tug.tension / Mathf.Max(1f, (float)tug.threshold));
                    col = k < 0.5f ? Color.Lerp(Color.white, Color.yellow, k * 2f) : Color.Lerp(Color.yellow, Color.red, (k - 0.5f) * 2f);
                }
                else if (state == HookState.Tugging) col = new Color(1f, 0.85f, 0.4f);
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                _mpb.SetColor(BaseColor, col);
                s.line.SetPropertyBlock(_mpb);
            }
        }

        void StepLine(ref Seat s, Vector2 tip, Vector2 hook, float dt)
        {
            int n = s.pts.Length;
            s.pts[0] = tip; s.pts[n - 1] = hook;
            Vector3 g = new Vector3(0f, -lineGravity * dt * dt, 0f);
            for (int i = 1; i < n - 1; i++)
            {
                Vector3 p = s.pts[i];
                s.pts[i] += (p - s.prev[i]) * 0.96f + g;
                s.prev[i] = p;
            }
            float seg = Vector2.Distance(tip, hook) * lineSlack / (n - 1);
            for (int iter = 0; iter < 6; iter++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 d = s.pts[i + 1] - s.pts[i];
                    float len = d.magnitude;
                    if (len < 1e-5f) continue;
                    Vector3 corr = d * ((len - seg) / len * 0.5f);
                    if (i != 0) s.pts[i] += corr;
                    if (i + 1 != n - 1) s.pts[i + 1] -= corr;
                }
            }
            s.line.SetPositions(s.pts);
        }

        void ResetLine(ref Seat s, float boatX, Vector2 hook)
        {
            s.prevY = hook.y;
            s.lastBoatX = boatX;
            Vector2 tip = HookPhysics.RodTip(boatX, _c);
            for (int i = 0; i < s.pts.Length; i++)
            {
                s.pts[i] = Vector2.Lerp(tip, hook, i / (float)(s.pts.Length - 1));
                s.prev[i] = s.pts[i];
            }
        }

        static void SetVisible(ref Seat s, bool on)
        {
            s.on = on;
            s.boat.gameObject.SetActive(on);
            s.hook.gameObject.SetActive(on);
            s.line.gameObject.SetActive(on);
        }
    }
}
