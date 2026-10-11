using System;
using System.Collections.Generic;
using BaitSwitch.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaitSwitch.UI
{
    /// <summary>
    /// The Workbench (docs/09, docs/12): a live 3D diorama of your creature with glowing snap marks.
    /// Drag a part card onto a mark and it snaps on (bounce, sparkles, a deadpan line); drag a fitted part off to
    /// remove it. Eyes are added one by one (1 → cyclops, 2 → pair, 3 → trio, cluster → seven) and plucked by dragging.
    /// Hatchery = fish, Vestry = Angler (hat on the hood, rod in the hand, hook on the line, bait on the hook, relic aboard).
    /// Paint swatches are dropped onto the body, eyes, coat, hood, lantern, hull or rod.
    /// </summary>
    public sealed class Workbench : MonoBehaviour
    {
        // ---------------------------------------------------------------- data
        sealed class Spot
        {
            public string key;
            public Transform anchor;
            public Image ring;
            public Func<Card, bool> accepts;
            public Action<Card> drop;
            public Func<bool> removable;
            public Action remove;
            public Sprite occupant;
        }

        public sealed class Card
        {
            public Slot slot;
            public PartDef part;     // null for eye tokens
            public int eyeToken;     // 1 = single eye, 7 = cluster
            public Sprite icon;
            public Color swatch;
            public bool isPaint;
            public string label;
        }

        Action _onBack, _onDive;
        RectTransform _root, _previewRect, _tray, _tabs, _overlay;
        RawImage _preview;
        FishRig _rig;
        Text _hint, _statTitle, _statPlus, _statMinus, _toast, _sheetTitle, _traits, _trayHint;
        readonly Text[] _statName = new Text[4], _statVal = new Text[4];
        readonly Image[] _statFill = new Image[4];
        readonly StatBlock _stats = new StatBlock();
        InputField _name;
        Camera _cam;
        RenderTexture _rt;
        Transform _world, _subject;
        byte[] _app;
        bool _angler;
        string _tab;
        readonly List<Spot> _spots = new List<Spot>();
        Image _ghost;
        Card _dragCard;
        Spot _dragFrom;
        float _yaw = 25f, _spinVel, _punch, _toastT;
        readonly List<(RectTransform rt, Vector2 v, float life)> _sparks = new List<(RectTransform, Vector2, float)>();
        readonly Stack<RectTransform> _sparkPool = new Stack<RectTransform>();

        static readonly (string label, Slot slot)[] FishTabs =
            { (Copy.TabBody, Slot.Body), (Copy.TabEyes, Slot.Eyes), (Copy.TabFins, Slot.Fins), (Copy.TabTail, Slot.Tail), (Copy.TabMouth, Slot.Mouth), (Copy.TabSpecial, Slot.Special), (Copy.TabPaint, Slot.SkinColor) };
        static readonly (string label, Slot slot)[] AnglerTabs =
            { (Copy.TabHat, Slot.Hat), (Copy.TabRod, Slot.Rod), (Copy.TabHook, Slot.Hook), (Copy.TabBait, Slot.Bait), (Copy.TabRelic, Slot.Relic), (Copy.TabPaint, Slot.CoatColor) };
        static readonly Slot[] FishPaints = { Slot.SkinColor, Slot.GlowColor, Slot.EyeColor, Slot.Pattern };
        static readonly Slot[] AnglerPaints = { Slot.CoatColor, Slot.HoodEyes, Slot.FlameColor, Slot.BoatPaint, Slot.RodFinish };

        // ---------------------------------------------------------------- construction

        public static Workbench Create(Transform canvas, Action onBack, Action onDive)
        {
            var wb = canvas.gameObject.AddComponent<Workbench>();
            wb._onBack = onBack;
            wb._onDive = onDive;
            wb.BuildWorld();
            wb.BuildUI(canvas);
            wb.SetOpen(false);
            return wb;
        }

        void BuildWorld()
        {
            _world = new GameObject("WorkbenchDiorama").transform;
            _world.position = new Vector3(-1000f, 0f, 0f);
            var camGo = new GameObject("WorkbenchCamera");
            camGo.transform.SetParent(_world, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.fieldOfView = 28f;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 40f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = UIKit.Hex("#0E1B26");
            _cam.enabled = false;
            _rt = new RenderTexture(1280, 1080, 16, RenderTextureFormat.ARGB32) { name = "Workbench", antiAliasing = 4 };
            _cam.targetTexture = _rt;

            // set dressing behind the subject: kelp and coral for the Hatchery, a shrine and candles for the Vestry
            var lib = ArtLibrary.I;
            if (lib == null) return;
            void Dress(GameObject prefab, Vector3 pos, float scale, float yaw = 0f)
            {
                if (prefab == null) return;
                var g = Instantiate(prefab, _world);
                g.transform.localPosition = pos;
                g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                g.transform.localScale = Vector3.one * scale;
                ToonLibrary.Apply(g, Color.white, UIKit.Mint);
            }
            if (lib.kelp != null && lib.kelp.Length > 0) { Dress(lib.kelp[0], new Vector3(-2.2f, -2.2f, 3f), 1.2f); Dress(lib.kelp[lib.kelp.Length - 1], new Vector3(2.4f, -2.2f, 3.5f), 1.4f); }
            if (lib.coral != null && lib.coral.Length > 0) Dress(lib.coral[0], new Vector3(1.6f, -1.6f, 2f), 1.4f);
            Dress(lib.shrine, new Vector3(-2.6f, -1.5f, 4f), 1.6f, 180f);
            Dress(lib.candle, new Vector3(-1.9f, -1.5f, 3f), 1.6f);
            Dress(lib.candle, new Vector3(2.2f, -1.5f, 3f), 1.3f);
            Dress(lib.skeleton, new Vector3(0.3f, -1.55f, 3f), 1.3f, 180f);
        }

        void BuildUI(Transform canvas)
        {
            _root = UIKit.Stretch("WorkbenchRoot", canvas);
            UIKit.Image("Backdrop", _root, UIKit.White, UIKit.Hex("#0B141C"), true);
            var desk = UIKit.Image("Desk", _root, UIKit.ParchmentSprite, Color.white);
            ((RectTransform)desk.transform).offsetMin = new Vector2(30, 30);
            ((RectTransform)desk.transform).offsetMax = new Vector2(-30, -30);

            // preview window (left)
            _previewRect = UIKit.Rect("Preview", _root, new Vector2(0, 0), new Vector2(0.58f, 1), new Vector2(70, 150), new Vector2(-20, -150));
            UIKit.Image("Frame", _previewRect, UIKit.RoundSprite, UIKit.Ink).rectTransform.offsetMin = new Vector2(-8, -8);
            ((RectTransform)_previewRect.Find("Frame")).offsetMax = new Vector2(8, 8);
            _preview = UIKit.Stretch("View", _previewRect).gameObject.AddComponent<RawImage>();
            _preview.texture = _rt;
            _preview.raycastTarget = true;
            var spin = _preview.gameObject.AddComponent<PreviewSpin>();
            spin.wb = this;
            _overlay = UIKit.Stretch("Marks", _previewRect);

            // header: title tabs, name, hint
            var tabsRow = UIKit.Rect("Faction", _root, new Vector2(0, 1), new Vector2(0.58f, 1), new Vector2(70, -140), new Vector2(-20, -50));
            FactionTab(tabsRow, Copy.Hatchery, 0f, () => SetFaction(false));
            FactionTab(tabsRow, Copy.Vestry, 0.32f, () => SetFaction(true));
            _hint = UIKit.Text("Hint", UIKit.Rect("HintBox", _root, new Vector2(0, 0), new Vector2(0.58f, 0), new Vector2(70, 60), new Vector2(-20, 140)),
                               Copy.WorkbenchHint, UIKit.Body, 28, UIKit.Ink, TextAnchor.MiddleLeft);

            // right column: name, tabs, tray, stat card, buttons
            var right = UIKit.Rect("Right", _root, new Vector2(0.58f, 0), new Vector2(1, 1), new Vector2(20, 60), new Vector2(-70, -50));
            UIKit.Text("NameLabel", UIKit.Rect("NameLabelBox", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(0, 0)), Copy.NameLabel, UIKit.Body, 26, UIKit.Ink);
            _name = NameField(UIKit.Rect("NameBox", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -110), new Vector2(0, -46)));
            _tabs = UIKit.Rect("Tabs", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -190), new Vector2(0, -120));
            _tray = UIKit.Rect("Tray", right, new Vector2(0, 0.53f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -236));
            _trayHint = UIKit.Text("TrayHint", UIKit.Rect("TH", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -236), new Vector2(0, -196)), Copy.WorkbenchClickHint, UIKit.Body, 22, new Color(0.12f, 0.08f, 0.16f, 0.75f));
            var grid = _tray.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(128, 148);
            grid.spacing = new Vector2(14, 14);
            grid.childAlignment = TextAnchor.UpperLeft;

            // the specimen sheet: live stats of the whole creature + what each fitted part does
            var stat = UIKit.Rect("Sheet", right, new Vector2(0, 0.13f), new Vector2(1, 0.43f));
            UIKit.Image("Card", stat, UIKit.RoundSprite, new Color(0.25f, 0.18f, 0.12f, 0.12f));
            _sheetTitle = UIKit.Text("SheetTitle", UIKit.Rect("ST", stat, new Vector2(0, 0.82f), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-20, 0)), "", UIKit.Display, 34, UIKit.Ink);
            for (int i = 0; i < 4; i++)
            {
                var row = UIKit.Rect("Stat" + i, stat, new Vector2(0, 0.62f - i * 0.15f), new Vector2(0.55f, 0.77f - i * 0.15f), new Vector2(20, 0), new Vector2(0, 0));
                _statName[i] = UIKit.Text("N", UIKit.Rect("NB", row, new Vector2(0, 0), new Vector2(0.38f, 1)), "", UIKit.Body, 22, UIKit.Ink);
                var track = UIKit.Rect("Track", row, new Vector2(0.4f, 0.25f), new Vector2(0.84f, 0.75f));
                UIKit.Image("Bg", track, UIKit.RoundSprite, new Color(0.3f, 0.22f, 0.16f, 0.25f));
                _statFill[i] = UIKit.Image("Fill", track, UIKit.RoundSprite, UIKit.Midwater);
                _statFill[i].rectTransform.anchorMax = new Vector2(0.5f, 1);
                var mid = UIKit.Image("Avg", track, UIKit.White, new Color(0.12f, 0.08f, 0.16f, 0.6f));
                mid.rectTransform.anchorMin = new Vector2(0.5f, -0.2f); mid.rectTransform.anchorMax = new Vector2(0.5f, 1.2f); mid.rectTransform.sizeDelta = new Vector2(2, 0);
                _statVal[i] = UIKit.Text("V", UIKit.Rect("VB", row, new Vector2(0.86f, 0), new Vector2(1.1f, 1)), "", UIKit.Body, 20, UIKit.Ink);
            }
            _traits = UIKit.Text("Traits", UIKit.Rect("TR", stat, new Vector2(0.56f, 0.04f), new Vector2(1, 0.8f), new Vector2(10, 0), new Vector2(-16, 0)), "", UIKit.Body, 18, UIKit.Ink, TextAnchor.UpperLeft);
            _traits.supportRichText = true;
            _traits.resizeTextForBestFit = true;   // shrink rather than spill under the buttons when many parts carry traits
            _traits.resizeTextMinSize = 11;
            _traits.resizeTextMaxSize = 18;
            _traits.verticalOverflow = VerticalWrapMode.Truncate;
            _statTitle = UIKit.Text("Title", UIKit.Rect("T", right, new Vector2(0, 0.48f), new Vector2(1, 0.53f)), "", UIKit.Display, 28, UIKit.Ink);
            _statPlus = UIKit.Text("Plus", UIKit.Rect("P", right, new Vector2(0, 0.45f), new Vector2(1, 0.49f)), "", UIKit.Body, 21, UIKit.Hex("#2E7D4F"));
            _statMinus = UIKit.Text("Minus", UIKit.Rect("M", right, new Vector2(0, 0.42f), new Vector2(1, 0.46f)), "", UIKit.Body, 21, UIKit.Hex("#A8323A"));

            var back = UIKit.CardButton(right, Copy.Back, 40, () => { Save(); _onBack?.Invoke(); }, new Vector2(220, 80));
            Place(back, new Vector2(0, 0), new Vector2(120, 45));
            var rnd = UIKit.CardButton(right, Copy.Randomize, 30, Randomize, new Vector2(300, 80));
            Place(rnd, new Vector2(0.5f, 0), new Vector2(0, 45));
            var dive = UIKit.CardButton(right, Copy.DiveIn, 46, () => { Save(); _onDive?.Invoke(); }, new Vector2(240, 90));
            Place(dive, new Vector2(1, 0), new Vector2(-130, 45));
            var seal = UIKit.Image("Seal", dive.transform, UIKit.SealSprite, Color.white);
            var sr = seal.rectTransform; sr.anchorMin = sr.anchorMax = new Vector2(1, 1); sr.sizeDelta = new Vector2(64, 64); sr.anchoredPosition = new Vector2(-6, -6);

            _toast = UIKit.Text("Toast", UIKit.Rect("ToastBox", _previewRect, new Vector2(0, 0.86f), new Vector2(1, 0.98f)), "", UIKit.Display, 40, UIKit.Cream, TextAnchor.MiddleCenter);
            UIKit.Shadow(_toast, new Color(0, 0, 0, 0.8f), 3);

            _ghost = UIKit.Image("Ghost", _root, null, new Color(1, 1, 1, 0.9f));
            _ghost.rectTransform.anchorMin = _ghost.rectTransform.anchorMax = Vector2.zero;
            _ghost.rectTransform.sizeDelta = new Vector2(130, 130);
            _ghost.enabled = false;
        }

        static void Place(Button b, Vector2 anchor, Vector2 pos)
        {
            var r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = anchor;
            r.anchoredPosition = pos;
        }

        void FactionTab(RectTransform row, string label, float x, Action act)
        {
            var b = UIKit.MenuButton(row, label, 64, act, UIKit.Ink);
            var r = (RectTransform)b.transform;
            r.anchorMin = new Vector2(x, 0); r.anchorMax = new Vector2(x + 0.32f, 1);
            r.offsetMin = r.offsetMax = Vector2.zero;
            b.GetComponentInChildren<Text>().color = UIKit.Ink;
            Destroy(b.GetComponent<HoverHook>());
            b.transform.Find("Hook").gameObject.SetActive(false);
            var hh = b.gameObject.AddComponent<TabHover>();
            hh.text = b.GetComponentInChildren<Text>();
        }

        InputField NameField(RectTransform box)
        {
            var bg = box.gameObject.AddComponent<Image>();
            bg.sprite = UIKit.RoundSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1f, 0.97f, 0.9f, 0.9f);
            var txt = UIKit.Text("Text", box, "", UIKit.Display, 40, UIKit.Ink);
            ((RectTransform)txt.transform).offsetMin = new Vector2(18, 0);
            txt.supportRichText = false;
            var f = box.gameObject.AddComponent<InputField>();
            f.textComponent = txt;
            f.characterLimit = 18;
            f.onEndEdit.AddListener(v => GameSettings.Name = v);
            return f;
        }

        // ---------------------------------------------------------------- open / close

        public void SetOpen(bool open)
        {
            _root.gameObject.SetActive(open);
            _world.gameObject.SetActive(open);
            _cam.enabled = open;
            if (!open) return;
            _app = (byte[])GameSettings.Appearance.Clone();
            _name.text = GameSettings.Name;
            SetFaction(_angler);
        }

        void Save()
        {
            if (_app != null) GameSettings.Appearance = _app;
            if (_name != null) GameSettings.Name = _name.text;
        }

        public void SetFaction(bool angler)
        {
            _angler = angler;
            BuildTabs();
            Rebuild();
            ShowTab(angler ? AnglerTabs[0].label : FishTabs[0].label);
        }

        void BuildTabs()
        {
            foreach (Transform c in _tabs) Destroy(c.gameObject);
            var tabs = _angler ? AnglerTabs : FishTabs;
            float w = 1f / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                string label = tabs[i].label;
                var b = UIKit.CardButton(_tabs, label, 26, () => ShowTab(label), Vector2.zero);
                var r = (RectTransform)b.transform;
                r.anchorMin = new Vector2(i * w, 0); r.anchorMax = new Vector2((i + 1) * w, 1);
                r.offsetMin = new Vector2(3, 0); r.offsetMax = new Vector2(-3, 0);
            }
        }

        void ShowTab(string label)
        {
            _tab = label;
            foreach (Transform c in _tray) Destroy(c.gameObject);
            var cat = PartCatalog.I;
            if (cat == null) return;
            if (label == Copy.TabPaint)
            {
                foreach (var slot in _angler ? AnglerPaints : FishPaints)
                    foreach (var p in cat.OfSlot(slot)) AddCard(new Card { slot = slot, part = p, isPaint = true, swatch = p.tint, icon = p.thumbnail, label = p.displayName });
                return;
            }
            if (label == Copy.TabEyes)
            {
                AddCard(new Card { slot = Slot.Eyes, eyeToken = 1, icon = Thumb("two"), label = Copy.Eye });
                AddCard(new Card { slot = Slot.Eyes, eyeToken = 7, icon = Thumb("seven"), label = Copy.EyeCluster });
                return;
            }
            var tabs = _angler ? AnglerTabs : FishTabs;
            foreach (var t in tabs)
                if (t.label == label)
                    foreach (var p in cat.OfSlot(t.slot)) AddCard(new Card { slot = t.slot, part = p, icon = p.thumbnail, label = p.displayName });
        }

        Sprite Thumb(string eyesKind)
        {
            foreach (var p in PartCatalog.I.OfSlot(Slot.Eyes))
                if (p.prefab != null && p.prefab.name.Contains(eyesKind)) return p.thumbnail;
            return null;
        }

        void AddCard(Card c)
        {
            var rt = UIKit.Rect(c.label, _tray, Vector2.zero, Vector2.zero);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = UIKit.RoundSprite;
            bg.type = Image.Type.Sliced;
            bool equipped = c.part != null && _app != null && _app[(int)c.slot] == c.part.id;
            bg.color = equipped ? new Color(0.95f, 0.76f, 0.31f, 0.55f) : new Color(1f, 0.97f, 0.9f, 0.75f);
            var icon = UIKit.Image("Icon", rt, c.icon, Color.white);
            var ir = icon.rectTransform; ir.offsetMin = new Vector2(12, 42); ir.offsetMax = new Vector2(-12, -8);
            icon.preserveAspect = true;
            if (c.isPaint)
            {
                icon.sprite = UIKit.SealSprite;
                icon.color = c.swatch;
            }
            else if (c.icon == null) { icon.sprite = UIKit.HookSprite; icon.color = UIKit.Ink; }
            var t = UIKit.Text("Label", UIKit.Rect("L", rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, 2), new Vector2(-4, 42)), c.label, UIKit.Body, 22, UIKit.Ink, TextAnchor.MiddleCenter);
            var d = rt.gameObject.AddComponent<CardDrag>();
            d.wb = this;
            d.card = c;
        }

        // ---------------------------------------------------------------- building the subject + spots

        void Rebuild()
        {
            if (_subject != null) Destroy(_subject.gameObject);
            foreach (var s in _spots) if (s.ring != null) Destroy(s.ring.gameObject);
            _spots.Clear();
            var holder = new GameObject("Subject").transform;
            holder.SetParent(_world, false);
            _subject = holder;
            _rig = null;
            if (_angler) BuildAngler(holder); else BuildFish(holder);
            _cam.transform.localPosition = _angler ? new Vector3(0f, 1.0f, -10.5f) : new Vector3(0f, 0.25f, -8.6f);
            _cam.transform.localRotation = Quaternion.Euler(_angler ? 7f : 2f, 0f, 0f);
            _punch = 1f;
            RefreshSheet();
        }

        void BuildFish(Transform holder)
        {
            var fish = FishAssembler.Build(_app, holder, "Specimen");
            fish.transform.localScale = Vector3.one * 2.4f;
            _rig = fish.GetComponent<FishRig>();
            var sockets = FishAssembler.Sockets(fish);
            Transform S(string k) => sockets.TryGetValue(k, out var t) ? t : fish.transform;

            AddSpot("body", fish.transform, c => c.slot == Slot.Body || (c.isPaint && c.slot != Slot.EyeColor), c => Set(c.slot, c.part.id), null, null);
            foreach (var e in new[] { "eye_L", "eye_R", "eye_C" })
            {
                string key = e;
                int bit = key == "eye_L" ? 1 : key == "eye_R" ? 2 : 4;
                AddSpot(key, S("socket_" + key), c => c.eyeToken > 0 || (c.isPaint && c.slot == Slot.EyeColor),
                        c => { if (c.isPaint) Set(c.slot, c.part.id); else AddEye(bit, c.eyeToken == 7); },
                        () => (EyeMask() & bit) != 0, () => RemoveEye(bit));
            }
            AddSpot("fin_L", S("socket_fin_L"), c => c.slot == Slot.Fins, c => Set(Slot.Fins, c.part.id), null, null);
            AddSpot("fin_R", S("socket_fin_R"), c => c.slot == Slot.Fins, c => Set(Slot.Fins, c.part.id), null, null);
            AddSpot("tail", S("socket_tail"), c => c.slot == Slot.Tail, c => Set(Slot.Tail, c.part.id), null, null);
            AddSpot("mouth", S("socket_mouth"), c => c.slot == Slot.Mouth || (c.slot == Slot.Special && SpecialSocket(c) == "socket_mouth"),
                    c => Set(c.slot, c.part.id), () => SpecialSocket(Current(Slot.Special)) == "socket_mouth", RemoveSpecial);
            foreach (var k in new[] { "socket_front", "socket_top", "socket_belly" })
            {
                string sk = k;
                AddSpot(sk, S(sk), c => c.slot == Slot.Special && SpecialSocket(c) == sk, c => Set(Slot.Special, c.part.id),
                        () => SpecialSocket(Current(Slot.Special)) == sk, RemoveSpecial);
            }
        }

        void BuildAngler(Transform holder)
        {
            var lib = ArtLibrary.I;
            Color coat = Tint(Slot.CoatColor, KitBuilder.Raincoat), paint = Tint(Slot.BoatPaint, UIKit.Hex("#E8E2D2"));
            Color flame = Tint(Slot.FlameColor, UIKit.Raincoat), eyes = Tint(Slot.HoodEyes, UIKit.Raincoat), finish = Tint(Slot.RodFinish, UIKit.Hex("#3B2C22"));
            var root = new GameObject("Vestry").transform;
            root.SetParent(holder, false);
            root.localRotation = Quaternion.Euler(0f, 180f, 0f);
            root.localScale = Vector3.one * 1.5f;
            Transform seat = root, bow = root;
            if (lib != null && lib.boat != null)
            {
                var boat = Instantiate(lib.boat, root);
                ToonLibrary.Apply(boat, paint, Color.white);
                var sk = FishAssembler.Sockets(boat);
                if (sk.TryGetValue("socket_seat", out var st)) seat = st;
                if (sk.TryGetValue("socket_bow", out var bt)) bow = bt;
                AddSpot("hull", boat.transform, c => c.isPaint && c.slot == Slot.BoatPaint, c => Set(c.slot, c.part.id), null, null);
            }
            Transform coatT = seat;
            if (lib != null && lib.raincoat != null)
            {
                var coatGo = Instantiate(lib.raincoat, seat);
                coatGo.transform.localScale = Vector3.one * 0.9f;
                ToonLibrary.Apply(coatGo, coat, eyes);
                coatT = coatGo.transform;
                var head = Anchor(coatT, new Vector3(0f, 0.95f, 0f));
                var face = Anchor(coatT, new Vector3(-0.3f, 0.64f, 0f));
                var chest = Anchor(coatT, new Vector3(0f, 0.25f, 0f));
                AddSpot("coat", chest, c => c.isPaint && c.slot == Slot.CoatColor, c => Set(c.slot, c.part.id), null, null);
                AddSpot("hood", face, c => c.isPaint && c.slot == Slot.HoodEyes, c => Set(c.slot, c.part.id), null, null);
                var hatDef = Current(Slot.Hat);
                if (hatDef != null && hatDef.prefab != null)
                {
                    var h = Instantiate(hatDef.prefab, coatT);
                    h.transform.localPosition = new Vector3(0f, 0.93f, 0f);
                    ToonLibrary.Apply(h, Color.Lerp(coat, Color.black, 0.15f), coat);
                }
                AddSpot("hat", head, c => c.slot == Slot.Hat, c => Set(Slot.Hat, c.part.id), () => !IsNone(Current(Slot.Hat)), () => SetNone(Slot.Hat));
                // rod in the hand, hook on the line, bait on the hook
                var rodDef = Current(Slot.Rod);
                var hand = Anchor(coatT, new Vector3(-0.25f, 0.35f, -0.25f));
                Transform tip = hand;
                if (rodDef != null && rodDef.prefab != null)
                {
                    var rod = Instantiate(rodDef.prefab, hand);
                    rod.transform.localRotation = Quaternion.Euler(-40f, 0f, 20f);
                    ToonLibrary.Apply(rod, finish, flame);
                    tip = Anchor(rod.transform, new Vector3(0f, 1.35f, 0f));
                }
                AddSpot("rod", hand, c => c.slot == Slot.Rod || (c.isPaint && c.slot == Slot.RodFinish), c => Set(c.slot, c.part.id), null, null);
                var hookAt = new GameObject("HookAt").transform;
                hookAt.SetParent(holder, false);
                hookAt.position = tip.position + Vector3.down * 1.4f;
                var hookDef = Current(Slot.Hook);
                if (hookDef != null && hookDef.prefab != null)
                {
                    var hk = Instantiate(hookDef.prefab, hookAt);
                    hk.transform.localScale = Vector3.one * 2.2f;
                    ToonLibrary.Apply(hk, UIKit.Hex("#B9BEC6"), Color.white, 0.006f);
                }
                var baitDef = Current(Slot.Bait);
                var baitAt = Anchor(hookAt, new Vector3(0f, -0.28f, 0f));
                if (baitDef != null && baitDef.prefab != null)
                {
                    var bt2 = Instantiate(baitDef.prefab, baitAt);
                    bt2.transform.localScale = Vector3.one * 2.2f;
                    ToonLibrary.Apply(bt2, UIKit.Hex("#E07A8A"), flame, 0.006f);
                }
                Line(tip.position, hookAt.position, holder);
                AddSpot("hook", hookAt, c => c.slot == Slot.Hook, c => Set(Slot.Hook, c.part.id), null, null);
                AddSpot("bait", baitAt, c => c.slot == Slot.Bait, c => Set(Slot.Bait, c.part.id), null, null);
            }
            if (lib != null && lib.lantern != null)
            {
                var l = Instantiate(lib.lantern, bow);
                l.transform.localScale = Vector3.one * 1.8f;
                ToonLibrary.Apply(l, Color.white, flame);
                AddSpot("lantern", l.transform, c => c.isPaint && c.slot == Slot.FlameColor, c => Set(c.slot, c.part.id), null, null);
            }
            var relicDef = Current(Slot.Relic);
            var relicAt = Anchor(seat, new Vector3(0.55f, 0.05f, 0f));
            if (relicDef != null && relicDef.prefab != null)
            {
                var r = Instantiate(relicDef.prefab, relicAt);
                r.transform.localScale = Vector3.one * 1.6f;
                ToonLibrary.Apply(r, UIKit.Hex("#B9A27A"), flame);
            }
            AddSpot("relic", relicAt, c => c.slot == Slot.Relic, c => Set(Slot.Relic, c.part.id), () => !IsNone(Current(Slot.Relic)), () => SetNone(Slot.Relic));
        }

        static Transform Anchor(Transform parent, Vector3 local)
        {
            var t = new GameObject("Anchor").transform;
            t.SetParent(parent, false);
            t.localPosition = local;
            return t;
        }

        void Line(Vector3 a, Vector3 b, Transform parent)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.widthMultiplier = 0.02f;
            lr.sharedMaterial = ToonLibrary.Get(UIKit.Cream, UIKit.Cream * 0.4f, 0f);
        }

        void AddSpot(string key, Transform anchor, Func<Card, bool> accepts, Action<Card> drop, Func<bool> removable, Action remove)
        {
            var ring = UIKit.Image("Mark_" + key, _overlay, UIKit.RingSprite, new Color(1, 1, 1, 0f), true);
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ring.rectTransform.sizeDelta = new Vector2(84, 84);
            var spot = new Spot { key = key, anchor = anchor, ring = ring, accepts = accepts, drop = drop, removable = removable, remove = remove };
            var rd = ring.gameObject.AddComponent<SpotDrag>();
            rd.wb = this;
            rd.spot = spot;
            _spots.Add(spot);
        }

        // ---------------------------------------------------------------- appearance edits

        PartDef Current(Slot s) => PartCatalog.I != null ? PartCatalog.I.Find(s, _app[(int)s]) : null;
        Color Tint(Slot s, Color fallback) { var p = Current(s); return p != null && p.cosmetic ? p.tint : fallback; }
        static bool IsNone(PartDef p) => p == null || p.prefab == null;
        static string SpecialSocket(Card c) => c?.part != null ? FishAssembler.SpecialSocket(c.part) : null;
        static string SpecialSocket(PartDef p) => FishAssembler.SpecialSocket(p);

        void Set(Slot s, byte id)
        {
            bool changed = _app[(int)s] != id;
            _app[(int)s] = id;
            Rebuild();
            Toast(changed ? Copy.Snapped : "");
            ShowTab(_tab);
        }

        void SetNone(Slot s)
        {
            foreach (var p in PartCatalog.I.OfSlot(s))
                if (p.prefab == null) { Set(s, p.id); Toast(Copy.Removed); return; }
        }

        void RemoveSpecial() => SetNone(Slot.Special);

        int EyeMask()
        {
            var p = Current(Slot.Eyes);
            string n = p != null && p.prefab != null ? p.prefab.name : "eyes_two";
            if (n.Contains("cyclops")) return 4;
            if (n.Contains("three") || n.Contains("seven")) return 7;
            return 3;
        }

        void AddEye(int bit, bool cluster)
        {
            if (cluster) { SetEyes("seven"); return; }
            int mask = EyeMask() | bit;
            if (Current(Slot.Eyes) != null && Current(Slot.Eyes).prefab != null && Current(Slot.Eyes).prefab.name.Contains("seven")) mask = 3 | bit;
            SetEyesByCount(mask);
        }

        void RemoveEye(int bit)
        {
            int mask = EyeMask() & ~bit;
            if (mask == 0) { Toast(Copy.NeedAnEye); _punch = 0.6f; return; }
            SetEyesByCount(mask);
            Toast(Copy.Removed);
        }

        void SetEyesByCount(int mask)
        {
            int n = (mask & 1) + ((mask >> 1) & 1) + ((mask >> 2) & 1);
            SetEyes(n <= 1 ? "cyclops" : n == 2 ? "two" : "three");
        }

        void SetEyes(string kind)
        {
            foreach (var p in PartCatalog.I.OfSlot(Slot.Eyes))
                if (p.prefab != null && p.prefab.name.Contains(kind)) { Set(Slot.Eyes, p.id); return; }
        }

        void Randomize()
        {
            var fresh = PartCatalog.I.RandomAppearance(new System.Random());
            // keep the other faction's choices; randomize only what you're looking at
            for (int i = 0; i < _app.Length; i++)
                if ((PartDef.FactionOf((Slot)i) == Faction.Angler) == _angler) _app[i] = fresh[i];
            Rebuild();
            _spinVel = 900f;
            for (int i = 0; i < 18; i++) Spark(Vector2.zero, i);
            ShowTab(_tab);
        }

        void Toast(string t)
        {
            _toast.text = t;
            _toastT = string.IsNullOrEmpty(t) ? 0f : 2f;
            // every fitting gets a sound: a wet snap on, a pop off, a dull refusal
            if (t == Copy.Snapped) { Audio.AudioDirector.Ui("bubble_0", 0.9f); Audio.AudioDirector.Ui("chime", 0.35f); }
            else if (t == Copy.Removed) Audio.AudioDirector.Play("bubble_2", 0.6f, 0.75f);
            else if (t == Copy.NeedAnEye) Audio.AudioDirector.Play("line_tick", 0.6f, 0.5f);
        }

        void ShowStat(Card c)
        {
            if (c == null) { _statTitle.text = _statPlus.text = _statMinus.text = ""; return; }
            _statTitle.text = c.label;
            _statPlus.text = c.part != null && !string.IsNullOrEmpty(c.part.plus) ? "+ " + c.part.plus : "";
            _statMinus.text = c.part != null && !string.IsNullOrEmpty(c.part.minus) ? "– " + c.part.minus : "";
            _hint.text = c.eyeToken > 0 ? Copy.WorkbenchEyesHint : Copy.WorkbenchHint;
        }

        // ---------------------------------------------------------------- drag & drop

        internal void Hover(Card c) => ShowStat(c);

        /// <summary>Click a card: it goes straight onto the creature (the first mark that accepts it).</summary>
        internal void Equip(Card c)
        {
            ShowStat(c);
            if (c.eyeToken > 0)
            {
                int mask = EyeMask();
                int bit = (mask & 2) == 0 ? 2 : (mask & 1) == 0 ? 1 : 4;
                if (c.eyeToken == 7) SetEyes("seven"); else AddEye(bit, false);
                return;
            }
            foreach (var s in _spots)
                if (s.accepts(c)) { Spark(s.ring.rectTransform.anchoredPosition, 12); s.drop(c); return; }
            if (c.part != null) Set(c.slot, c.part.id);
        }

        void RefreshSheet()
        {
            var cat = PartCatalog.I;
            if (cat == null || _app == null) return;
            cat.Resolve(_app, _angler ? Faction.Angler : Faction.Fish, _stats);
            var cfg = GameConfig.I;
            string[] names = _angler ? Copy.AnglerStats : Copy.FishStats;
            float[] vals = new float[4];
            if (!_angler)
            {
                vals[0] = _stats.TopSpeedMulCapped * Mathf.Sqrt(_stats.Mul(Stat.AccelMul));
                vals[1] = 1f / (Mathf.Max(0.3f, _stats.Mul(Stat.HitboxMul)) * Mathf.Max(0.3f, _stats.Mul(Stat.TwitchMul)) * Mathf.Max(0.3f, _stats.Mul(Stat.SonarOutlineMul)));
                vals[2] = _stats.Mul(Stat.StaminaMul) * (0.5f + 0.5f * _stats.Mul(Stat.StaminaRegenMul));
                vals[3] = (1f / Mathf.Max(0.3f, _stats.Mul(Stat.NibbleTimeMul))) * (1f + _stats.NibbleRangeBonusCapped);
                _sheetTitle.text = string.Format(Copy.SpecimenCard, Mathf.Abs((_name.text ?? "").GetHashCode()) % 900 + 100);
            }
            else
            {
                float line = _stats.Get(Stat.MaxLineSet, cfg.maxLineLength) / cfg.maxLineLength;
                vals[0] = line * _stats.Mul(Stat.HookFollowMul);
                vals[1] = _stats.Mul(Stat.ReelMul);
                vals[2] = (_stats.Get(Stat.SnapThresholdSet, cfg.snapThreshold) + _stats.Add(Stat.SnapThresholdAdd)) / cfg.snapThreshold * (_stats.Has(Ability.NeverSnaps) ? 1.6f : 1f);
                vals[3] = _stats.Mul(Stat.CuriosityMul) * (_stats.Get(Stat.CuriosityRangeSet, cfg.curiosityRange) / cfg.curiosityRange);
                _sheetTitle.text = string.Format(Copy.VowsCard, _name.text);
            }
            for (int i = 0; i < 4; i++)
            {
                _statName[i].text = names[i];
                float v = vals[i];
                _statFill[i].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v / 2f), 1);
                _statFill[i].color = v >= 1.005f ? UIKit.Hex("#3E8A55") : v <= 0.995f ? UIKit.Hex("#A8323A") : UIKit.Midwater;
                int pct = Mathf.RoundToInt((v - 1f) * 100f);
                _statVal[i].text = pct == 0 ? "avg" : (pct > 0 ? "+" : "") + pct + "%";
            }
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _app.Length; i++)
            {
                var slot = (Slot)i;
                if ((PartDef.FactionOf(slot) == Faction.Angler) != _angler) continue;
                var p = cat.Find(slot, _app[i]);
                if (p == null || p.cosmetic) continue;
                if (string.IsNullOrEmpty(p.plus) && string.IsNullOrEmpty(p.minus)) continue;
                sb.Append("<b>").Append(p.displayName).Append("</b>  ");
                if (!string.IsNullOrEmpty(p.plus)) sb.Append("<color=#2E7D4F>").Append(p.plus).Append("</color> ");
                if (!string.IsNullOrEmpty(p.minus)) sb.Append("<color=#A8323A>").Append(p.minus).Append("</color>");
                sb.Append("\n");
            }
            _traits.text = sb.ToString();
        }

        internal void BeginDrag(Card c, Vector2 screen)
        {
            _dragCard = c;
            _dragFrom = null;
            _ghost.sprite = c.isPaint ? UIKit.SealSprite : c.icon != null ? c.icon : UIKit.HookSprite;
            _ghost.color = c.isPaint ? c.swatch : Color.white;
            _ghost.enabled = true;
            ShowStat(c);
            MoveGhost(screen);
        }

        void BeginRemove(Spot s, Vector2 screen)
        {
            if (s.removable == null || !s.removable()) return;
            _dragFrom = s;
            _dragCard = null;
            _ghost.sprite = UIKit.RingSprite;
            _ghost.color = UIKit.HookRed;
            _ghost.enabled = true;
            MoveGhost(screen);
        }

        internal void Drag(Vector2 screen) => MoveGhost(screen);

        internal void EndDrag(Vector2 screen)
        {
            _ghost.enabled = false;
            var target = Nearest(screen, _dragCard);
            if (_dragCard != null && target != null)
            {
                Spark(target.ring.rectTransform.anchoredPosition, 12);
                target.drop(_dragCard);
            }
            else if (_dragFrom != null && (target == null || target != _dragFrom))
            {
                var from = _dragFrom;
                Spark(from.ring.rectTransform.anchoredPosition, 8);
                from.remove?.Invoke();
            }
            _dragCard = null;
            _dragFrom = null;
        }

        void MoveGhost(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
            _ghost.rectTransform.anchorMin = _ghost.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _ghost.rectTransform.anchoredPosition = local;
        }

        Spot Nearest(Vector2 screen, Card c)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, null, out var local)) return null;
            Spot best = null;
            float bestD = 95f;
            foreach (var s in _spots)
            {
                if (c != null && !s.accepts(c)) continue;
                if (c == null && s != _dragFrom) continue;
                float d = Vector2.Distance(local, s.ring.rectTransform.anchoredPosition);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        internal void Spin(float dx) { _spinVel = dx * 30f; }

        void Spark(Vector2 at, int n)
        {
            for (int i = 0; i < n; i++)
            {
                RectTransform rt = _sparkPool.Count > 0 ? _sparkPool.Pop() : UIKit.Image("Spark", _overlay, UIKit.RingSprite, UIKit.Raincoat).rectTransform;
                rt.gameObject.SetActive(true);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = Vector2.one * UnityEngine.Random.Range(10f, 22f);
                rt.anchoredPosition = at;
                float a = UnityEngine.Random.value * Mathf.PI * 2f;
                _sparks.Add((rt, new Vector2(Mathf.Cos(a), Mathf.Sin(a) + 0.6f) * UnityEngine.Random.Range(120f, 320f), 0.6f));
            }
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            if (_subject == null) return;
            float dt = Time.unscaledDeltaTime;
            _spinVel = Mathf.Lerp(_spinVel, 0f, dt * 3f);
            _yaw += _spinVel * dt;
            float idle = Mathf.Sin(Time.unscaledTime * 0.6f) * 12f;
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 2.5f);
            float bounce = 1f + Mathf.Sin(_punch * 14f) * 0.08f * _punch;
            _subject.localRotation = Quaternion.Euler(0f, _yaw + (_angler ? -25f : 0f) + idle, 0f);
            _subject.localScale = Vector3.one * bounce;
            _subject.localPosition = new Vector3(0f, Mathf.Sin(Time.unscaledTime * 1.3f) * 0.05f, 0f);
            if (_rig != null)
            {
                // the specimen breathes on the bench: a slow tail beat and fin flutter
                float ph = Time.unscaledTime * 4.2f;
                if (_rig.tail != null) _rig.tail.localRotation = Quaternion.AngleAxis(Mathf.Sin(ph) * 16f, _rig.tailAxis) * _rig.tailBase;
                float fin = Mathf.Sin(ph * 0.6f + 0.8f) * 14f;
                if (_rig.finL != null) _rig.finL.localRotation = Quaternion.AngleAxis(fin, _rig.finAxisL) * _rig.finLBase;
                if (_rig.finR != null) _rig.finR.localRotation = Quaternion.AngleAxis(-fin, _rig.finAxisR) * _rig.finRBase;
            }

            // project marks onto the preview; glow the ones that accept what you're dragging
            var rect = _overlay.rect;
            foreach (var s in _spots)
            {
                if (s.anchor == null) { s.ring.enabled = false; continue; }
                Vector3 vp = _cam.WorldToViewportPoint(s.anchor.position);
                s.ring.rectTransform.anchoredPosition = new Vector2((vp.x - 0.5f) * rect.width, (vp.y - 0.5f) * rect.height);
                bool valid = _dragCard != null && s.accepts(_dragCard);
                bool held = _dragFrom == s || (_dragCard == null && _dragFrom == null && s.removable != null && s.removable());
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
                Color c = valid ? new Color(UIKit.Mint.r, UIKit.Mint.g, UIKit.Mint.b, pulse) : held ? new Color(1f, 1f, 1f, 0.22f) : new Color(1, 1, 1, 0f);
                s.ring.color = c;
                s.ring.rectTransform.sizeDelta = Vector2.one * (valid ? 84f + 10f * pulse : 64f);
                s.ring.raycastTarget = s.removable != null;
            }

            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var (rt, v, life) = _sparks[i];
                life -= dt;
                if (life <= 0f) { rt.gameObject.SetActive(false); _sparkPool.Push(rt); _sparks.RemoveAt(i); continue; }
                rt.anchoredPosition += v * dt;
                v *= 0.92f;
                rt.GetComponent<Image>().color = new Color(UIKit.Raincoat.r, UIKit.Raincoat.g, UIKit.Raincoat.b, life / 0.6f);
                _sparks[i] = (rt, v, life);
            }
            if (_toastT > 0f) { _toastT -= dt; var c2 = _toast.color; c2.a = Mathf.Clamp01(_toastT * 2f); _toast.color = c2; }
        }

        // ---------------------------------------------------------------- small input components

        sealed class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerClickHandler
        {
            public Workbench wb;
            public Card card;
            public void OnPointerEnter(PointerEventData e) => wb.Hover(card);
            public void OnBeginDrag(PointerEventData e) => wb.BeginDrag(card, e.position);
            public void OnDrag(PointerEventData e) => wb.Drag(e.position);
            public void OnEndDrag(PointerEventData e) => wb.EndDrag(e.position);
            public void OnPointerClick(PointerEventData e) { if (!e.dragging) wb.Equip(card); }
        }

        sealed class SpotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public Workbench wb;
            public Spot spot;
            public void OnBeginDrag(PointerEventData e) => wb.BeginRemove(spot, e.position);
            public void OnDrag(PointerEventData e) => wb.Drag(e.position);
            public void OnEndDrag(PointerEventData e) => wb.EndDrag(e.position);
        }

        sealed class PreviewSpin : MonoBehaviour, IDragHandler
        {
            public Workbench wb;
            public void OnDrag(PointerEventData e) => wb.Spin(e.delta.x);
        }

        sealed class TabHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Text text;
            public void OnPointerEnter(PointerEventData e) { if (text) text.color = UIKit.HookRed; }
            public void OnPointerExit(PointerEventData e) { if (text) text.color = UIKit.Ink; }
        }
    }
}
