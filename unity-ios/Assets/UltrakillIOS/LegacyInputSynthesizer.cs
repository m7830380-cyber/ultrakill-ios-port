using NewBlood;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace UltrakillIOS
{
    /// <summary>
    /// Drives ULTRAKILL's LegacyInput device (WASD / mouse buttons) from touch UI.
    /// </summary>
    internal static class LegacyInputSynthesizer
    {
        public static void ApplyMovement(Vector2 move)
        {
            var legacy = LegacyInput.current ?? InputSystem.GetDevice<LegacyInput>();
            if (legacy == null)
            {
                return;
            }

            SetButton(legacy.wKey, move.y > 0.35f);
            SetButton(legacy.sKey, move.y < -0.35f);
            SetButton(legacy.aKey, move.x < -0.35f);
            SetButton(legacy.dKey, move.x > 0.35f);
        }

        public static void ApplyLookDelta(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            InputSystem.QueueDeltaStateEvent(mouse.delta, delta);
        }

        public static void SetFire(bool pressed) => SetMouseButton(0, pressed);
        public static void SetAltFire(bool pressed) => SetMouseButton(1, pressed);
        public static void SetJump(bool pressed) => SetKey(KeyCode.Space, pressed);
        public static void SetSlide(bool pressed) => SetKey(KeyCode.LeftControl, pressed);
        public static void SetDash(bool pressed) => SetKey(KeyCode.LeftShift, pressed);

        private static void SetMouseButton(int index, bool pressed)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            ButtonControl control = index switch
            {
                0 => mouse.leftButton,
                1 => mouse.rightButton,
                2 => mouse.middleButton,
                _ => null
            };

            if (control != null)
            {
                SetButton(control, pressed);
            }
        }

        private static void SetKey(KeyCode key, bool pressed)
        {
            var legacy = LegacyInput.current ?? InputSystem.GetDevice<LegacyInput>();
            if (legacy == null)
            {
                return;
            }

            ButtonControl control = KeyToControl(legacy, key);
            if (control != null)
            {
                SetButton(control, pressed);
            }
        }

        private static ButtonControl KeyToControl(LegacyInput legacy, KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Space: return legacy.spaceKey;
                case KeyCode.LeftControl: return legacy.leftControlKey;
                case KeyCode.LeftShift: return legacy.leftShiftKey;
                case KeyCode.E: return legacy.eKey;
                case KeyCode.Q: return legacy.qKey;
                case KeyCode.R: return legacy.rKey;
                case KeyCode.Alpha1: return legacy.alpha1Key;
                case KeyCode.Alpha2: return legacy.alpha2Key;
                case KeyCode.Alpha3: return legacy.alpha3Key;
                default: return null;
            }
        }

        private static void SetButton(ButtonControl control, bool pressed)
        {
            if (control == null)
            {
                return;
            }

            using (StateEvent.From(control.device, out var eventPtr))
            {
                control.WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }
    }
}
