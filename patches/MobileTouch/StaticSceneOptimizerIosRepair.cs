#if ULTRAKILL_FULL_PORT
using System;
using System.Collections;
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
        private static bool _warnedStaticData;

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

            object best = null;
            var bestScore = 0;
            foreach (var data in Resources.FindObjectsOfTypeAll(dataType))
            {
                if (data == null)
                {
                    continue;
                }

                HydrateAtlasFields(data, dataType, flags);
                var score = ScoreBakedData(data, dataType, flags);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = data;
                }
            }

            if (best == null)
            {
                best = PickStaticSceneDataShell(dataType);
            }

            if (best != null)
            {
                HydrateAtlasFields(best, dataType, flags);
                TryRebuildBakedMeshesFromScene(best, dataType, flags, out var orderedRends);
                if (orderedRends.Count > 0)
                {
                    TryAssignStaticMRends(optimizer, optType, flags, orderedRends);
                }
            }

            if (best == null || !IsBakeDataUsable(best, dataType, flags))
            {
                var meshN = 0;
                foreach (var data in Resources.FindObjectsOfTypeAll(dataType))
                {
                    if (data is null)
                    {
                        continue;
                    }

                    var meshes = dataType.GetField("bakedMeshes", flags)?.GetValue(data) as IList;
                    meshN = Math.Max(meshN, meshes?.Count ?? 0);
                }

                if (!_warnedStaticData)
                {
                    _warnedStaticData = true;
                    UltrakillLog.Warn(Area,
                        "StaticSceneData still unusable after scene rebuild: maxBakedMeshes=" + meshN
                        + " (atlas GUID refs empty in bundle — need IPA with SceneOpt rebuild fix)");
                }

                return;
            }

            bakedField.SetValue(optimizer, best);
            var main = dataType.GetField("mainTexAtlas", flags)?.GetValue(best) as Texture;
            var meshList = dataType.GetField("bakedMeshes", flags)?.GetValue(best) as IList;
            UltrakillLog.Info(Area,
                "Bound StaticSceneData atlas=" + (main != null ? main.name : "null")
                + " bakedMeshes=" + (meshList != null ? meshList.Count : 0));
        }

        private static bool IsBakeDataUsable(object data, Type dataType, BindingFlags flags)
        {
            var meshes = dataType.GetField("bakedMeshes", flags)?.GetValue(data) as IList;
            if (meshes == null || meshes.Count == 0)
            {
                return false;
            }

            var main = dataType.GetField("mainTexAtlas", flags)?.GetValue(data) as Texture;
            var blend = dataType.GetField("blendTexAtlas", flags)?.GetValue(data) as Texture;
            if (main == null || !IsSceneAtlasTexture(main))
            {
                return false;
            }

            if (blend == null || !IsSceneAtlasTexture(blend))
            {
                dataType.GetField("blendTexAtlas", flags)?.SetValue(data, main);
            }

            return true;
        }

        private static object PickStaticSceneDataShell(Type dataType)
        {
            foreach (var data in Resources.FindObjectsOfTypeAll(dataType))
            {
                if (data != null)
                {
                    return data;
                }
            }

            return null;
        }

        private static void TryAssignStaticMRends(
            object optimizer,
            Type optType,
            BindingFlags flags,
            System.Collections.Generic.List<MeshRenderer> ordered)
        {
            var listField = optType.GetField("staticMRends", flags);
            if (listField == null)
            {
                return;
            }

            var list = listField.GetValue(optimizer) as IList;
            if (list == null)
            {
                return;
            }

            list.Clear();
            foreach (var r in ordered)
            {
                if (r != null)
                {
                    list.Add(r);
                }
            }

            if (ordered.Count > 0)
            {
                UltrakillLog.Info(Area, "staticMRends=" + ordered.Count + " from scene Combined/static geo");
            }
        }

        /// <summary>
        /// Retail StaticSceneData often loads with empty bakedMeshes on iOS while Combined Mesh assets are on filters.
        /// </summary>
        private static bool TryRebuildBakedMeshesFromScene(
            object data,
            Type dataType,
            BindingFlags flags,
            out System.Collections.Generic.List<MeshRenderer> orderedRenderers)
        {
            orderedRenderers = new System.Collections.Generic.List<MeshRenderer>();
            var meshField = dataType.GetField("bakedMeshes", flags);
            var mrIdxField = dataType.GetField("mrMeshIndices", flags);
            var subField = dataType.GetField("firstSubMesh", flags);
            if (meshField == null || mrIdxField == null || subField == null)
            {
                return false;
            }

            var meshes = meshField.GetValue(data) as IList;
            if (meshes == null)
            {
                return false;
            }

            if (meshes.Count > 0)
            {
                return true;
            }

            const int enviroLayer = 8;
            const int outdoorLayer = 24;
            var candidates = new System.Collections.Generic.List<MeshRenderer>();
            foreach (var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (r == null)
                {
                    continue;
                }

                var layer = r.gameObject.layer;
                if (layer != enviroLayer && layer != outdoorLayer)
                {
                    continue;
                }

                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    continue;
                }

                candidates.Add(r);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            candidates.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var meshToIndex = new System.Collections.Generic.Dictionary<Mesh, int>();
            var mrIndices = new System.Collections.Generic.List<ushort>();
            var firstSubs = new System.Collections.Generic.List<ushort>();

            foreach (var r in candidates)
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                if (!meshToIndex.TryGetValue(mesh, out var idx))
                {
                    idx = meshes.Count;
                    meshes.Add(mesh);
                    meshToIndex[mesh] = idx;
                }

                mrIndices.Add((ushort)idx);
                firstSubs.Add(0);
                orderedRenderers.Add(r);
            }

            mrIdxField.SetValue(data, mrIndices);
            subField.SetValue(data, firstSubs);
            UltrakillLog.Info(Area,
                "Rebuilt bakedMeshes=" + meshes.Count + " from " + orderedRenderers.Count + " static renderers");
            return meshes.Count > 0;
        }

        private static int ScoreBakedData(object data, Type dataType, BindingFlags flags)
        {
            var meshes = dataType.GetField("bakedMeshes", flags)?.GetValue(data) as IList;
            var meshCount = meshes?.Count ?? 0;
            if (meshCount == 0)
            {
                return 0;
            }

            var score = meshCount;
            if (dataType.GetField("mainTexAtlas", flags)?.GetValue(data) is Texture main && IsSceneAtlasTexture(main))
            {
                score += 1000;
            }

            if (dataType.GetField("blendTexAtlas", flags)?.GetValue(data) is Texture blend && IsSceneAtlasTexture(blend))
            {
                score += 100;
            }

            return score;
        }

        private static bool IsRejectedUiFontTexture(Texture t)
        {
            if (t == null)
            {
                return true;
            }

            var n = t.name.ToLowerInvariant();
            return n.Contains("sdf") || n.Contains("liberation") || n.Contains("font") || n.Contains("tmp")
                || n.Contains("vcr") || n.Contains("osd") || n.Contains("mono_") || n.Contains("ui/")
                || n.Contains("hud") || n.Contains("icon") || n.Contains("tahoma") || n.Contains("fs-")
                || n.Contains("8px") || n.Contains("arial") || n.Contains("glyph") || n.Contains("emoji");
        }

        private static bool IsSceneAtlasTexture(Texture t)
        {
            if (t is not Texture2D tex || IsRejectedUiFontTexture(tex))
            {
                return false;
            }

            var pixels = (long)tex.width * tex.height;
            if (pixels < 256 * 256)
            {
                return false;
            }

            var n = tex.name.ToLowerInvariant();
            if (n.Contains("static") || n.Contains("baked") || n.Contains("blend") || n.Contains("level")
                || n.Contains("enviro") || n.Contains("outdoor") || n.Contains("texture2d"))
            {
                return true;
            }

            // Name contains "atlas" but not UI/font (fs-tahoma-8px Atlas must not qualify).
            return n.Contains("atlas") && !n.Contains("tahoma") && !n.Contains("fs-");
        }

        private static long AtlasPickScore(Texture2D t, bool wantBlend)
        {
            if (!IsSceneAtlasTexture(t))
            {
                return 0;
            }

            var n = t.name.ToLowerInvariant();
            var pixels = (long)t.width * t.height;
            if (wantBlend)
            {
                if (n.Contains("blend"))
                {
                    return pixels + 500_000_000L;
                }

                return 0;
            }

            if (n.Contains("blend"))
            {
                return 0;
            }

            if (n.Contains("static") || n.Contains("baked") || n.Contains("level"))
            {
                return pixels + 300_000_000L;
            }

            return pixels;
        }

        private static void PickAtlasesFromStaticGeoMaterials(out Texture2D main, out Texture2D blend)
        {
            main = null;
            blend = null;
            var mainScore = 0L;
            var blendScore = 0L;
            const int enviroLayer = 8;
            const int outdoorLayer = 24;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (r == null)
                {
                    continue;
                }

                var layer = r.gameObject.layer;
                if (layer != enviroLayer && layer != outdoorLayer)
                {
                    continue;
                }

                var mats = r.sharedMaterials;
                if (mats == null)
                {
                    continue;
                }

                foreach (var mat in mats)
                {
                    if (mat == null || !mat.HasProperty("_MainTex"))
                    {
                        continue;
                    }

                    var tex = mat.GetTexture("_MainTex") as Texture2D;
                    if (tex == null || IsRejectedUiFontTexture(tex))
                    {
                        continue;
                    }

                    var pixels = (long)tex.width * tex.height;
                    if (pixels < 128 * 128)
                    {
                        continue;
                    }

                    var n = tex.name.ToLowerInvariant();
                    if (n.Contains("blend") && pixels > blendScore)
                    {
                        blendScore = pixels;
                        blend = tex;
                    }
                    else if (pixels > mainScore)
                    {
                        mainScore = pixels;
                        main = tex;
                    }
                }
            }
        }

        /// <summary>Only fill atlas refs when scene bake textures are already loaded but GUID refs failed (not TMP/UI atlases).</summary>
        private static void HydrateAtlasFields(object data, Type dataType, BindingFlags flags)
        {
            var mainF = dataType.GetField("mainTexAtlas", flags);
            var blendF = dataType.GetField("blendTexAtlas", flags);
            if (mainF == null)
            {
                return;
            }

            if (mainF.GetValue(data) is Texture existing && IsSceneAtlasTexture(existing))
            {
                return;
            }

            PickAtlasesFromStaticGeoMaterials(out var mainPick, out var blendPick);
            var mainScore = mainPick != null ? (long)mainPick.width * mainPick.height : 0L;
            var blendScore = blendPick != null ? (long)blendPick.width * blendPick.height : 0L;
            foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                if (t == null)
                {
                    continue;
                }

                var mainS = AtlasPickScore(t, false);
                if (mainS > mainScore)
                {
                    mainScore = mainS;
                    mainPick = t;
                }

                var blendS = AtlasPickScore(t, true);
                if (blendS > blendScore)
                {
                    blendScore = blendS;
                    blendPick = t;
                }
            }

            if (mainPick == null)
            {
                foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
                {
                    if (t != null && string.Equals(t.name, "Texture2D_2", StringComparison.Ordinal))
                    {
                        mainPick = t;
                        break;
                    }
                }
            }

            if (blendPick == null)
            {
                foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
                {
                    if (t != null && string.Equals(t.name, "Texture2D_1", StringComparison.Ordinal))
                    {
                        blendPick = t;
                        break;
                    }
                }
            }

            if (mainPick != null)
            {
                mainF.SetValue(data, mainPick);
                UltrakillLog.Info(Area, "Hydrated mainTexAtlas from loaded texture '" + mainPick.name + "'");
            }

            if (blendPick != null && blendF != null)
            {
                blendF.SetValue(data, blendPick);
                UltrakillLog.Info(Area, "Hydrated blendTexAtlas from '" + blendPick.name + "'");
            }
        }

        private static void TryPopulateStaticMRends(object optimizer, Type optType, BindingFlags flags)
        {
            var listField = optType.GetField("staticMRends", flags);
            if (listField == null)
            {
                return;
            }

            var list = listField.GetValue(optimizer);
            if (list is not IList ilist || ilist.Count > 0)
            {
                return;
            }

            var baked = optType.GetField("bakedDataAsset", flags)?.GetValue(optimizer);
            var dataType = Type.GetType("StaticSceneData, Assembly-CSharp");
            if (baked == null || dataType == null || !IsBakeDataUsable(baked, dataType, flags))
            {
                return;
            }

            var indices = dataType.GetField("mrMeshIndices", flags)?.GetValue(baked) as IList;
            if (indices == null || indices.Count == 0)
            {
                return;
            }

            const int enviroLayer = 8;
            const int outdoorLayer = 24;
            var added = 0;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (r == null)
                {
                    continue;
                }

                var layer = r.gameObject.layer;
                if (layer != enviroLayer && layer != outdoorLayer)
                {
                    continue;
                }

                ilist.Add(r);
                added++;
            }

            if (added > 0)
            {
                UltrakillLog.Info(Area, "Populated staticMRends=" + added + " from enviro/outdoor layers");
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
                var baked = optType.GetField("bakedDataAsset", flags)?.GetValue(optimizer);
                var dataType = Type.GetType("StaticSceneData, Assembly-CSharp");
                var canKickMeshes = baked != null && dataType != null && IsBakeDataUsable(baked, dataType, flags);
                if (canKickMeshes)
                {
                    TryPopulateStaticMRends(optimizer, optType, flags);
                }

                EnsureBatchMaterials(optimizer, optType, flags);

                optType.GetMethod("FixPosition", flags)?.Invoke(optimizer, null);
                if (_kickPasses < 2 && canKickMeshes)
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
                    "Geo white: optimizer still has no bakedData/staticMRends — install latest IPA (SceneOpt scene rebuild)");
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
