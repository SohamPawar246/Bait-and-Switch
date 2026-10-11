using BaitSwitch.Net;
using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// In-sea HUD (IMGUI, scaled to the screen): round clock and phase, a goal line that always says what you are for,
    /// the parchment role card at round start (hold H to bring it back), event toasts, the tug panel with a hint for
    /// each side, the blend meter, controls, and the Book of Tithes as a proper table. All text from Copy.
    /// Strings are cached; nothing is formatted per frame.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        public float toastSeconds = 3f;

        SeaClient _client;
        LocalPredictor _predictor;
        GameConfig _c;
        GUIStyle _title, _big, _mid, _small, _gold, _inkTitle, _ink, _inkSmall, _inkBold, _card;
        float _u = 1f;
        int _styledFor = -1;

        readonly string[] _toast = new string[4];
        readonly float[] _toastT = new float[4];
        int _lastClockSec = -1;
        float _tipT;
        int _tipIndex;
        Role _tipRole = Role.Spectator;
        string _clock = "", _topBar = "", _countdown = "", _appetite = "";
        int _topKey = -1, _countKey = -1, _cardRound = -1;
        float _switchedAt = -10f;
        Texture2D _band;
        static Texture2D _parch;

        // roster cache (rebuilt when a new roster arrives, once a second)
        string[] _rosterNames;
        string[] _rosterRows = new string[0];
        string _rosterTitle = "";
        Color[] _rosterColors = new Color[0];
        GUIStyle _rosterStyle, _tagStyle, _promptStyle;

        // ledger cache
        int _ledgerKey = -1, _ledgerBest, _ledgerYou = -1;
        string _ledgerTitle = "", _ledgerOutcome = "", _ledgerWinner = "", _ledgerFooter = "";
        string[] _ledgerCols = new string[0];
        string[] _ledgerRow = new string[0], _ledgerPts = new string[0], _ledgerCatch = new string[0], _ledgerEsc = new string[0];

        void Awake()
        {
            _client = GetComponent<SeaClient>();
            _predictor = GetComponent<LocalPredictor>();
        }

        void OnEnable() { if (_client != null) _client.EventReceived += OnEvent; }
        void OnDisable() { if (_client != null) _client.EventReceived -= OnEvent; }

        void OnEvent(EventMsg m)
        {
            int myBoat = _client.BoatId;
            switch (m.kind)
            {
                case EventKind.Switched: Toast(Copy.Saved); _switchedAt = Time.unscaledTime; break;
                case EventKind.Escaped: Toast(m.a > 0 ? Copy.SnapFish : Copy.Escaped); break;
                case EventKind.Snapped: if (m.a == myBoat) Toast(Copy.SnapAngler); break;
                case EventKind.TornLoose: if (m.a == myBoat) Toast(Copy.TornLoose); break;
                case EventKind.Tangled: if (m.a == myBoat) Toast(Copy.Tangled); break;
                case EventKind.Eaten: if (m.a == myBoat) Toast(Copy.BaitTaken); break;
                case EventKind.LeviathanRises: Toast(Copy.LeviathanRises); break;
                case EventKind.BoatDragged: Toast(Copy.BoatTaken); break;
                case EventKind.SomethingNewSwims: Toast(Copy.SomethingNew); break;
                case EventKind.SonarPing: Toast(Copy.SonarPing); break;
                case EventKind.PhaseChanged: Toast(m.a == (int)Phase.Dusk ? Copy.Dusk : m.a == (int)Phase.Night ? Copy.Night : null); break;
                case EventKind.RoundEnd: Toast(m.a == 1 ? Copy.FishWin : Copy.AnglersWin); break;
                case EventKind.Points: Toast(string.Format(Copy.HudPoints, m.a)); break;
                case EventKind.Rotation: Toast(Copy.Rotation); break;
                case EventKind.Reeled:
                    if (Time.unscaledTime - _switchedAt < 1f) break; // it was you: you already got "YOU HAVE BEEN SAVED."
                    Toast(m.b < 0xFF ? string.Format(Copy.FeedReeled, SeatName(m.a), SeatName(m.b)) : string.Format(Copy.FeedReeledAlone, SeatName(m.a)));
                    break;
            }
        }

        string SeatName(int seat)
        {
            var names = _client.HasRound ? _client.RoundInfo.seatNames : null;
            return names != null && seat >= 0 && seat < names.Length ? names[seat] : "?";
        }

        void Tips()
        {
            var role = _client.Role;
            if (role != _tipRole) { _tipRole = role; _tipIndex = 0; _tipT = 2f; }
            bool seen = role == Role.Fish ? UI.GameSettings.TipsFishSeen : role == Role.Angler ? UI.GameSettings.TipsAnglerSeen : true;
            if (seen) return;
            string[] tips = role == Role.Fish ? Copy.TipsFish : Copy.TipsAngler;
            _tipT -= Time.unscaledDeltaTime;
            if (_tipT > 0f) return;
            if (_tipIndex >= tips.Length)
            {
                if (role == Role.Fish) UI.GameSettings.TipsFishSeen = true; else UI.GameSettings.TipsAnglerSeen = true;
                return;
            }
            Toast(tips[_tipIndex++]);
            _tipT = 7f;
        }

        void Toast(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            int slot = 0;
            for (int i = 0; i < _toastT.Length; i++) if (_toastT[i] < _toastT[slot]) slot = i;
            _toast[slot] = text;
            _toastT[slot] = Time.unscaledTime + toastSeconds;
        }

        // ------------------------------------------------------------------ styles

        void Styles()
        {
            if (_styledFor == Screen.height && _title != null) return;
            _styledFor = Screen.height;
            _c = GameConfig.I;
            _u = Mathf.Max(0.6f, Screen.height / 1080f);
            var lib = ArtLibrary.I;
            Font disp = lib != null ? lib.display : null, body = lib != null ? lib.body : null, bold = lib != null && lib.bodyBold != null ? lib.bodyBold : body;
            GUIStyle S(Font f, float size, TextAnchor a, Color c, bool wrap = true)
            {
                var st = new GUIStyle(GUI.skin.label) { font = f, fontSize = Mathf.RoundToInt(size * _u), alignment = a, wordWrap = wrap, clipping = TextClipping.Overflow };
                st.normal.textColor = c;
                return st;
            }
            var cream = new Color(0.96f, 0.91f, 0.85f);
            var ink = new Color(0.12f, 0.08f, 0.16f);
            _title = S(disp, 64, TextAnchor.MiddleCenter, cream);
            _big = S(disp, 46, TextAnchor.MiddleCenter, cream);
            _mid = S(body, 30, TextAnchor.MiddleCenter, cream);
            _small = S(body, 23, TextAnchor.MiddleCenter, cream);
            _gold = S(body, 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.76f, 0.31f));
            _inkTitle = S(disp, 54, TextAnchor.MiddleLeft, ink);
            _ink = S(body, 27, TextAnchor.UpperLeft, ink);
            _inkBold = S(bold, 27, TextAnchor.UpperLeft, ink);
            _inkSmall = S(body, 22, TextAnchor.UpperLeft, new Color(0.25f, 0.18f, 0.28f));
            _rosterStyle = S(body, 21, TextAnchor.MiddleLeft, cream, false);
            _tagStyle = S(bold, 20, TextAnchor.MiddleCenter, cream, false);
            _promptStyle = S(bold, 24, TextAnchor.MiddleCenter, new Color(0.6f, 0.96f, 0.77f), false);
            _card = new GUIStyle(GUI.skin.box) { border = new RectOffset(6, 6, 6, 6) };
            _card.normal.background = Parchment();
        }

        void Shadowed(Rect r, string text, GUIStyle st, float alpha = 1f)
        {
            var c = st.normal.textColor;
            st.normal.textColor = new Color(0.03f, 0.02f, 0.05f, 0.7f * alpha);
            GUI.Label(new Rect(r.x + 2f * _u, r.y + 2f * _u, r.width, r.height), text, st);
            st.normal.textColor = new Color(c.r, c.g, c.b, c.a * alpha);
            GUI.Label(r, text, st);
            st.normal.textColor = c;
        }

        static Texture2D Band()
        {
            var t = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) { float k = y / 63f; t.SetPixel(0, y, new Color(0.02f, 0.02f, 0.05f, 0.6f * k * k)); }
            t.Apply();
            return t;
        }

        static Texture2D Parchment()
        {
            if (_parch != null) return _parch;
            _parch = new Texture2D(64, 64) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float e = Mathf.Min(Mathf.Min(x, 63 - x), Mathf.Min(y, 63 - y));
                    float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                    Color c = Color.Lerp(new Color(0.93f, 0.88f, 0.77f), new Color(0.8f, 0.71f, 0.56f), n * 0.5f);
                    if (e < 4) c = Color.Lerp(c, new Color(0.55f, 0.42f, 0.3f), (4 - e) / 5f);
                    if (e < 1) c = new Color(0.12f, 0.08f, 0.16f);
                    _parch.SetPixel(x, y, c);
                }
            _parch.Apply();
            return _parch;
        }

        // ------------------------------------------------------------------ draw

        void OnGUI()
        {
            if (_client == null || !Mirror.NetworkClient.active || Application.isBatchMode || !UI.AppFlow.SeaOnScreen) return;
            Styles();
            float w = Screen.width, h = Screen.height, u = _u;
            var m = _client.Match;
            var state = (MatchState)m.state;

            if (_band == null) _band = Band();
            GUI.DrawTexture(new Rect(0, 0, w, 130 * u), _band);
            GUI.DrawTextureWithTexCoords(new Rect(0, h - 140 * u, w, 140 * u), _band, new Rect(0, 1, 1, -1));

            if (state == MatchState.Waiting) { Shadowed(new Rect(0, 40 * u, w, 50 * u), Copy.HudWaiting, _mid); return; }

            bool lev = _client.Role == Role.Fish && _client.OwnIndex >= 0 && (_client.LatestFlags(_client.OwnIndex) & FishFlags.Leviathan) != 0;
            bool converting = _client.Role != Role.Fish && _client.Role != Role.Angler && _client.HasRound && _client.RoundInfo.role != Role.Spectator;

            // top bar + the goal line
            if (state == MatchState.Playing)
            {
                float t = _client.MatchTime;
                int left = Mathf.Max(0, Mathf.CeilToInt(_c.roundSeconds - t));
                if (left != _lastClockSec) { _lastClockSec = left; _clock = string.Format(Copy.HudClock, left / 60, left % 60); }
                int key = left * 4096 + m.phase * 256 + m.fishLeft * 4 + m.round;
                if (key != _topKey)
                {
                    _topKey = key;
                    string phase = m.phase == (byte)Phase.Night ? Copy.HudNight : m.phase == (byte)Phase.Dusk ? Copy.HudDusk : Copy.HudDay;
                    _topBar = $"{string.Format(Copy.HudRound, m.round, _c.roundsPerMatch)}   {_clock}   {phase}   {string.Format(Copy.FishLeft, m.fishLeft)}";
                }
                var keep = _mid.normal.textColor;
                if (left <= 10) _mid.normal.textColor = Color.Lerp(keep, new Color(1f, 0.42f, 0.36f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f));
                Shadowed(new Rect(0, 10 * u, w, 44 * u), _topBar, _mid);
                _mid.normal.textColor = keep;
                string goal = lev ? Copy.GoalShortLeviathan : _client.Role == Role.Fish ? Copy.GoalShortFish
                            : _client.Role == Role.Angler ? Copy.GoalShortAngler : converting ? Copy.GoalShortConverting : null;
                if (goal != null) Shadowed(new Rect(0, 54 * u, w, 34 * u), goal, _gold);
                Tips();
            }

            // bottom: who you are, how to move, where the rules live
            string role = _client.Role == Role.Fish ? Copy.HudYouFish : _client.Role == Role.Angler ? Copy.HudYouAngler
                        : converting ? Copy.HudConverting : Copy.HudSpectating;
            Shadowed(new Rect(0, h - 84 * u, w, 36 * u), role, _small);
            string controls = _client.Role == Role.Angler ? Copy.HudControlsAngler : lev ? Copy.HudControlsLeviathan : _client.Role == Role.Fish ? Copy.HudControlsFish : Copy.HoldForRules;
            Shadowed(new Rect(0, h - 50 * u, w, 36 * u), controls, _small);
            if (_client.Role == Role.Fish || _client.Role == Role.Angler)
                Shadowed(new Rect(w - 360 * u, h - 50 * u, 340 * u, 36 * u), Copy.HoldForRules, _small, 0.6f);

            // the role card: at round start, and whenever H is held
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool hold = kb != null && kb.hKey.isPressed;
            if (_client.HasRound && (state == MatchState.Intro || (hold && state == MatchState.Playing)))
                RoleCard(w, h, state == MatchState.Intro, lev);

            // blend meter (fish only)
            if (state == MatchState.Playing && _client.Role == Role.Fish && _predictor != null && _predictor.Active && !lev)
            {
                bool bad = _predictor.Dashing || _predictor.Speed > _c.schoolCruise * 1.25f;
                var prev = GUI.color;
                GUI.color = bad ? new Color(1f, 0.45f, 0.4f) : new Color(0.55f, 1f, 0.6f);
                var st = _small.alignment;
                _small.alignment = TextAnchor.MiddleLeft;
                Shadowed(new Rect(24 * u, h - 128 * u, 520 * u, 36 * u), bad ? Copy.HudBlendBad : Copy.HudBlendGood, _small);
                _small.alignment = st;
                GUI.color = prev;
            }

            // tug panel (participants only)
            if (_client.InTug)
            {
                var tug = _client.Tug;
                float pw = 560 * u, ph = 150 * u, px = w * 0.5f - pw * 0.5f, py = h * 0.66f;
                var prev = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.45f);
                GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);
                GUI.color = prev;
                Shadowed(new Rect(px, py + 6 * u, pw, 30 * u), Copy.TugTitle, _gold);
                float k = tug.tension / Mathf.Max(1f, (float)tug.threshold);
                Color tc = k < 0.5f ? Color.Lerp(new Color(0.95f, 0.95f, 0.9f), new Color(1f, 0.8f, 0.2f), k * 2f) : Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(1f, 0.25f, 0.2f), (k - 0.5f) * 2f);
                Bar(new Rect(px + 150 * u, py + 44 * u, pw - 180 * u, 22 * u), k, Copy.HudTension, tc);
                Bar(new Rect(px + 150 * u, py + 76 * u, pw - 180 * u, 22 * u), tug.stamina / 255f, Copy.HudStamina, new Color(0.4f, 0.8f, 1f));
                string hint = tug.exhausted ? Copy.HudExhausted : _client.Role == Role.Angler ? Copy.TugHintAngler : Copy.TugHintFish;
                Shadowed(new Rect(px, py + 106 * u, pw, 34 * u), hint, _small);
            }

            if (_client.HasRoster && (state == MatchState.Playing || state == MatchState.Intro || state == MatchState.Waiting))
            {
                RosterPanel(u);
                Nametags(u);
            }

            // toasts, under the goal line
            float ty = 96 * u;
            for (int i = 0; i < _toast.Length; i++)
            {
                if (Time.unscaledTime > _toastT[i] || _toast[i] == null) continue;
                float life = _toastT[i] - Time.unscaledTime, age = toastSeconds - life;
                float alpha = Mathf.Clamp01(life / 0.6f) * Mathf.Clamp01(age / 0.15f);
                Shadowed(new Rect(0, ty - (1f - Mathf.Clamp01(age / 0.2f)) * 10f * u, w, 40 * u), _toast[i], _mid, alpha);
                ty += 40 * u;
            }

            if ((state == MatchState.Ledger || state == MatchState.Final) && _client.HasLedger) DrawLedger(w, h, state == MatchState.Final);
        }

        void BuildRoster()
        {
            var r = _client.Roster;
            _rosterNames = r.names;
            int n = r.names.Length, humans = 0;
            _rosterRows = new string[n];
            _rosterColors = new Color[n];
            for (int i = 0; i < n; i++)
            {
                var role = (Role)r.roles[i];
                if (r.human[i]) humans++;
                string label = role == Role.Fish ? Copy.RoleFish : role == Role.Angler ? Copy.RoleAngler : Copy.RoleRobing;
                _rosterRows[i] = label + " · " + r.names[i] + (i == r.you ? Copy.RosterYou : r.human[i] ? "" : Copy.RosterBot) + "   " + r.points[i];
                Color c = role == Role.Fish ? new Color(0.6f, 0.96f, 0.77f) : role == Role.Angler ? new Color(0.95f, 0.76f, 0.31f) : new Color(0.78f, 0.6f, 1f);
                if (!r.human[i]) c.a = 0.55f;
                _rosterColors[i] = c;
            }
            _rosterTitle = string.Format(Copy.RosterTitle, humans, n);
        }

        /// <summary>Top-left: everyone in the sea, their side (fish / Angler / being robed), bots dimmed, you marked.</summary>
        void RosterPanel(float u)
        {
            if (!ReferenceEquals(_client.Roster.names, _rosterNames)) BuildRoster();
            int n = _rosterRows.Length;
            float x = 18 * u, y = 14 * u, w = 400 * u, row = 28 * u;
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.42f);
            GUI.DrawTexture(new Rect(x - 8 * u, y - 4 * u, w, row * (n + 1) + 12 * u), Texture2D.whiteTexture);
            GUI.color = prev;
            Shadowed(new Rect(x, y, w, row), _rosterTitle, _rosterStyle, 0.9f);
            for (int i = 0; i < n; i++)
            {
                var keep = _rosterStyle.normal.textColor;
                _rosterStyle.normal.textColor = _rosterColors[i];
                Shadowed(new Rect(x + 8 * u, y + row * (i + 1), w - 16 * u, row), _rosterRows[i], _rosterStyle);
                _rosterStyle.normal.textColor = keep;
            }
        }

        /// <summary>Teammate nametags: fish see fellow fish, Anglers see the other boats. Never the other side.</summary>
        void Nametags(float u)
        {
            var cam = Camera.main;
            if (cam == null || _c == null) return;
            var r = _client.Roster;
            bool meFish = _client.Role == Role.Fish, meAngler = _client.Role == Role.Angler;
            Vector2 own = _predictor != null && _predictor.Active ? _predictor.VisualPos : Vector2.zero;
            bool nearBait = false;
            for (int i = 0; i < r.names.Length; i++)
            {
                if (meFish && r.hook[i] >= 0 && _client.SampleHook(r.hook[i], out Vector2 hp, out HookState hs, out _)
                    && hs == HookState.Baited && (hp - own).sqrMagnitude < (_c.nibbleRange + 0.4f) * (_c.nibbleRange + 0.4f)) nearBait = true;
                if (i == r.you) continue;
                Vector3 world;
                Color col;
                if (meAngler && r.hook[i] >= 0 && _client.SampleHook(r.hook[i], out _, out _, out float bx))
                { world = new Vector3(bx, _c.surfaceY + 1.9f, 0f); col = new Color(0.95f, 0.76f, 0.31f); }
                else if (meFish && r.fish[i] >= 0 && r.fish[i] < _client.FishCount)
                {
                    _client.Sample(r.fish[i], out Vector2 fp, out _, out FishFlags ff);
                    if ((ff & FishFlags.Dead) != 0) continue;
                    world = new Vector3(fp.x, fp.y + 0.85f, 0f); col = new Color(0.6f, 0.96f, 0.77f);
                }
                else continue;
                Vector3 sp = cam.WorldToScreenPoint(world);
                if (sp.z <= 0f) continue;
                var keep = _tagStyle.normal.textColor;
                _tagStyle.normal.textColor = col;
                Shadowed(new Rect(sp.x - 160 * u, Screen.height - sp.y - 14 * u, 320 * u, 28 * u), r.names[i], _tagStyle);
                _tagStyle.normal.textColor = keep;
            }
            // a nudge for the verb fish players kept missing
            if (nearBait && (MatchState)_client.Match.state == MatchState.Playing)
            {
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(own.x, own.y - 0.9f, 0f));
                if (sp.z > 0f) Shadowed(new Rect(sp.x - 180 * u, Screen.height - sp.y - 14 * u, 360 * u, 30 * u), Copy.NibblePrompt, _promptStyle);
            }
        }

        void RoleCard(float w, float h, bool intro, bool lev)
        {
            float u = _u;
            var r = _client.RoundInfo;
            bool fish = r.role == Role.Fish;
            if (_cardRound != r.round * 4 + (int)r.role) { _cardRound = r.round * 4 + (int)r.role; _appetite = string.Format(Copy.Appetite, AppetiteName(r)); }
            float cw = Mathf.Min(w - 80 * u, 1040 * u), ch = 560 * u, cx = w * 0.5f - cw * 0.5f, cy = h * 0.5f - ch * 0.5f;
            GUI.Box(new Rect(cx, cy, cw, ch), GUIContent.none, _card);
            float x = cx + 48 * u, iw = cw - 96 * u, y = cy + 26 * u;
            GUI.Label(new Rect(x, y, iw, 70 * u), fish ? Copy.IntroFish : Copy.IntroAngler, _inkTitle);
            y += 82 * u;
            if (lev) { GUI.Label(new Rect(x, y, iw, 70 * u), Copy.GoalLeviathan, _inkBold); y += 74 * u; }
            string[] goals = fish ? Copy.GoalFish : Copy.GoalAngler;
            for (int i = 0; i < goals.Length; i++)
            {
                GUI.Label(new Rect(x, y, 40 * u, 60 * u), (i + 1) + ".", _inkBold);
                GUI.Label(new Rect(x + 40 * u, y, iw - 40 * u, 66 * u), goals[i], _ink);
                y += 70 * u;
            }
            y += 6 * u;
            GUI.Label(new Rect(x, y, iw, 34 * u), _appetite, _inkBold);
            y += 40 * u;
            GUI.Label(new Rect(x, y, iw, 34 * u), fish ? Copy.HudControlsFish : Copy.HudControlsAngler, _inkSmall);
            if (intro)
            {
                int left = Mathf.Max(1, Mathf.CeilToInt(_c.introSeconds - _client.MatchTime));
                if (left != _countKey) { _countKey = left; _countdown = string.Format(Copy.RoundBegins, left); }
                var a = _inkSmall.alignment;
                _inkSmall.alignment = TextAnchor.LowerRight;
                GUI.Label(new Rect(cx, cy + ch - 50 * u, cw - 40 * u, 34 * u), _countdown, _inkSmall);
                _inkSmall.alignment = a;
            }
        }

        void Bar(Rect r, float k, string label, Color c)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(k), r.height - 4), Texture2D.whiteTexture);
            GUI.color = prev;
            var a = _small.alignment;
            _small.alignment = TextAnchor.MiddleRight;
            Shadowed(new Rect(r.x - 140 * _u, r.y - 6 * _u, 128 * _u, r.height + 12 * _u), label, _small);
            _small.alignment = a;
        }

        void BuildLedger(bool final)
        {
            var l = _client.Ledger;
            int n = l.names != null ? l.names.Length : 0;
            _ledgerTitle = final ? Copy.LedgerFinal : string.Format(Copy.LedgerHeader, l.round);
            _ledgerOutcome = l.fishWon ? Copy.FishWin : Copy.AnglersWin;
            _ledgerRow = new string[n]; _ledgerPts = new string[n]; _ledgerCatch = new string[n]; _ledgerEsc = new string[n];
            _ledgerBest = 0; _ledgerYou = -1;
            string me = UI.GameSettings.Name;
            for (int i = 0; i < n; i++)
            {
                if (l.points[i] > l.points[_ledgerBest]) _ledgerBest = i;
                if (_ledgerYou < 0 && l.names[i] == me) _ledgerYou = i;
                string title = final && l.titles != null && i < l.titles.Length && !string.IsNullOrEmpty(l.titles[i]) ? "  — " + l.titles[i] : "";
                _ledgerRow[i] = l.names[i] + title;
                _ledgerPts[i] = l.points[i].ToString();
                _ledgerCatch[i] = l.catches[i].ToString();
                _ledgerEsc[i] = l.escapes[i].ToString();
            }
            _ledgerWinner = final && n > 0 ? string.Format(Copy.LedgerWinner, l.names[_ledgerBest]) : "";
            _ledgerFooter = Copy.LedgerFooters[l.round % Copy.LedgerFooters.Length];
            _ledgerCols = Copy.LedgerColumns.Split('·');
            for (int i = 0; i < _ledgerCols.Length; i++) _ledgerCols[i] = _ledgerCols[i].Trim();
        }

        void DrawLedger(float w, float h, bool final)
        {
            var l = _client.Ledger;
            int key = l.round * 2 + (final ? 1 : 0);
            if (key != _ledgerKey) { _ledgerKey = key; BuildLedger(final); }
            float u = _u;
            int n = _ledgerRow.Length;
            float cw = Mathf.Min(w - 80 * u, 980 * u), ch = (300 + n * 46 + (final ? 50 : 0)) * u, cx = w * 0.5f - cw * 0.5f, cy = Mathf.Max(20 * u, h * 0.5f - ch * 0.5f);
            GUI.Box(new Rect(cx, cy, cw, ch), GUIContent.none, _card);
            float x = cx + 48 * u, iw = cw - 96 * u, y = cy + 24 * u;
            GUI.Label(new Rect(x, y, iw, 64 * u), _ledgerTitle, _inkTitle);
            y += 66 * u;
            GUI.Label(new Rect(x, y, iw, 36 * u), _ledgerOutcome, _inkBold);
            y += 50 * u;
            float c1 = x + iw * 0.58f, c2 = x + iw * 0.72f, c3 = x + iw * 0.86f;
            var cols = _ledgerCols;
            if (cols.Length == 4)
            {
                GUI.Label(new Rect(x, y, iw * 0.5f, 30 * u), cols[0], _inkSmall);
                GUI.Label(new Rect(c1, y, iw * 0.14f, 30 * u), cols[1], _inkSmall);
                GUI.Label(new Rect(c2, y, iw * 0.14f, 30 * u), cols[2], _inkSmall);
                GUI.Label(new Rect(c3, y, iw * 0.14f, 30 * u), cols[3], _inkSmall);
            }
            y += 34 * u;
            for (int i = 0; i < n; i++)
            {
                if (i == _ledgerYou || (final && i == _ledgerBest))
                {
                    var prev = GUI.color;
                    GUI.color = i == _ledgerYou ? new Color(0.95f, 0.76f, 0.31f, 0.35f) : new Color(0.84f, 0.27f, 0.27f, 0.18f);
                    GUI.DrawTexture(new Rect(x - 10 * u, y - 4 * u, iw + 20 * u, 42 * u), Texture2D.whiteTexture);
                    GUI.color = prev;
                }
                var st = i == _ledgerYou ? _inkBold : _ink;
                GUI.Label(new Rect(x, y, iw * 0.56f, 40 * u), _ledgerRow[i], st);
                GUI.Label(new Rect(c1, y, iw * 0.14f, 40 * u), _ledgerPts[i], st);
                GUI.Label(new Rect(c2, y, iw * 0.14f, 40 * u), _ledgerCatch[i], st);
                GUI.Label(new Rect(c3, y, iw * 0.14f, 40 * u), _ledgerEsc[i], st);
                y += 46 * u;
            }
            if (final) { y += 8 * u; GUI.Label(new Rect(x, y, iw, 40 * u), _ledgerWinner, _inkBold); y += 44 * u; }
            y += 10 * u;
            GUI.Label(new Rect(x, y, iw, 60 * u), _ledgerFooter, _inkSmall);
        }

        static string AppetiteName(RoundStartMsg r)
        {
            var p = PartCatalog.I != null ? PartCatalog.I.Find((Slot)r.appetiteSlot, r.appetitePart) : null;
            return p != null ? p.displayName : ((Slot)r.appetiteSlot).ToString();
        }
    }
}
