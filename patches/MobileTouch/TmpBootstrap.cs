#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace UltrakillIOS
{
    /// <summary>
    /// IPA has no TMP Essential Resources. Inject TMP_Settings + a dynamic OS font.
    /// Do NOT AssetBundle.LoadFromFile fonts.bundle — Addressables owns that file; a second
    /// load makes Main Menu fail with "same files is already loaded".
    /// </summary>
    internal static class TmpBootstrap
    {
        private const string Area = "TMP";
        private static bool s_settingsReady;
        private static bool s_fontReady;
        private static TMP_FontAsset s_font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EnsureSettings();
            EnsureRuntimeFont();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSettings();
            EnsureRuntimeFont();
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

        private static void EnsureRuntimeFont()
        {
            if (s_fontReady && s_font != null)
            {
                return;
            }

            try
            {
                var osFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Helvetica Neue", "Helvetica", "Arial", "San Francisco" },
                    90);
                if (osFont == null)
                {
                    UltrakillLog.Warn(Area, "OS font unavailable");
                    return;
                }

                var font = TMP_FontAsset.CreateFontAsset(
                    osFont,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic);

                if (font == null)
                {
                    UltrakillLog.Warn(Area, "TMP_FontAsset.CreateFontAsset returned null");
                    return;
                }

                font.name = "UltrakillIOS-RuntimeFont";
                font.hideFlags = HideFlags.HideAndDontSave;

                var shader = Shader.Find("TextMeshPro/Mobile/Distance Field")
                    ?? Shader.Find("TextMeshPro/Distance Field")
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

                s_font = font;
                s_fontReady = true;
                UltrakillLog.Info(Area, "Runtime TMP font ready from OS font '" + osFont.name + "'");
            }
            catch (Exception ex)
            {
                UltrakillLog.Error(Area, "Runtime font create failed: " + ex.Message);
            }
        }

        private static void ApplyFontToScene()
        {
            if (!s_fontReady || s_font == null)
            {
                return;
            }

            var texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
            var fixedCount = 0;
            foreach (var text in texts)
            {
                if (text == null)
                {
                    continue;
                }

                // Retail scene fonts often deserialize as missing scripts; replace everything.
                if (text.font != s_font)
                {
                    text.font = s_font;
                    fixedCount++;
                }

                if (text.fontSharedMaterial != null)
                {
                    var shader = Shader.Find("TextMeshPro/Mobile/Distance Field")
                        ?? Shader.Find("TextMeshPro/Distance Field")
                        ?? Shader.Find("UI/Default");
                    if (shader != null && text.fontSharedMaterial.shader != shader)
                    {
                        text.fontSharedMaterial.shader = shader;
                    }
                }
            }

            UltrakillLog.Info(Area, "Applied runtime TMP font; replaced=" + fixedCount + " total=" + texts.Length);
        }

        private static void SetField(object obj, string name, object value)
        {
            if (obj == null)
            {
                return;
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            obj.GetType().GetField(name, flags)?.SetValue(obj, value);
        }
    }
}
#endif
