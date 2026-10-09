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
        private static readonly Dictionary<int, Texture> AlbedoByMaterialId = new Dictionary<int, Texture>(4096);

        private static readonly string[] AlbedoTexAliases =
        {
            "_MainTex", "_BaseMap", "_BaseColorMap", "_Albedo", "_Diffuse",
            "_ColorMap", "_MainTexture", "_Texture", "_tex", "_EmissiveTex",
            "EmissiveTex", "_DetailAlbedoMap", "_ParallaxMap",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallHost()
        {
            WarmupFallbackShader();
            var go = new GameObject("UltrakillIOS.ShaderRepair");
            DontDestroyOnLoad(go);
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
            else if (!_fallbackShader.isSupported)
            {
                UltrakillLog.Warn("Shader", "Fallback shader not supported: " + _fallbackShader.name);
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

        private static bool IsHiddenUtility(string name)
        {
            return name.StartsWith("Hidden/", StringComparison.Ordinal)
                || name.StartsWith("Legacy Shaders/", StringComparison.Ordinal);
        }

        internal static bool ShaderNeedsFallback(Shader sh)
        {
            if (sh == null)
            {
                return true;
            }

            var name = sh.name ?? "";
            if (name.Length == 0 || name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (name.StartsWith("UltrakillIOS/", StringComparison.Ordinal))
            {
                return false;
            }

            if (IsUiOrSky(name) || IsHiddenUtility(name))
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

            return m.GetTexture("_MainTex") == null && AlbedoByMaterialId.ContainsKey(m.GetInstanceID());
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

            var fromInstance = ExtractAlbedo(m);
            if (fromInstance != null)
            {
                return fromInstance;
            }

            if (sharedSource != null && sharedSource != m)
            {
                return ExtractAlbedo(sharedSource);
            }

            return null;
        }

        private static bool ApplyFallbackMaterial(Material m, Material sharedSource, Shader fallback)
        {
            if (m == null)
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
            var hiddenDisabled = 0;

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
                    if (IsHiddenUtility(sn))
                    {
                        if (r.enabled)
                        {
                            r.enabled = false;
                            hiddenDisabled++;
                        }

                        continue;
                    }

                    Material sharedSrc = shared != null && i < shared.Length ? shared[i] : null;
                    if (ApplyFallbackMaterial(m, sharedSrc, fallback))
                    {
                        remapped++;
                        changed = true;
                    }
                }

                if (changed)
                {
                    r.materials = mats;
                }
            }

            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain?.materialTemplate == null)
                {
                    continue;
                }

                if (ApplyFallbackMaterial(terrain.materialTemplate, terrain.materialTemplate, fallback))
                {
                    remapped++;
                }
            }

            if (remapped > 0)
            {
                var mode = ShadersBundleIsStub ? "stub-bundle textured" : "broken-shader";
                UltrakillLog.Info("Shader", "Remapped " + remapped + " world material slots -> " + fallback.name + " (" + mode + ")");
            }

            if (hiddenDisabled > 0)
            {
                UltrakillLog.Info("Shader", "Disabled " + hiddenDisabled + " Hidden/utility renderers");
            }

            return remapped;
        }

        private sealed class RetailShaderRepairHost : MonoBehaviour
        {
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
                StartCoroutine(RemapBurst());
            }

            private IEnumerator RemapBurst()
            {
                for (var i = 0; i < 240; i++)
                {
                    RemapBrokenMaterialsOnRenderers(includeInactive: true);
                    yield return i < 30 ? null : new WaitForSecondsRealtime(0.25f);
                }
            }
        }
    }
}
#endif
