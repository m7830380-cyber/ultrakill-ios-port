#if ULTRAKILL_FULL_PORT
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Session 215625 proved stub-mode shader replacement binds tex=0 on 1331 slots (textured=0) — white walls.
    /// Retail materials keep their bundle shaders; we only replace truly broken shaders (InternalError / unsupported).
    /// </summary>
    internal static class RetailShaderRepair
    {
        private const long StubBundleMaxBytes = 5_000_000;

        public static bool ShadersBundleIsStub { get; private set; }

        private static Shader _unlitFallback;
        private static readonly HashSet<int> FixedSlots = new HashSet<int>();

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

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
                UltrakillLog.Warn("Shader",
                    "shaders.bundle is stub size (" + bytes + " bytes). Runtime will NOT replace working retail shaders "
                    + "(that path caused tex=0 white). Fix content: real iOS shaders.bundle.");
            }
        }

        internal static Shader WarmupFallbackShader()
        {
            if (_unlitFallback != null)
            {
                return _unlitFallback;
            }

            _unlitFallback = Shader.Find("UltrakillIOS/UnlitTexture")
                ?? Resources.Load<Shader>("Shaders/UltrakillIOS_UnlitTexture")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Unlit/Color");

            if (_unlitFallback != null)
            {
                UltrakillLog.Info("Shader", "Broken-shader fallback ready: " + _unlitFallback.name);
            }

            return _unlitFallback;
        }

        private static bool IsUiOrSky(string name)
        {
            return name.StartsWith("UI/", StringComparison.Ordinal)
                || name.StartsWith("TextMeshPro/", StringComparison.Ordinal)
                || name.StartsWith("Sprites/", StringComparison.Ordinal)
                || name.StartsWith("Skybox/", StringComparison.Ordinal)
                || name.StartsWith("GUI/", StringComparison.Ordinal);
        }

        private static bool IsInternalError(string name)
        {
            return !string.IsNullOrEmpty(name)
                && name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool SlotNeedsRepair(Material instance)
        {
            if (instance == null)
            {
                return false;
            }

            var sh = instance.shader;
            var name = sh != null ? sh.name : "";

            if (name.IndexOf("HideVertices", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Wireframe", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (sh == null || IsInternalError(name))
            {
                return true;
            }

            if (IsUiOrSky(name))
            {
                return false;
            }

            return !sh.isSupported;
        }

        private static Shader PickFallbackShader(Renderer r)
        {
            if (r != null && (r.lightmapIndex >= 0 || r.realtimeLightmapIndex >= 0))
            {
                var lm = Shader.Find("Mobile/Lightmap/Diffuse")
                    ?? Shader.Find("Legacy Shaders/Lightmapped/Diffuse")
                    ?? Shader.Find("Mobile/Diffuse");
                if (lm != null && lm.isSupported)
                {
                    return lm;
                }
            }

            return WarmupFallbackShader();
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
                if (!m.HasProperty(prop))
                {
                    continue;
                }

                try
                {
                    var t = m.GetTexture(prop);
                    if (t != null)
                    {
                        return t;
                    }
                }
                catch
                {
                    /* ignore */
                }
            }

            return null;
        }

        private static void ApplyPropertyBlockAlbedo(Renderer r, int submesh, Material mat)
        {
            if (r == null || mat == null || !mat.HasProperty("_MainTex"))
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block, submesh);
            var tex = block.GetTexture(MainTexId);
            if (tex == null)
            {
                tex = block.GetTexture(BaseMapId);
            }

            if (tex != null)
            {
                mat.SetTexture("_MainTex", tex);
            }
        }

        private static Material BuildReplacement(Renderer r, int submesh, Material instance, Material shared, Shader fallback)
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
                    /* ignore */
                }
            }

            repl.shader = fallback;

            var albedo = ExtractAlbedo(src);
            if (albedo != null && repl.HasProperty("_MainTex"))
            {
                repl.SetTexture("_MainTex", albedo);
            }

            ApplyPropertyBlockAlbedo(r, submesh, repl);

            return repl;
        }

        internal static int RemapBrokenMaterialsOnRenderers(bool includeInactive)
        {
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
                    if (!SlotNeedsRepair(m))
                    {
                        continue;
                    }

                    var slotKey = (r.GetInstanceID() << 4) ^ (i & 0xF);
                    if (FixedSlots.Contains(slotKey))
                    {
                        continue;
                    }

                    Material sharedSrc = shared != null && i < shared.Length ? shared[i] : null;
                    var fallback = PickFallbackShader(r);
                    if (fallback == null)
                    {
                        continue;
                    }

                    mats[i] = BuildReplacement(r, i, m, sharedSrc, fallback);
                    FixedSlots.Add(slotKey);
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
                if (sh != null && IsInternalError(sh.name) && sr.sprite != null)
                {
                    var def = Shader.Find("Sprites/Default");
                    if (def != null)
                    {
                        sr.material = new Material(def) { mainTexture = sr.sprite.texture };
                        remapped++;
                    }
                }
            }

            if (remapped > 0)
            {
                UltrakillLog.Info("Shader", "Fixed " + remapped + " broken shader slots (tex=" + withTex + " notex=" + noTex + ")");
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
                FixedSlots.Clear();
                StartCoroutine(RepairBrokenOnly());
            }

            private IEnumerator RepairBrokenOnly()
            {
                for (var i = 0; i < 12; i++)
                {
                    RemapBrokenMaterialsOnRenderers(includeInactive: true);
                    if (i == 2 || i == 11)
                    {
                        LogSceneMaterialStats();
                    }

                    yield return i < 4 ? null : new WaitForSecondsRealtime(0.5f);
                }
            }

            private static void LogSceneMaterialStats()
            {
                var retail = 0;
                var broken = 0;
                var texturedRetail = 0;
                var lightmapped = 0;

                foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
                {
                    if (r == null || r.GetComponentInParent<Canvas>() != null)
                    {
                        continue;
                    }

                    if (r.lightmapIndex >= 0 || r.realtimeLightmapIndex >= 0)
                    {
                        lightmapped++;
                    }

                    foreach (var m in r.materials)
                    {
                        if (m == null)
                        {
                            continue;
                        }

                        var sn = m.shader != null ? m.shader.name : "";
                        if (IsInternalError(sn) || (m.shader != null && !m.shader.isSupported))
                        {
                            broken++;
                            continue;
                        }

                        if (IsUiOrSky(sn))
                        {
                            continue;
                        }

                        retail++;
                        if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null)
                        {
                            texturedRetail++;
                        }
                    }
                }

                UltrakillLog.Info("Shader",
                    "Instances: retailMats=" + retail + " withMainTex=" + texturedRetail
                    + " broken=" + broken + " lightmappedRenderers=" + lightmapped
                    + " stubBundle=" + ShadersBundleIsStub);
            }
        }
    }
}
#endif
