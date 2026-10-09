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
        private static readonly HashSet<int> RepairedSlots = new HashSet<int>();

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

            _fallbackShader = Shader.Find("UltrakillIOS/UnlitTexture")
                ?? Resources.Load<Shader>("Shaders/UltrakillIOS_UnlitTexture")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Unlit/Color");

            if (_fallbackShader != null)
            {
                UltrakillLog.Info("Shader", "Fallback ready: " + _fallbackShader.name);
            }
            else
            {
                UltrakillLog.Warn("Shader", "Fallback shader missing");
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

        private static bool SkipMaterialSlot(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return name.IndexOf("HideVertices", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Wireframe", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool SlotNeedsRepair(Material instance, Material shared, Shader fallback)
        {
            if (instance == null)
            {
                return false;
            }

            var sh = instance.shader;
            var name = sh != null ? sh.name : "";

            if (SkipMaterialSlot(name))
            {
                return false;
            }

            if (sh == null || name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (IsUiOrSky(name))
            {
                return false;
            }

            if (sh != fallback)
            {
                return !sh.isSupported || ShadersBundleIsStub;
            }

            // Already on fallback — still broken if no albedo (white walls).
            return ShadersBundleIsStub && fallback != null && instance.HasProperty("_MainTex")
                && instance.GetTexture("_MainTex") == null && ExtractAlbedo(shared ?? instance) != null;
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

            string[] props =
            {
                "_MainTex", "_BaseMap", "_BaseColorMap", "_Albedo", "_Diffuse",
                "_ColorMap", "_MainTexture", "_Texture", "_EmissiveTex", "_DetailAlbedoMap",
            };

            foreach (var prop in props)
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

        private static Material BuildFallbackMaterial(Material instance, Material shared, Shader fallback)
        {
            var src = shared ?? instance;
            var repl = new Material(fallback);

            if (src != null)
            {
                try
                {
                    repl.CopyPropertiesFromMaterial(src);
                }
                catch
                {
                    /* broken shader materials can throw */
                }
            }

            repl.shader = fallback;

            var albedo = ExtractAlbedo(src) ?? ExtractAlbedo(repl);
            if (albedo != null && repl.HasProperty("_MainTex"))
            {
                repl.SetTexture("_MainTex", albedo);
            }

            if (repl.HasProperty("_Color"))
            {
                var c = repl.GetColor("_Color");
                if (c.maxColorComponent < 0.01f)
                {
                    repl.SetColor("_Color", Color.white);
                }
            }

            if (repl.HasProperty("_Colorize"))
            {
                var cz = repl.GetColor("_Colorize");
                if (cz.maxColorComponent < 0.01f)
                {
                    repl.SetColor("_Colorize", Color.white);
                }
            }

            return repl;
        }

        internal static int RemapBrokenMaterialsOnRenderers(bool includeInactive)
        {
            var fallback = WarmupFallbackShader();
            if (fallback == null)
            {
                return 0;
            }

            var remapped = 0;
            var withTex = 0;
            var noTex = 0;

            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(includeInactive))
            {
                if (r == null || r is ParticleSystemRenderer || r.GetComponentInParent<Canvas>() != null)
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
                    Material sharedSrc = shared != null && i < shared.Length ? shared[i] : null;

                    if (!SlotNeedsRepair(m, sharedSrc, fallback))
                    {
                        continue;
                    }

                    var slotKey = (r.GetInstanceID() << 4) ^ (i & 0xF);
                    mats[i] = BuildFallbackMaterial(m, sharedSrc, fallback);
                    RepairedSlots.Add(slotKey);
                    remapped++;
                    changed = true;

                    var t = mats[i].HasProperty("_MainTex") ? mats[i].GetTexture("_MainTex") : null;
                    if (t != null)
                    {
                        withTex++;
                    }
                    else
                    {
                        noTex++;
                    }
                }

                if (changed)
                {
                    r.materials = mats;
                }
            }

            foreach (var sr in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(includeInactive))
            {
                if (sr == null || sr.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                var sh = sr.sharedMaterial != null ? sr.sharedMaterial.shader : null;
                if (sh != null && sh.name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var def = Shader.Find("Sprites/Default");
                    if (def != null && sr.sprite != null)
                    {
                        sr.material = new Material(def) { mainTexture = sr.sprite.texture };
                        remapped++;
                    }
                }
            }

            if (remapped > 0)
            {
                UltrakillLog.Info("Shader", "Replaced " + remapped + " material slots -> " + fallback.name
                    + " (tex=" + withTex + " notex=" + noTex + ")");
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
                RepairedSlots.Clear();
                StartCoroutine(RemapBurst());
            }

            private IEnumerator RemapBurst()
            {
                int lastRemap = -1;
                for (var i = 0; i < 80; i++)
                {
                    var n = RemapBrokenMaterialsOnRenderers(includeInactive: true);
                    if (n > 0)
                    {
                        lastRemap = n;
                    }

                    if (i == 5 || i == 25 || i == 79)
                    {
                        LogSceneMaterialStats();
                    }

                    yield return i < 15 ? null : new WaitForSecondsRealtime(0.35f);
                }

                if (lastRemap >= 0)
                {
                    UltrakillLog.Info("Shader", "Remap burst finished; last pass replaced " + lastRemap + " slots");
                }
            }

            private static void LogSceneMaterialStats()
            {
                var fallback = WarmupFallbackShader();
                if (fallback == null)
                {
                    return;
                }

                var onFallback = 0;
                var textured = 0;
                var internalErr = 0;

                foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
                {
                    if (r == null || r.GetComponentInParent<Canvas>() != null)
                    {
                        continue;
                    }

                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null)
                        {
                            continue;
                        }

                        var sn = m.shader != null ? m.shader.name : "";
                        if (sn.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            internalErr++;
                            continue;
                        }

                        if (m.shader == fallback)
                        {
                            onFallback++;
                            if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null)
                            {
                                textured++;
                            }
                        }
                    }
                }

                UltrakillLog.Info("Shader", "Scene mats: fallback=" + onFallback + " textured=" + textured
                    + " internalError=" + internalErr);
            }
        }
    }
}
#endif
