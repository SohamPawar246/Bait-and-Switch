using UnityEngine;
using UnityEngine.UI;

namespace BaitSwitch.UI
{
    /// <summary>
    /// The main-menu sea (docs/12 "a dithered sea"): a half-sunken dinghy with a gull on the bow, its lantern still lit,
    /// a sou'wester floating beside it, ripples breathing out from the hull, a moon/sun glint path, and a huge eye that
    /// opens beneath the boat every 20–30 s. Light cycles day → dusk → night over 45 s; the ordered-dither image maps
    /// it onto the phase ramp. Rendered by its own camera into a low-res texture shown full-screen.
    /// </summary>
    public sealed class MenuDiorama : MonoBehaviour
    {
        public Camera cam;
        public Renderer water;
        public Transform boat, gull, hat, lantern;
        public Light sun;
        public float cycleSeconds = 45f;
        public float pixelSize = 2f;
        public Vector2 waterSize = new Vector2(30f, 30f);

        // Ramps: docs palette pushed toward the reference stills (green day, gold dusk, blue night).
        // sampled from the reference stills: jade day, olive-gold dusk, ink-blue night
        static readonly Color[] Day = { Hex("#0C2A1C"), Hex("#1F5A3A"), Hex("#6EA27A"), Hex("#E6EFD2") };
        static readonly Color[] Dusk = { Hex("#1A150A"), Hex("#58461B"), Hex("#B8923A"), Hex("#F3DFA2") };
        static readonly Color[] Night = { Hex("#03050B"), Hex("#121D36"), Hex("#4A6A9C"), Hex("#E9F1FF") };

        public RawImage Target { get; private set; }
        public float Phase01 { get; private set; }
        public Color[] Ramp { get; } = new Color[4];

        RenderTexture _rt;
        Material _water, _dither;
        Vector3 _boatPos, _gullPos, _hatPos, _lanternPos;
        Quaternion _boatRot, _hatRot;
        float _eyeT, _eyeNext = 12f, _clock;
        MaterialPropertyBlock _mpb;

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        void Awake()
        {
            if (boat) { _boatPos = boat.localPosition; _boatRot = boat.localRotation; }
            if (gull) _gullPos = gull.localPosition;
            if (hat) { _hatPos = hat.localPosition; _hatRot = hat.localRotation; }
            if (lantern) _lanternPos = lantern.localPosition;
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>Show the diorama through a dithered RawImage (created by AppFlow).</summary>
        public void Bind(RawImage img)
        {
            Target = img;
            var lib = ArtLibrary.I;
            if (lib != null && lib.ditherImage != null) img.material = _dither = new Material(lib.ditherImage);
            Resize();
        }

        void Resize()
        {
            int w = Mathf.Max(320, Mathf.RoundToInt(Screen.width / pixelSize));
            int h = Mathf.Max(180, Mathf.RoundToInt(Screen.height / pixelSize));
            if (_rt != null && _rt.width == w && _rt.height == h) return;
            if (_rt != null) { cam.targetTexture = null; _rt.Release(); Destroy(_rt); }
            _rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "MenuSea" };
            cam.targetTexture = _rt;
            if (Target != null) Target.texture = _rt;
        }

        public void SetActive(bool on)
        {
            gameObject.SetActive(on);
            if (cam) cam.enabled = on;
            if (Target) Target.enabled = on;
        }

        void OnDestroy() { if (_rt != null) _rt.Release(); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _clock += dt;
            float t = _clock;
            Resize();

            // light phase 0..3 (day → dusk → night → back to day), blended ramps
            float p = (t % cycleSeconds) / cycleSeconds * 3f;
            Phase01 = p;
            Color[] a, b;
            float k;
            if (p < 1f) { a = Day; b = Dusk; k = Smooth(p); }
            else if (p < 2f) { a = Dusk; b = Night; k = Smooth(p - 1f); }
            else { a = Night; b = Day; k = Smooth(Mathf.Clamp01((p - 2f) * 1.6f - 0.6f)); }
            for (int i = 0; i < 4; i++) Ramp[i] = Color.Lerp(a[i], b[i], k);
            float night = p < 1f ? 0f : p < 2f ? k : 1f - k;
            float dusk = p < 1f ? k : p < 2f ? 1f - k : 0f;

            if (_dither != null)
            {
                _dither.SetColor("_C0", Ramp[0]); _dither.SetColor("_C1", Ramp[1]);
                _dither.SetColor("_C2", Ramp[2]); _dither.SetColor("_C3", Ramp[3]);
                _dither.SetFloat("_PixelSize", pixelSize);
                _dither.SetFloat("_Bias", 0.08f - 0.1f * night); // lift the mid-tones so the hull reads like the references
                _dither.SetFloat("_Contrast", 1.35f);
                _dither.SetFloat("_Smooth", 0.3f);
            }

            if (sun)
            {
                sun.color = Color.Lerp(Color.Lerp(new Color(1f, 0.97f, 0.9f), new Color(1f, 0.6f, 0.3f), dusk), new Color(0.55f, 0.65f, 1f), night);
                sun.intensity = Mathf.Lerp(1.5f, 0.8f, night);
                sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(52f, 30f, dusk), 25f + 40f * (p / 3f), 0f); // from over the camera shoulder: the hull faces the light
            }

            // eye beneath the boat
            if (_eyeT <= 0f && t > _eyeNext) { _eyeT = 3.2f; _eyeNext = t + Random.Range(20f, 30f); }
            float eyeOpen = 0f;
            if (_eyeT > 0f)
            {
                _eyeT -= dt;
                eyeOpen = Mathf.Clamp01(Mathf.Min(3.2f - _eyeT, _eyeT) * 1.6f);
            }

            // the half-sunken boat breathes; the gull bobs and looks around; the hat drifts
            if (boat)
            {
                boat.localPosition = _boatPos + new Vector3(0f, Mathf.Sin(t * 0.9f) * 0.05f, 0f);
                boat.localRotation = _boatRot * Quaternion.Euler(Mathf.Sin(t * 0.7f) * 2.2f, 0f, Mathf.Sin(t * 0.9f + 1f) * 1.6f);
            }
            if (gull)
            {
                gull.localPosition = _gullPos + new Vector3(0f, Mathf.Sin(t * 0.9f) * 0.05f, 0f);
                float look = Mathf.PerlinNoise(t * 0.25f, 3f) > 0.62f ? 40f : 0f;
                gull.localRotation = Quaternion.Slerp(gull.localRotation, Quaternion.Euler(0f, look, 0f), dt * 3f);
            }
            if (hat)
            {
                hat.localPosition = _hatPos + new Vector3(Mathf.Sin(t * 0.13f) * 0.6f, Mathf.Sin(t * 1.3f) * 0.04f, Mathf.Cos(t * 0.11f) * 0.4f);
                hat.localRotation = _hatRot * Quaternion.Euler(Mathf.Sin(t * 1.1f) * 6f, t * 6f, Mathf.Cos(t * 0.9f) * 6f);
            }
            if (lantern) lantern.localPosition = _lanternPos + new Vector3(0f, Mathf.Sin(t * 1.7f) * 0.02f, 0f);

            if (water && boat)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                water.GetPropertyBlock(_mpb);
                _mpb.SetVector("_CenterWS", new Vector4(boat.position.x, boat.position.z, 0f, 0f));
                _mpb.SetFloat("_EyeOpen", eyeOpen);
                _mpb.SetFloat("_GlintStrength", Mathf.Lerp(0.35f, 1.1f, Mathf.Max(night, dusk * 0.7f)));
                _mpb.SetFloat("_RingStrength", Mathf.Lerp(0.95f, 0.75f, night));
                water.SetPropertyBlock(_mpb);
            }
        }

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3 - 2 * x); }
    }

    /// <summary>The dither dissolve every screen change goes through (docs/12 "Transitions between every step").</summary>
    public sealed class TransitionManager : MonoBehaviour
    {
        public static TransitionManager I { get; private set; }
        RawImage _img;
        Material _mat;
        float _amount;
        System.Action _mid;
        int _phase; // 0 idle, 1 closing, 2 opening
        float _speed = 2.6f;

        public bool Busy => _phase != 0;

        public static TransitionManager Create(Transform parent)
        {
            var c = UIKit.Canvas("Transition", 500, parent);
            var tm = c.gameObject.AddComponent<TransitionManager>();
            var img = UIKit.Stretch("Fade", c.transform).gameObject.AddComponent<RawImage>();
            img.raycastTarget = false;
            var lib = ArtLibrary.I;
            if (lib != null && lib.ditherFade != null) img.material = tm._mat = new Material(lib.ditherFade);
            else img.color = new Color(0, 0, 0, 0);
            tm._img = img;
            tm.Set(0f);
            I = tm;
            return tm;
        }

        /// <summary>Cover the screen through the dither, run mid at full cover, then reveal.</summary>
        public void Play(System.Action mid, float seconds = 0.4f)
        {
            _mid = mid;
            _speed = 1f / Mathf.Max(0.05f, seconds);
            _phase = 1;
            _img.raycastTarget = true;
        }

        /// <summary>Start fully covered and reveal (e.g. launch → menu).</summary>
        public void Reveal(float seconds = 0.8f) { Set(1f); _speed = 1f / seconds; _phase = 2; }

        public void SetColor(Color c) { if (_mat != null) _mat.SetColor("_Color", c); }

        void Set(float a)
        {
            _amount = a;
            if (_mat != null) _mat.SetFloat("_Amount", a);
            else { var c = _img.color; c.a = a; _img.color = c; }
        }

        void Update()
        {
            if (_phase == 1)
            {
                Set(Mathf.MoveTowards(_amount, 1f, Time.unscaledDeltaTime * _speed));
                if (_amount >= 1f) { var m = _mid; _mid = null; m?.Invoke(); _phase = 2; }
            }
            else if (_phase == 2)
            {
                Set(Mathf.MoveTowards(_amount, 0f, Time.unscaledDeltaTime * _speed));
                if (_amount <= 0f) { _phase = 0; _img.raycastTarget = false; }
            }
        }
    }
}
