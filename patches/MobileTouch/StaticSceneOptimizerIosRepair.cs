#if ULTRAKILL_FULL_PORT
using System;
using System.Collections;
using System.Collections.Generic;
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
                // Keep disabled until KickOne aligns counts — Start() SetupMeshes OOBs otherwise.
                if (obj is Behaviour b)
                {
                    b.enabled = false;
                }

                KickOne(obj, optType);
            }
        }

        private static string _lastStateLog;
        private static int _kickPasses;
        private static bool _warnedBakeMissing;
        private static bool _warnedStaticData;
        private static bool _expandedSubmeshes;
        /// <summary>True when we invented bakedMeshes from scene filters — UVs are NOT atlas-packed; never SetupMeshes.</summary>
        private static bool _sceneRebuildOnly;

        private static void TryBindBakedData(object optimizer, Type optType, BindingFlags flags)
        {
            var bakedField = optType.GetField("bakedDataAsset", flags);
            if (bakedField == null)
            {
                return;
            }

            var dataType = Type.GetType("StaticSceneData, Assembly-CSharp");
            if (dataType == null)
            {
                return;
            }

            // AssetRipper BakedData SO = Missing Script; assemble from JSON+mesh instead.
            var assembled = TutorialBakeDataWarmup.GetCreatedStaticSceneData();
            if (assembled != null && IsBakeDataUsable(assembled, dataType, flags))
            {
                _sceneRebuildOnly = false;
                bakedField.SetValue(optimizer, assembled);
                var mainA = dataType.GetField("mainTexAtlas", flags)?.GetValue(assembled) as Texture;
                var meshListA = dataType.GetField("bakedMeshes", flags)?.GetValue(assembled) as IList;
                var subA = dataType.GetField("firstSubMesh", flags)?.GetValue(assembled) as IList;
                UltrakillLog.Info(Area,
                    "Bound assembled StaticSceneData atlas=" + (mainA != null ? mainA.name : "null")
                    + " bakedMeshes=" + (meshListA != null ? meshListA.Count : 0)
                    + " firstSubMesh=" + (subA != null ? subA.Count : 0));
                return;
            }

            if (bakedField.GetValue(optimizer) != null)
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
                var meshesBefore = dataType.GetField("bakedMeshes", flags)?.GetValue(best) as IList;
                var hadRetailMeshes = meshesBefore != null && meshesBefore.Count > 0;
                HydrateAtlasFields(best, dataType, flags);
                if (!hadRetailMeshes)
                {
                    _sceneRebuildOnly = true;
                    if (optimizer is Behaviour beh)
                    {
                        beh.enabled = false;
                    }

                    if (!_warnedStaticData)
                    {
                        _warnedStaticData = true;
                        UltrakillLog.Warn(Area,
                            "BakedData SO empty/Missing Script — need tutorial_bake.json.txt in bake companion "
                            + "(AssetRipper Common Issues: SO needs matching scripts).");
                    }

                    return;
                }
            }

            if (best == null || !IsBakeDataUsable(best, dataType, flags))
            {
                if (!_warnedStaticData)
                {
                    _warnedStaticData = true;
                    UltrakillLog.Warn(Area, "StaticSceneData unusable after companion load");
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

        /// <summary>
        /// SetupMeshes indexes firstSubMesh/mrMeshIndices by staticMRends[i].
        /// Scene enviro MR count (345) != bake slots (218) → ArgumentOutOfRange + purple geo.
        /// Always spawn exactly firstSubMesh.Count identity renderers for the Combined Mesh.
        /// </summary>
        private static bool AlignStaticMRendsToBake(
            object optimizer,
            Type optType,
            object baked,
            Type dataType,
            BindingFlags flags)
        {
            var listField = optType.GetField("staticMRends", flags);
            var list = listField?.GetValue(optimizer) as IList;
            if (list == null)
            {
                return false;
            }

            var firstSubs = dataType.GetField("firstSubMesh", flags)?.GetValue(baked) as IList;
            var meshes = dataType.GetField("bakedMeshes", flags)?.GetValue(baked) as IList;
            var indices = dataType.GetField("mrMeshIndices", flags)?.GetValue(baked) as IList;
            if (firstSubs == null || firstSubs.Count == 0 || meshes == null || meshes.Count == 0
                || indices == null || indices.Count < firstSubs.Count)
            {
                return false;
            }

            var needed = firstSubs.Count;
            if (list.Count == needed && list.Count > 0)
            {
                var allOurs = true;
                for (var i = 0; i < list.Count; i++)
                {
                    var mr = list[i] as MeshRenderer;
                    if (mr == null || mr.gameObject.name.IndexOf("BakedSub_", StringComparison.Ordinal) < 0)
                    {
                        allOurs = false;
                        break;
                    }
                }

                if (allOurs)
                {
                    return true;
                }
            }

            list.Clear();
            var mesh = meshes[0] as Mesh;
            if (mesh == null)
            {
                return false;
            }

            var root = GameObject.Find("UltrakillIOS.BakedStaticGeo");
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }

            root = new GameObject("UltrakillIOS.BakedStaticGeo");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            const int enviroLayer = 8;
            for (var i = 0; i < needed; i++)
            {
                var go = new GameObject("BakedSub_" + i);
                go.transform.SetParent(root.transform, false);
                go.layer = enviroLayer;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                list.Add(go.AddComponent<MeshRenderer>());
            }

            DisableConflictingSceneStaticGeo(list);
            UltrakillLog.Info(Area, "Aligned staticMRends=" + list.Count + " to bake firstSubMesh=" + needed);
            return list.Count == needed;
        }

        /// <summary>Hide ripped enviro MRs so they don't z-fight / show InternalError purple over bake.</summary>
        private static void DisableConflictingSceneStaticGeo(IList keep)
        {
            var keepSet = new System.Collections.Generic.HashSet<int>();
            for (var i = 0; i < keep.Count; i++)
            {
                if (keep[i] is MeshRenderer mr && mr != null)
                {
                    keepSet.Add(mr.GetInstanceID());
                }
            }

            const int enviroLayer = 8;
            const int outdoorLayer = 24;
            var disabled = 0;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (r == null || keepSet.Contains(r.GetInstanceID()))
                {
                    continue;
                }

                var layer = r.gameObject.layer;
                if (layer != enviroLayer && layer != outdoorLayer)
                {
                    continue;
                }

                var n = r.gameObject.name;
                if (n.IndexOf("BakedSub_", StringComparison.Ordinal) >= 0)
                {
                    continue;
                }

                r.enabled = false;
                disabled++;
            }

            if (disabled > 0)
            {
                UltrakillLog.Info(Area, "Disabled conflicting scene static MRs=" + disabled);
            }
        }

        private static void DisableOptimizersUntilKick()
        {
            var optType = Type.GetType("StaticSceneOptimizer, Assembly-CSharp");
            if (optType == null)
            {
                return;
            }

            foreach (var obj in UnityEngine.Object.FindObjectsOfType(optType, true))
            {
                if (obj is Behaviour b && b.enabled)
                {
                    b.enabled = false;
                }
            }
        }

        private static void KickOne(object optimizer, Type optType)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                TutorialBakeDataWarmup.EnsureLoaded();
                optType.GetField("usedComputeShadersAtStart", flags)?.SetValue(optimizer, false);
                TryBindBakedData(optimizer, optType, flags);
                if (_sceneRebuildOnly)
                {
                    optType.GetField("nothingBaked", flags)?.SetValue(optimizer, true);
                    LogState(optimizer, optType, flags);
                    return;
                }

                optType.GetField("nothingBaked", flags)?.SetValue(optimizer, false);
                var baked = optType.GetField("bakedDataAsset", flags)?.GetValue(optimizer);
                var dataType = Type.GetType("StaticSceneData, Assembly-CSharp");
                var canKickMeshes = baked != null && dataType != null && IsBakeDataUsable(baked, dataType, flags);

                EnsureBatchMaterials(optimizer, optType, flags);

                if (optimizer is Behaviour beh)
                {
                    beh.enabled = true;
                }

                optType.GetMethod("FixPosition", flags)?.Invoke(optimizer, null);
                if (_kickPasses < 2 && canKickMeshes)
                {
                    optType.GetMethod("SetupMaterial", flags)?.Invoke(optimizer, new object[] { false });

                    // Combined Mesh is non-readable on device → GetTriangles extract fails (session 000552).
                    // SetStaticBatchInfo also fails on iOS → same atlas tile on every wall.
                    // One MeshRenderer + N materials maps material[i] → submesh[i] with correct UVs.
                    if (InstallCombinedMultiMaterial(optimizer, optType, baked, dataType, flags))
                    {
                        _kickPasses = 2;
                    }
                    else if (AlignStaticMRendsToBake(optimizer, optType, baked, dataType, flags))
                    {
                        optType.GetMethod("SetupMeshes", flags)?.Invoke(optimizer, null);
                        _kickPasses++;
                        UltrakillLog.Info(Area, "SetupMeshes OK pass=" + _kickPasses);
                        ExpandToStandaloneSubmeshes(optimizer, optType, baked, dataType, flags);
                    }
                    else
                    {
                        UltrakillLog.Warn(Area, "No usable bake install path");
                    }
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

        /// <summary>
        /// Draw Combined Mesh via multi-material slots (no CPU mesh read, no SetStaticBatchInfo).
        /// </summary>
        private static bool InstallCombinedMultiMaterial(
            object optimizer,
            Type optType,
            object baked,
            Type dataType,
            BindingFlags flags)
        {
            if (_expandedSubmeshes)
            {
                return true;
            }

            var meshes = dataType.GetField("bakedMeshes", flags)?.GetValue(baked) as IList;
            if (meshes == null || meshes.Count == 0)
            {
                return false;
            }

            var mesh = meshes[0] as Mesh;
            if (mesh == null || mesh.subMeshCount < 1)
            {
                return false;
            }

            var env = optType.GetField("batchMaterialEnvironment", flags)?.GetValue(optimizer) as Material;
            var outdoors = optType.GetField("batchMaterialOutdoors", flags)?.GetValue(optimizer) as Material;
            if (env == null)
            {
                return false;
            }

            var atlas = dataType.GetField("mainTexAtlas", flags)?.GetValue(baked) as Texture;
            if (atlas != null)
            {
                if (env.HasProperty("_MainTex"))
                {
                    env.SetTexture("_MainTex", atlas);
                }

                if (outdoors != null && outdoors.HasProperty("_MainTex"))
                {
                    outdoors.SetTexture("_MainTex", atlas);
                }
            }

            var listField = optType.GetField("staticMRends", flags);
            var list = listField?.GetValue(optimizer) as IList;
            if (list == null)
            {
                return false;
            }

            list.Clear();
            var old = GameObject.Find("UltrakillIOS.BakedStaticGeo");
            if (old != null)
            {
                UnityEngine.Object.Destroy(old);
            }

            var root = new GameObject("UltrakillIOS.BakedStaticGeo");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            var go = new GameObject("BakedCombined");
            go.transform.SetParent(root.transform, false);
            go.layer = 8;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var n = mesh.subMeshCount;
            var mats = new Material[n];
            for (var i = 0; i < n; i++)
            {
                mats[i] = env;
            }

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            list.Add(mr);
            DisableConflictingSceneStaticGeo(list);

            _expandedSubmeshes = true;
            UltrakillLog.Info(Area, "Installed combined multi-material bake submeshes=" + n
                + " atlas=" + (atlas != null ? atlas.name : "null")
                + " readable=" + mesh.isReadable);
            return true;
        }

        /// <summary>
        /// Fallback when multi-material install fails: extract submeshes (needs readable mesh).
        /// </summary>
        private static void ExpandToStandaloneSubmeshes(
            object optimizer,
            Type optType,
            object baked,
            Type dataType,
            BindingFlags flags)
        {
            if (baked == null || dataType == null)
            {
                return;
            }

            var rends = optType.GetField("staticMRends", flags)?.GetValue(optimizer) as IList;
            var firstSubs = dataType.GetField("firstSubMesh", flags)?.GetValue(baked) as IList;
            var meshes = dataType.GetField("bakedMeshes", flags)?.GetValue(baked) as IList;
            var meshIdx = dataType.GetField("mrMeshIndices", flags)?.GetValue(baked) as IList;
            if (rends == null || firstSubs == null || meshes == null || meshIdx == null
                || rends.Count == 0 || rends.Count != firstSubs.Count)
            {
                return;
            }

            var cache = new Dictionary<long, Mesh>();
            var expanded = 0;
            for (var i = 0; i < rends.Count; i++)
            {
                var mr = rends[i] as MeshRenderer;
                if (mr == null)
                {
                    continue;
                }

                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null)
                {
                    continue;
                }

                var mi = Convert.ToInt32(meshIdx[i]);
                var si = Convert.ToInt32(firstSubs[i]);
                if (mi < 0 || mi >= meshes.Count)
                {
                    continue;
                }

                var src = meshes[mi] as Mesh;
                if (src == null || si < 0 || si >= src.subMeshCount)
                {
                    continue;
                }

                var key = ((long)src.GetInstanceID() << 32) | (uint)si;
                if (!cache.TryGetValue(key, out var piece) || piece == null)
                {
                    piece = ExtractSubmesh(src, si);
                    if (piece == null)
                    {
                        continue;
                    }

                    cache[key] = piece;
                }

                mf.sharedMesh = piece;
                expanded++;
            }

            if (expanded > 0 && !_expandedSubmeshes)
            {
                _expandedSubmeshes = true;
                UltrakillLog.Info(Area, "Expanded bake slots to standalone submeshes=" + expanded
                    + " uniquePieces=" + cache.Count);
            }
        }

        private static Mesh ExtractSubmesh(Mesh src, int subMesh)
        {
            try
            {
                var tris = src.GetTriangles(subMesh);
                if (tris == null || tris.Length == 0)
                {
                    return null;
                }

                var srcV = src.vertices;
                var srcUv = src.uv;
                var srcN = src.normals;
                var map = new Dictionary<int, int>(Mathf.Min(tris.Length, 4096));
                var verts = new List<Vector3>(tris.Length);
                var uvs = new List<Vector2>(tris.Length);
                var norms = new List<Vector3>(tris.Length);
                var newTris = new int[tris.Length];
                for (var t = 0; t < tris.Length; t++)
                {
                    var old = tris[t];
                    if (!map.TryGetValue(old, out var ni))
                    {
                        ni = verts.Count;
                        map[old] = ni;
                        verts.Add(srcV[old]);
                        uvs.Add(srcUv != null && old < srcUv.Length ? srcUv[old] : Vector2.zero);
                        norms.Add(srcN != null && old < srcN.Length ? srcN[old] : Vector3.up);
                    }

                    newTris[t] = ni;
                }

                var m = new Mesh { name = src.name + "_sub" + subMesh };
                m.SetVertices(verts);
                m.SetUVs(0, uvs);
                m.SetNormals(norms);
                m.SetTriangles(newTris, 0, true);
                m.RecalculateBounds();
                return m;
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "ExtractSubmesh " + subMesh + ": " + ex.Message);
                return null;
            }
        }

        private static void EnsureBatchMaterials(object optimizer, Type optType, BindingFlags flags)
        {
            UkMasterShaderBootstrap.EnsureReady();
            var master = UkMasterShaderBootstrap.Master ?? UkMasterShaderBootstrap.Stationary;
            if (master == null)
            {
                return;
            }

            var outF = optType.GetField("batchMaterialOutdoors", flags);
            var envF = optType.GetField("batchMaterialEnvironment", flags);
            var outdoors = outF?.GetValue(optimizer) as Material;
            var env = envF?.GetValue(optimizer) as Material;

            static bool Bad(Material m)
            {
                if (m == null || m.shader == null)
                {
                    return true;
                }

                var n = m.shader.name ?? "";
                return n.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0
                    || !m.shader.isSupported;
            }

            if (Bad(outdoors))
            {
                outdoors = new Material(master) { name = "UltrakillIOS.BatchOutdoors" };
                outF?.SetValue(optimizer, outdoors);
            }

            if (Bad(env))
            {
                env = new Material(master) { name = "UltrakillIOS.BatchEnvironment" };
                envF?.SetValue(optimizer, env);
            }

            // Keep atlas on batch mats even if SetupMaterial already ran.
            var baked = optType.GetField("bakedDataAsset", flags)?.GetValue(optimizer);
            var dataType = Type.GetType("StaticSceneData, Assembly-CSharp");
            if (baked != null && dataType != null)
            {
                var atlas = dataType.GetField("mainTexAtlas", flags)?.GetValue(baked) as Texture;
                if (atlas != null)
                {
                    if (outdoors != null && outdoors.HasProperty("_MainTex"))
                    {
                        outdoors.SetTexture("_MainTex", atlas);
                    }

                    if (env != null && env.HasProperty("_MainTex"))
                    {
                        env.SetTexture("_MainTex", atlas);
                    }
                }
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

        [DefaultExecutionOrder(-32000)]
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
                _sceneRebuildOnly = false;
                _warnedStaticData = false;
                _expandedSubmeshes = false;
                DisableOptimizersUntilKick();
                StartCoroutine(KickAfterStart());
            }

            private void Start()
            {
                // Runs before StaticSceneOptimizer.Start (that type uses int.MaxValue execution order).
                DisableOptimizersUntilKick();
            }

            private System.Collections.IEnumerator KickAfterStart()
            {
                DisableOptimizersUntilKick();
                yield return null;
                DisableOptimizersUntilKick();
                yield return null;
                KickAllInLoadedScenes();
                yield return new WaitForSecondsRealtime(0.3f);
                KickAllInLoadedScenes();
            }
        }
    }
}
#endif
