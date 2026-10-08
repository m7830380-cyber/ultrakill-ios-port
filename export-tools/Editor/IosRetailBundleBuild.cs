using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEngine;

/// <summary>
/// Rebuilds the retail Addressables bundles for iOS from an AssetRipper export, keeping the retail bundle file names
/// and the GUID internal names so the retail catalog.json can be reused unchanged apart from the platform folder.
/// Run with: Unity -batchmode -buildTarget iOS -projectPath ExportedProject -executeMethod IosRetailBundleBuild.Build
///   -catalogMap path/to/catalog-map.json -outDir path/to/output
/// </summary>
public static class IosRetailBundleBuild
{
    [Serializable] private class CatalogMap { public List<BundleInfo> bundles; public List<SceneInfo> scenes; }
    [Serializable] private class BundleInfo { public string file; public BundleOptions options; public List<AssetInfo> assets; }
    [Serializable] private class BundleOptions { public string m_BundleName; }
    [Serializable] private class AssetInfo { public string internalId; public string primaryKey; public string type; }
    [Serializable] private class SceneInfo { public string key; public string internalId; public string bundle; }

    private const string BundleRoot = "Assets/Asset_Bundles";

    // Build-IosBundles.ps1 renames AssetRipper's "<hash>.bundle" folders, which Unity treats as opaque macOS plugins.
    private const string BundleFolderSuffix = "_bundle";

    public static void Build()
    {
        var mapPath = GetArg("-catalogMap");
        var outDir = GetArg("-outDir");
        var map = JsonUtility.FromJson<CatalogMap>(File.ReadAllText(mapPath));
        var scenePaths = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath)
            .GroupBy(p => Path.GetFileNameWithoutExtension(p)).ToDictionary(g => g.Key, g => g.First());

        var builds = new List<AssetBundleBuild>();
        var claimed = new HashSet<string>();
        string shaderBundle = null, monoScriptBundle = null;
        int matched = 0, unmatched = 0;

        foreach (var bundle in map.bundles)
        {
            if (bundle.file.StartsWith("shader_")) { shaderBundle = bundle.file; continue; }
            if (bundle.file.StartsWith("monoscript_")) { monoScriptBundle = bundle.file; continue; }

            var assetNames = new List<string>();
            var addressableNames = new List<string>();
            var sceneAssets = bundle.assets.Where(a => a.type.EndsWith("SceneInstance")).ToList();

            if (sceneAssets.Count > 0)
            {
                foreach (var scene in sceneAssets)
                {
                    if (scenePaths.TryGetValue(scene.internalId, out var path))
                    {
                        assetNames.Add(path);
                        addressableNames.Add(scene.internalId);
                        matched++;
                    }
                    else
                    {
                        Debug.LogWarning("[IosBundles] scene not exported: " + scene.primaryKey + " (" + scene.internalId + ")");
                        unmatched++;
                    }
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
                foreach (var file in files)
                {
                    var raw = NormalizeName(Path.GetFileNameWithoutExtension(file));
                    AddCandidate(byName, raw, file);
                    var deduped = DuplicateSuffix.Replace(raw, "");
                    if (deduped != raw)
                    {
                        AddCandidate(byName, deduped, file);
                    }
                }

                foreach (var asset in bundle.assets.GroupBy(a => a.internalId).Select(PickPrimaryEntry))
                {
                    var path = FindExportedPath(asset, byName, claimed);
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
                    addressableNames.Add(asset.internalId);
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

            builds.Add(new AssetBundleBuild
            {
                assetBundleName = bundle.file,
                assetNames = assetNames.ToArray(),
                addressableNames = addressableNames.ToArray(),
            });
        }

        Debug.Log("[IosBundles] matched " + matched + " catalog assets, unmatched " + unmatched + ", bundles " + builds.Count);

        Directory.CreateDirectory(outDir);
        var parameters = new BundleBuildParameters(BuildTarget.iOS, BuildTargetGroup.iOS, outDir)
        {
            BundleCompression = UnityEngine.BuildCompression.LZ4,
        };

        var tasks = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleBuiltInShaderExtraction);
        var shaderIndex = tasks.ToList().FindIndex(t => t is CreateBuiltInShadersBundle);
        tasks[shaderIndex] = new CreateBuiltInShadersBundle(shaderBundle);
        tasks.Insert(shaderIndex + 1, new CreateMonoScriptBundle(monoScriptBundle));

        var code = ContentPipeline.BuildAssetBundles(parameters, new BundleBuildContent(builds), out IBundleBuildResults results, tasks);
        Debug.Log("[IosBundles] build result: " + code + ", bundles written: " + (results?.BundleInfos.Count ?? 0));
        if (code < ReturnCode.Success)
        {
            EditorApplication.Exit(1);
        }
    }

    private static AssetInfo PickPrimaryEntry(IGrouping<string, AssetInfo> entries)
    {
        // A texture is listed as Texture2D and Sprite under one GUID; the bundle needs the main asset.
        return entries.OrderBy(e => e.type == "UnityEngine.Sprite" ? 1 : 0).First();
    }

    private static string FindExportedPath(AssetInfo asset, Dictionary<string, List<string>> byName, HashSet<string> claimed)
    {
        var name = NormalizeName(Path.GetFileNameWithoutExtension(asset.primaryKey));
        var ext = Path.GetExtension(asset.primaryKey).ToLowerInvariant();
        var candidates = byName.TryGetValue(name, out var list)
            ? list.Where(p => !claimed.Contains(p)).ToList()
            : new List<string>();

        if (candidates.Count == 0)
        {
            return null;
        }

        var wanted = asset.type.Substring(asset.type.LastIndexOf('.') + 1);
        return candidates.FirstOrDefault(p => Path.GetExtension(p).ToLowerInvariant() == ext)
            ?? candidates.FirstOrDefault(p => AssetTypeMatches(p, wanted))
            ?? candidates[0];
    }

    // AssetRipper writes repeated names as Name_0, Name_1, ...
    private static readonly System.Text.RegularExpressions.Regex DuplicateSuffix = new System.Text.RegularExpressions.Regex("_\\d+$");

    private static string NormalizeName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }

    private static void AddCandidate(Dictionary<string, List<string>> byName, string key, string path)
    {
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
