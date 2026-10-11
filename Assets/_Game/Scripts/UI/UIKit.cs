using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaitSwitch.UI
{
    /// <summary>
    /// Runtime UGUI construction in the game's look (docs/12): cream ink on aged parchment, Pirata One for display,
    /// Crimson Text for body, a hook marker that slides in on hover, labels that sink 2 px.
    /// Parchment and wax-seal sprites are generated once in code (no textures to import).
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Ink = Hex("#1E1428");
        public static readonly Color Cream = Hex("#F4E9D8");
        public static readonly Color Abyss = Hex("#1B2440");
        public static readonly Color Midwater = Hex("#1F5F7A");
        public static readonly Color Shallows = Hex("#3FA7A3");
        public static readonly Color Raincoat = Hex("#F2C14E");
        public static readonly Color HookRed = Hex("#D64545");
        public static readonly Color Lilac = Hex("#B79CED");
        public static readonly Color Mint = Hex("#9BF6C4");
        public static readonly Color Leviathan = Hex("#C77DFF");
        public static readonly Color Parchment = Hex("#E9DCC0");

        static Sprite _parchment, _round, _seal, _ring, _hook, _white;

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        public static Font Display => ArtLibrary.I != null && ArtLibrary.I.display != null ? ArtLibrary.I.display : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Body => ArtLibrary.I != null && ArtLibrary.I.body != null ? ArtLibrary.I.body : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font BodyBold => ArtLibrary.I != null && ArtLibrary.I.bodyBold != null ? ArtLibrary.I.bodyBold : Body;

        // ------------------------------------------------------------ generated sprites

        public static Sprite White => _white ??= MakeSprite(Tex(4, 4, (x, y) => Color.white), 0);

        /// <summary>Aged parchment card with a dark ink border and soft burnt edges (9-sliced).</summary>
        public static Sprite ParchmentSprite => _parchment ??= MakeSprite(Tex(128, 128, (x, y) =>
        {
            float e = Mathf.Min(Mathf.Min(x, 127 - x), Mathf.Min(y, 127 - y));
            float n = Mathf.PerlinNoise(x * 0.11f, y * 0.11f) * 0.5f + Mathf.PerlinNoise(x * 0.37f + 9, y * 0.37f) * 0.25f;
            Color c = Color.Lerp(Parchment, Hex("#C9B48C"), n * 0.55f);
            if (e < 3) return Ink;
            if (e < 9) c = Color.Lerp(Hex("#8C6E46"), c, (e - 3) / 6f);
            return c;
        }), 18);

        /// <summary>Soft rounded rect for tray cards and bars.</summary>
        public static Sprite RoundSprite => _round ??= MakeSprite(Tex(64, 64, (x, y) =>
        {
            float dx = Mathf.Max(0, Mathf.Abs(x - 31.5f) - 18), dy = Mathf.Max(0, Mathf.Abs(y - 31.5f) - 18);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return new Color(1, 1, 1, Mathf.Clamp01(14 - d));
        }), 24);

        /// <summary>A wax seal (red disc with a pressed rim and an eye).</summary>
        public static Sprite SealSprite => _seal ??= MakeSprite(Tex(96, 96, (x, y) =>
        {
            float dx = x - 47.5f, dy = y - 47.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Sin(Mathf.Atan2(dy, dx) * 9) * 1.6f;
            if (r > 46) return Color.clear;
            Color c = Color.Lerp(HookRed, Hex("#7A1E22"), r / 46f * 0.6f);
            if (r > 35 && r < 39) c *= 0.75f;
            float ex = dx / 22f, ey = dy / 11f;
            if (ex * ex + ey * ey < 1f) c = Color.Lerp(c, Hex("#F08A7A"), 0.5f);
            if (dx * dx + dy * dy < 30) c = Hex("#5A1418");
            c.a = 1;
            return c;
        }), 0);

        /// <summary>A glowing ring for snap targets.</summary>
        public static Sprite RingSprite => _ring ??= MakeSprite(Tex(64, 64, (x, y) =>
        {
            float r = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f));
            float a = Mathf.Clamp01(1f - Mathf.Abs(r - 25f) / 4f) + Mathf.Clamp01(1f - r / 25f) * 0.18f;
            return new Color(1, 1, 1, a);
        }), 0);

        /// <summary>A small fishhook glyph (hover marker and cursor).</summary>
        public static Sprite HookSprite => _hook ??= MakeSprite(Tex(32, 48, (x, y) =>
        {
            float a = 0;
            if (x >= 18 && x <= 21 && y >= 14 && y <= 46) a = 1;                       // shank
            float dx = x - 13f, dy = y - 14f, r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r >= 5.5f && r <= 9f && y <= 15) a = 1;                                 // bend
            if (x >= 3 && x <= 6 && y >= 12 && y <= 21) a = 1;                          // point
            if (x >= 6 && x <= 9 && y >= 18 && y <= 21) a = 1;                          // barb
            float ex = x - 19.5f, ey = y - 45f;                                          // eye
            if (ex * ex + ey * ey < 12 && ex * ex + ey * ey > 3) a = 1;
            return new Color(1, 1, 1, a);
        }), 0);

        static Texture2D Tex(int w, int h, Func<int, int, Color> f)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y);
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static Sprite MakeSprite(Texture2D t, int border) =>
            Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));

        // ------------------------------------------------------------ construction

        public static Canvas Canvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin = default, Vector2 offMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent) => Rect(name, parent, Vector2.zero, Vector2.one);

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Stretch(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Text Text(string name, Transform parent, string text, Font font, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = Stretch(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Outline Shadow(Graphic g, Color c, float d = 2f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(d, -d);
            return o;
        }

        /// <summary>A menu-style text button: no box, cream label, hook slides in and the label sinks on hover/focus.</summary>
        public static Button MenuButton(Transform parent, string label, int size, Action onClick, Color? color = null)
        {
            var rt = Rect(label, parent, new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            rt.sizeDelta = new Vector2(560, size * 1.5f);
            rt.pivot = new Vector2(0, 0.5f);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            var txt = Text("Label", rt, label, Display, size, color ?? Cream);
            Shadow(txt, new Color(0, 0, 0, 0.55f));
            var hook = Image("Hook", rt, HookSprite, Raincoat);
            var hr = (RectTransform)hook.transform;
            hr.anchorMin = hr.anchorMax = new Vector2(0, 0.5f);
            hr.sizeDelta = new Vector2(size * 0.45f, size * 0.68f);
            hr.anchoredPosition = new Vector2(-size * 0.5f, 0);
            var fx = rt.gameObject.AddComponent<HoverHook>();
            fx.label = (RectTransform)txt.transform;
            fx.hook = hook;
            fx.size = size;
            b.onClick.AddListener(() => { Audio.AudioDirector.Ui("ui_click"); onClick?.Invoke(); });
            return b;
        }

        /// <summary>A parchment card button (used on cards and the workbench).</summary>
        public static Button CardButton(Transform parent, string label, int size, Action onClick, Vector2 sizeDelta)
        {
            var rt = Rect(label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = sizeDelta;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ParchmentSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            var b = rt.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.highlightedColor = new Color(1f, 0.93f, 0.8f);
            cb.pressedColor = new Color(0.85f, 0.75f, 0.6f);
            b.colors = cb;
            var t = Text("Label", rt, label, Display, size, Ink, TextAnchor.MiddleCenter);
            b.onClick.AddListener(() => { Audio.AudioDirector.Ui("ui_click"); onClick?.Invoke(); });
            return b;
        }

        public static Slider Slider(Transform parent, string label, float value, Action<float> onChange)
        {
            var row = Rect(label, parent, new Vector2(0, 0.5f), new Vector2(1, 0.5f));
            row.sizeDelta = new Vector2(0, 54);
            var t = Text("Label", row, label, Body, 28, Ink);
            ((RectTransform)t.transform).anchorMax = new Vector2(0.42f, 1);
            var area = Rect("Slider", row, new Vector2(0.45f, 0.3f), new Vector2(0.98f, 0.7f));
            var bg = Image("Track", area, RoundSprite, new Color(0.3f, 0.22f, 0.16f, 0.6f));
            var fillArea = Rect("Fill Area", area, Vector2.zero, Vector2.one);
            var fill = Image("Fill", fillArea, RoundSprite, Midwater);
            var handleArea = Rect("Handle Area", area, Vector2.zero, Vector2.one);
            var handle = Image("Handle", handleArea, SealSprite, Color.white, true);
            ((RectTransform)handle.transform).sizeDelta = new Vector2(40, 0);
            ((RectTransform)handle.transform).anchorMin = new Vector2(0, -0.6f);
            ((RectTransform)handle.transform).anchorMax = new Vector2(0, 1.6f);
            var s = area.gameObject.AddComponent<Slider>();
            s.fillRect = (RectTransform)fill.transform;
            s.handleRect = (RectTransform)handle.transform;
            s.targetGraphic = handle;
            s.minValue = 0; s.maxValue = 1; s.value = value;
            s.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return s;
        }

        public static Toggle Toggle(Transform parent, string label, bool value, Action<bool> onChange)
        {
            var row = Rect(label, parent, new Vector2(0, 0.5f), new Vector2(1, 0.5f));
            row.sizeDelta = new Vector2(0, 54);
            var t = Text("Label", row, label, Body, 28, Ink);
            ((RectTransform)t.transform).anchorMax = new Vector2(0.8f, 1);
            var box = Rect("Box", row, new Vector2(0.86f, 0.1f), new Vector2(0.86f, 0.9f));
            box.sizeDelta = new Vector2(44, 0);
            var bg = box.gameObject.AddComponent<Image>();
            bg.sprite = RoundSprite;
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = new Color(0.3f, 0.22f, 0.16f, 0.6f);
            var check = Image("Check", box, SealSprite, Color.white);
            var tg = box.gameObject.AddComponent<Toggle>();
            tg.graphic = check;
            tg.targetGraphic = bg;
            tg.isOn = value;
            tg.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return tg;
        }

        public static RectTransform VerticalStack(string name, Transform parent, float spacing, TextAnchor align = TextAnchor.UpperLeft)
        {
            var rt = Stretch(name, parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlHeight = false;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            return rt;
        }
    }

    /// <summary>Hover / focus effect for menu buttons: the hook slides in, the label sinks 2 px and warms.</summary>
    public sealed class HoverHook : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public RectTransform label;
        public Image hook;
        public int size;
        float _k, _target;
        Text _text;

        void Awake() { _text = label != null ? label.GetComponent<Text>() : null; }
        public void OnPointerEnter(PointerEventData e) { if (_target < 1) Audio.AudioDirector.Ui("ui_hover", 0.6f); _target = 1; }
        public void OnPointerExit(PointerEventData e) => _target = 0;
        public void OnSelect(BaseEventData e) => _target = 1;
        public void OnDeselect(BaseEventData e) => _target = 0;

        void Update()
        {
            _k = Mathf.MoveTowards(_k, _target, Time.unscaledDeltaTime * 6f);
            float e = _k * _k * (3 - 2 * _k);
            if (label != null) label.anchoredPosition = new Vector2(e * size * 0.55f, -2f * e);
            if (hook != null)
            {
                var c = hook.color; c.a = e; hook.color = c;
                hook.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-size * 0.8f, size * 0.15f, e), Mathf.Sin(Time.unscaledTime * 3f) * 2f * e);
            }
            if (_text != null) _text.color = Color.Lerp(UIKit.Cream, UIKit.Raincoat, e * 0.6f);
        }
    }
}
