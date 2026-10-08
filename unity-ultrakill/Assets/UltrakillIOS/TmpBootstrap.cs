#if ULTRAKILL_FULL_PORT
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// The IPA never imported TMP Essential Resources, so TMP_Settings.defaultStyleSheet is null and all text is blank.
    /// Inject a runtime TMP_Settings + style sheet, then pull a font from the external fonts.bundle.
    /// </summary>
    internal static class TmpBootstrap
    {
        private const string Area = "TMP";
        private static bool s_settingsReady;
        private static bool s_fontReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EnsureSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSettings();
            EnsureFontFromBundle();
            ApplyFontToScene();
        }

        private static void EnsureSettings()
        {
            if (s_settingsReady)
            {
                return;
            }

            try
            {
                var settingsType = typeof(TMP_Settings);
                var instanceField = settingsType.GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? settingsType.GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                // Prefer Resources if the IPA later ships real essentials.
                var existing = Resources.Load<TMP_Settings>("TMP Settings");
                if (existing != null)
                {
                    instanceField?.SetValue(null, existing);
                    s_settingsReady = true;
                    UltrakillLog.Info(Area, "Using Resources TMP Settings");
                    return;
                }

                var settings = ScriptableObject.CreateInstance<TMP_Settings>();
                settings.hideFlags = HideFlags.HideAndDontSave;
                var styleSheet = ScriptableObject.CreateInstance<TMP_StyleSheet>();
                styleSheet.hideFlags = HideFlags.HideAndDontSave;

                // Ensure at least a Normal style so GetStyle does not NRE.
                TryEnsureNormalStyle(styleSheet);

                SetField(settings, "m_defaultStyleSheet", styleSheet);
                SetField(settings, "m_enableEmojiSupport", true);
                SetField(settings, "m_getFontFeaturesAtRuntime", true);

                instanceField?.SetValue(null, settings);
                s_settingsReady = instanceField?.GetValue(null) != null;
                UltrakillLog.Info(Area, s_settingsReady
                    ? "Injected runtime TMP_Settings + empty style sheet"
                    : "Failed to inject TMP_Settings (field missing)");
            }
            catch (Exception ex)
            {
                UltrakillLog.Error(Area, "TMP settings bootstrap failed: " + ex.Message);
            }
        }

        private static void EnsureFontFromBundle()
        {
            if (s_fontReady)
            {
                return;
            }

            try
            {
                var streaming = ExternalContentBootstrap.ContentStreamingAssetsPath;
                if (string.IsNullOrEmpty(streaming))
                {
                    return;
                }

                var path = Path.Combine(streaming, "aa", "iOS", "assets_assets_assets", "fonts.bundle");
                if (!File.Exists(path))
                {
                    UltrakillLog.Warn(Area, "fonts.bundle not found at " + path);
                    return;
                }

                var bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                {
                    UltrakillLog.Warn(Area, "Could not open fonts.bundle");
                    return;
                }

                var fonts = bundle.LoadAllAssets<TMP_FontAsset>();
                UltrakillLog.Info(Area, "fonts.bundle TMP_FontAsset count=" + fonts.Length);
                var font = fonts.FirstOrDefault(f => f != null);
                if (font == null)
                {
                    return;
                }

                // Point materials at a shader that exists in this player.
                var shader = Shader.Find("TextMeshPro/Mobile/Distance Field")
                    ?? Shader.Find("TextMeshPro/Distance Field")
                    ?? Shader.Find("UI/Default_UK")
                    ?? Shader.Find("UI/Default");
                if (shader != null && font.material != null)
                {
                    font.material.shader = shader;
                }

                var settings = typeof(TMP_Settings)
                    .GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.GetValue(null) as TMP_Settings;
                if (settings != null)
                {
                    SetField(settings, "m_defaultFontAsset", font);
                }

                TMP_Settings.fallbackFontAssets?.Clear();
                s_fontReady = true;
                UltrakillLog.Info(Area, "Default TMP font set to " + font.name);
            }
            catch (Exception ex)
            {
                UltrakillLog.Error(Area, "Font load failed: " + ex.Message);
            }
        }

        private static void ApplyFontToScene()
        {
            if (!s_fontReady)
            {
                return;
            }

            TMP_FontAsset font = null;
            try
            {
                var settings = typeof(TMP_Settings)
                    .GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.GetValue(null) as TMP_Settings;
                font = GetField(settings, "m_defaultFontAsset") as TMP_FontAsset;
            }
            catch { /* ignore */ }

            if (font == null)
            {
                return;
            }

            var texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
            var fixedCount = 0;
            foreach (var text in texts)
            {
                if (text.font == null)
                {
                    text.font = font;
                    fixedCount++;
                }

                if (text.fontSharedMaterial != null)
                {
                    var shader = Shader.Find("TextMeshPro/Mobile/Distance Field")
                        ?? Shader.Find("UI/Default_UK")
                        ?? Shader.Find("UI/Default");
                    if (shader != null && text.fontSharedMaterial.shader != shader)
                    {
                        text.fontSharedMaterial.shader = shader;
                    }
                }
            }

            UltrakillLog.Info(Area, "Applied TMP font to scene texts; null-font fixes=" + fixedCount + " total=" + texts.Length);
        }

        private static void TryEnsureNormalStyle(TMP_StyleSheet sheet)
        {
            try
            {
                var style = new TMP_Style("Normal", string.Empty, string.Empty);
                var listField = typeof(TMP_StyleSheet).GetField("m_StyleList", BindingFlags.Instance | BindingFlags.NonPublic);
                if (listField == null)
                {
                    return;
                }

                var list = Activator.CreateInstance(listField.FieldType) as System.Collections.IList;
                list?.Add(style);
                listField.SetValue(sheet, list);
                typeof(TMP_StyleSheet)
                    .GetMethod("RefreshStyles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.Invoke(sheet, null);
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "Could not seed Normal TMP style: " + ex.Message);
            }
        }

        private static void SetField(object obj, string name, object value)
        {
            if (obj == null)
            {
                return;
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var field = obj.GetType().GetField(name, flags);
            field?.SetValue(obj, value);
        }

        private static object GetField(object obj, string name)
        {
            if (obj == null)
            {
                return null;
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            return obj.GetType().GetField(name, flags)?.GetValue(obj);
        }
    }
}
#endif
