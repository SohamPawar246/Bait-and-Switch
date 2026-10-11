using BaitSwitch.Net;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BaitSwitch.UI
{
    /// <summary>
    /// The whole front end: studio intro → dithered-sea main menu → (Workbench | Settings) → loading line → the sea,
    /// plus Pause and the software cursor. Every change goes through the dither dissolve. Built entirely in code.
    /// </summary>
    public sealed class AppFlow : MonoBehaviour
    {
        public MenuDiorama diorama;
        public VideoClip intro;
        public Camera gameCamera;

        enum S { Intro, Menu, Workbench, Loading, Game }

        S _s = S.Menu;
        Transform _ui;
        RawImage _sea;
        RectTransform _menu, _settings, _pause, _loading, _introRoot;
        Workbench _workbench;
        InputField _nameField;
        RulesBook _rules;
        Text _status, _loadText, _loadTip, _toast;
        RectTransform _line, _hook;
        RectTransform[] _marks = new RectTransform[4];
        Button _retry, _offline;
        float _lineDepth, _lineTarget, _toastT, _loadT;
        bool _settingsFromPause, _gotStage4;
        int _stage;
        EdgegapBroker _broker;
        NetworkManager _nm;
        VideoPlayer _video;
        RenderTexture _videoRt;
        Image _cursor;
        float _introStart;
        bool _introStarted, _awaitGesture;
        Text _gestureText, _skipHint;

        public static AppFlow I { get; private set; }
        public bool InGame => _s == S.Game;

        /// <summary>Navigation hooks for tests and tooling: "menu", "workbench", "settings", "pause", "host".</summary>
        public void Navigate(string where)
        {
            switch (where)
            {
                case "menu": Go(S.Menu); break;
                case "workbench": Go(S.Workbench); break;
                case "settings": OpenSettings(false); break;
                case "pause": _pause.gameObject.SetActive(true); break;
                case "host": _forceLocal = 1; Go(S.Loading); break;
                case "join": _forceLocal = 2; Go(S.Loading); break;
                case "rules": _rules.Show(); break;
            }
        }

        void Awake() { I = this; }

        /// <summary>True while the sea itself is on screen (not the menu, workbench or loading line).</summary>
        public static bool SeaOnScreen => I != null && I._s == S.Game;
        public static bool IntroPlaying => I != null && I._s == S.Intro;

        void Start()
        {
#if UNITY_SERVER
            if (diorama != null) diorama.gameObject.SetActive(false);
            enabled = false;
            return;
#else
            if (Application.isBatchMode) { if (diorama != null) diorama.gameObject.SetActive(false); enabled = false; return; }
#endif
            GameSettings.ApplyAll();
            Audio.AudioDirector.Ensure();
            _nm = NetworkManager.singleton;
            _broker = _nm != null ? _nm.GetComponent<EdgegapBroker>() : null;
            EnsureEventSystem();
            _ui = new GameObject("UI").transform;
            BuildBackground();
            BuildMenu();
            BuildSettings();
            BuildPause();
            BuildLoading();
            _workbench = Workbench.Create(UIKit.Canvas("Workbench", 30, _ui).transform, () => Go(S.Menu), DiveIn);
            _rules = RulesBook.Create(UIKit.Canvas("Rules", 70, _ui).transform);
            TransitionManager.Create(_ui);
            BuildCursor();
            if (_broker != null)
            {
                _broker.OnStage += (stage, text) => SetStage(stage, text);
                _broker.OnLocation += loc => { if (_status) _status.text = string.Format(Copy.BootStirs, loc); };
                _broker.OnSlow += () => { if (_loadText) _loadText.text = Copy.BootSlow; };
                _broker.OnError += err =>
                {
                    Debug.LogWarning("[Broker] " + err);
                    if (_loadText) _loadText.text = Copy.BootDeepSilent;
                    if (_retry) _retry.gameObject.SetActive(true);
                };
                _broker.OnConnected += () => SetStage(4, Copy.BootOpen);
            }

            if (NetworkClient.active || NetworkServer.active) { Enter(S.Game); return; }   // MPPM auto-start
            if (intro != null) ShowIntro();   // the studio intro plays on every launch (skippable)
            else { Enter(S.Menu); TransitionManager.I.Reveal(1f); }
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }

        bool BrokerConfigured => _broker != null && (_broker.useDirectAddress || !_broker.brokerUrl.Contains("YOURNAME"));

        // ------------------------------------------------------------------ building

        void BuildBackground()
        {
            var c = UIKit.Canvas("Sea", 0, _ui);
            _sea = UIKit.Stretch("DitheredSea", c.transform).gameObject.AddComponent<RawImage>();
            _sea.raycastTarget = false;
            if (diorama != null) diorama.Bind(_sea);
        }

        void BuildMenu()
        {
            var c = UIKit.Canvas("Menu", 10, _ui);
            _menu = UIKit.Stretch("MenuRoot", c.transform);

            // the calm left third: a darker band dithered with the same Bayer pattern
            var band = UIKit.Rect("Band", _menu, Vector2.zero, new Vector2(0.36f, 1f)).gameObject.AddComponent<RawImage>();
            band.raycastTarget = false;
            if (ArtLibrary.I != null && ArtLibrary.I.ditherFade != null)
            {
                band.material = new Material(ArtLibrary.I.ditherFade);
                band.material.SetColor("_Color", new Color(UIKit.Abyss.r * 0.6f, UIKit.Abyss.g * 0.6f, UIKit.Abyss.b * 0.6f, 1f));
                band.material.SetFloat("_Amount", 0.62f);
                band.material.SetFloat("_PixelSize", 3f);
            }
            else band.color = new Color(UIKit.Abyss.r, UIKit.Abyss.g, UIKit.Abyss.b, 0.6f);

            var logo = UIKit.Text("Logo", UIKit.Rect("LogoBox", _menu, new Vector2(0, 1), new Vector2(0.4f, 1), new Vector2(90, -330), new Vector2(0, -40)),
                                  "BAIT\n<color=#F2C14E>&</color> SWITCH", UIKit.Display, 132, UIKit.Cream, TextAnchor.UpperLeft);
            logo.supportRichText = true;
            logo.lineSpacing = 0.8f;
            UIKit.Shadow(logo, new Color(0, 0, 0, 0.6f), 4f);
            var tag = UIKit.Text("Tagline", UIKit.Rect("TagBox", _menu, new Vector2(0, 1), new Vector2(0.36f, 1), new Vector2(96, -405), new Vector2(-20, -330)),
                                 Copy.Tagline, UIKit.Body, 29, new Color(UIKit.Cream.r, UIKit.Cream.g, UIKit.Cream.b, 0.85f), TextAnchor.UpperLeft);
            UIKit.Shadow(tag, new Color(0, 0, 0, 0.5f));

            // your name in the Book (shown to everyone, fish or Angler)
            var nameBox = UIKit.Rect("NameBox", _menu, new Vector2(0, 1), new Vector2(0.36f, 1), new Vector2(110, -485), new Vector2(-70, -420));
            UIKit.Text("NameLabel", UIKit.Rect("NL", nameBox, new Vector2(0, 0.62f), new Vector2(1, 1.25f)), Copy.NameLabel, UIKit.Body, 24, new Color(1, 1, 1, 0.75f));
            var field = UIKit.Rect("Field", nameBox, new Vector2(0, 0), new Vector2(1, 0.62f));
            var fbg = field.gameObject.AddComponent<Image>();
            fbg.sprite = UIKit.RoundSprite; fbg.type = Image.Type.Sliced; fbg.color = new Color(0.96f, 0.91f, 0.85f, 0.14f);
            var ft = UIKit.Text("Text", field, "", UIKit.Display, 38, UIKit.Cream);
            ((RectTransform)ft.transform).offsetMin = new Vector2(16, 0);
            ft.supportRichText = false;
            _nameField = field.gameObject.AddComponent<InputField>();
            _nameField.textComponent = ft;
            _nameField.characterLimit = 18;
            _nameField.text = GameSettings.Name;
            _nameField.onEndEdit.AddListener(v => GameSettings.Name = v);

            var stack = UIKit.Rect("Buttons", _menu, new Vector2(0, 1), new Vector2(0.36f, 1), new Vector2(110, -1000), new Vector2(0, -505));
            float y = 0;
            void Add(string label, int size, System.Action a)
            {
                var b = UIKit.MenuButton(stack, label, size, a);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(0, -y);
                y += size * 1.5f;
            }
            Add(Copy.MenuDive, 64, DiveIn);
            Add(Copy.MenuWorkbench, 48, () => Go(S.Workbench));
            Add(Copy.MenuHowTo, 48, () => _rules.Show());
            Add(Copy.MenuSettings, 48, () => OpenSettings(false));
#if !UNITY_WEBGL || UNITY_EDITOR
            Add(Copy.MenuQuit, 48, Quit);
            // offline / LAN fallback, always offered so the game stays playable if the Deep is unreachable
            y += 12;
            Add(Copy.HostLocal, 28, () => { _forceLocal = 1; Go(S.Loading); });
            Add(Copy.JoinLocal, 28, () => { _forceLocal = 2; Go(S.Loading); });
#endif

            _status = UIKit.Text("Status", UIKit.Rect("StatusBox", _menu, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(96, 26), new Vector2(0, 70)),
                                 BrokerConfigured ? Copy.StatusBroker : Copy.StatusLocal, UIKit.Body, 24, new Color(1, 1, 1, 0.7f), TextAnchor.LowerLeft);
            UIKit.Text("Credit", UIKit.Rect("CreditBox", _menu, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(0, 26), new Vector2(-40, 70)),
                       Copy.JamCredit, UIKit.Body, 24, new Color(1, 1, 1, 0.7f), TextAnchor.LowerRight);
            _toast = UIKit.Text("Toast", UIKit.Rect("ToastBox", _menu, new Vector2(0.36f, 0.04f), new Vector2(1, 0.14f)), "", UIKit.Display, 40, UIKit.Cream, TextAnchor.MiddleCenter);
            UIKit.Shadow(_toast, new Color(0, 0, 0, 0.7f));
        }

        int _forceLocal; // 0 auto, 1 host, 2 join

        RectTransform Card(string name, Transform parent, Vector2 size, string title)
        {
            var root = UIKit.Stretch(name, parent);
            var dim = UIKit.Image("Dim", root, UIKit.White, new Color(0, 0, 0, 0.45f), true);
            var card = UIKit.Rect("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            card.sizeDelta = size;
            var bg = card.gameObject.AddComponent<Image>();
            bg.sprite = UIKit.ParchmentSprite;
            bg.type = Image.Type.Sliced;
            var seal = UIKit.Image("Seal", card, UIKit.SealSprite, Color.white);
            var sr = (RectTransform)seal.transform;
            sr.anchorMin = sr.anchorMax = new Vector2(1, 1);
            sr.sizeDelta = new Vector2(110, 110);
            sr.anchoredPosition = new Vector2(-40, -36);
            sr.localRotation = Quaternion.Euler(0, 0, -14);
            var t = UIKit.Text("Title", UIKit.Rect("TitleBox", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -120), new Vector2(-150, -30)),
                               title, UIKit.Display, 64, UIKit.Ink, TextAnchor.MiddleLeft);
            root.gameObject.SetActive(false);
            return card;
        }

        void BuildSettings()
        {
            var c = UIKit.Canvas("Settings", 60, _ui);
            var card = Card("SettingsRoot", c.transform, new Vector2(900, 760), Copy.SettingsTitle);
            _settings = (RectTransform)card.parent;
            var stack = UIKit.VerticalStack("Rows", UIKit.Rect("RowsBox", card, Vector2.zero, Vector2.one, new Vector2(60, 130), new Vector2(-60, -140)), 12);
            UIKit.Slider(stack, Copy.Master, GameSettings.Master, v => GameSettings.Master = v);
            UIKit.Slider(stack, Copy.MusicVol, GameSettings.Music, v => GameSettings.Music = v);
            UIKit.Slider(stack, Copy.EffectsVol, GameSettings.Sfx, v => GameSettings.Sfx = v);
            UIKit.Toggle(stack, Copy.SwimMouse, GameSettings.SwimWithMouse, v => GameSettings.SwimWithMouse = v);
            UIKit.Toggle(stack, Copy.FullscreenOpt, GameSettings.Fullscreen, v => GameSettings.Fullscreen = v);
            UIKit.Toggle(stack, Copy.SystemCursorOpt, GameSettings.SystemCursor, v => GameSettings.SystemCursor = v);
            var back = UIKit.CardButton(card, Copy.Back, 40, CloseSettings, new Vector2(260, 80));
            var br = (RectTransform)back.transform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0);
            br.anchoredPosition = new Vector2(0, 70);
        }

        void BuildPause()
        {
            var c = UIKit.Canvas("Pause", 50, _ui);
            var card = Card("PauseRoot", c.transform, new Vector2(820, 690), Copy.PauseTitle);
            _pause = (RectTransform)card.parent;
            ((RectTransform)card.Find("TitleBox")).offsetMin = new Vector2(50, -170);
            card.Find("TitleBox").GetComponentInChildren<Text>().fontSize = 54;
            UIKit.Text("Hint", UIKit.Rect("HintBox", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(54, -230), new Vector2(-50, -170)),
                       Copy.PauseHint, UIKit.Body, 28, new Color(UIKit.Ink.r, UIKit.Ink.g, UIKit.Ink.b, 0.75f));
            float y = -290;
            foreach (var (label, act) in new (string, System.Action)[] { (Copy.Resume, ClosePause), (Copy.MenuHowTo, () => _rules.Show()), (Copy.MenuSettings, () => OpenSettings(true)), (Copy.LeaveMatch, Leave) })
            {
                var b = UIKit.CardButton(card, label, 40, act, new Vector2(560, 78));
                var r = (RectTransform)b.transform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1);
                r.anchoredPosition = new Vector2(0, y);
                y -= 92;
            }
        }

        void BuildLoading()
        {
            var c = UIKit.Canvas("Loading", 40, _ui);
            _loading = UIKit.Stretch("LoadingRoot", c.transform);
            // the line goes down from the top of the screen through four depth marks
            var col = UIKit.Rect("Column", _loading, new Vector2(0.62f, 0.12f), new Vector2(0.62f, 1f));
            col.sizeDelta = new Vector2(4, 0);
            _line = (RectTransform)UIKit.Image("Line", col, UIKit.White, new Color(UIKit.Cream.r, UIKit.Cream.g, UIKit.Cream.b, 0.85f)).transform;
            _line.anchorMin = new Vector2(0, 1); _line.anchorMax = new Vector2(1, 1); _line.pivot = new Vector2(0.5f, 1);
            _hook = (RectTransform)UIKit.Image("Hook", col, UIKit.HookSprite, UIKit.Cream).transform;
            _hook.anchorMin = _hook.anchorMax = new Vector2(0.5f, 1);
            _hook.sizeDelta = new Vector2(40, 60);
            _hook.pivot = new Vector2(0.62f, 0.95f);
            string[] labels = { Copy.BootCalling, string.Format(Copy.BootStirs, Copy.BootSomewhere), Copy.BootLanterns, Copy.BootOpen };
            for (int i = 0; i < 4; i++)
            {
                var m = UIKit.Rect($"Mark{i}", col, new Vector2(0.5f, 1f - (i + 1) * 0.2f), new Vector2(0.5f, 1f - (i + 1) * 0.2f));
                m.sizeDelta = new Vector2(46, 4);
                m.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.35f);
                var lt = UIKit.Text("Label", UIKit.Rect("LabelBox", m, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(30, -24), new Vector2(560, 24)),
                                    labels[i], UIKit.Body, 28, new Color(1, 1, 1, 0.4f));
                UIKit.Shadow(lt, new Color(0, 0, 0, 0.6f));
                _marks[i] = m;
            }
            _loadText = UIKit.Text("Stage", UIKit.Rect("StageBox", _loading, new Vector2(0.05f, 0.42f), new Vector2(0.55f, 0.56f)), Copy.BootCalling, UIKit.Display, 64, UIKit.Cream);
            UIKit.Shadow(_loadText, new Color(0, 0, 0, 0.7f), 3);
            _loadTip = UIKit.Text("Tip", UIKit.Rect("TipBox", _loading, new Vector2(0.05f, 0.3f), new Vector2(0.55f, 0.4f)), "", UIKit.Body, 32, new Color(1, 1, 1, 0.8f));
            UIKit.Shadow(_loadTip, new Color(0, 0, 0, 0.6f));
            var cancel = UIKit.MenuButton(UIKit.Rect("CancelBox", _loading, new Vector2(0.05f, 0.12f), new Vector2(0.4f, 0.2f)), Copy.Cancel, 44, () => { StopNet(); Go(S.Menu); });
            _retry = UIKit.MenuButton(UIKit.Rect("RetryBox", _loading, new Vector2(0.05f, 0.2f), new Vector2(0.22f, 0.28f)), Copy.Retry, 44, StartConnect);
            _retry.gameObject.SetActive(false);
            // if the Deep never answers, fish offline with the bots instead of staring at a dead line
            _offline = UIKit.MenuButton(UIKit.Rect("OfflineBox", _retry.transform, new Vector2(0f, 1.05f), new Vector2(2.4f, 2.05f)), Copy.PlayOffline, 44,
                                        () => { StopNet(); _forceLocal = 1; StartConnect(); });
#if UNITY_WEBGL && !UNITY_EDITOR
            _offline.gameObject.SetActive(false);   // a browser can't host a sea
#endif
            _loading.gameObject.SetActive(false);
        }

        void BuildCursor()
        {
            var c = UIKit.Canvas("Cursor", 1000, _ui);
            _cursor = UIKit.Image("Cursor", c.transform, UIKit.HookSprite, UIKit.Cream);
            var r = _cursor.rectTransform;
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.sizeDelta = new Vector2(30, 45);
            r.pivot = new Vector2(0.2f, 0.32f); // hotspot near the point
        }

        void ShowIntro()
        {
            _s = S.Intro;
            var c = UIKit.Canvas("Intro", 400, _ui);
            _introRoot = UIKit.Stretch("IntroRoot", c.transform);
            UIKit.Image("Black", _introRoot, UIKit.White, Color.black, true);
            var img = UIKit.Stretch("Video", _introRoot).gameObject.AddComponent<RawImage>();
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = intro.width > 0 && intro.height > 0 ? (float)intro.width / intro.height : 16f / 9f;
            _videoRt = new RenderTexture((int)Mathf.Max(16, intro.width), (int)Mathf.Max(16, intro.height), 0);
            img.texture = _videoRt;
            var skip = UIKit.Text("Skip", UIKit.Rect("SkipBox", _introRoot, new Vector2(0.7f, 0.02f), new Vector2(0.98f, 0.08f)), Copy.SkipHint, UIKit.Body, 24, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleRight);
            UIKit.Shadow(skip, new Color(0, 0, 0, 0.75f), 2f);   // readable over a white frame too
            _video = _introRoot.gameObject.AddComponent<VideoPlayer>();
            _video.playOnAwake = false;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _videoRt;
#if UNITY_WEBGL && !UNITY_EDITOR
            _video.source = VideoSource.Url;     // WebGL can't play embedded clips: stream it from StreamingAssets
            _video.url = System.IO.Path.Combine(Application.streamingAssetsPath, "intro.mp4");
#else
            _video.clip = intro;
#endif
            _video.audioOutputMode = VideoAudioOutputMode.Direct;
            _video.waitForFirstFrame = true;
            _video.skipOnDrop = true;
            _video.loopPointReached += _ => EndIntro();
            _video.errorReceived += (_, msg) => { Debug.LogWarning("[Intro] " + msg); EndIntro(); };
            // prepare first (decoder spin-up can take a moment), then play from frame one
            _video.prepareCompleted += v => { _introStarted = true; _introStart = Time.unscaledTime; v.Play(); };
#if UNITY_WEBGL && !UNITY_EDITOR
            // browsers only play video (and any audio) after a click: ask for one, then start the intro
            _awaitGesture = true;
            _gestureText = UIKit.Text("Begin", UIKit.Stretch("BeginBox", _introRoot), Copy.ClickToBegin, UIKit.Display, 64, UIKit.Cream, TextAnchor.MiddleCenter);
            UIKit.Shadow(_gestureText, new Color(0, 0, 0, 0.75f), 3f);
            skip.enabled = false;
            _skipHint = skip;
#else
            _video.Prepare();
#endif
            _introStarted = false;
            _introStart = Time.unscaledTime;
            Enter(S.Menu);           // the menu sits behind the video, ready
            _s = S.Intro;
        }

        void EndIntro()
        {
            if (_introRoot == null) return;
            GameSettings.IntroSeen = true;
            var root = _introRoot;
            _introRoot = null;
            TransitionManager.I.Play(() =>
            {
                if (_video != null) _video.Stop();
                Destroy(root.parent.gameObject);
                if (_videoRt != null) _videoRt.Release();
                _s = S.Menu;
            }, 0.5f);
        }

        // ------------------------------------------------------------------ navigation

        void Go(S s)
        {
            if (TransitionManager.I != null && TransitionManager.I.Busy) return;
            TransitionManager.I.Play(() => Enter(s));
        }

        void Enter(S s)
        {
            _s = s;
            bool menuish = s == S.Menu || s == S.Workbench || s == S.Loading;
            if (diorama != null) diorama.SetActive(s == S.Menu || s == S.Loading);
            if (_sea != null) _sea.enabled = s == S.Menu || s == S.Loading;
            if (gameCamera != null) gameCamera.enabled = s == S.Game;
            _menu.gameObject.SetActive(s == S.Menu);
            if (s == S.Menu && _nameField != null) _nameField.text = GameSettings.Name;
            _loading.gameObject.SetActive(s == S.Loading);
            _workbench.SetOpen(s == S.Workbench);
            _settings.gameObject.SetActive(false);
            _pause.gameObject.SetActive(false);
            if (s == S.Loading) StartConnect();
            if (!menuish) _forceLocal = 0;
        }

        void OpenSettings(bool fromPause)
        {
            _settingsFromPause = fromPause;
            _pause.gameObject.SetActive(false);
            _settings.gameObject.SetActive(true);
        }

        void CloseSettings()
        {
            _settings.gameObject.SetActive(false);
            if (_settingsFromPause && _s == S.Game) _pause.gameObject.SetActive(true);
        }

        void ClosePause() => _pause.gameObject.SetActive(false);

        void Leave()
        {
            _pause.gameObject.SetActive(false);
            TransitionManager.I.Play(() => { StopNet(); Enter(S.Menu); });
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>First dive ever: read the Book of Rites first (its last page dives in).</summary>
        void DiveIn()
        {
            Audio.AudioDirector.Ui("ui_confirm");
            if (_nameField != null) GameSettings.Name = _nameField.text;
            if (!GameSettings.RulesSeen && _rules != null) { _rules.Show(() => Go(S.Loading)); return; }
            Go(S.Loading);
        }

        // ------------------------------------------------------------------ connecting

        void StartConnect()
        {
            _retry.gameObject.SetActive(false);
            _gotStage4 = false;
            _loadT = 0f;
            _stage = 0;
            _lineDepth = 0f;
            SetStage(1, Copy.BootCalling);
            _loadTip.text = Copy.Tips[Random.Range(0, Copy.Tips.Length)];
            if (_nm == null) { _loadText.text = Copy.BootBrokerDown; _retry.gameObject.SetActive(true); return; }
            if (NetworkClient.active || NetworkServer.active) StopNet();
#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(WebConnect());   // the browser build joins the very server that served this page
            return;
#endif
            if (_forceLocal == 0 && BrokerConfigured) { _broker.Play(); return; }
            bool host = _forceLocal == 1 || (_forceLocal == 0 && DevHud.PortFree(_nm));
            _nm.networkAddress = "localhost";
            if (host) _nm.StartHost(); else _nm.StartClient();
            SetStage(3, Copy.BootLanterns);
        }

        /// <summary>
        /// Browser play: this page came from an Edgegap server (https://fqdn:webport/). Connect back to the same host over
        /// WSS on its "ws" port, given in the URL (?ws=, from the broker's /play redirect) or by the server's /ports.json.
        /// </summary>
        System.Collections.IEnumerator WebConnect()
        {
            SetStage(2, Copy.BootLanterns);
            System.Uri page;
            try { page = new System.Uri(Application.absoluteURL); }
            catch { page = new System.Uri("https://localhost/"); }
            string ws = null;
            foreach (var kv in page.Query.TrimStart('?').Split('&'))
            {
                var p = kv.Split('=');
                if (p.Length == 2 && p[0] == "ws" && p[1].Length > 0) ws = p[1];
            }
            if (string.IsNullOrEmpty(ws))
            {
                using (var req = UnityEngine.Networking.UnityWebRequest.Get(new System.Uri(page, "ports.json")))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success && req.downloadHandler.text.Contains("\"ws\""))
                    {
                        var digits = new System.Text.StringBuilder();
                        foreach (char c in req.downloadHandler.text) if (char.IsDigit(c)) digits.Append(c);
                        ws = digits.ToString();
                    }
                }
            }
            string host = page.Host;
            if (string.IsNullOrEmpty(ws) && _broker != null && !string.IsNullOrEmpty(_broker.brokerUrl))
            {
                // not served by an Edgegap server (itch.io): ask the broker for a sea, wait for it, use its wss port
                string b = _broker.brokerUrl.TrimEnd('/');
                using (var join = new UnityEngine.Networking.UnityWebRequest(b + "/join", "POST"))
                {
                    join.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));
                    join.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    join.SetRequestHeader("Content-Type", "application/json");
                    yield return join.SendWebRequest();
                }
                float t0 = Time.unscaledTime;
                while (Time.unscaledTime - t0 < 180f && _s == S.Loading)
                {
                    using (var st = UnityEngine.Networking.UnityWebRequest.Get(b + "/status"))
                    {
                        yield return st.SendWebRequest();
                        if (st.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        {
                            var txt = st.downloadHandler.text;
                            if (txt.Contains("\"state\":\"ready\""))
                            {
                                host = JsonField(txt, "host");
                                ws = JsonField(txt, "wsPort");
                                if (!string.IsNullOrEmpty(ws) && ws != "null") break;
                                using (var again = new UnityEngine.Networking.UnityWebRequest(b + "/join", "POST"))   // fills the browser ports
                                {
                                    again.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));
                                    again.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                                    again.SetRequestHeader("Content-Type", "application/json");
                                    yield return again.SendWebRequest();
                                }
                            }
                            else if (txt.Contains("\"state\":\"error\"")) break;
                            else SetStage(2, Copy.BootLanterns);
                        }
                    }
                    yield return new WaitForSecondsRealtime(2f);
                }
                page = new System.Uri("https://" + host + "/");
            }
            if (!ushort.TryParse(ws, out ushort port) || _nm == null)
            {
                Debug.LogWarning("[Web] no websocket port for " + page);
                _loadText.text = Copy.BootDeepSilent;
                _retry.gameObject.SetActive(true);
                yield break;
            }
            var sws = _nm.GetComponent<Mirror.SimpleWeb.SimpleWebTransport>();
            if (sws != null)
            {
                sws.clientUseWss = page.Scheme == "https";
                sws.clientWebsocketSettings = new Mirror.SimpleWeb.ClientWebsocketSettings
                {
                    ClientPortOption = Mirror.SimpleWeb.WebsocketPortOption.SpecifyPort,
                    CustomClientPort = port,
                };
            }
            _nm.networkAddress = page.Host;
            _nm.StartClient();
            SetStage(3, Copy.BootLanterns);
        }

        /// <summary>Tiny JSON reader for the broker's flat replies: the value of "key" as text (no quotes).</summary>
        static string JsonField(string json, string key)
        {
            int k = json.IndexOf("\"" + key + "\"", System.StringComparison.Ordinal);
            if (k < 0) return "";
            int c = json.IndexOf(':', k);
            if (c < 0) return "";
            int i = c + 1;
            while (i < json.Length && (json[i] == ' ' || json[i] == '"')) i++;
            int j = i;
            while (j < json.Length && json[j] != '"' && json[j] != ',' && json[j] != '}') j++;
            return json.Substring(i, j - i).Trim();
        }

        void StopNet()
        {
            if (_broker != null && _broker.Busy) _broker.Cancel();
            if (_nm == null) return;
            if (NetworkServer.active && NetworkClient.active) _nm.StopHost();
            else if (NetworkServer.active) _nm.StopServer();
            else if (NetworkClient.active) _nm.StopClient();
        }

        void SetStage(int stage, string text)
        {
            _stage = Mathf.Max(_stage, stage);
            if (_loadText != null) _loadText.text = text;
            _lineTarget = Mathf.Clamp01(_stage * 0.2f);
            for (int i = 0; i < 4; i++)
            {
                var lt = _marks[i].GetComponentInChildren<Text>();
                if (i == 1 && stage == 2) lt.text = text;
                lt.color = new Color(1, 1, 1, i < _stage ? 0.95f : 0.35f);
            }
            if (stage >= 4) _gotStage4 = true;
        }

        public void Toast(string text, float seconds = 2.5f)
        {
            if (_toast == null) return;
            _toast.text = text;
            _toastT = seconds;
        }

        // ------------------------------------------------------------------ per frame

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (_s == S.Intro)
            {
                bool skip = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (kb != null && kb.anyKey.wasPressedThisFrame);
                if (_awaitGesture)
                {
                    // the click that unlocks browser media starts the intro instead of skipping it
                    if (skip && _video != null)
                    {
                        _awaitGesture = false;
                        if (_gestureText != null) _gestureText.enabled = false;
                        if (_skipHint != null) _skipHint.enabled = true;
                        _video.Prepare();
                        _introStart = Time.unscaledTime;
                    }
                    skip = false;
                }
                if (skip && _video != null && (_video.time > 0.4f || Time.unscaledTime - _introStart > 1f)) EndIntro();
                // never let a codec problem trap the player: not ready within 8 s, or stalled after starting → the menu
                if (_video != null && !_awaitGesture && !_introStarted && Time.unscaledTime - _introStart > 8f) EndIntro();
                else if (_video != null && _introStarted && !_video.isPlaying && Time.unscaledTime - _introStart > 2f) EndIntro();
            }

            if (_s == S.Loading)
            {
                _loadT += Time.unscaledDeltaTime;
                if (NetworkClient.isConnected && !_gotStage4) SetStage(4, Copy.BootOpen);
                if (_gotStage4 && NetworkClient.isConnected && _lineDepth > 0.79f && !TransitionManager.I.Busy) Go(S.Game);
                if (!BrokerConfigured && _loadT > 12f && !NetworkClient.isConnected && !_retry.gameObject.activeSelf)
                {
                    _loadText.text = Copy.BootTimeout;
                    _retry.gameObject.SetActive(true);
                }
                // the hook sinks slowly between marks so it never looks frozen
                _lineDepth = Mathf.MoveTowards(_lineDepth, Mathf.Min(_lineTarget, 0.8f) + (_stage < 4 ? 0.05f * Mathf.Sin(Time.unscaledTime) : 0f), Time.unscaledDeltaTime * 0.35f);
                float h = ((RectTransform)_line.parent).rect.height * _lineDepth;
                _line.sizeDelta = new Vector2(0, h);
                _hook.anchoredPosition = new Vector2(0, -h);
                _hook.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.7f) * 6f);
            }

            if (_s == S.Game)
            {
                if (!NetworkClient.active && !NetworkServer.active && !TransitionManager.I.Busy)
                {
                    TransitionManager.I.Play(() => { Enter(S.Menu); Toast(Copy.Disconnected); });
                }
                else if (kb != null && kb.escapeKey.wasPressedThisFrame)
                {
                    if (_settings.gameObject.activeSelf) CloseSettings();
                    else _pause.gameObject.SetActive(!_pause.gameObject.activeSelf);
                }
            }
            else if (_rules != null && _rules.Open) { }
            else if (kb != null && kb.escapeKey.wasPressedThisFrame && _settings.gameObject.activeSelf) CloseSettings();
            else if (kb != null && kb.escapeKey.wasPressedThisFrame && _s == S.Workbench) Go(S.Menu);

            if (_toastT > 0f)
            {
                _toastT -= Time.unscaledDeltaTime;
                var c = _toast.color; c.a = Mathf.Clamp01(_toastT * 2f); _toast.color = c;
            }
            UpdateCursor(mouse);
        }

        void UpdateCursor(Mouse mouse)
        {
            if (_cursor == null || mouse == null) return;
            bool overlay = _s != S.Game || _pause.gameObject.activeSelf || _settings.gameObject.activeSelf;
            var client = _nm != null ? _nm.GetComponent<SeaClient>() : null;
            bool angler = client != null && client.Role == Role.Angler;
            bool fishMouse = client != null && client.Role == Role.Fish && GameSettings.SwimWithMouse;
            // hardware cursors: no lag, exact click point (the old UI-drawn hook trailed the mouse and swayed off target)
            _cursor.enabled = false;
            if (GameSettings.SystemCursor) CursorKit.Set(overlay || angler || fishMouse ? CursorKit.Mode.System : CursorKit.Mode.Hidden);
            else if (overlay) CursorKit.Set(CursorKit.Mode.Hook);
            else if (angler) CursorKit.Set(CursorKit.Mode.RingGold);
            else if (fishMouse) CursorKit.Set(CursorKit.Mode.RingMint);
            else CursorKit.Set(CursorKit.Mode.Hidden);
        }
    }
}
