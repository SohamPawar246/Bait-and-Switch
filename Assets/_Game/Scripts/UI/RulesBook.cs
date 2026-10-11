using System;
using UnityEngine;
using UnityEngine.UI;

namespace BaitSwitch.UI
{
    /// <summary>
    /// "The Book of Rites": how to play, as a parchment book with pages (goal, fish, Angler, tug, Leviathan, winning).
    /// Opened from the menu and automatically before the first dive. Keyboard ←/→ or the page buttons turn pages.
    /// </summary>
    public sealed class RulesBook : MonoBehaviour
    {
        struct Page { public string title, body, keys; public Color accent; }

        static readonly Page[] Pages =
        {
            new Page { title = Copy.RiteGoalTitle, body = Copy.RiteGoal, keys = Copy.RiteGoalKeys, accent = UIKit.Leviathan },
            new Page { title = Copy.RiteFishTitle, body = Copy.RiteFish, keys = Copy.HudControlsFish, accent = UIKit.Shallows },
            new Page { title = Copy.RiteAnglerTitle, body = Copy.RiteAngler, keys = Copy.HudControlsAngler, accent = UIKit.Raincoat },
            new Page { title = Copy.RiteTugTitle, body = Copy.RiteTug, keys = Copy.RiteTugKeys, accent = UIKit.HookRed },
            new Page { title = Copy.RiteLeviathanTitle, body = Copy.RiteLeviathan, keys = Copy.HudControlsLeviathan, accent = UIKit.Leviathan },
            new Page { title = Copy.RiteWinTitle, body = Copy.RiteWin, keys = Copy.RiteWinKeys, accent = UIKit.Raincoat },
        };

        RectTransform _root;
        Text _title, _body, _keys, _pageNo;
        Image _accent;
        Button _prev, _next;
        Text _nextLabel;
        int _page;
        Action _onDone;

        public bool Open => _root != null && _root.gameObject.activeSelf;

        public static RulesBook Create(Transform canvas)
        {
            var b = canvas.gameObject.AddComponent<RulesBook>();
            b.Build(canvas);
            return b;
        }

        void Build(Transform canvas)
        {
            _root = UIKit.Stretch("RulesRoot", canvas);
            UIKit.Image("Dim", _root, UIKit.White, new Color(0, 0, 0, 0.6f), true);
            var book = UIKit.Rect("Book", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            book.sizeDelta = new Vector2(1500, 860);
            UIKit.Image("Cover", book, UIKit.RoundSprite, UIKit.Hex("#3B2416")).rectTransform.offsetMin = new Vector2(-26, -26);
            ((RectTransform)book.Find("Cover")).offsetMax = new Vector2(26, 26);
            var left = UIKit.Image("Left", book, UIKit.ParchmentSprite, Color.white).rectTransform;
            left.anchorMax = new Vector2(0.5f, 1);
            var right = UIKit.Image("Right", book, UIKit.ParchmentSprite, Color.white).rectTransform;
            right.anchorMin = new Vector2(0.5f, 0);
            UIKit.Image("Spine", book, UIKit.White, new Color(0.25f, 0.15f, 0.08f, 0.5f)).rectTransform.anchorMin = new Vector2(0.497f, 0);
            ((RectTransform)book.Find("Spine")).anchorMax = new Vector2(0.503f, 1);

            UIKit.Text("Header", UIKit.Rect("H", left, new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -110), new Vector2(-40, -40)),
                       Copy.RitesHeader, UIKit.Display, 44, new Color(0.12f, 0.08f, 0.16f, 0.55f));
            _title = UIKit.Text("Title", UIKit.Rect("T", left, new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -260), new Vector2(-40, -110)), "", UIKit.Display, 76, UIKit.Ink);
            _accent = UIKit.Image("Rule", left, UIKit.White, UIKit.HookRed);
            _accent.rectTransform.anchorMin = new Vector2(0, 1); _accent.rectTransform.anchorMax = new Vector2(0.6f, 1);
            _accent.rectTransform.offsetMin = new Vector2(62, -276); _accent.rectTransform.offsetMax = new Vector2(0, -270);
            _body = UIKit.Text("Body", UIKit.Rect("B", left, new Vector2(0, 0), new Vector2(1, 1), new Vector2(60, 140), new Vector2(-40, -290)), "", UIKit.Body, 34, UIKit.Ink, TextAnchor.UpperLeft);
            // long pages shrink to fit above the Back / Turn the page buttons instead of running under them
            _body.resizeTextForBestFit = true;
            _body.resizeTextMinSize = 18;
            _body.resizeTextMaxSize = 32;
            _body.verticalOverflow = VerticalWrapMode.Truncate;
            _body.supportRichText = true;
            _body.lineSpacing = 1.1f;

            var seal = UIKit.Image("Seal", right, UIKit.SealSprite, Color.white).rectTransform;
            seal.anchorMin = seal.anchorMax = new Vector2(0.5f, 0.62f);
            seal.sizeDelta = new Vector2(260, 260);
            UIKit.Text("KeysHeader", UIKit.Rect("KH", right, new Vector2(0, 0.3f), new Vector2(1, 0.38f), new Vector2(50, 0), new Vector2(-50, 0)),
                       Copy.RitesKeys, UIKit.Display, 40, UIKit.Ink, TextAnchor.MiddleCenter);
            _keys = UIKit.Text("Keys", UIKit.Rect("K", right, new Vector2(0, 0.12f), new Vector2(1, 0.3f), new Vector2(60, 0), new Vector2(-60, 0)), "", UIKit.Body, 30, UIKit.Ink, TextAnchor.UpperCenter);
            _pageNo = UIKit.Text("PageNo", UIKit.Rect("PN", right, new Vector2(0, 0.86f), new Vector2(1, 0.96f), new Vector2(0, 0), new Vector2(-50, 0)), "", UIKit.Display, 34, new Color(0.12f, 0.08f, 0.16f, 0.5f), TextAnchor.MiddleRight);

            _prev = UIKit.CardButton(book, Copy.Back, 36, () => Turn(-1), new Vector2(220, 76));
            Place(_prev, new Vector2(0, 0), new Vector2(150, -10));
            _next = UIKit.CardButton(book, Copy.NextPage, 36, () => Turn(1), new Vector2(260, 76));
            Place(_next, new Vector2(1, 0), new Vector2(-170, -10));
            _nextLabel = _next.GetComponentInChildren<Text>();
            var close = UIKit.CardButton(book, "×", 40, Close, new Vector2(70, 70));
            Place(close, new Vector2(1, 1), new Vector2(-20, -20));
            _root.gameObject.SetActive(false);
        }

        static void Place(Button b, Vector2 anchor, Vector2 pos)
        {
            var r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = anchor;
            r.anchoredPosition = pos;
        }

        public void Show(Action onDone = null)
        {
            _onDone = onDone;
            _page = 0;
            _root.gameObject.SetActive(true);
            Refresh();
        }

        void Close()
        {
            _root.gameObject.SetActive(false);
            GameSettings.RulesSeen = true;
            var d = _onDone;
            _onDone = null;
            d?.Invoke();
        }

        void Turn(int dir)
        {
            if (dir > 0 && _page == Pages.Length - 1) { Close(); return; }
            _page = Mathf.Clamp(_page + dir, 0, Pages.Length - 1);
            Refresh();
        }

        void Refresh()
        {
            var p = Pages[_page];
            _title.text = p.title;
            _body.text = p.body;
            _keys.text = p.keys;
            _accent.color = p.accent;
            _pageNo.text = $"{_page + 1} / {Pages.Length}";
            _prev.gameObject.SetActive(_page > 0);
            _nextLabel.text = _page == Pages.Length - 1 ? (_onDone != null ? Copy.DiveIn : Copy.Back) : Copy.NextPage;
            var seal = _root.Find("Book/Right/Seal");
            if (seal != null) seal.localRotation = Quaternion.Euler(0, 0, -12 + _page * 9);
        }

        void Update()
        {
            if (!Open) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Turn(1);
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Turn(-1);
            if (kb.escapeKey.wasPressedThisFrame) Close();
        }
    }
}
