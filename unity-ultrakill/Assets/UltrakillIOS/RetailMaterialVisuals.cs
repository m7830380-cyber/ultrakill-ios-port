#if ULTRAKILL_FULL_PORT
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltrakillIOS
{
    internal static class RetailMaterialVisuals
    {
        private static Dictionary<string, Texture> _texturesByName;

        internal static bool IsBrokenShader(Shader shader)
        {
            var n = shader != null ? shader.name : "";
            return n.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static void CopyTexturesAndShader(Material dest, Material src)
        {
            if (dest == null || src == null || src.shader == null || IsBrokenShader(src.shader))
            {
                return;
            }

            var resolved = RetailShaderRegistry.Resolve(src.shader.name);
            if (resolved != null)
            {
                dest.shader = resolved;
            }
            else if (!IsBrokenShader(dest.shader))
            {
                dest.shader = src.shader;
            }

            try
            {
                foreach (var prop in src.GetTexturePropertyNames())
                {
                    var tex = src.GetTexture(prop);
                    if (tex != null && dest.HasProperty(prop))
                    {
                        dest.SetTexture(prop, tex);
                    }
                }

                if (dest.HasProperty("_Color") && src.HasProperty("_Color"))
                {
                    dest.SetColor("_Color", src.GetColor("_Color"));
                }

                if (dest.HasProperty("_Colorize") && src.HasProperty("_Colorize"))
                {
                    dest.SetColor("_Colorize", src.GetColor("_Colorize"));
                }
            }
            catch
            {
                /* ignore */
            }
        }

        internal static void RebuildTextureIndex()
        {
            _texturesByName = new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);
            foreach (var tex in Resources.FindObjectsOfTypeAll<Texture>())
            {
                if (tex == null || string.IsNullOrEmpty(tex.name))
                {
                    continue;
                }

                _texturesByName[tex.name] = tex;
                var stem = StripInstanceSuffix(tex.name);
                if (!_texturesByName.ContainsKey(stem))
                {
                    _texturesByName[stem] = tex;
                }
            }
        }

        internal static int HydrateMissingTextures(Material mat)
        {
            if (mat == null || RetailContentWarmup.MaterialHasAnyTexture(mat))
            {
                return 0;
            }

            if (_texturesByName == null || _texturesByName.Count == 0)
            {
                RebuildTextureIndex();
            }

            var stem = StripInstanceSuffix(mat.name);
            if (!_texturesByName.TryGetValue(stem, out var pick))
            {
                foreach (var kv in _texturesByName)
                {
                    if (stem.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0
                        || kv.Key.IndexOf(stem, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        pick = kv.Value;
                        break;
                    }
                }
            }

            if (pick == null)
            {
                return 0;
            }

            var n = 0;
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", pick);
                n++;
            }

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", pick);
                n++;
            }

            return n > 0 ? 1 : 0;
        }

        private static string StripInstanceSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            const string inst = " (Instance)";
            if (name.EndsWith(inst, StringComparison.Ordinal))
            {
                return name.Substring(0, name.Length - inst.Length);
            }

            return name;
        }
    }
}
#endif
