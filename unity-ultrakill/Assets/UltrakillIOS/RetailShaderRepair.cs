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
        }

        internal static Shader WarmupFallbackShader()
        {
            if (_unlitFallback != null)
            {
                return _unlitFallback;
            }

            _unlitFallback = Shader.Find("UltrakillIOS/UnlitTexture")
                ?? Resources.Load<Shader>("Shaders/UltrakillIOS_UnlitTexture")
                ?? Shader.Find("Unlit/Texture");

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

        internal static void HydrateMainTexPublic(Material mat, Renderer r, int submesh)
        {
            HydrateMainTex(mat, r, submesh);
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
                return false;
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
                    if (m == null || m.shader == null)
                    {
                        continue;
                    }

                    var sn = m.shader.name;
                    if (IsUiOrSky(sn) || IsInternalError(sn))
                    {
                        continue;
                    }

                    var before = m.shader;
                    var resolved = Shader.Find(sn);
                    if (resolved != null && resolved != before && resolved.isSupported)
                    {
                        m.shader = resolved;
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
            ShadersBundleWarmup.TryWarmup();
            UkMasterShaderBootstrap.EnsureReady();
            var recovered = UkMasterShaderBootstrap.RecoverInternalErrorMaterials(includeInactive);
            RebindAndHydrateStubMaterials(includeInactive);

            // Only null-shader slots left — do not touch InternalError (handled above).
            var remapped = 0;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(includeInactive))
            {
                if (r == null || r is ParticleSystemRenderer || r.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                var mats = r.materials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || m.shader != null)
                    {
                        continue;
                    }

                    var slotKey = (r.GetInstanceID() << 8) ^ i;
                    if (FixedSlots.Contains(slotKey))
                    {
                        continue;
                    }

                    var fb = WarmupFallbackShader();
                    if (fb == null)
                    {
                        continue;
                    }

                    mats[i] = new Material(fb);
                    HydrateMainTex(mats[i], r, i);
                    FixedSlots.Add(slotKey);
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
                UltrakillLog.Info("Shader", "Fixed " + remapped + " null-shader slots");
            }

            return recovered + remapped;
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
                if (!ShouldRepairScene(scene))
                {
                    return;
                }

                FixedSlots.Clear();
                StartCoroutine(RepairPipeline());
            }

            private static bool ShouldRepairScene(Scene scene)
            {
                if (!scene.IsValid())
                {
                    return false;
                }

                var n = scene.name;
                return !string.Equals(n, "Bootstrap", StringComparison.OrdinalIgnoreCase);
            }

            private IEnumerator RepairPipeline()
            {
                yield return null;
                for (var i = 0; i < 8; i++)
                {
                    StaticSceneOptimizerIosRepair.KickAllInLoadedScenes();
                    RemapBrokenMaterialsOnRenderers(includeInactive: true);

                    if (i == 2 || i == 7)
                    {
                        LogSceneMaterialStats();
                    }

                    yield return i < 4 ? null : new WaitForSecondsRealtime(0.35f);
                }
            }

            private static void LogSceneMaterialStats()
            {
                var retail = 0;
                var broken = 0;
                var textured = 0;
                var iosFallback = 0;

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
                            iosFallback++;
                        }

                        retail++;
                        if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null)
                        {
                            textured++;
                        }
                    }
                }

                UltrakillLog.Info("Shader",
                    "Shared: retailMats=" + retail + " withMainTex=" + textured
                    + " iosFallback=" + iosFallback + " internalError=" + broken
                    + " stubBundle=" + ShadersBundleIsStub
                    + " bundleShaders=" + ShadersBundleWarmup.LoadedShaderCount);
            }
        }
    }
}
#endif
