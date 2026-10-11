using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace UltrakillIOS
{
    // On-screen controls. Touches are turned into input on virtual devices (keyboard + mouse for the
    // actions, a gamepad for analog movement), so the game sees ordinary PC/controller input and keeps
    // its own logic, rebinding and sensitivity settings.
    public class TouchHud : MonoBehaviour
    {
        public static TouchHud Instance;
        public static float LookScale = 1.0f;

        enum Kind { Action, Pause }

        class Btn
        {
            public string Label;
            public Func<PlayerInput, InputActionState> Action;
            public Key DefaultKey;
            public int DefaultMouse = -1;       // 0 = left, 1 = right
            public Kind Kind = Kind.Action;
            public bool Look;                   // dragging from this button also aims
            public float Dx, Dy, R;             // centre from right/left edge and bottom, radius (screen-height units)
            public bool FromLeft;
            public bool FromTop;
            // runtime
            public Vector2 Center;
            public float Radius;
            public bool Down;
            public int Finger = -1;
        }

        readonly List<Btn> m_Buttons = new List<Btn>();
        Keyboard m_Kb;
        Mouse m_Mouse;
        Gamepad m_Pad;

        int m_StickFinger = -1;
        Vector2 m_StickOrigin, m_StickPos;
        int m_LookFinger = -1;
        Vector2 m_LookLast;
        Vector2 m_LookAccum;
        bool m_Visible;
        bool m_PauseRequested;
        Texture2D m_Circle, m_Ring;
        GUIStyle m_Label;

        void Awake()
        {
            Instance = this;
            Add("FIRE", p => p.Fire1, Key.None, 0, 0.30f, 0.21f, 0.11f, look: true);
            Add("ALT", p => p.Fire2, Key.None, 1, 0.12f, 0.40f, 0.075f, look: true);
            Add("JUMP", p => p.Jump, Key.Space, -1, 0.11f, 0.15f, 0.08f);
            Add("DASH", p => p.Dodge, Key.LeftShift, -1, 0.30f, 0.44f, 0.065f);
            Add("SLIDE", p => p.Slide, Key.LeftCtrl, -1, 0.49f, 0.11f, 0.065f);
            Add("PUNCH", p => p.Punch, Key.F, -1, 0.48f, 0.30f, 0.065f);
            Add("HOOK", p => p.Hook, Key.R, -1, 0.13f, 0.62f, 0.06f);
            Add("VAR", p => p.NextVariation, Key.E, -1, 0.31f, 0.64f, 0.05f);
            Add("ARM", p => p.ChangeFist, Key.G, -1, 0.64f, 0.10f, 0.045f);
            string[] slots = { "1", "2", "3", "4", "5" };
            Func<PlayerInput, InputActionState>[] slotActions = { p => p.Slot1, p => p.Slot2, p => p.Slot3, p => p.Slot4, p => p.Slot5 };
            Key[] slotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
            for (int i = 0; i < 5; i++)
            {
                var b = Add(slots[i], slotActions[i], slotKeys[i], -1, 0.0f, 0.075f, 0.045f);
                b.FromTop = true;
                b.FromLeft = true;
                b.Dx = 0; // placed in Layout()
                b.Label = slots[i];
            }
            var pause = Add("II", null, Key.None, -1, 0.08f, 0.08f, 0.045f);
            pause.Kind = Kind.Pause;
            pause.FromLeft = true;
            pause.FromTop = true;

            m_Circle = MakeCircle(128, false);
            m_Ring = MakeCircle(128, true);
        }

        Btn Add(string label, Func<PlayerInput, InputActionState> action, Key key, int mouse, float dx, float dy, float r, bool look = false)
        {
            var b = new Btn { Label = label, Action = action, DefaultKey = key, DefaultMouse = mouse, Dx = dx, Dy = dy, R = r, Look = look };
            m_Buttons.Add(b);
            return b;
        }

        void OnEnable()
        {
            try
            {
                m_Kb = InputSystem.AddDevice<Keyboard>("UKTouchKeyboard");
                m_Mouse = InputSystem.AddDevice<Mouse>("UKTouchMouse");
                m_Pad = InputSystem.AddDevice<Gamepad>("UKTouchGamepad");
            }
            catch (Exception e)
            {
                Log.Error("virtual devices: " + e);
            }
        }

        void Layout()
        {
            float h = Screen.height, w = Screen.width;
            // keep clear of rounded corners / notch
            var safe = Screen.safeArea;
            float left = safe.xMin, right = safe.xMax, bottom = safe.yMin, top = safe.yMax;
            int slot = 0;
            foreach (var b in m_Buttons)
            {
                b.Radius = b.R * h;
                if (b.FromTop && b.Kind == Kind.Action)
                {
                    // weapon slot row, top centre
                    float spacing = 0.12f * h;
                    b.Center = new Vector2(w * 0.5f + (slot - 2) * spacing, top - b.Dy * h);
                    slot++;
                    continue;
                }
                float x = b.FromLeft ? left + b.Dx * h : right - b.Dx * h;
                float y = b.FromTop ? top - b.Dy * h : bottom + b.Dy * h;
                b.Center = new Vector2(x, y);
            }
        }

        static bool GameplayActive()
        {
            var nm = MonoSingleton<NewMovement>.Instance;
            if (nm == null || !nm.isActiveAndEnabled) return false;
            var om = MonoSingleton<OptionsManager>.Instance;
            if (om != null && (om.paused || om.mainMenu)) return false;
            return true;
        }

        void Update()
        {
            Layout();
            bool active;
            try { active = GameplayActive(); }
            catch { active = false; }
            if (active != m_Visible)
            {
                m_Visible = active;
                ReleaseAll();
            }
            if (m_Visible) ProcessTouches();
            if (m_PauseRequested)
            {
                m_PauseRequested = false;
                try
                {
                    var om = MonoSingleton<OptionsManager>.Instance;
                    if (om != null && !om.paused) om.Pause();
                }
                catch (Exception e) { Log.Error("pause: " + e.Message); }
            }
            PushState();
        }

        void ReleaseAll()
        {
            foreach (var b in m_Buttons) { b.Down = false; b.Finger = -1; }
            m_StickFinger = -1;
            m_LookFinger = -1;
            m_LookAccum = Vector2.zero;
        }

        void ProcessTouches()
        {
            var ts = Touchscreen.current;
            var seen = new HashSet<int>();
            if (ts != null)
            {
                foreach (var t in ts.touches)
                {
                    var phase = t.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;
                    int id = t.touchId.ReadValue();
                    Vector2 pos = t.position.ReadValue();
                    bool ended = phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                    if (ended) { Release(id); continue; }
                    seen.Add(id);
                    if (!IsTracked(id)) Begin(id, pos);
                    Move(id, pos);
                }
            }
            // fingers that vanished without an Ended phase
            foreach (var b in m_Buttons)
                if (b.Finger >= 0 && !seen.Contains(b.Finger)) { b.Down = false; b.Finger = -1; }
            if (m_StickFinger >= 0 && !seen.Contains(m_StickFinger)) m_StickFinger = -1;
            if (m_LookFinger >= 0 && !seen.Contains(m_LookFinger)) m_LookFinger = -1;
        }

        bool IsTracked(int id)
        {
            if (id == m_StickFinger || id == m_LookFinger) return true;
            foreach (var b in m_Buttons) if (b.Finger == id) return true;
            return false;
        }

        void Begin(int id, Vector2 pos)
        {
            Btn hit = null;
            float best = float.MaxValue;
            foreach (var b in m_Buttons)
            {
                if (b.Finger >= 0) continue;
                float d = Vector2.Distance(pos, b.Center);
                if (d <= b.Radius * 1.15f && d < best) { best = d; hit = b; }
            }
            if (hit != null)
            {
                hit.Finger = id;
                hit.Down = true;
                if (hit.Kind == Kind.Pause) m_PauseRequested = true;
                if (hit.Look && m_LookFinger < 0) { m_LookFinger = id; m_LookLast = pos; }
                return;
            }
            if (pos.x < Screen.width * 0.42f)
            {
                if (m_StickFinger < 0) { m_StickFinger = id; m_StickOrigin = pos; m_StickPos = pos; }
            }
            else if (m_LookFinger < 0)
            {
                m_LookFinger = id;
                m_LookLast = pos;
            }
        }

        void Move(int id, Vector2 pos)
        {
            if (id == m_StickFinger) m_StickPos = pos;
            if (id == m_LookFinger)
            {
                m_LookAccum += pos - m_LookLast;
                m_LookLast = pos;
            }
        }

        void Release(int id)
        {
            foreach (var b in m_Buttons) if (b.Finger == id) { b.Down = false; b.Finger = -1; }
            if (id == m_StickFinger) m_StickFinger = -1;
            if (id == m_LookFinger) m_LookFinger = -1;
        }

        // Resolve the key/mouse button the game currently has bound to an action (honours rebinding).
        void Resolve(Btn b, PlayerInput pi, out Key key, out int mouse)
        {
            key = b.DefaultKey;
            mouse = b.DefaultMouse;
            if (pi == null || b.Action == null) return;
            InputAction action = null;
            try { action = b.Action(pi)?.Action; } catch { }
            if (action == null) return;
            foreach (var binding in action.bindings)
            {
                string path = binding.effectivePath;
                if (string.IsNullOrEmpty(path) || binding.isComposite || binding.isPartOfComposite) continue;
                if (path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
                {
                    var ctl = m_Kb != null ? InputControlPath.TryFindControl(m_Kb, path) as KeyControl : null;
                    if (ctl != null) { key = ctl.keyCode; mouse = -1; return; }
                }
                else if (path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
                {
                    if (path.EndsWith("leftButton")) { mouse = 0; key = Key.None; return; }
                    if (path.EndsWith("rightButton")) { mouse = 1; key = Key.None; return; }
                    if (path.EndsWith("middleButton")) { mouse = 2; key = Key.None; return; }
                }
            }
        }

        readonly List<Key> m_Keys = new List<Key>();

        void PushState()
        {
            if (m_Kb == null || m_Mouse == null || m_Pad == null) return;
            PlayerInput pi = null;
            try { pi = MonoSingleton<InputManager>.Instance?.InputSource; } catch { }

            m_Keys.Clear();
            var ms = new MouseState { position = new Vector2(-100, -100) };
            if (m_Visible)
            {
                foreach (var b in m_Buttons)
                {
                    if (!b.Down || b.Kind != Kind.Action) continue;
                    Resolve(b, pi, out Key key, out int mouse);
                    if (key != Key.None) m_Keys.Add(key);
                    if (mouse >= 0) ms = ms.WithButton((MouseButton)mouse, true);
                }
                float scale = LookScale * 160f / Mathf.Max(100f, Screen.dpi > 0 ? Screen.dpi : 326f);
                ms.delta = m_LookAccum * scale;
            }
            m_LookAccum = Vector2.zero;
            InputSystem.QueueStateEvent(m_Kb, new KeyboardState(m_Keys.ToArray()));
            InputSystem.QueueStateEvent(m_Mouse, ms);

            var gs = new GamepadState();
            if (m_Visible && m_StickFinger >= 0)
            {
                float rad = 0.13f * Screen.height;
                Vector2 v = (m_StickPos - m_StickOrigin) / rad;
                if (v.magnitude > 1f)
                {
                    // drag the origin along so the stick never needs to be re-centred
                    m_StickOrigin = m_StickPos - v.normalized * rad;
                    v = v.normalized;
                }
                gs.leftStick = v.magnitude < 0.12f ? Vector2.zero : v;
            }
            InputSystem.QueueStateEvent(m_Pad, gs);
        }

        void OnGUI()
        {
            if (!m_Visible || Event.current.type != EventType.Repaint) return;
            if (m_Label == null)
            {
                m_Label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            m_Label.fontSize = Mathf.RoundToInt(Screen.height * 0.028f);
            float h = Screen.height;
            var prev = GUI.color;
            foreach (var b in m_Buttons)
            {
                var r = new Rect(b.Center.x - b.Radius, h - b.Center.y - b.Radius, b.Radius * 2, b.Radius * 2);
                GUI.color = b.Down ? new Color(1f, 0.25f, 0.2f, 0.55f) : new Color(1f, 1f, 1f, 0.22f);
                GUI.DrawTexture(r, m_Circle);
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                GUI.DrawTexture(r, m_Ring);
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.Label(r, b.Label, m_Label);
            }
            if (m_StickFinger >= 0)
            {
                float rad = 0.13f * h;
                var o = new Rect(m_StickOrigin.x - rad, h - m_StickOrigin.y - rad, rad * 2, rad * 2);
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.DrawTexture(o, m_Ring);
                float k = rad * 0.45f;
                var kn = new Rect(m_StickPos.x - k, h - m_StickPos.y - k, k * 2, k * 2);
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
                GUI.DrawTexture(kn, m_Circle);
            }
            GUI.color = prev;
        }

        static Texture2D MakeCircle(int size, bool ring)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.93f) * 25f) : Mathf.Clamp01((1f - d) * c * 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }
    }
}
