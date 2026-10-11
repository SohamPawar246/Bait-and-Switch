using System.Collections.Generic;
using BaitSwitch.Net;
using BaitSwitch.UI;
using BaitSwitch.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaitSwitch.Audio
{
    /// <summary>
    /// All sound: music per light phase (Day = music box, Dusk = the same tune slower and a semitone flat, Night = drone,
    /// choir and far bells), the Angler's tension layer while the hook is down, the tug whine and reel clicks, ambience,
    /// UI ticks and every game event. Clips are synthesized by tools/audio/synth.py into Resources/Audio.
    /// SFX are 2D, panned by screen position and quieter off-screen. Never runs on a headless server.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        public static AudioDirector I { get; private set; }

        [Header("Mix")]
        public float musicLevel = 0.55f;
        public float ambienceLevel = 0.35f;
        public float tensionLevel = 0.5f;
        public float crossfade = 2.5f;
        public float duskPitch = 0.944f;   // a semitone flat (2^(-1/12)) and as much slower

        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        AudioSource[] _sfx;
        AudioSource[] _music;
        AudioSource _amb, _tension, _whine;
        int _cur;
        string _musicKey = "";
        float _duck = 1f, _duckT;
        float _tensionVol, _whineVol, _reelT, _switchedAt = -10f;
        bool _wasDashing;
        int _lastState = -1;
        SeaClient _client;
        LocalPredictor _pred;

        public static void Ensure()
        {
            if (I != null || Application.isBatchMode) return;
            var go = new GameObject("AudioDirector");
            DontDestroyOnLoad(go);
            go.AddComponent<AudioDirector>();
        }

        void Awake()
        {
            I = this;
            foreach (var c in Resources.LoadAll<AudioClip>("Audio")) _clips[c.name] = c;
            if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            _sfx = new AudioSource[18];
            for (int i = 0; i < _sfx.Length; i++) _sfx[i] = Source(false);
            _music = new[] { Source(true), Source(true) };
            _amb = Source(true);
            _tension = Source(true);
            _whine = Source(true);
            Loop(_tension, "tension");
            Loop(_whine, "whine_loop");
        }

        AudioSource Source(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        void Loop(AudioSource s, string clip)
        {
            if (!_clips.TryGetValue(clip, out var c)) return;
            s.clip = c;
            s.volume = 0f;
            s.Play();
        }

        // ------------------------------------------------------------------ one-shots

        public static void Ui(string clip, float vol = 1f) { if (I != null) I.Play2D(clip, vol * 0.7f, 0f, 1f + Random.Range(-0.03f, 0.03f)); }
        public static void Play(string clip, float vol = 1f, float pitch = 1f) { if (I != null) I.Play2D(clip, vol, 0f, pitch); }

        /// <summary>A world sound: panned by where it is on screen, quieter (not silent) off-screen.</summary>
        public static void At(string clip, Vector2 world, float vol = 1f, float jitter = 0.06f)
        {
            if (I == null) return;
            Vector3 v = GameCamera.Viewport(world);
            float off = Mathf.Max(Mathf.Abs(v.x - 0.5f), Mathf.Abs(v.y - 0.5f));
            float atten = Mathf.Clamp01(1.35f - off * 1.3f);
            I.Play2D(clip, vol * Mathf.Max(0.12f, atten), Mathf.Clamp((v.x - 0.5f) * 1.4f, -0.85f, 0.85f), 1f + Random.Range(-jitter, jitter));
        }

        void Play2D(string clip, float vol, float pan, float pitch)
        {
            if (!_clips.TryGetValue(clip, out var c) || vol <= 0.001f) return;
            AudioSource pick = null;
            float best = float.MaxValue;
            foreach (var s in _sfx)
            {
                if (!s.isPlaying) { pick = s; break; }
                float left = s.clip != null ? s.clip.length - s.time : 0f;
                if (left < best) { best = left; pick = s; }
            }
            pick.clip = c;
            pick.volume = Mathf.Clamp01(vol) * GameSettings.Sfx;
            pick.panStereo = pan;
            pick.pitch = pitch;
            pick.Play();
        }

        // ------------------------------------------------------------------ per frame

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_client == null)
            {
                _client = FindFirstObjectByType<SeaClient>();
                if (_client != null) { _client.EventReceived += OnEvent; _pred = _client.GetComponent<LocalPredictor>(); }
            }
            bool inGame = _client != null && Mirror.NetworkClient.active && _client.HasData && AppFlow.SeaOnScreen;
            var match = inGame ? _client.Match : default;
            var state = (MatchState)match.state;

            // music mood
            string clip = "music_day";
            float pitch = 1f, level = musicLevel;
            if (!inGame) level *= AppFlow.IntroPlaying ? 0f : 0.8f;   // the studio intro has its own sound
            else if (state == MatchState.Ledger || state == MatchState.Final) { clip = "music_night"; level *= 0.7f; }
            else if (state == MatchState.Playing && match.phase == (byte)Phase.Night) clip = "music_night";
            else if (state == MatchState.Playing && match.phase == (byte)Phase.Dusk) pitch = duskPitch;
            string key = clip + pitch.ToString("F3");
            if (key != _musicKey && _clips.TryGetValue(clip, out var mc))
            {
                _musicKey = key;
                _cur ^= 1;
                var s = _music[_cur];
                s.clip = mc;
                s.pitch = pitch;
                s.volume = 0f;
                s.time = 0f;
                s.Play();
            }
            if (_duckT > 0f) _duckT -= dt;
            _duck = Mathf.MoveTowards(_duck, _duckT > 0f ? 0f : 1f, dt * (_duckT > 0f ? 4f : 0.35f));
            float mv = level * GameSettings.Music * _duck;
            for (int i = 0; i < 2; i++)
            {
                var s = _music[i];
                float target = i == _cur ? mv : 0f;
                s.volume = Mathf.MoveTowards(s.volume, target, dt / crossfade);
                if (i != _cur && s.volume <= 0f && s.isPlaying) s.Stop();
            }

            // ambience: waves on the shore (menu), pressure underwater (game)
            string amb = inGame ? "amb_underwater" : "amb_waves";
            if ((_amb.clip == null || _amb.clip.name != amb) && _clips.TryGetValue(amb, out var ac)) { _amb.clip = ac; _amb.Play(); }
            _amb.volume = Mathf.MoveTowards(_amb.volume, AppFlow.IntroPlaying ? 0f : ambienceLevel * GameSettings.Sfx, dt * 0.5f);

            // the Angler's tension layer (hook in the water) + the tug whine and reel clicks
            bool angler = inGame && _client.Role == Role.Angler;
            bool down = angler && _pred != null && _pred.HookActive && _pred.VisualHookPos.y < GameConfig.I.surfaceY - 0.3f;
            _tensionVol = Mathf.MoveTowards(_tensionVol, down ? tensionLevel : 0f, dt * (down ? 0.25f : 0.6f));
            _tension.volume = _tensionVol * GameSettings.Music * _duck;
            bool tug = inGame && _client.InTug;
            float k = 0f;
            if (tug) { var t = _client.Tug; k = Mathf.Clamp01(t.tension / Mathf.Max(1f, (float)t.threshold)); }
            _whineVol = Mathf.MoveTowards(_whineVol, tug ? 0.12f + 0.3f * k : 0f, dt * 2f);
            _whine.volume = _whineVol * GameSettings.Sfx;
            _whine.pitch = 0.7f + k * 0.9f;
            var mouse = Mouse.current;
            if (tug && mouse != null && mouse.leftButton.isPressed)
            {
                _reelT -= dt;
                if (_reelT <= 0f) { _reelT = 0.075f; Play2D("reel_click", 0.5f, 0f, 0.9f + Random.Range(0f, 0.25f) + k * 0.3f); }
            }

            if (inGame)
            {
                // round start toll
                if (match.state != _lastState && state == MatchState.Intro) Play("round_start", 0.8f);
                // your own dash and strike
                bool dashing = _client.Role == Role.Fish && _pred != null && _pred.Active && _pred.Dashing;
                if (dashing && !_wasDashing) Play("dash", 0.8f, 1f + Random.Range(-0.05f, 0.05f));
                _wasDashing = dashing;
                if (angler && !tug && mouse != null && mouse.leftButton.wasPressedThisFrame && down) Play("strike", 0.45f, 1f + Random.Range(-0.08f, 0.08f));
            }
            _lastState = inGame ? match.state : -1;
        }

        void OnEvent(EventMsg m)
        {
            if (_client == null) return;
            var p = new Vector2(Quant.Unpack(m.x), Quant.Unpack(m.y));
            int boat = _client.BoatId, own = _client.OwnIndex;
            switch (m.kind)
            {
                case EventKind.Hooked:
                    if (m.b == own && own >= 0 || m.a == boat) { Play("hooked"); GameCamera.Shake(0.45f); }
                    else At("hooked", p, 0.55f);
                    break;
                case EventKind.Snapped:
                    if (m.a == boat || m.b == own && own >= 0) { Play("snap"); GameCamera.Shake(0.55f); }
                    else At("snap", p, 0.6f);
                    break;
                case EventKind.TornLoose: At("snap", p, 0.55f, 0.15f); break;
                case EventKind.Tangled: At("snap", p, 0.45f, 0.2f); break;
                case EventKind.Landed:
                    At("landed", new Vector2(p.x, GameConfig.I.surfaceY), m.a == boat ? 1f : 0.6f);
                    if (m.a == boat) GameCamera.Shake(0.35f);
                    break;
                case EventKind.Switched: Play("convert"); _switchedAt = Time.unscaledTime; GameCamera.Shake(0.3f); break;
                case EventKind.Reeled: if (Time.unscaledTime - _switchedAt > 1f) At("convert", p, 0.45f, 0f); break;
                case EventKind.LeviathanRises: _duckT = 2.2f; Play("leviathan"); GameCamera.Shake(0.65f); break;
                case EventKind.BoatDragged: Play("gulp"); GameCamera.Shake(m.a == boat ? 0.9f : 0.5f); break;
                case EventKind.SonarPing: Play("sonar", 0.75f); break;
                case EventKind.Nibble:
                    if (m.a == boat) Play("line_tick", 0.9f, 1f + Random.Range(-0.1f, 0.1f));
                    else if (own >= 0 && _pred != null && _pred.Active && (p - _pred.VisualPos).sqrMagnitude < 2.5f) Play("nibble", 0.7f, 1f + Random.Range(-0.1f, 0.1f));
                    else At("nibble", p, 0.2f, 0.15f);
                    break;
                case EventKind.Eaten: At("nibble", p, 0.8f, 0.1f); if (m.a == boat) Play("line_tick", 1f, 0.7f); break;
                case EventKind.InkCloud: At("ink", p, 0.9f); break;
                case EventKind.MissScatter: At("splash_small", p, 0.45f, 0.15f); At("bubble_1", p, 0.6f); break;
                case EventKind.Shock: At("shock", p, 0.8f); break;
                case EventKind.Mark: At("chime", p, 0.5f, 0f); break;
                case EventKind.PhaseChanged:
                    if (m.a == (int)Phase.Dusk) Play("dusk", 0.75f);
                    else if (m.a == (int)Phase.Night) { Play("night", 0.85f); _duckT = 1.5f; }
                    break;
                case EventKind.RoundEnd:
                case EventKind.MatchEnd: Play("round_end", 0.85f); break;
                case EventKind.Points: Play("points", 0.45f, 1f + Random.Range(-0.04f, 0.04f)); break;
                case EventKind.Escaped: Play("escape", 0.9f); break;
                case EventKind.SomethingNewSwims: Play("chime", 0.45f, 0.7f); break;
                case EventKind.Rotation: Play("round_end", 0.6f); break;
            }
        }
    }
}
