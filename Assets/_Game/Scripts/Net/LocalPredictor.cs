using BaitSwitch.Sim;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaitSwitch.Net
{
    /// <summary>
    /// Local player input + prediction.
    /// Fish: WASD swim (LMB nibble, Space dash, E ink); FishMotor runs locally and reconciles by replaying unacked inputs.
    /// Angler: A/D row, mouse = hook target, LMB strike/reel, RMB reel up + rebait; boat and hook are simulated locally
    /// and corrected by comparing the server's state at the acked input with our own history.
    /// Corrections become visual offsets that ease out. F1 (dev) switches role.
    /// </summary>
    public sealed class LocalPredictor : MonoBehaviour
    {
        struct Frame { public sbyte mx, my; public Buttons b; public Vector2 cursor; }

        GameConfig _c;
        Role _role = Role.Spectator;
        int _own = -1, _boat = -1;
        uint _anglerFrom;   // last input seq sent before angler prediction began (older acks are fish-era inputs)
        bool _fishActive, _hookActive;
        uint _seq, _lastTick;
        float _acc;
        Frame[] _ring;
        uint[] _ringSeq;
        FishBody[] _bodyHist;
        Frame _cur, _prev1, _prev2;

        FishBody _body;
        Vector2 _offset;

        float _boatX;
        Vector2 _hookPos, _hookVel;
        Vector2[] _hookHist;
        float[] _boatHist;
        HookState _serverHookState;
        Vector2 _hookOffset;
        float _boatOffset;

        public bool Active => _fishActive;
        public float Speed => _body.vel.magnitude;
        public bool Dashing => _body.dashT > 0f;
        public Vector2 VisualPos { get; private set; }
        public float VisualHeading { get; private set; }
        public bool HookActive => _hookActive;
        public int BoatId => _boat;
        public Vector2 VisualHookPos { get; private set; }
        public float VisualBoatX { get; private set; }

        public void Begin(int ownIndex, Role role, int boatId)
        {
            _c = GameConfig.I;
            if (_ring == null || _ring.Length != _c.inputBufferLen)
            {
                _ring = new Frame[_c.inputBufferLen];
                _ringSeq = new uint[_c.inputBufferLen];
                _bodyHist = new FishBody[_c.inputBufferLen];
                _hookHist = new Vector2[_c.inputBufferLen];
                _boatHist = new float[_c.inputBufferLen];
            }
            // _seq keeps counting across role changes so the server's input queue stays valid.
            _role = role;
            _own = ownIndex;
            _boat = boatId;
            _fishActive = false;
            _hookActive = false;
            _lastTick = 0;
            _offset = Vector2.zero;
            _hookOffset = Vector2.zero;
            _boatOffset = 0f;
        }

        public void End()
        {
            _own = -1; _boat = -1; _role = Role.Spectator;
            _fishActive = _hookActive = false;
            _seq = 0;
        }

        // ---------- server corrections ----------

        public void OnServerState(uint tick, uint ack, in FishSnap snap, Vector2 vel)
        {
            if (_role != Role.Fish || _own < 0 || (_fishActive && tick <= _lastTick)) return;
            _lastTick = tick;
            var b = new FishBody { pos = snap.pos, vel = vel, heading = snap.heading, flags = (byte)snap.flags, size = snap.sizeStep };
            bool held = (snap.flags & (FishFlags.Hooked | FishFlags.Dead)) != 0;
            if (!_fishActive || held)
            {
                // Hooked or landed: the server moves us; no prediction until we swim again.
                if (_fishActive) _offset += _body.pos - b.pos;
                _body = b;
                if (_seq < ack) _seq = ack;
                _fishActive = true;
                return;
            }

            float dt = 1f / _c.inputHz;
            int len = _ring.Length;
            int ai = (int)(ack % (uint)len);
            if (_ringSeq[ai] == ack) { b.dashT = _bodyHist[ai].dashT; b.dashCd = _bodyHist[ai].dashCd; b.dashHeld = _bodyHist[ai].dashHeld; }
            else { b.dashT = _body.dashT; b.dashCd = _body.dashCd; b.dashHeld = _body.dashHeld; }
            for (uint s = ack + 1; s <= _seq; s++)
            {
                int i = (int)(s % (uint)len);
                if (_ringSeq[i] != s) break;
                FishMotor.Step(ref b, Move(_ring[i]), _ring[i].b, dt, _c);
            }
            _offset += _body.pos - b.pos;
            if (_offset.sqrMagnitude > _c.correctionSnapDist * _c.correctionSnapDist) _offset = Vector2.zero;
            _body = b;
        }

        public void OnServerHook(uint tick, uint ack, Vector2 hookPos, HookState state, float boatX)
        {
            if (_role != Role.Angler || _boat < 0) return;
            _serverHookState = state;
            if (!_hookActive)
            {
                _boatX = boatX; _hookPos = hookPos; _hookVel = Vector2.zero;
                if (_seq < ack) _seq = ack;
                _anglerFrom = _seq;
                _hookOffset = Vector2.zero; _boatOffset = 0f;
                _hookActive = true;
                return;
            }
            // Until the server has processed an input we sent as an Angler, its acks point at fish-era history:
            // reconciling against that made a freshly converted boat lurch left and right. Follow the server instead.
            if (ack <= _anglerFrom)
            {
                _boatX = boatX; _hookPos = hookPos; _hookVel = Vector2.zero;
                _hookOffset = Vector2.zero; _boatOffset = 0f;
                return;
            }
            int i = (int)(ack % (uint)_ring.Length);
            bool known = _ringSeq[i] == ack && ack > _anglerFrom;
            Vector2 hookErr = known ? hookPos - _hookHist[i] : hookPos - _hookPos;
            float boatErr = known ? boatX - _boatHist[i] : boatX - _boatX;
            if (state == HookState.ReelingUp) hookErr = hookPos - _hookPos; // server-driven: follow it

            if (hookErr.sqrMagnitude > _c.hookResyncDist * _c.hookResyncDist) { _hookPos = hookPos; _hookVel = Vector2.zero; }
            else { _hookPos += hookErr; _hookOffset -= hookErr; }
            _boatX += boatErr;
            _boatOffset -= boatErr;
        }

        // ---------- per frame ----------

        void Update()
        {
            if (_c == null || !NetworkClient.isConnected) return;
            bool fish = _role == Role.Fish && _fishActive;
            bool angler = _role == Role.Angler && _hookActive;
            if (!fish && !angler) return;
            ReadInput(angler);

            float dt = 1f / _c.inputHz;
            _acc += Time.unscaledDeltaTime;
            int steps = 0;
            while (_acc >= dt && steps < _c.maxSimStepsPerFrame) { _acc -= dt; steps++; TickInput(dt, angler); }
            if (steps == _c.maxSimStepsPerFrame) _acc = 0f;

            float decay = Mathf.Pow(0.5f, Time.unscaledDeltaTime / Mathf.Max(0.001f, _c.correctionHalfLife));
            if (fish)
            {
                // Same-frame response: committed body advanced by the partial tick with the current stick.
                var v = _body;
                if ((v.flags & (byte)(FishFlags.Hooked | FishFlags.Dead)) == 0) FishMotor.Step(ref v, Move(_cur), _cur.b, _acc, _c);
                _offset *= decay;
                VisualPos = v.pos + _offset;
                VisualHeading = v.heading;
            }
            else
            {
                _hookOffset *= decay;
                _boatOffset *= decay;
                VisualHookPos = _hookPos + _hookOffset;
                VisualBoatX = _boatX + _boatOffset;
            }
        }

        void ReadInput(bool angler)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            int x = 0, y = 0;
            Buttons b = Buttons.None;
            if (kb != null)
            {
                if (kb.aKey.isPressed) x -= 1;
                if (kb.dKey.isPressed) x += 1;
                if (!angler)
                {
                    if (kb.sKey.isPressed) y -= 1;
                    if (kb.wKey.isPressed) y += 1;
                    if (kb.spaceKey.isPressed) b |= Buttons.Dash;
                    if (kb.eKey.isPressed) b |= Buttons.Ink;
                }
                else if (kb.qKey.isPressed) b |= Buttons.Sonar;
            }
            Vector2 cursor = _cur.cursor;
            if (mouse != null)
            {
                if (angler)
                {
                    if (mouse.leftButton.isPressed) b |= Buttons.Strike | Buttons.Reel;
                    if (mouse.rightButton.isPressed) b |= Buttons.Rebait;
                    if (mouse.middleButton.isPressed) b |= Buttons.Mark;
                    cursor = View.GameCamera.MouseWorld(mouse.position.ReadValue());
                }
                else if (mouse.leftButton.isPressed) b |= Buttons.Nibble | Buttons.Bite;
            }
            var m = new Vector2(x, y);
            if (!angler && UI.GameSettings.SwimWithMouse && mouse != null && m.sqrMagnitude < 0.01f && Camera.main != null)
            {
                // mouse-follow swimming: steer toward the cursor, slowing as you arrive
                Vector2 to = View.GameCamera.MouseWorld(mouse.position.ReadValue()) - _body.pos;
                m = to.sqrMagnitude > 0.04f ? to.normalized * Mathf.Clamp01(to.magnitude / 2.5f) : Vector2.zero;
            }
            if (m.sqrMagnitude > 1f) m.Normalize();
            _cur = new Frame { mx = (sbyte)Mathf.RoundToInt(m.x * 127f), my = (sbyte)Mathf.RoundToInt(m.y * 127f), b = b, cursor = cursor };
        }

        void TickInput(float dt, bool angler)
        {
            _seq++;
            int i = (int)(_seq % (uint)_ring.Length);
            _ring[i] = _cur;
            _ringSeq[i] = _seq;

            if (angler)
            {
                _boatX = HookPhysics.StepBoat(_boatX, _cur.mx / 127f, dt, _c);
                bool serverReeling = _serverHookState == HookState.ReelingUp;
                bool rebaiting = (_cur.b & Buttons.Rebait) != 0 && _serverHookState == HookState.Empty;
                if (serverReeling || rebaiting) HookPhysics.StepReel(ref _hookPos, ref _hookVel, _boatX, dt, _c);
                else
                {
                    bool strike = (_cur.b & Buttons.Strike) != 0 && (_prev1.b & Buttons.Strike) == 0;
                    if (strike && _serverHookState != HookState.Striking) _hookVel.y += _c.strikeJerk / _c.strikeJerkTime;
                    HookPhysics.StepFree(ref _hookPos, ref _hookVel, _cur.cursor, _boatX, dt, _c);
                }
                _hookHist[i] = _hookPos;
                _boatHist[i] = _boatX;
            }
            else
            {
                if ((_body.flags & (byte)(FishFlags.Hooked | FishFlags.Dead)) == 0)
                    FishMotor.Step(ref _body, Move(_cur), _cur.b, dt, _c);
                _bodyHist[i] = _body;
            }

            NetworkClient.Send(new InputMsg
            {
                seq = _seq,
                moveX = _cur.mx, moveY = _cur.my, buttons = _cur.b,
                cursorX = Quant.Pack(_cur.cursor.x), cursorY = Quant.Pack(_cur.cursor.y),
                prevX1 = _prev1.mx, prevY1 = _prev1.my, prevB1 = _prev1.b,
                prevX2 = _prev2.mx, prevY2 = _prev2.my, prevB2 = _prev2.b,
            }, Channels.Unreliable);
            _prev2 = _prev1;
            _prev1 = _cur;
        }

        static Vector2 Move(Frame f) => new Vector2(f.mx / 127f, f.my / 127f);
    }
}
