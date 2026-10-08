#if ULTRAKILL_FULL_PORT
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace UltrakillIOS
{
    /// <summary>
    /// SceneHelper.OnSceneLoaded destroys any EventSystem then Instantiate(null) because its
    /// serialized prefab ref is missing on iOS — so the main menu has no UI input at all.
    /// Also maps touch → mouse for uGUI, and disables broken gameplay Update spam on menus.
    /// </summary>
    internal sealed class MenuUiBootstrap : MonoBehaviour
    {
        private const string Area = "UI";

        private static readonly string[] MenuDisableTypeNames =
        {
            "BloodsplatterManager",
            "StainVoxelManager",
            "FistControl",
            "HookArm",
            "WeaponCharges",
            "StyleCalculator",
            "FinalRank",
            "CheatsController",
            "ClimbStep",
            "PlayerAnimations",
            "CameraFrustumTargeter",
            "PostProcessV2_Handler",
            "DebugUI",
            "LineToPoint",
            "TextOverride",
            "GamepadObjectSelector",
            "WeaponWheel",
            "ULTRAKILL.Portal.PortalManagerV2",
            "FireObjectPool",
            "SandboxHud",
            "GunColorController",
        };

        private bool _menuMode;
        private bool _mouseDown;
        private Vector2 _lastPos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindObjectOfType<MenuUiBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("UltrakillIOS.MenuUI");
            DontDestroyOnLoad(go);
            go.AddComponent<MenuUiBootstrap>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(FixAfterSceneHelper());
        }

        private IEnumerator FixAfterSceneHelper()
        {
            // SceneHelper runs on sceneLoaded and destroys EventSystem; wait past that.
            yield return null;
            yield return null;

            _menuMode = DetectMenuMode();
            UltrakillLog.Info(Area, "Scene='" + SceneManager.GetActiveScene().name
                + "' SceneHelper.CurrentScene='" + GetSceneHelperCurrent()
                + "' menuMode=" + _menuMode);

            EnsureEventSystem();
            if (_menuMode)
            {
                DisableBrokenGameplayBehaviours();
            }
        }

        private void Update()
        {
            if (!_menuMode)
            {
                // Re-check occasionally — hashed scene names make first detect flaky.
                if (Time.frameCount % 30 == 0)
                {
                    _menuMode = DetectMenuMode();
                }

                if (!_menuMode)
                {
                    return;
                }
            }

            EnsureEventSystem();
            DriveMouseFromTouches();
        }

        internal static bool IsMenuModeActive()
        {
            var boot = UnityEngine.Object.FindObjectOfType<MenuUiBootstrap>();
            return boot != null && boot._menuMode;
        }

        private static string GetSceneHelperCurrent()
        {
            try
            {
                var t = Type.GetType("SceneHelper, Assembly-CSharp");
                var p = t?.GetProperty("CurrentScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                return p?.GetValue(null) as string ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static bool DetectMenuMode()
        {
            var current = GetSceneHelperCurrent();
            if (!string.IsNullOrEmpty(current)
                && (current.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0
                    || current.Equals("Intro", StringComparison.OrdinalIgnoreCase)
                    || current.Equals("Bootstrap", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var scene = SceneManager.GetActiveScene().name;
            if (scene.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0
                || scene.Equals("Bootstrap", StringComparison.OrdinalIgnoreCase)
                || scene.Equals("Intro", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Addressable Main Menu often loads under a hash name.
            if (TypeExistsInScene("MainMenuAgony") || TypeExistsInScene("MainMenu"))
            {
                return true;
            }

            return false;
        }

        private static bool TypeExistsInScene(string typeName)
        {
            var t = Type.GetType(typeName + ", Assembly-CSharp");
            if (t == null)
            {
                return false;
            }

            return UnityEngine.Object.FindObjectOfType(t) != null;
        }

        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (existing != null)
            {
                if (existing.currentInputModule == null)
                {
                    AddInputModule(existing.gameObject);
                }

                return;
            }

            var go = new GameObject("UltrakillIOS.EventSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
            AddInputModule(go);
            UltrakillLog.Info(Area, "Created replacement EventSystem (SceneHelper prefab was null)");
        }

        private static void AddInputModule(GameObject go)
        {
#if ENABLE_INPUT_SYSTEM
            if (go.GetComponent<InputSystemUIInputModule>() == null)
            {
                go.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (go.GetComponent<StandaloneInputModule>() == null)
            {
                go.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private void DriveMouseFromTouches()
        {
            EnsureMouseDevice();

            if (Input.touchCount <= 0)
            {
                if (_mouseDown)
                {
                    SetMouse(_lastPos, false);
                    _mouseDown = false;
                }

                return;
            }

            var touch = Input.GetTouch(0);
            _lastPos = touch.position;
            var pressed = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            SetMouse(_lastPos, pressed);
            _mouseDown = pressed;
        }

        private static void EnsureMouseDevice()
        {
            if (Mouse.current != null)
            {
                return;
            }

            try
            {
                InputSystem.AddDevice<Mouse>();
                UltrakillLog.Info(Area, "Added virtual Mouse for UI");
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "Could not add Mouse device: " + ex.Message);
            }
        }

        private static void SetMouse(Vector2 screenPos, bool leftPressed)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            InputState.Change(mouse.position, screenPos);
            using (StateEvent.From(mouse, out var eventPtr))
            {
                mouse.position.WriteValueIntoEvent(screenPos, eventPtr);
                mouse.leftButton.WriteValueIntoEvent(leftPressed ? 1f : 0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }

        private static void DisableBrokenGameplayBehaviours()
        {
            var disabled = 0;
            foreach (var name in MenuDisableTypeNames)
            {
                var t = Type.GetType(name + ", Assembly-CSharp")
                    ?? Type.GetType(name + ", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (t == null || !typeof(Behaviour).IsAssignableFrom(t))
                {
                    continue;
                }

                var objs = UnityEngine.Object.FindObjectsOfType(t, true);
                foreach (var obj in objs)
                {
                    if (obj is Behaviour b && b.enabled)
                    {
                        b.enabled = false;
                        disabled++;
                    }
                }
            }

            UltrakillLog.Info(Area, "Disabled " + disabled + " broken gameplay behaviours on menu");
        }
    }
}
#endif
