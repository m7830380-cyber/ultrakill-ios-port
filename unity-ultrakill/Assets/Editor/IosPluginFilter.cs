using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Retail Managed dump includes engine + PC Steam DLLs; exclude from iOS player.
/// </summary>
[InitializeOnLoad]
public static class IosPluginFilter
{
    static IosPluginFilter()
    {
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()

    {
        var retail = Path.Combine(Application.dataPath, "Plugins", "RetailManaged");
        if (!Directory.Exists(retail))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(retail, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(path);
            if (!ShouldExcludeFromIos(name))
            {
                continue;
            }

            var rel = "Assets/Plugins/RetailManaged/" + name;
            var importer = AssetImporter.GetAtPath(rel) as PluginImporter;
            if (importer == null)
            {
                continue;
            }

            importer.SetCompatibleWithPlatform(BuildTarget.iOS, false);
            importer.SaveAndReimport();
        }
    }

    private static bool ShouldExcludeFromIos(string fileName)
    {
        if (fileName.StartsWith("UnityEngine."))
        {
            return true;
        }

        if (fileName == "UnityEngine.dll" || fileName == "mscorlib.dll" || fileName == "netstandard.dll")
        {
            return true;
        }

        if (fileName.StartsWith("System."))
        {
            return true;
        }

        if (fileName.StartsWith("Facepunch."))
        {
            return true;
        }

        return false;
    }
}
