#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
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
                }
            }
            catch
            {
                /* ignore */
            }
        }

        internal static void KickAllInLoadedScenes()
        {
            ApplyNoComputePreference();
            UkMasterShaderBootstrap.EnsureReady();
            var optType = Type.GetType("StaticSceneOptimizer, Assembly-CSharp");
            if (optType == null)
            {
                return;
            }

            foreach (var obj in UnityEngine.Object.FindObjectsOfType(optType, true))
            {
                if (obj is Behaviour b)
                {
                    b.enabled = true;
                }

                KickOne(obj, optType);
            }
        }

        private static string _lastStateLog;
        private static int _kickPasses;
        private static bool _warnedBakeMissing;

        private static void TryBindBakedData(object optimizer, Type optType, BindingFlags flags)
        {
            var bakedField = optType.GetField("bakedDataAsset", flags);
            if (bakedField == null || bakedField.GetValue(optimizer) != null)
            {
                return;
            }

            var dataType = Type.GetType("StaticSceneData, Assembly-CSharp");
            if (dataType == null)
            {
                return;
            }

            var found = 0;
            var withAtlas = 0;
            foreach (var data in Resources.FindObjectsOfTypeAll(dataType))
            {
                if (data == null)
                {
                    continue;
                }

                found++;
                var atlasField = dataType.GetField("mainTexAtlas", flags);
                var atlas = atlasField?.GetValue(data) as Texture;
                if (atlas == null)
                {
                    continue;
                }

                withAtlas++;
                bakedField.SetValue(optimizer, data);
                UltrakillLog.Info(Area, "Bound StaticSceneData atlas=" + atlas.name + " (candidates=" + found + ")");
                return;
            }

            if (found > 0)
            {
                UltrakillLog.Warn(Area, "StaticSceneData assets=" + found + " but none have mainTexAtlas");
            }
        }

        private static void KickOne(object optimizer, Type optType)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                optType.GetField("usedComputeShadersAtStart", flags)?.SetValue(optimizer, false);
                optType.GetField("nothingBaked", flags)?.SetValue(optimizer, false);
                TryBindBakedData(optimizer, optType, flags);
                EnsureBatchMaterials(optimizer, optType, flags);

                optType.GetMethod("FixPosition", flags)?.Invoke(optimizer, null);
                if (_kickPasses < 2)
                {
                    optType.GetMethod("SetupMaterial", flags)?.Invoke(optimizer, new object[] { false });
                    optType.GetMethod("SetupMeshes", flags)?.Invoke(optimizer, null);
                    _kickPasses++;
                }

                LogState(optimizer, optType, flags);
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                UltrakillLog.Warn(Area, "Kick failed: " + inner.GetType().Name + ": " + inner.Message);
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "Kick failed: " + ex.Message);
            }
        }

        private static void EnsureBatchMaterials(object optimizer, Type optType, BindingFlags flags)
        {
            var master = UkMasterShaderBootstrap.Master ?? UkMasterShaderBootstrap.Stationary;
            if (master == null)
            {
                return;
            }

            var outF = optType.GetField("batchMaterialOutdoors", flags);
            var envF = optType.GetField("batchMaterialEnvironment", flags);
            var outdoors = outF?.GetValue(optimizer) as Material;
            var env = envF?.GetValue(optimizer) as Material;

            if (outdoors == null)
            {
                outdoors = new Material(master);
                outF?.SetValue(optimizer, outdoors);
            }

            if (env == null)
            {
                env = new Material(master);
                envF?.SetValue(optimizer, env);
            }
        }

        private static void LogState(object optimizer, Type optType, BindingFlags flags)
        {
            var baked = optType.GetField("bakedDataAsset", flags)?.GetValue(optimizer);
            var outdoors = optType.GetField("batchMaterialOutdoors", flags)?.GetValue(optimizer) as Material;
            Texture outTex = null;
            if (outdoors != null && outdoors.HasProperty("_MainTex"))
            {
                outTex = outdoors.GetTexture("_MainTex");
            }

            var rends = optType.GetField("staticMRends", flags)?.GetValue(optimizer) as System.Collections.IList;
            var nothingBaked = optType.GetField("nothingBaked", flags)?.GetValue(optimizer);
            var go = (optimizer as UnityEngine.Object)?.name ?? "?";
            var beh = optimizer as Behaviour;
            var scriptOk = beh != null && beh.GetType().Name == "StaticSceneOptimizer";
            var msg = "optimizer go=" + go
                + " scriptOk=" + scriptOk
                + " bakedData=" + (baked != null ? "yes" : "NULL")
                + " staticMRends=" + (rends?.Count ?? 0)
                + " nothingBaked=" + nothingBaked
                + " outdoorMainTex=" + (outTex != null ? outTex.name : "null");
            if (!_warnedBakeMissing && (rends?.Count ?? 0) == 0 && baked == null)
            {
                _warnedBakeMissing = true;
                UltrakillLog.Warn(Area,
                    "Geo white: optimizer has no bake data in memory — need iOS Tutorial bundle with StaticSceneData "
                    + "(legacy Build-IosBundles scene build), not stub shaders alone");
            }
            if (msg != _lastStateLog)
            {
                _lastStateLog = msg;
                UltrakillLog.Info(Area, msg);
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
                if (!scene.IsValid() || string.Equals(scene.name, "Bootstrap", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _kickPasses = 0;
                _lastStateLog = null;
                StartCoroutine(KickAfterStart());
            }

            private System.Collections.IEnumerator KickAfterStart()
            {
                yield return null;
                yield return null;
                KickAllInLoadedScenes();
                yield return new WaitForSecondsRealtime(0.3f);
                KickAllInLoadedScenes();
            }
        }
    }
}
#endif
