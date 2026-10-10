using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEngine;

/// <summary>
/// Rebuilds the retail Addressables bundles for iOS from an AssetRipper export, keeping the retail bundle file names
/// and the GUID internal names so the retail catalog.json can be reused unchanged apart from the platform folder.
/// Run with: Unity -batchmode -buildTarget iOS -projectPath ExportedProject -executeMethod IosRetailBundleBuild.Build
///   -catalogMap path/to/catalog-map.json -outDir path/to/output
/// Optional: -onlyBundle assets_assets_assets/shaders.bundle
/// </summary>
public static class IosRetailBundleBuild
{
    [Serializable] private class CatalogMap { public List<BundleInfo> bundles; public List<SceneInfo> scenes; }
    [Serializable] private class BundleInfo { public string file; public BundleOptions options; public List<AssetInfo> assets; }
    [Serializable] private class BundleOptions { public string m_BundleName; }
    [Serializable] private class AssetInfo { public string internalId; public string primaryKey; public string type; }
    [Serializable] private class SceneInfo { public string key; public string internalId; public string bundle; }

    private const string BundleRoot = "Assets/Asset_Bundles";
    private const string BundleFolderSuffix = "_bundle";
    private static readonly Regex ShaderNameRegex = new Regex(@"Shader\s+""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex DuplicateSuffix = new Regex(@"_\d+$");

    public static void Build()
    {
        var mapPath = GetArg("-catalogMap");
        var outDir = GetArg("-outDir");
        string onlyBundle = null;
        try { onlyBundle = GetArg("-onlyBundle"); } catch { /* optional */ }

        var map = JsonUtility.FromJson<CatalogMap>(File.ReadAllText(mapPath));
        var scenePaths = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath)
            .GroupBy(p => Path.GetFileNameWithoutExtension(p)).ToDictionary(g => g.Key, g => g.First());

        var builds = new List<AssetBundleBuild>();
        var sceneBuilds = new List<AssetBundleBuild>();
        var claimed = new HashSet<string>();
        string shaderBundle = null, monoScriptBundle = null;
        int matched = 0, unmatched = 0;

        foreach (var bundle in map.bundles)
        {
            if (onlyBundle != null && bundle.file != onlyBundle && !bundle.file.StartsWith("shader_") && !bundle.file.StartsWith("monoscript_"))
            {
                continue;
            }

            if (bundle.file.StartsWith("shader_")) { shaderBundle = bundle.file; continue; }
            if (bundle.file.StartsWith("monoscript_")) { monoScriptBundle = bundle.file; continue; }

            var assetNames = new List<string>();
            var addressableNames = new List<string>();
            var sceneAssets = bundle.assets.Where(a => a.type.EndsWith("SceneInstance")).ToList();

            if (sceneAssets.Count > 0)
            {
                foreach (var scene in sceneAssets)
                {
                    if (!scenePaths.TryGetValue(scene.internalId, out var path))
                    {
                        Debug.LogWarning("[IosBundles] scene not exported: " + scene.primaryKey + " (" + scene.internalId + ")");
                        unmatched++;
                        continue;
                    }

                    assetNames.Add(path);
                    addressableNames.Add(scene.internalId);
                    matched++;
                }
            }
            else
            {
                var folder = BundleRoot + "/" + bundle.options.m_BundleName + BundleFolderSuffix;
                var files = Directory.Exists(folder)
                    ? Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".meta")).Select(f => f.Replace('\\', '/'))
                        .Where(IsImportedAsset).ToList()
                    : new List<string>();

                var byName = new Dictionary<string, List<string>>();
                var shaderPathByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in files)
                {
                    var raw = NormalizeName(Path.GetFileNameWithoutExtension(file));
                    AddCandidate(byName, raw, file);
                    var deduped = DuplicateSuffix.Replace(raw, "");
                    if (deduped != raw)
                    {
                        AddCandidate(byName, deduped, file);
                    }

                    if (file.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
                    {
                        IndexShaderFile(file, byName, shaderPathByName);
                    }
                }

                foreach (var asset in bundle.assets.GroupBy(a => a.internalId).Select(PickPrimaryEntry))
                {
                    var path = FindExportedPath(asset, byName, claimed, shaderPathByName);
                    if (path == null)
                    {
                        if (unmatched < 200)
                        {
                            Debug.LogWarning("[IosBundles] no export match in " + bundle.file + ": " + asset.primaryKey + " (" + asset.type + ")");
                        }
                        unmatched++;
                        continue;
                    }

                    claimed.Add(path);
                    assetNames.Add(path);
                    // Keep retail catalog primary keys so Addressables and materials resolve the same names.
                    addressableNames.Add(asset.primaryKey);
                    matched++;
                }

                foreach (var leftover in files.Where(f => !claimed.Contains(f)))
                {
                    claimed.Add(leftover);
                    assetNames.Add(leftover);
                    addressableNames.Add(leftover);
                }
            }

            if (assetNames.Count == 0)
            {
                Debug.LogWarning("[IosBundles] bundle has no exported assets, skipping: " + bundle.file);
                continue;
            }

            var abb = new AssetBundleBuild
            {
                assetBundleName = bundle.file,
                assetNames = assetNames.ToArray(),
                addressableNames = addressableNames.ToArray(),
            };
            if (sceneAssets.Count > 0)
            {
                sceneBuilds.Add(abb);
            }
            else
            {
                builds.Add(abb);
            }

            Debug.Log("[IosBundles] queued " + bundle.file + " with " + assetNames.Count + " assets"
                + (sceneAssets.Count > 0 ? " (scene/legacy)" : " (sbp)"));
        }

        Debug.Log("[IosBundles] matched " + matched + " catalog assets, unmatched " + unmatched
            + ", sceneBundles " + sceneBuilds.Count + ", assetBundles " + builds.Count);

        Directory.CreateDirectory(outDir);

        if (sceneBuilds.Count > 0)
        {
            // SBP rejects scene+asset mixes; legacy API packs scene dependencies (StaticSceneData, baked meshes).
            var manifest = BuildPipeline.BuildAssetBundles(
                outDir,
                sceneBuilds.ToArray(),
                BuildAssetBundleOptions.ChunkBasedCompression,
                BuildTarget.iOS);
            Debug.Log("[IosBundles] legacy scene build manifest: " + (manifest != null ? manifest.name : "null"));

            // AssetRipper often leaves bakedDataAsset as a Missing/DLL script ref, so the scene bundle
            // omits BakedData + atlas-UV Combined Mesh. Pack them explicitly for runtime LoadFromFile.
            PackTutorialBakeCompanion(outDir);
        }

        if (builds.Count == 0)
        {
            return;
        }

        var parameters = new BundleBuildParameters(BuildTarget.iOS, BuildTargetGroup.iOS, outDir)
        {
            BundleCompression = UnityEngine.BuildCompression.LZ4,
        };

        var tasks = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleBuiltInShaderExtraction);
        var shaderIndex = tasks.ToList().FindIndex(t => t is CreateBuiltInShadersBundle);
        tasks[shaderIndex] = new CreateBuiltInShadersBundle(shaderBundle ?? "shader_unitybuiltinshaders.bundle");
        if (!string.IsNullOrEmpty(monoScriptBundle))
        {
            tasks.Insert(shaderIndex + 1, new CreateMonoScriptBundle(monoScriptBundle));
        }

        var code = ContentPipeline.BuildAssetBundles(parameters, new BundleBuildContent(builds), out IBundleBuildResults results, tasks);
        Debug.Log("[IosBundles] SBP build result: " + code + ", bundles written: " + (results?.BundleInfos.Count ?? 0));
        if (code < ReturnCode.Success)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void PackTutorialBakeCompanion(string outDir)
    {
        // Do NOT pack BakedData_0.asset — Missing Script (DLL fileID); fields never deserialize on device.
        // Pack mesh + atlases + tutorial_bake.json.txt (index lists) instead.
        const string bakeMesh = "Assets/Mesh/Combined Mesh (root_ StaticSceneOptimizer)_0.asset";
        const string mainAtlas = "Assets/Texture2D/Texture2D_2.png";
        const string blendAtlas = "Assets/Texture2D/Texture2D_1.png";
        const string bakeJson = "Assets/IosBundleTools/tutorial_bake.json.txt";
        AssetDatabase.Refresh();
        var paths = new[] { bakeMesh, mainAtlas, blendAtlas, bakeJson }
            .Where(p => !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p)))
            .ToArray();
        if (paths.Length < 3)
        {
            Debug.LogWarning("[IosBundles] Tutorial bake companion incomplete; found " + paths.Length
                + " (need mesh+atlases+json). Run scripts/Export-TutorialBakeJson.py");
            return;
        }

        Debug.Log("[IosBundles] Tutorial bake companion (no SO) assets=" + string.Join(", ", paths));

        // Allow runtime GetTriangles extract if multi-material path is unavailable.
        try
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(bakeMesh);
            if (mesh != null && !mesh.isReadable)
            {
                var so = new SerializedObject(mesh);
                var p = so.FindProperty("m_IsReadable");
                if (p != null)
                {
                    p.boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mesh);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[IosBundles] Marked bake Combined Mesh isReadable=true");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[IosBundles] Could not mark bake mesh readable: " + ex.Message);
        }

        var abb = new AssetBundleBuild
        {
            assetBundleName = "specialscenes_scenes_tutorial_bakedata.bundle",
            assetNames = paths,
        };
        var manifest = BuildPipeline.BuildAssetBundles(
            outDir,
            new[] { abb },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.iOS);
        Debug.Log("[IosBundles] bake companion manifest: " + (manifest != null ? manifest.name : "null"));
    }

    private static void IndexShaderFile(string file, Dictionary<string, List<string>> byName, Dictionary<string, string> shaderPathByName)
    {
        try
        {
            var text = File.ReadAllText(file);
            var match = ShaderNameRegex.Match(text.Length > 800 ? text.Substring(0, 800) : text);
            if (!match.Success)
            {
                return;
            }

            var shaderName = match.Groups[1].Value;
            shaderPathByName[shaderName] = file;
            AddCandidate(byName, NormalizeName(shaderName), file);
            AddCandidate(byName, NormalizeName(shaderName.Replace('/', '-')), file);
            AddCandidate(byName, NormalizeName(Path.GetFileName(shaderName)), file);

            // Catalog files look like ULTRAKILL-unlit-transparent-ambient.shader while the
            // shader name is often psx/unlit/transparent/ambient or ULTRAKILL/....
            var leaf = NormalizeName(shaderName.Split('/').Last());
            AddCandidate(byName, leaf, file);
            AddCandidate(byName, "ultrakill-" + leaf, file);
            AddCandidate(byName, "psx-" + leaf, file);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[IosBundles] failed reading shader " + file + ": " + ex.Message);
        }
    }

    private static AssetInfo PickPrimaryEntry(IGrouping<string, AssetInfo> entries)
    {
        return entries.OrderBy(e => e.type == "UnityEngine.Sprite" ? 1 : 0).First();
    }

    private static string FindExportedPath(
        AssetInfo asset,
        Dictionary<string, List<string>> byName,
        HashSet<string> claimed,
        Dictionary<string, string> shaderPathByName)
    {
        var name = NormalizeName(Path.GetFileNameWithoutExtension(asset.primaryKey));
        var ext = Path.GetExtension(asset.primaryKey).ToLowerInvariant();

        if (ext == ".shader")
        {
            // Prefer exact Shader "Name" hits from the file body.
            foreach (var kv in shaderPathByName)
            {
                if (claimed.Contains(kv.Value))
                {
                    continue;
                }

                var leaf = NormalizeName(kv.Key.Split('/').Last());
                if (name == leaf || name.EndsWith(leaf) || leaf.EndsWith(name.Replace("ultrakill-", ""))
                    || name.Replace("ultrakill-", "") == leaf
                    || name.Replace('-', '/') == NormalizeName(kv.Key)
                    || NormalizeName(kv.Key.Replace('/', '-')) == name)
                {
                    return kv.Value;
                }
            }
        }

        var candidates = byName.TryGetValue(name, out var list)
            ? list.Where(p => !claimed.Contains(p)).ToList()
            : new List<string>();

        if (candidates.Count == 0 && ext == ".shader")
        {
            // Fuzzy: strip common prefixes and compare alphanumerics only.
            var compact = Compact(name);
            candidates = byName
                .Where(kv => Compact(kv.Key) == compact || Compact(kv.Key).EndsWith(compact) || compact.EndsWith(Compact(kv.Key)))
                .SelectMany(kv => kv.Value)
                .Where(p => !claimed.Contains(p) && p.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        var wanted = asset.type.Substring(asset.type.LastIndexOf('.') + 1);
        return candidates.FirstOrDefault(p => Path.GetExtension(p).ToLowerInvariant() == ext)
            ?? candidates.FirstOrDefault(p => AssetTypeMatches(p, wanted))
            ?? candidates[0];
    }

    private static string Compact(string name)
    {
        return new string(NormalizeName(name).Where(char.IsLetterOrDigit).ToArray());
    }

    private static string NormalizeName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }

    private static void AddCandidate(Dictionary<string, List<string>> byName, string key, string path)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (!byName.TryGetValue(key, out var list))
        {
            byName[key] = list = new List<string>();
        }
        if (!list.Contains(path))
        {
            list.Add(path);
        }
    }

    private static bool IsImportedAsset(string path)
    {
        if (AssetDatabase.GetMainAssetTypeAtPath(path) != null)
        {
            return true;
        }
        Debug.LogWarning("[IosBundles] skipping asset Unity could not import: " + path);
        return false;
    }

    private static bool AssetTypeMatches(string path, string wantedTypeName)
    {
        var type = AssetDatabase.GetMainAssetTypeAtPath(path);
        return type != null && (type.Name == wantedTypeName || (wantedTypeName == "GameObject" && path.EndsWith(".prefab")));
    }

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length)
        {
            throw new ArgumentException("Missing " + name);
        }
        return args[index + 1];
    }
}
