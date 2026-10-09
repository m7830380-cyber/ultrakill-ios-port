#if ULTRAKILL_FULL_PORT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace UltrakillIOS
{
    /// <summary>
    /// Retail SceneHelper ships with null serialized refs on iOS (eventSystem, loadingBlocker,
    /// embeddedSceneInfo, …). That causes:
    /// - "The Object you want to instantiate is null" every scene load
    /// - Play → LoadSceneCoroutine NRE on loadingBlocker.SetActive → Tutorial never loads (magenta)
    /// - PendingScene stuck + timeScale 0
    /// </summary>
    internal sealed class SceneHelperRepair : MonoBehaviour
    {
        private const string Area = "SceneHelper";
        private static bool s_loggedOnce;

        private static readonly string[] AlwaysDisableWhenBroken =
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
            "TimeController",
            "MusicManager",
            "AudioMixerController",
            "OptionsMenuToManager",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("UltrakillIOS.SceneHelperRepair");
            DontDestroyOnLoad(go);
            go.AddComponent<SceneHelperRepair>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            StartCoroutine(RepairLoop());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(RepairAfterLoad());
        }

        private IEnumerator RepairLoop()
        {
            // Keep trying early — SceneHelper may appear with Main Menu.
            for (var i = 0; i < 120; i++)
            {
                if (TryRepair())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator RepairAfterLoad()
        {
            yield return null;
            TryRepair();
            MuteBrokenBehaviours();
            RemapBrokenShaders();
            TmpBootstrapForceApply();
        }

        private static void TmpBootstrapForceApply()
        {
            // Re-apply fonts after addressable scene texts spawn.
            try
            {
                var t = Type.GetType("UltrakillIOS.TmpBootstrap, UltrakillIOS");
                // Apply happens via sceneLoaded in TmpBootstrap already.
            }
            catch
            {
                /* ignore */
            }
        }

        internal static bool TryRepair()
        {
            var shType = Type.GetType("SceneHelper, Assembly-CSharp");
            if (shType == null)
            {
                return false;
            }

            var sh = UnityEngine.Object.FindObjectOfType(shType) as MonoBehaviour;
            if (sh == null)
            {
                return false;
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var fixedFields = new List<string>();

            // eventSystem prefab — SceneHelper.OnSceneLoaded Instantiates this
            if (GetField(sh, "eventSystem", flags) == null)
            {
                SetField(sh, "eventSystem", flags, BuildEventSystemPrefab());
                fixedFields.Add("eventSystem");
            }

            if (GetField(sh, "loadingBlocker", flags) == null)
            {
                SetField(sh, "loadingBlocker", flags, BuildHiddenPanel("UK_LoadingBlocker"));
                fixedFields.Add("loadingBlocker");
            }

            if (GetField(sh, "preloadingBadge", flags) == null)
            {
                SetField(sh, "preloadingBadge", flags, BuildHiddenPanel("UK_PreloadingBadge"));
                fixedFields.Add("preloadingBadge");
            }

            if (GetField(sh, "loadingBar", flags) == null)
            {
                SetField(sh, "loadingBar", flags, BuildLoadingBar());
                fixedFields.Add("loadingBar");
            }

            if (GetField(sh, "embeddedSceneInfo", flags) == null)
            {
                var infoType = Type.GetType("EmbeddedSceneInfo, Assembly-CSharp");
                if (infoType != null)
                {
                    var info = ScriptableObject.CreateInstance(infoType);
                    info.hideFlags = HideFlags.HideAndDontSave;
                    SetField(info, "specialScenes", BindingFlags.Instance | BindingFlags.Public, Array.Empty<string>());
                    SetField(info, "ranklessScenes", BindingFlags.Instance | BindingFlags.Public, Array.Empty<string>());
                    SetField(sh, "embeddedSceneInfo", flags, info);
                    fixedFields.Add("embeddedSceneInfo");
                }
            }

            // Unstick failed Play attempts
            ClearStuckLoadState(shType);

            if (Time.timeScale <= 0f)
            {
                Time.timeScale = 1f;
            }

            EnsureLiveEventSystem();

            if (fixedFields.Count > 0 || !s_loggedOnce)
            {
                UltrakillLog.Info(Area, "Repaired SceneHelper fields=[" + string.Join(",", fixedFields)
                    + "] CurrentScene='" + GetStaticString(shType, "CurrentScene")
                    + "' PendingScene='" + GetStaticString(shType, "PendingScene") + "'");
                s_loggedOnce = true;
            }

            return fixedFields.Count > 0 || GetField(sh, "eventSystem", flags) != null;
        }

        private static void ClearStuckLoadState(Type shType)
        {
            try
            {
                var pending = shType.GetProperty("PendingScene", BindingFlags.Public | BindingFlags.Static);
                var pendingVal = pending?.GetValue(null) as string;
                if (!string.IsNullOrEmpty(pendingVal))
                {
                    // Private setter via backing field
                    var backing = shType.GetField("<PendingScene>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static)
                        ?? shType.GetField("PendingScene", BindingFlags.NonPublic | BindingFlags.Static);
                    if (backing != null)
                    {
                        backing.SetValue(null, null);
                        UltrakillLog.Warn(Area, "Cleared stuck PendingScene='" + pendingVal + "'");
                    }
                }
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "PendingScene clear failed: " + ex.Message);
            }
        }

        private static string GetStaticString(Type t, string prop)
        {
            try
            {
                return t.GetProperty(prop, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static GameObject BuildEventSystemPrefab()
        {
            var go = new GameObject("UK_EventSystemPrefab");
            DontDestroyOnLoad(go);
            go.SetActive(false);
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            return go;
        }

        private static GameObject BuildHiddenPanel(string name)
        {
            var go = new GameObject(name);
            DontDestroyOnLoad(go);
            go.SetActive(false);
            return go;
        }

        private static TMP_Text BuildLoadingBar()
        {
            var go = new GameObject("UK_LoadingBar");
            DontDestroyOnLoad(go);
            go.SetActive(false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = "";
            return text;
        }

        private static void EnsureLiveEventSystem()
        {
            var systems = UnityEngine.Object.FindObjectsOfType<EventSystem>();
            if (systems.Length == 0)
            {
                var go = new GameObject("UltrakillIOS.EventSystem");
                DontDestroyOnLoad(go);
                go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                go.AddComponent<InputSystemUIInputModule>();
#else
                go.AddComponent<StandaloneInputModule>();
#endif
                UltrakillLog.Info(Area, "Spawned live EventSystem");
                return;
            }

            // Keep one; kill extras (SceneHelper may have left duplicates).
            for (var i = 1; i < systems.Length; i++)
            {
                if (systems[i] != null)
                {
                    UnityEngine.Object.Destroy(systems[i].gameObject);
                }
            }
        }

        private static void MuteBrokenBehaviours()
        {
            var disabled = 0;
            foreach (var name in AlwaysDisableWhenBroken)
            {
                var t = Type.GetType(name + ", Assembly-CSharp");
                if (t == null || !typeof(Behaviour).IsAssignableFrom(t))
                {
                    continue;
                }

                foreach (var obj in UnityEngine.Object.FindObjectsOfType(t, true))
                {
                    if (obj is Behaviour b && b.enabled)
                    {
                        b.enabled = false;
                        disabled++;
                    }
                }
            }

            if (disabled > 0)
            {
                UltrakillLog.Info(Area, "Muted " + disabled + " broken Update/NRE behaviours");
            }
        }

        private static void RemapBrokenShaders()
        {
            var fallback = Shader.Find("UltrakillIOS/UnlitTexture")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("UI/Default")
                ?? Shader.Find("Standard");
            if (fallback == null)
            {
                UltrakillLog.Warn(Area, "No fallback shader for magenta remap");
                return;
            }

            var remapped = 0;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                if (r == null)
                {
                    continue;
                }

                var mats = r.sharedMaterials;
                if (mats == null)
                {
                    continue;
                }

                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null)
                    {
                        continue;
                    }

                    var sh = m.shader;
                    if (sh == null || !sh.isSupported
                        || sh.name.Contains("InternalErrorShader")
                        || sh.name.Contains("Hidden/InternalError"))
                    {
                        m.shader = fallback;
                        remapped++;
                        changed = true;
                    }
                }

                if (changed)
                {
                    r.sharedMaterials = mats;
                }
            }

            // Also fix TMP / UI graphic materials
            foreach (var g in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Graphic>(true))
            {
                if (g != null && g.material != null)
                {
                    var sh = g.material.shader;
                    if (sh == null || sh.name.Contains("InternalErrorShader"))
                    {
                        g.material.shader = fallback;
                        remapped++;
                    }
                }
            }

            if (remapped > 0)
            {
                UltrakillLog.Info(Area, "Remapped " + remapped + " magenta/missing shaders -> " + fallback.name);
            }
        }

        private static object GetField(object obj, string name, BindingFlags flags)
        {
            return obj.GetType().GetField(name, flags)?.GetValue(obj);
        }

        private static void SetField(object obj, string name, BindingFlags flags, object value)
        {
            obj.GetType().GetField(name, flags)?.SetValue(obj, value);
        }
    }
}
#endif
