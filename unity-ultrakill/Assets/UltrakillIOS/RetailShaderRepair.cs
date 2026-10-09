#if ULTRAKILL_FULL_PORT
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    internal static class RetailShaderRepair
    {
        private const long StubBundleMaxBytes = 5_000_000;

        public static bool ShadersBundleIsStub { get; private set; }

        private static Shader _fallbackShader;
        private static readonly Dictionary<int, Texture> AlbedoByMaterialId = new Dictionary<int, Texture>(8192);

        private static readonly string[] AlbedoTexAliases =
        {
            "_MainTex", "_BaseMap", "_BaseColorMap", "_Albedo", "_Diffuse",
            "_ColorMap", "_MainTexture", "_Texture", "_tex", "_EmissiveTex",
            "EmissiveTex", "_DetailAlbedoMap", "_ParallaxMap", "_MetallicGlossMap",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallHost()
        {
            WarmupFallbackShader();
            var go = new GameObject("UltrakillIOS.ShaderRepair");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<RetailShaderRepairHost>();
        }

        public static void RegisterShadersBundleSize(long bytes)
        {
            ShadersBundleIsStub = bytes > 0 && bytes < StubBundleMaxBytes;
            if (ShadersBundleIsStub)
            {
                UltrakillLog.Warn("Shader", "shaders.bundle is stub size (" + bytes + " bytes); forcing textured fallback for world materials");
            }
        }

        internal static Shader WarmupFallbackShader()
        {
            if (_fallbackShader != null)
            {
                return _fallbackShader;
            }

            _fallbackShader = Shader.Find("UltrakillIOS/UnlitTexture");
            if (_fallbackShader == null)
            {
                _fallbackShader = Resources.Load<Shader>("Shaders/UltrakillIOS_UnlitTexture");
            }

            if (_fallbackShader == null)
            {
                _fallbackShader = Shader.Find("Unlit/Texture") ?? Shader.Find("Unlit/Color");
            }

            if (_fallbackShader == null)
            {
                UltrakillLog.Warn("Shader", "Fallback shader missing — world will stay magenta");
            }
            else
            {
                UltrakillLog.Info("Shader", "Fallback ready: " + _fallbackShader.name);
            }

            return _fallbackShader;
        }

        private static bool IsUiOrSky(string name)
        {
            return name.StartsWith("UI/", StringComparison.Ordinal)
                || name.StartsWith("TextMeshPro/", StringComparison.Ordinal)
                || name.StartsWith("Sprites/", StringComparison.Ordinal)
                || name.StartsWith("Skybox/", StringComparison.Ordinal)
                || name.StartsWith("GUI/", StringComparison.Ordinal);
        }

        /// <summary>Skip this material slot only (do not remap). Never treat InternalError as skippable.</summary>
        private static bool SkipMaterialSlot(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return name.IndexOf("HideVertices", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Wireframe", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool ShaderNeedsFallback(Shader sh)
        {
            if (sh == null)
            {
                return true;
            }

            var name = sh.name ?? "";
            if (name.Length == 0)
            {
                return true;
            }

            if (name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (name.StartsWith("UltrakillIOS/", StringComparison.Ordinal))
            {
                return false;
            }

            if (IsUiOrSky(name) || SkipMaterialSlot(name))
            {
                return false;
            }

            if (!sh.isSupported || ShadersBundleIsStub)
            {
                return true;
            }

            return false;
        }

        private static bool NeedsTextureBackfill(Material m)
        {
            if (m == null || _fallbackShader == null || m.shader != _fallbackShader)
            {
                return false;
            }

            if (!m.HasProperty("_MainTex"))
            {
                return false;
            }

            return m.GetTexture("_MainTex") == null;
        }

        private static Texture SafeGetTexture(Material m, string prop)
        {
            if (m == null || string.IsNullOrEmpty(prop) || !m.HasProperty(prop))
            {
                return null;
            }

            try
            {
                return m.GetTexture(prop);
            }
            catch
            {
                return null;
            }
        }

        private static Texture ExtractAlbedo(Material m)
        {
            if (m == null)
            {
                return null;
            }

            foreach (var prop in AlbedoTexAliases)
            {
                var t = SafeGetTexture(m, prop);
                if (t != null)
                {
                    return t;
                }
            }

            try
            {
                foreach (var prop in m.GetTexturePropertyNames())
                {
                    var t = SafeGetTexture(m, prop);
                    if (t != null)
                    {
                        return t;
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            return null;
        }

        private static void RememberAlbedo(Material m, Texture tex)
        {
            if (m != null && tex != null)
            {
                AlbedoByMaterialId[m.GetInstanceID()] = tex;
            }
        }

        private static Texture RecallAlbedo(Material m, Material sharedSource)
        {
            if (m != null && AlbedoByMaterialId.TryGetValue(m.GetInstanceID(), out var cached) && cached != null)
            {
                return cached;
            }

            if (sharedSource != null)
            {
                var fromShared = ExtractAlbedo(sharedSource);
                if (fromShared != null)
                {
                    return fromShared;
                }
            }

            return ExtractAlbedo(m);
        }

        private static bool ApplyFallbackMaterial(Material m, Material sharedSource, Shader fallback)
        {
            if (m == null)
            {
                return false;
            }

            var sn = m.shader != null ? m.shader.name : "";
            if (SkipMaterialSlot(sn))
            {
                return false;
            }

            var needsShader = ShaderNeedsFallback(m.shader);
            var needsTex = NeedsTextureBackfill(m);
            if (!needsShader && !needsTex)
            {
                return false;
            }

            var albedo = RecallAlbedo(m, sharedSource);
            if (albedo != null)
            {
                RememberAlbedo(m, albedo);
            }

            var color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            var colorize = m.HasProperty("_Colorize") ? m.GetColor("_Colorize") : Color.white;

            if (needsShader)
            {
                m.shader = fallback;
            }

            if (m.HasProperty("_MainTex") && albedo != null)
            {
                m.SetTexture("_MainTex", albedo);
            }

            if (m.HasProperty("_Color"))
            {
                m.SetColor("_Color", color.maxColorComponent < 0.01f ? Color.white : color);
            }

            if (m.HasProperty("_Colorize"))
            {
                m.SetColor("_Colorize", colorize.maxColorComponent < 0.01f ? Color.white : colorize);
            }

            return true;
        }

        internal static int RemapBrokenMaterialsOnRenderers(bool includeInactive)
        {
            var fallback = WarmupFallbackShader();
            if (fallback == null)
            {
                return 0;
            }

            var remapped = 0;
            var internalErrorLeft = 0;

            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(includeInactive))
            {
                if (r == null || r is ParticleSystemRenderer)
                {
                    continue;
                }

                if (r.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                var shared = r.sharedMaterials;
                var mats = r.materials;
                if (mats == null || mats.Length == 0)
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

                    var sn = m.shader != null ? m.shader.name : "";
                    if (SkipMaterialSlot(sn))
                    {
                        continue;
                    }

                    Material sharedSrc = shared != null && i < shared.Length ? shared[i] : null;
                    if (ApplyFallbackMaterial(m, sharedSrc, fallback))
                    {
                        remapped++;
                        changed = true;
                    }
                    else if (sn.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        internalErrorLeft++;
                    }
                }

                if (changed)
                {
                    r.materials = mats;
                }
            }

            if (remapped > 0)
            {
                var mode = ShadersBundleIsStub ? "stub-bundle textured" : "broken-shader";
                UltrakillLog.Info("Shader", "Remapped " + remapped + " world material slots -> " + fallback.name + " (" + mode + ")");
            }

            if (internalErrorLeft > 0)
            {
                UltrakillLog.Warn("Shader", internalErrorLeft + " material slots still on InternalErrorShader after pass");
            }

            return remapped;
        }

        private sealed class RetailShaderRepairHost : MonoBehaviour
        {
            private int _pass;

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
                _pass = 0;
                StartCoroutine(RemapBurst());
            }

            private IEnumerator RemapBurst()
            {
                for (var i = 0; i < 300; i++)
                {
                    RemapBrokenMaterialsOnRenderers(includeInactive: true);
                    _pass++;
                    if (_pass == 1 || _pass == 20 || _pass == 120)
                    {
                        LogInternalErrorRenderers();
                    }

                    yield return i < 40 ? null : new WaitForSecondsRealtime(0.2f);
                }
            }

            private static void LogInternalErrorRenderers()
            {
                var n = 0;
                foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
                {
                    if (r == null || r.GetComponentInParent<Canvas>() != null)
                    {
                        continue;
                    }

                    foreach (var m in r.sharedMaterials)
                    {
                        if (m?.shader != null && m.shader.name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            n++;
                            break;
                        }
                    }
                }

                if (n > 0)
                {
                    UltrakillLog.Warn("Shader", "Renderers still using InternalErrorShader: " + n);
                }
            }
        }
    }
}
#endif
