using BaitSwitch.Net;
using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// Scale-the-Depths framing: a perspective camera that follows what you control instead of showing the whole sea.
    /// Fish: close on your own fish with velocity look-ahead. Angler: rides the hook down (pulled toward the mouse so
    /// you can scan the school), or the boat when the line is up. Tugs zoom in; snaps, bites and Leviathans shake it.
    /// The perspective gives real parallax to the layered rocks, kelp and far silhouettes built by GreyboxBuilder.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class GameCamera : MonoBehaviour
    {
        public static GameCamera I { get; private set; }

        [Header("Framing (visible height in world units)")]
        public float fov = 30f;
        public float fishView = 11.5f;
        public float anglerView = 14f;
        public float tugView = 9.5f;
        public float spectatorView = 19f;
        public float leviathanView = 15f;
        [Header("Follow")]
        public float smoothTime = 0.35f;
        public float zoomRate = 2.2f;
        public float lookAhead = 0.35f;     // seconds of velocity to lead by
        public float mouseLead = 2.2f;      // angler: how far the mouse drags the view (world units at the edge)
        public float skyMargin = 2.6f;      // how much sky may show above the surface
        public float bedMargin = 0.9f;
        [Header("Shake")]
        public float shakeDecay = 2.6f;
        public float shakeFreq = 18f;

        Camera _cam;
        SeaClient _client;
        LocalPredictor _predictor;
        Vector2 _pos, _vel, _lastTarget;
        float _view;
        float _shake;
        bool _init;

        void Awake()
        {
            I = this;
            _cam = GetComponent<Camera>();
            _cam.orthographic = false;
            _cam.fieldOfView = fov;
        }

        void OnEnable() => _init = false;

        public static void Shake(float amount)
        {
            if (I != null) I._shake = Mathf.Min(1.2f, Mathf.Max(I._shake, amount));
        }

        /// <summary>Screen point → the gameplay plane (z = 0). Works for perspective and orthographic cameras.</summary>
        public static Vector2 MouseWorld(Vector2 screen)
        {
            var cam = I != null && I.isActiveAndEnabled ? I._cam : Camera.main;
            if (cam == null) return Vector2.zero;
            var ray = cam.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.z) < 1e-5f) return new Vector2(ray.origin.x, ray.origin.y);
            float t = -ray.origin.z / ray.direction.z;
            Vector3 p = ray.origin + ray.direction * t;
            return new Vector2(p.x, p.y);
        }

        /// <summary>World point → 0..1 viewport (for audio panning and off-screen attenuation).</summary>
        public static Vector3 Viewport(Vector2 world)
        {
            var cam = I != null && I.isActiveAndEnabled ? I._cam : null;
            return cam != null ? cam.WorldToViewportPoint(new Vector3(world.x, world.y, 0f)) : new Vector3(0.5f, 0.5f, 1f);
        }

        void LateUpdate()
        {
            var c = GameConfig.I;
            if (c == null) return;
            if (_client == null)
            {
                var nm = FindFirstObjectByType<SeaClient>();
                if (nm != null) { _client = nm; _predictor = nm.GetComponent<LocalPredictor>(); }
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Vector2 target = new Vector2(0f, (c.surfaceY + c.seabedY) * 0.5f);
            Vector2 lead = Vector2.zero;
            float want = spectatorView;

            if (_client != null && _client.HasData)
            {
                if (_client.Role == Role.Fish && _client.OwnIndex >= 0)
                {
                    Vector2 p;
                    if (_predictor != null && _predictor.Active) p = _predictor.VisualPos;
                    else _client.Sample(_client.OwnIndex, out p, out _, out _);
                    bool lev = (_client.LatestFlags(_client.OwnIndex) & FishFlags.Leviathan) != 0;
                    target = p;
                    want = lev ? leviathanView : fishView;
                    if (_init) lead = (p - _lastTarget) / Mathf.Max(1e-4f, dt) * lookAhead;
                    _lastTarget = p;
                }
                else if (_client.Role == Role.Angler && _client.BoatId >= 0)
                {
                    int b = _client.BoatId;
                    bool on = _client.SampleHook(b, out Vector2 hook, out _, out float boatX);
                    if (_predictor != null && _predictor.HookActive) { hook = _predictor.VisualHookPos; boatX = _predictor.VisualBoatX; on = true; }
                    Vector2 boat = new Vector2(boatX, c.surfaceY - 2.5f);
                    bool down = on && hook.y < c.surfaceY - 0.5f;
                    target = down ? Vector2.Lerp(hook, boat, 0.22f) : boat;
                    want = _client.InTug ? tugView : anglerView + (down ? Mathf.Clamp01((c.surfaceY - hook.y) / 18f) * 2.5f : 0f);
                    // scan the school: the mouse drags the view toward the screen edge it sits on
                    var mouse = UnityEngine.InputSystem.Mouse.current;
                    if (mouse != null && !_client.InTug && Application.isFocused)
                    {
                        Vector2 v = mouse.position.ReadValue();
                        Vector2 k = new Vector2(v.x / Mathf.Max(1, Screen.width) - 0.5f, v.y / Mathf.Max(1, Screen.height) - 0.5f) * 2f;
                        lead = new Vector2(k.x * mouseLead * 1.4f, k.y * mouseLead);
                    }
                }
                else want = spectatorView;
            }

            if (!_init)
            {
                _pos = target;
                _view = want;
                _vel = Vector2.zero;
                _init = true;
            }
            _view = Mathf.Lerp(_view, want, 1f - Mathf.Exp(-zoomRate * dt));
            Vector2 goal = target + Vector2.ClampMagnitude(lead, 3f);
            _pos = Vector2.SmoothDamp(_pos, goal, ref _vel, smoothTime, Mathf.Infinity, dt);

            // keep the frame inside the sea (a strip of sky allowed above the surface, a little seabed below)
            float halfH = _view * 0.5f, halfW = halfH * Mathf.Max(0.5f, _cam.aspect);
            float seaHalf = c.seaSize.x * 0.5f + 1.5f;
            float x = halfW >= seaHalf ? 0f : Mathf.Clamp(_pos.x, -seaHalf + halfW, seaHalf - halfW);
            float yMin = c.seabedY - bedMargin + halfH, yMax = c.surfaceY + skyMargin - halfH;
            float y = yMin > yMax ? (yMin + yMax) * 0.5f : Mathf.Clamp(_pos.y, yMin, yMax);

            _cam.fieldOfView = fov;
            float dist = halfH / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Vector3 shake = Vector3.zero;
            if (_shake > 0.001f)
            {
                float t = Time.unscaledTime * shakeFreq;
                float a = _shake * _shake * 0.6f;
                shake = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * a;
                _shake = Mathf.Max(0f, _shake - shakeDecay * dt);
            }
            transform.SetPositionAndRotation(new Vector3(x, y, -dist) + shake,
                                             Quaternion.Euler(-_vel.y * 0.15f, _vel.x * 0.12f, shake.x * 4f));
        }
    }
}
