#if ULTRAKILL_FULL_PORT
using System;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Only touch materials whose shader is missing or unsupported (pink/magenta).
    /// Retail ULTRAKILL shaders come from addressables shaders.bundle — do not blanket-remap.
    /// </summary>
    internal static class RetailShaderRepair
    {
        private static readonly string[] AlbedoTexAliases =
        {
            "_MainTex", "_BaseMap", "_BaseColorMap", "_Albedo", "_Diffuse",
            "_ColorMap", "_MainTexture", "_Texture", "_tex",
        };

        internal static bool ShaderNeedsFallback(Shader sh)
        {
            if (sh == null)
            {
                return true;
            }

            if (!sh.isSupported)
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

            if (name.StartsWith("Hidden/InternalError", StringComparison.Ordinal))
            {
                return true;
            }

            // Already our textured fallback.
            if (name.StartsWith("UltrakillIOS/", StringComparison.Ordinal))
            {
                return false;
            }

            if (name.StartsWith("UI/", StringComparison.Ordinal)
                || name.StartsWith("TextMeshPro/", StringComparison.Ordinal)
                || name.StartsWith("Sprites/", StringComparison.Ordinal)
                || name.StartsWith("Skybox/", StringComparison.Ordinal))
            {
                return false;
            }

            return false;
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

                var mats = r.sharedMaterials;
                if (mats == null)
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

                    Texture albedo = null;
                    foreach (var prop in AlbedoTexAliases)
                    {
                        if (!m.HasProperty(prop))
                        {
                            continue;
                        }

                        var t = m.GetTexture(prop);
                        if (t != null)
                        {
                            albedo = t;
                            break;
                        }
                    }

                    m.shader = fallback;
                    if (m.HasProperty("_MainTex") && albedo != null)
                    {
                        m.SetTexture("_MainTex", albedo);
                    }

                    if (m.HasProperty("_Color"))
                    {
                        var c = m.GetColor("_Color");
                        if (c.maxColorComponent < 0.01f)
                        {
                            m.SetColor("_Color", Color.white);
                        }
                    }

                    if (m.HasProperty("_Colorize"))
                    {
                        m.SetColor("_Colorize", Color.white);
                    }

                    remapped++;
                    changed = true;
                }

                if (changed)
                {
                    r.sharedMaterials = mats;
                }
            }

            return remapped;
        }
    }
}
#endif
