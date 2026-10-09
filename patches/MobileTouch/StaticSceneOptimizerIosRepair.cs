#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Level meshes are batched by StaticSceneOptimizer (atlas _MainTex on batch materials).
    /// SceneHelper/Visual mute lists disabled it on sceneLoaded — which runs BEFORE Start(), so
    /// SetupMaterial/SetupMeshes never ran → white tutorial geometry (session 223222).
    /// </summary>
    internal static class StaticSceneOptimizerIosRepair
    {
        private const string Area = "SceneOpt";
        private static bool _computePrefApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            ApplyNoComputePreference();
            var go = new GameObject("UltrakillIOS.SceneOptRepair");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Host>();
        }

        internal static void ApplyNoComputePreference()
        {
            if (_computePrefApplied)
            {
                return;
            }

            try
            {
                var gs = Type.GetType("SettingsMenu.Components.Pages.GraphicsSettings, Assembly-CSharp");
                var f = gs?.GetField("disabledComputeShaders", BindingFlags.Static | BindingFlags.Public);
                if (f != null)
                {
                    f.SetValue(null, true);
                    _computePrefApplied = true;
                    UltrakillLog.Info(Area, "disabledComputeShaders=true (NO_COMPUTE static batch path for iOS)");
                }
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "Could not set disabledComputeShaders: " + ex.Message);
            }
        }

        internal static void KickAllInLoadedScenes()
        {
            ApplyNoComputePreference();
            var optType = Type.GetType("StaticSceneOptimizer, Assembly-CSharp");
            if (optType == null)
            {
                return;
            }

            var kicked = 0;
            foreach (var obj in UnityEngine.Object.FindObjectsOfType(optType, true))
            {
                if (obj is Behaviour b)
                {
                    b.enabled = true;
                }

                if (KickOne(obj, optType))
                {
                    kicked++;
                }
            }

            if (kicked > 0)
            {
                UltrakillLog.Info(Area, "Kicked " + kicked + " StaticSceneOptimizer instance(s)");
            }
        }

        private static bool KickOne(object optimizer, Type optType)
        {
            try
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var usedField = optType.GetField("usedComputeShadersAtStart", flags);
                usedField?.SetValue(optimizer, false);

                optType.GetMethod("FixPosition", flags)?.Invoke(optimizer, null);
                optType.GetMethod("SetupMaterial", flags)?.Invoke(optimizer, new object[] { false });
                optType.GetMethod("SetupMeshes", flags)?.Invoke(optimizer, null);

                var baked = optType.GetField("bakedDataAsset", flags)?.GetValue(optimizer);
                var outdoors = optType.GetField("batchMaterialOutdoors", flags)?.GetValue(optimizer) as Material;
                var env = optType.GetField("batchMaterialEnvironment", flags)?.GetValue(optimizer) as Material;
                Texture outTex = null;
                Texture envTex = null;
                if (outdoors != null && outdoors.HasProperty("_MainTex"))
                {
                    outTex = outdoors.GetTexture("_MainTex");
                }

                if (env != null && env.HasProperty("_MainTex"))
                {
                    envTex = env.GetTexture("_MainTex");
                }

                var rends = optType.GetField("staticMRends", flags)?.GetValue(optimizer) as System.Collections.IList;
                var rendCount = rends?.Count ?? 0;

                UltrakillLog.Info(Area,
                    "StaticSceneOptimizer OK bakedData=" + (baked != null ? "yes" : "NULL")
                    + " staticMRends=" + rendCount
                    + " outdoorMainTex=" + (outTex != null ? outTex.name : "null")
                    + " envMainTex=" + (envTex != null ? envTex.name : "null"));

                return rendCount > 0 || baked != null;
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "Kick failed: " + ex.Message);
                return false;
            }
        }

        private sealed class Host : MonoBehaviour
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
                StartCoroutine(KickAfterStart());
            }

            private System.Collections.IEnumerator KickAfterStart()
            {
                // sceneLoaded fires before scene Start() — wait so we don't fight Unity lifecycle.
                yield return null;
                yield return null;
                KickAllInLoadedScenes();
                yield return new WaitForSecondsRealtime(0.25f);
                KickAllInLoadedScenes();
            }
        }
    }
}
#endif
