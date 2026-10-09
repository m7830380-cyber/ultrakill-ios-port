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
                    "shaders.bundle stub (" + bytes + " B). Rebind + hydrate textures; build real iOS shaders.bundle for full look.");
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

        /// <summary>Only null shader or InternalError — do NOT swap supported retail/stub-bundle shaders.</summary>
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

            if (IsUiOrSky(name))
            {
                return false;
            }

            return sh == null || IsInternalError(name);
        }

        private static bool TryRebindShaderFromBundle(Material mat)
        {
            if (mat == null || mat.shader == null)
            {
                return false;
            }

            var name = mat.shader.name;
            if (string.IsNullOrEmpty(name) || IsInternalError(name))
            {
                return false;
            }

            var resolved = Shader.Find(name);
            if (resolved == null || !resolved.isSupported)
            {
                return false;
            }

            if (resolved != mat.shader)
            {
                mat.shader = resolved;
            }

            return true;
        }

        private static Texture ExtractAlbedo(Material m)
        {
            if (m == null)
            {
                return null;
            }

            if (m.HasProperty("_MainTex"))
            {
                var main = m.GetTexture("_MainTex");
                if (main != null)
                {
                    return main;
                }
            }

            try
            {
                foreach (var prop in m.GetTexturePropertyNames())
                {
                    if (string.IsNullOrEmpty(prop))
                    {
                        continue;
                    }

                    var t = m.GetTexture(prop);
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

        private static bool HydrateMainTex(Material mat, Renderer r, int submesh)
        {
            if (mat == null || !mat.HasProperty("_MainTex"))
            {
                return false;
            }

            if (mat.GetTexture("_MainTex") != null)
            {
                return true;
            }

            var albedo = ExtractAlbedo(mat);
            if (albedo == null && r != null)
            {
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block, submesh);
                albedo = block.GetTexture(MainTexId) ?? block.GetTexture(BaseMapId);
            }

            if (albedo == null)
            {
                return false;
            }

            mat.SetTexture("_MainTex", albedo);
            return true;
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

        private static Material BuildReplacement(Renderer r, int submesh, Material shared, Shader fallback)
        {
            var src = shared;
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
            HydrateMainTex(repl, r, submesh);
            return repl;
        }

        /// <summary>Stub bundle: re-resolve shader names + copy any texture slot onto _MainTex.</summary>
        internal static int RebindAndHydrateStubMaterials(bool includeInactive)
        {
            if (!ShadersBundleIsStub)
            {
                return 0;
            }

            var rebound = 0;
            var hydrated = 0;

            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(includeInactive))
            {
                if (r == null || r is ParticleSystemRenderer || r.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                var shared = r.sharedMaterials;
                if (shared == null || shared.Length == 0)
                {
                    continue;
                }

                var dirty = false;
                for (var i = 0; i < shared.Length; i++)
                {
                    var m = shared[i];
                    if (m == null || IsUiOrSky(m.shader != null ? m.shader.name : ""))
                    {
                        continue;
                    }

                    if (TryRebindShaderFromBundle(m))
                    {
                        rebound++;
                        dirty = true;
                    }

                    if (HydrateMainTex(m, r, i))
                    {
                        hydrated++;
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    r.sharedMaterials = shared;
                }
            }

            if (rebound > 0 || hydrated > 0)
            {
                UltrakillLog.Info("Shader", "Stub hydrate: rebound=" + rebound + " hydratedSlots=" + hydrated);
            }

            return rebound + hydrated;
        }

        internal static int RemapBrokenMaterialsOnRenderers(bool includeInactive)
        {
            RebindAndHydrateStubMaterials(includeInactive);

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

                    var slotKey = (r.GetInstanceID() << 8) ^ i;
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

                    mats[i] = BuildReplacement(r, i, sharedSrc, fallback);
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
                UltrakillLog.Info("Shader", "Fixed " + remapped + " InternalError slots (tex=" + withTex + " notex=" + noTex + ")");
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
                StartCoroutine(RepairPipeline());
            }

            private IEnumerator RepairPipeline()
            {
                for (var i = 0; i < 14; i++)
                {
                    StaticSceneOptimizerIosRepair.KickAllInLoadedScenes();
                    RemapBrokenMaterialsOnRenderers(includeInactive: true);

                    if (i == 2 || i == 8 || i == 13)
                    {
                        LogSceneMaterialStats();
                    }

                    yield return i < 5 ? null : new WaitForSecondsRealtime(0.4f);
                }
            }

            private static void LogSceneMaterialStats()
            {
                var retail = 0;
                var broken = 0;
                var texturedRetail = 0;
                var lightmapped = 0;
                var ultrakillFallback = 0;

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

                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null)
                        {
                            continue;
                        }

                        var sn = m.shader != null ? m.shader.name : "";
                        if (IsInternalError(sn))
                        {
                            broken++;
                            continue;
                        }

                        if (IsUiOrSky(sn))
                        {
                            continue;
                        }

                        if (sn.StartsWith("UltrakillIOS/", StringComparison.Ordinal))
                        {
                            ultrakillFallback++;
                        }

                        retail++;
                        if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null)
                        {
                            texturedRetail++;
                        }
                    }
                }

                UltrakillLog.Info("Shader",
                    "Shared: retailMats=" + retail + " withMainTex=" + texturedRetail
                    + " iosFallback=" + ultrakillFallback + " internalError=" + broken
                    + " lightmappedRenderers=" + lightmapped + " stubBundle=" + ShadersBundleIsStub);
            }
        }
    }
}
#endif
