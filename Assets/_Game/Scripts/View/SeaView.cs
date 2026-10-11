using BaitSwitch.Net;
using BaitSwitch.Sim;
using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// Pooled fish visuals: one instance per snapshot index, created once per Welcome.
    /// Every fish is drawn from the interpolated snapshot, except your own (predicted).
    /// Animation (all fish alike, so players stay indistinguishable): smooth turn-arounds through a head-on view,
    /// nose pitch toward travel, a tail beat and body wiggle that speed up with swimming, fin flaps, a gentle bob,
    /// squash on dashes, flailing when hooked, and bubble trails behind fast swimmers.
    /// Once per night every fish turns to face the camera for a breath, then carries on.
    /// Also: growth/Leviathan scale, ink clouds (inked fish are hidden from everyone else), marks and sonar rings.
    /// </summary>
    public sealed class SeaView : MonoBehaviour
    {
        public GameObject fishPrefab;
        public GameObject inkCloudPrefab;
        public GameObject markPrefab;
        public GameObject outlinePrefab;

        [Header("Animation")]
        public float turnSpeed = 620f;        // deg/s through the head-on view
        public float pitchRate = 9f;
        public float maxPitch = 48f;
        public float creepDelay = 9f;          // seconds into the night
        public float creepTime = 0.75f;

        struct Fx { public Transform t; public float until, size; public int fish; public bool follow; }

        Transform[] _pool;
        FishRig[] _rigs;
        Transform _root;
        SeaClient _client;
        LocalPredictor _predictor;
        GameConfig _c;
        Fx[] _marks, _outlines;
        Transform _you;   // local-only ring around your own fish (round start, or hold Tab); never sent to anyone

        void Awake()
        {
            _client = GetComponent<SeaClient>();
            _predictor = GetComponent<LocalPredictor>();
        }

        public void Build(int count)
        {
            if (_pool != null && _pool.Length == count) return;
            Clear();
            _c = GameConfig.I;
            if (!Application.isBatchMode) FxKit.Init();
            _root = new GameObject("Fish").transform;
            _pool = new Transform[count];
            _rigs = new FishRig[count];
            var rng = new System.Random();
            var cat = PartCatalog.I;
            for (int i = 0; i < count; i++)
            {
                _pool[i] = MakeFish(cat != null ? cat.RandomAppearance(rng) : null).transform;
                _rigs[i] = _pool[i].GetComponent<FishRig>();
            }
            bool fancy = ArtLibrary.I != null && ArtLibrary.I.fx != null;
            _marks = fancy ? MakeRings(new Color(1f, 0.78f, 0.3f, 1f), 8) : MakeFx(markPrefab, 8);
            _outlines = fancy ? MakeRings(new Color(0.35f, 0.95f, 1f, 1f), 8) : MakeFx(outlinePrefab, 8);
            if (fancy) { _you = FxKit.Quad("You", _root, FxKit.Ring, new Color(0.55f, 1f, 0.75f, 0.9f), true); _you.gameObject.SetActive(false); }
        }

        GameObject MakeFish(byte[] appearance)
        {
            GameObject go;
            if (PartCatalog.I != null && ArtLibrary.I != null) go = FishAssembler.Build(appearance, _root);
            else go = Instantiate(fishPrefab, _root);
            go.name = "Fish";   // never the index: nothing in the scene should hint who is who
            go.SetActive(false);
            return go;
        }

        /// <summary>Round start: every fish (school and players alike) assembled from the shuffled appearance table.</summary>
        public void Rebuild(byte[] table)
        {
            if (_pool == null || table == null) return;
            int slots = PartCatalog.SlotCount;
            if (table.Length < _pool.Length * slots) return;
            var a = new byte[slots];
            for (int i = 0; i < _pool.Length; i++)
            {
                System.Buffer.BlockCopy(table, i * slots, a, 0, slots);
                bool active = _pool[i].gameObject.activeSelf;
                Destroy(_pool[i].gameObject);
                _pool[i] = MakeFish(a).transform;
                _rigs[i] = _pool[i].GetComponent<FishRig>();
                _pool[i].gameObject.SetActive(active);
            }
        }

        Fx[] MakeFx(GameObject prefab, int n)
        {
            var fx = new Fx[prefab != null ? n : 0];
            for (int i = 0; i < fx.Length; i++)
            {
                var go = Instantiate(prefab, _root);
                go.SetActive(false);
                fx[i] = new Fx { t = go.transform, fish = -1 };
            }
            return fx;
        }

        Fx[] MakeRings(Color c, int n)
        {
            var fx = new Fx[n];
            for (int i = 0; i < n; i++)
            {
                var t = FxKit.Quad("Ring", _root, FxKit.Ring, c, true);
                t.gameObject.SetActive(false);
                fx[i] = new Fx { t = t, fish = -1 };
            }
            return fx;
        }

        public void Clear()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = null;
            _pool = null;
            _rigs = null;
        }

        public void OnEvent(EventMsg m)
        {
            if (_pool == null) return;
            Vector2 p = new Vector2(Quant.Unpack(m.x), Quant.Unpack(m.y));
            switch (m.kind)
            {
                case EventKind.InkCloud:
                    FxKit.Ink(p, _c.inkRadius, _c.inkDuration);
                    break;
                case EventKind.Mark:
                    Spawn(_marks, p, m.a, _c.markDuration, 0.55f);
                    break;
                case EventKind.SonarOutline:
                    Spawn(_outlines, p, m.a, _c.sonarOutline + 0.5f, 1.7f);
                    break;
                case EventKind.Hooked:
                    FxKit.Bubbles(new Vector3(p.x, p.y, 0f), 10, 0.3f, Vector2.zero);
                    FxKit.Sparks(p, new Color(1f, 0.85f, 0.5f), 10, 3f);
                    break;
                case EventKind.Escaped:
                case EventKind.Snapped:
                case EventKind.TornLoose:
                    FxKit.Bubbles(new Vector3(p.x, p.y, 0f), 14, 0.4f, Vector2.zero);
                    break;
                case EventKind.Landed:
                    FxKit.Splash(new Vector2(p.x, _c.surfaceY), 1.4f);
                    break;
                case EventKind.Eaten:
                case EventKind.Nibble:
                    FxKit.Bubbles(new Vector3(p.x, p.y, 0f), 2, 0.1f, Vector2.zero);
                    break;
                case EventKind.Shock:
                    FxKit.Sparks(p, new Color(0.5f, 0.85f, 1f), 18, 4f);
                    break;
                case EventKind.MissScatter:
                    FxKit.Bubbles(new Vector3(p.x, p.y, 0f), 6, 0.6f, Vector2.zero);
                    break;
            }
        }

        void Spawn(Fx[] pool, Vector2 p, int fish, float life, float size)
        {
            if (pool == null || pool.Length == 0) return;
            int pick = 0;
            for (int i = 0; i < pool.Length; i++) if (pool[i].until < pool[pick].until) pick = i;
            pool[pick].until = Time.time + life;
            pool[pick].fish = fish;
            pool[pick].follow = fish >= 0;
            pool[pick].size = size;
            pool[pick].t.localScale = Vector3.one * size;
            pool[pick].t.position = new Vector3(p.x, p.y, -0.5f);
            pool[pick].t.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            if (Mirror.NetworkServer.active && !Mirror.NetworkClient.active) { DrawServerOnly(); return; }
            if (_pool == null || _client == null || !_client.HasData) return;
            int own = _client.OwnIndex;
            bool predicted = _predictor != null && _predictor.Active;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            var match = _client.Match;
            float creepT = (MatchState)match.state == MatchState.Playing && match.phase == (byte)Phase.Night
                ? _client.MatchTime - (_c.nightStart + creepDelay) : -1f;
            bool creep = creepT >= 0f && creepT < creepTime;
            for (int i = 0; i < _pool.Length; i++)
            {
                _client.Sample(i, out Vector2 pos, out float heading, out FishFlags flags);
                bool held = (flags & (FishFlags.Hooked | FishFlags.Dead)) != 0;
                if (i == own && predicted && !held) { pos = _predictor.VisualPos; heading = _predictor.VisualHeading; }

                var t = _pool[i];
                bool inkedOther = (flags & FishFlags.Inked) != 0 && i != own;
                bool show = (flags & FishFlags.Dead) == 0 && !inkedOther;
                if (t.gameObject.activeSelf != show) { t.gameObject.SetActive(show); if (show && _rigs[i] != null) _rigs[i].init = false; }
                if (!show) continue;
                bool lev = (flags & FishFlags.Leviathan) != 0;
                float scale = lev ? _c.leviathanScale : 1f + _c.sizePerGrowth * _client.LatestSize(i);
                Animate(i, pos, heading, (flags & FishFlags.Hooked) != 0, lev, scale, creep, dt);
            }
            UpdateFx(_marks, false);
            UpdateFx(_outlines, true);

            if (_you != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                bool early = (MatchState)match.state == MatchState.Intro || ((MatchState)match.state == MatchState.Playing && _client.MatchTime < 4f);
                bool show = own >= 0 && own < _pool.Length && _pool[own].gameObject.activeSelf && _client.Role == Role.Fish
                            && (early || (kb != null && kb.tabKey.isPressed));
                if (_you.gameObject.activeSelf != show) _you.gameObject.SetActive(show);
                if (show)
                {
                    Vector3 p = _pool[own].position;
                    _you.position = new Vector3(p.x, p.y, -0.45f);
                    _you.localScale = Vector3.one * (1.5f + Mathf.Sin(Time.time * 5f) * 0.12f) * _pool[own].localScale.y;
                }
            }
        }

        void Animate(int i, Vector2 pos, float heading, bool hooked, bool lev, float scale, bool creep, float dt)
        {
            var t = _pool[i];
            var r = _rigs[i];
            if (r == null)
            {
                float hh = Mathf.DeltaAngle(0f, heading);
                t.SetPositionAndRotation(new Vector3(pos.x, pos.y, 0f), Mathf.Abs(hh) <= 90f ? Quaternion.Euler(0f, 0f, hh) : Quaternion.Euler(0f, 180f, 180f - hh));
                t.localScale = Vector3.one * scale;
                return;
            }
            float h = Mathf.DeltaAngle(0f, heading), ah = Mathf.Abs(h);
            if (!r.init) { r.init = true; r.last = pos; r.right = ah <= 90f; r.yaw = r.right ? 0f : 180f; r.speed = 0f; r.phase = i * 1.37f; }
            float sp = (pos - r.last).magnitude / Mathf.Max(1e-4f, dt);
            r.last = pos;
            r.speed = Mathf.Lerp(r.speed, Mathf.Min(sp, 20f), 1f - Mathf.Exp(-8f * dt));

            // facing: hysteresis so a fish swimming straight up doesn't flicker, then a real turn through the camera
            if (r.right && ah > 100f) r.right = false; else if (!r.right && ah < 80f) r.right = true;
            float yawT = creep && !hooked ? 90f : (r.right ? 0f : 180f);
            r.yaw = Mathf.MoveTowards(r.yaw, yawT, turnSpeed * (creep ? 1.6f : 1f) * dt);
            float pitchT = r.right ? h : Mathf.Sign(h) * (180f - ah);
            pitchT = Mathf.Clamp(pitchT, -maxPitch, maxPitch) * Mathf.Clamp01(0.35f + r.speed * 0.4f);
            if (creep && !hooked) pitchT = 0f;
            float time = Time.time;
            if (hooked) pitchT = Mathf.Sin(time * 13f + i) * 38f;
            r.pitch = Mathf.Lerp(r.pitch, pitchT, 1f - Mathf.Exp(-pitchRate * dt));

            // swim cycle
            float speed01 = Mathf.Clamp01(r.speed / 6f);
            float rate = hooked ? 24f : (4.5f + speed01 * 10f) * (lev ? 0.55f : 1f);
            if (creep) rate *= 0.2f;
            r.phase += rate * dt;
            float tailAmp = hooked ? 46f : 12f + speed01 * 26f;
            float s = Mathf.Sin(r.phase);
            if (r.tail != null) r.tail.localRotation = Quaternion.AngleAxis(s * tailAmp, r.tailAxis) * r.tailBase;
            float fin = Mathf.Sin(r.phase * 0.55f + 0.8f) * (hooked ? 42f : 14f + speed01 * 12f);
            if (r.finL != null) r.finL.localRotation = Quaternion.AngleAxis(fin, r.finAxisL) * r.finLBase;
            if (r.finR != null) r.finR.localRotation = Quaternion.AngleAxis(-fin, r.finAxisR) * r.finRBase;
            r.model.localRotation = r.modelBase * Quaternion.Euler(0f, Mathf.Sin(r.phase + 1.4f) * tailAmp * 0.22f, 0f);
            r.model.localPosition = r.modelPos + new Vector3(0f, Mathf.Sin(r.phase * 0.5f) * 0.03f, 0f);

            float stretch = Mathf.Clamp01((r.speed - 4f) / 6f) * 0.16f;
            t.SetPositionAndRotation(new Vector3(pos.x, pos.y, 0f), Quaternion.Euler(0f, r.yaw, 0f) * Quaternion.Euler(0f, 0f, r.pitch));
            t.localScale = new Vector3(scale * (1f + stretch), scale * (1f - stretch * 0.45f), scale * (1f - stretch * 0.45f));

            // bubble trail: fast swimmers (dashes) and struggling fish give themselves away
            r.bubbleT -= dt;
            if (r.bubbleT <= 0f && (r.speed > _c.dashSpeed * 0.55f || hooked || (lev && r.speed > 1f)))
            {
                r.bubbleT = hooked ? 0.18f : lev ? 0.12f : 0.045f;
                Vector3 back = t.rotation * Vector3.left * (0.45f * scale);
                FxKit.Bubbles(t.position + back, lev ? 2 : 1, 0.12f * scale, Vector2.zero);
            }
        }

        void UpdateFx(Fx[] pool, bool pulse)
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                ref Fx f = ref pool[i];
                if (!f.t.gameObject.activeSelf) continue;
                if (Time.time > f.until) { f.t.gameObject.SetActive(false); continue; }
                if (f.follow && f.fish >= 0 && f.fish < _pool.Length && _pool[f.fish].gameObject.activeSelf)
                {
                    Vector3 p = _pool[f.fish].position;
                    f.t.position = new Vector3(p.x, p.y + (pulse ? 0f : 0.75f + Mathf.Sin(Time.time * 4f) * 0.08f), -0.5f);
                }
                float k = pulse ? 1f + Mathf.Sin(Time.time * 9f) * 0.12f : 1f;
                float fadeIn = Mathf.Clamp01((f.until - Time.time) * 3f);
                f.t.localScale = Vector3.one * (f.size * k * (0.6f + 0.4f * fadeIn));
                if (!pulse) f.t.rotation = Quaternion.Euler(0f, 0f, Time.time * 90f);
            }
        }

        /// <summary>Debug view for a server-only editor: draw the sim directly (never in a headless build).</summary>
        void DrawServerOnly()
        {
            if (Application.isBatchMode) return;
            var server = GetComponent<SeaServer>();
            var sim = server != null ? server.Sim : null;
            if (sim == null) return;
            if (_pool == null || _pool.Length != sim.Count) Build(sim.Count);
            if (_pool == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            for (int i = 0; i < _pool.Length; i++)
            {
                var t = _pool[i];
                var f = sim.Fish[i];
                bool show = (f.flags & (byte)FishFlags.Dead) == 0;
                if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
                if (!show) continue;
                Animate(i, f.pos, f.heading, (f.flags & (byte)FishFlags.Hooked) != 0, (f.flags & (byte)FishFlags.Leviathan) != 0, FishMotor.Scale(f, _c), false, dt);
            }
        }
    }
}
