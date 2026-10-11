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
    /// IPA ships no TMP Essential Resources. Inject settings + a real font from Resources/Fonts/Arial.
    /// Never AssetBundle.LoadFromFile fonts.bundle (Addressables owns it).
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
            SceneManager.sceneLoaded += (_, __) =>
            {
                EnsureSettings();
                EnsureRuntimeFont();
                ApplyFontToScene();
            };
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
                var instanceField = settingsType.GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);

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
                    : "Failed to inject TMP_Settings");
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
                var source = Resources.Load<Font>("Fonts/Arial")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (source == null)
                {
                    UltrakillLog.Warn(Area, "No Arial font in Resources or builtin");
                    return;
                }

                var font = TMP_FontAsset.CreateFontAsset(
                    source,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic);

                if (font == null)
                {
                    UltrakillLog.Warn(Area, "CreateFontAsset failed for " + source.name);
                    return;
                }

                font.name = "UltrakillIOS-ArialSDF";
                font.hideFlags = HideFlags.HideAndDontSave;

                var shader = Shader.Find("TextMeshPro/Mobile/Distance Field")
                    ?? Shader.Find("TextMeshPro/Distance Field")
                    ?? Shader.Find("UI/Default");
                if (shader != null)
                {
                    if (font.material != null)
                    {
                        font.material.shader = shader;
                    }

                    if (font.material == null)
                    {
                        font.material = new Material(shader) { name = "UltrakillIOS-ArialMat" };
                    }
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
                UltrakillLog.Info(Area, "Runtime TMP font ready from '" + source.name + "' shader=" + (shader != null ? shader.name : "null"));
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
            var replaced = 0;
            var shader = Shader.Find("TextMeshPro/Mobile/Distance Field")
                ?? Shader.Find("TextMeshPro/Distance Field")
                ?? Shader.Find("UI/Default");

            foreach (var text in texts)
            {
                if (text == null)
                {
                    continue;
                }

                text.font = s_font;
                if (shader != null)
                {
                    text.fontSharedMaterial = s_font.material;
                }

                replaced++;
            }

            UltrakillLog.Info(Area, "Applied runtime TMP font to " + replaced + " texts");
        }

        private static void SetField(object obj, string name, object value)
        {
            if (obj == null)
            {
                return;
            }

            obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?.SetValue(obj, value);
        }
    }
}
#endif
