using UnityEngine;

namespace UltrakillIOS
{
    internal sealed class MobileTouchHud : MonoBehaviour
    {
        private const float MoveStickRadius = 120f;
        private const float LookSensitivity = 2.2f;

        private Vector2 _moveStickCenter;
        private Vector2 _moveVector;
        private int _moveFingerId = -1;

        private Vector2 _lastLookPos;
        private int _lookFingerId = -1;

        private bool _fireHeld;
        private bool _altFireHeld;
        private bool _jumpHeld;
        private bool _slideHeld;
        private bool _dashHeld;

        private void Start()
        {
            _moveStickCenter = new Vector2(MoveStickRadius + 48f, Screen.height - MoveStickRadius - 48f);
        }

        private void Update()
        {
            if (!ShouldRun())
            {
                return;
            }

#if ULTRAKILL_FULL_PORT
            // Main menu / UI scenes: do not steal touches — MenuUiBootstrap drives the cursor.
            if (MenuUiBootstrap.IsMenuModeActive())
            {
                _moveVector = Vector2.zero;
                _moveFingerId = -1;
                _lookFingerId = -1;
                LegacyInputSynthesizer.ApplyMovement(Vector2.zero);
                LegacyInputSynthesizer.SetFire(false);
                LegacyInputSynthesizer.SetAltFire(false);
                LegacyInputSynthesizer.SetJump(false);
                LegacyInputSynthesizer.SetSlide(false);
                LegacyInputSynthesizer.SetDash(false);
                return;
            }
#endif

            PollTouches();
            LegacyInputSynthesizer.ApplyMovement(_moveVector);
        }

        private static bool ShouldRun()
        {
#if UNITY_IOS || UNITY_ANDROID
            return true;
#else
            return Application.isMobilePlatform;
#endif
        }

        private void PollTouches()
        {
            _moveVector = Vector2.zero;

            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                var pos = touch.position;

                if (IsMoveZone(pos))
                {
                    HandleMoveTouch(touch);
                }
                else if (IsLookZone(pos))
                {
                    HandleLookTouch(touch);
                }
            }

            LegacyInputSynthesizer.SetFire(_fireHeld);
            LegacyInputSynthesizer.SetAltFire(_altFireHeld);
            LegacyInputSynthesizer.SetJump(_jumpHeld);
            LegacyInputSynthesizer.SetSlide(_slideHeld);
            LegacyInputSynthesizer.SetDash(_dashHeld);
        }

        private void HandleMoveTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (_moveFingerId < 0)
                {
                    _moveFingerId = touch.fingerId;
                }
            }

            if (touch.fingerId != _moveFingerId)
            {
                return;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _moveFingerId = -1;
                _moveVector = Vector2.zero;
                return;
            }

            var delta = touch.position - _moveStickCenter;
            _moveVector = Vector2.ClampMagnitude(delta / MoveStickRadius, 1f);
        }

        private void HandleLookTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (_lookFingerId < 0)
                {
                    _lookFingerId = touch.fingerId;
                    _lastLookPos = touch.position;
                }
            }

            if (touch.fingerId != _lookFingerId)
            {
                return;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _lookFingerId = -1;
                return;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                var delta = touch.position - _lastLookPos;
                _lastLookPos = touch.position;
                LegacyInputSynthesizer.ApplyLookDelta(delta * LookSensitivity);
            }
        }

        private static bool IsMoveZone(Vector2 pos) => pos.x < Screen.width * 0.45f && pos.y < Screen.height * 0.55f;

        private static bool IsLookZone(Vector2 pos) => pos.x > Screen.width * 0.55f && pos.y < Screen.height * 0.7f;

        private void OnGUI()
        {
            if (!ShouldRun())
            {
                return;
            }

#if ULTRAKILL_FULL_PORT
            if (MenuUiBootstrap.IsMenuModeActive())
            {
                return;
            }
#endif

            DrawMoveStick();
            DrawActionButtons();
        }

        private void DrawMoveStick()
        {
            var bg = new Rect(_moveStickCenter.x - MoveStickRadius, Screen.height - _moveStickCenter.y - MoveStickRadius, MoveStickRadius * 2f, MoveStickRadius * 2f);
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.DrawTexture(bg, Texture2D.whiteTexture);

            var knob = _moveStickCenter + _moveVector * (MoveStickRadius * 0.55f);
            var knobRect = new Rect(knob.x - 36f, Screen.height - knob.y - 36f, 72f, 72f);
            GUI.color = new Color(1f, 1f, 1f, 0.45f);
            GUI.DrawTexture(knobRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawActionButtons()
        {
            const float size = 72f;
            var fire = ActionRect(Screen.width - size - 24f, Screen.height * 0.35f, size);
            var alt = ActionRect(Screen.width - size * 2f - 36f, Screen.height * 0.35f, size);
            var jump = ActionRect(Screen.width - size - 24f, Screen.height * 0.22f, size);
            var slide = ActionRect(Screen.width - size * 2f - 36f, Screen.height * 0.22f, size);
            var dash = ActionRect(Screen.width - size - 24f, Screen.height * 0.09f, size);

            _fireHeld = DrawButton(fire, "FIRE");
            _altFireHeld = DrawButton(alt, "ALT");
            _jumpHeld = DrawButton(jump, "JMP");
            _slideHeld = DrawButton(slide, "SLD");
            _dashHeld = DrawButton(dash, "DSH");
        }

        private static Rect ActionRect(float x, float yFromBottom, float size)
        {
            return new Rect(x, Screen.height - yFromBottom - size, size, size);
        }

        private static bool DrawButton(Rect rect, string label)
        {
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            var held = GUI.RepeatButton(rect, label);
            GUI.color = Color.white;
            return held;
        }
    }
}
