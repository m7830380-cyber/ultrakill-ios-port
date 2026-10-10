#if ULTRAKILL_FULL_PORT
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>Shaders resident after Addressables / warmup — Shader.Find and material repair use this map.</summary>
    internal static class RetailShaderRegistry
    {
        private static readonly Dictionary<string, Shader> ByName = new Dictionary<string, Shader>(StringComparer.OrdinalIgnoreCase);

        internal static void Register(Shader shader)
        {
            if (shader == null || string.IsNullOrEmpty(shader.name))
            {
                return;
            }

            if (shader.name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            if (!shader.isSupported)
            {
                return;
            }

            ByName[shader.name] = shader;
        }

        internal static void RefreshFromMemory()
        {
            foreach (var shader in Resources.FindObjectsOfTypeAll<Shader>())
            {
                Register(shader);
            }
        }

        internal static Shader Resolve(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName))
            {
                return null;
            }

            if (shaderName.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return null;
            }

            if (ByName.TryGetValue(shaderName, out var cached) && cached != null)
            {
                return cached;
            }

            var found = Shader.Find(shaderName);
            if (found != null && found.isSupported)
            {
                Register(found);
                return found;
            }

            return null;
        }

        internal static int Count => ByName.Count;
    }
}
#endif
