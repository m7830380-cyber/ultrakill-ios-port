#if ULTRAKILL_FULL_PORT
using System;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Retail materials reference shaders from addressables shaders.bundle. When that bundle is
    /// the tiny iOS stub (~0.6MB), many shaders fail at runtime (magenta) or draw untextured white.
    /// </summary>
    internal static class RetailShaderRepair
    {
        private const long StubBundleMaxBytes = 5_000_000;

        /// <summary>True when phone content has stub shaders.bundle — remap world mats to textured fallback.</summary>
        public static bool ShadersBundleIsStub { get; private set; }

        private static readonly string[] AlbedoTexAliases =
        {
            "_MainTex", "_BaseMap", "_BaseColorMap", "_Albedo", "_Diffuse",
            "_ColorMap", "_MainTexture", "_Texture", "_tex",
        };

        public static void RegisterShadersBundleSize(long bytes)
        {
            ShadersBundleIsStub = bytes > 0 && bytes < StubBundleMaxBytes;
            if (ShadersBundleIsStub)
            {
                UltrakillLog.Warn("Shader", "shaders.bundle is stub size (" + bytes + " bytes); using textured fallback for world materials");
            }
        }

        private static bool IsUiOrSky(string name)
        {
            return name.StartsWith("UI/", StringComparison.Ordinal)
                || name.StartsWith("TextMeshPro/", StringComparison.Ordinal)
                || name.StartsWith("Sprites/", StringComparison.Ordinal)
                || name.StartsWith("Skybox/", StringComparison.Ordinal)
                || name.StartsWith("GUI/", StringComparison.Ordinal);
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

            if (IsUiOrSky(name))
            {
                return false;
            }

            if (!sh.isSupported)
            {
                return true;
            }

            // Stub bundle: retail shader names compile but break or draw wrong — force textured unlit.
            if (ShadersBundleIsStub)
            {
                return true;
            }

            return false;
        }

        private static Texture ExtractAlbedo(Material m)
        {
            foreach (var prop in AlbedoTexAliases)
            {
                if (!m.HasProperty(prop))
                {
                    continue;
                }

                var t = m.GetTexture(prop);
                if (t != null)
                {
                    return t;
                }
            }

            if (m.mainTexture != null)
            {
                return m.mainTexture;
            }

            return null;
        }

        internal static int RemapBrokenMaterialsOnRenderers(bool includeInactive)
        {
            var fallback = Shader.Find("UltrakillIOS/UnlitTexture")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Unlit/Color");
            if (fallback == null)
            {
                UltrakillLog.Warn("Shader", "No textured fallback shader available");
                return 0;
            }

            var remapped = 0;
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

                var mats = r.materials;
                if (mats == null || mats.Length == 0)
                {
                    continue;
                }

                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !ShaderNeedsFallback(m.shader))
                    {
                        continue;
                    }

                    var albedo = ExtractAlbedo(m);
                    var color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                    var colorize = m.HasProperty("_Colorize") ? m.GetColor("_Colorize") : Color.white;

                    m.shader = fallback;
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

                    remapped++;
                    changed = true;
                }

                if (changed)
                {
                    r.materials = mats;
                }
            }

            if (remapped > 0)
            {
                var mode = ShadersBundleIsStub ? "stub-bundle textured" : "broken-shader";
                UltrakillLog.Info("Shader", "Remapped " + remapped + " world materials -> " + fallback.name + " (" + mode + ")");
            }

            return remapped;
        }
    }
}
#endif
