#if ULTRAKILL_FULL_PORT
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UltrakillIOS
{
    internal static class AddressablesContentRemap
    {
        private const int LoggedRemapLimit = 25;
        private static int _loggedRemaps;

        public static void Install(string externalStreamingAssetsPath, string bundledStreamingAssetsPath)
        {
            if (string.IsNullOrEmpty(externalStreamingAssetsPath))
            {
                return;
            }

            var bundled = (bundledStreamingAssetsPath ?? Application.streamingAssetsPath).Replace('\\', '/');
            var external = externalStreamingAssetsPath.Replace('\\', '/');
            Addressables.ResourceManager.InternalIdTransformFunc = location => Remap(location, bundled, external);
            UltrakillLog.Info("Addressables", "Remap installed: '" + bundled + "' -> '" + external + "'");
            UltrakillLog.Info("Addressables", "RuntimePath: " + Addressables.RuntimePath);
        }

        private static string Remap(IResourceLocation location, string bundled, string external)
        {
            var id = location?.InternalId;
            if (string.IsNullOrEmpty(id))
            {
                return id;
            }

            // Retail catalog was built on Windows and stores paths with backslashes.
            var normalized = id.Replace('\\', '/');
            var result = normalized;
            if (normalized.StartsWith(bundled, System.StringComparison.Ordinal))
            {
                result = external + normalized.Substring(bundled.Length);
            }
            else
            {
                var marker = "StreamingAssets/";
                var idx = normalized.IndexOf(marker, System.StringComparison.Ordinal);
                if (idx >= 0)
                {
                    result = Path.Combine(external, normalized.Substring(idx + marker.Length));
                }
            }

            if (_loggedRemaps < LoggedRemapLimit && result != id)
            {
                _loggedRemaps++;
                var exists = File.Exists(result) ? "exists" : "NOT FOUND";
                UltrakillLog.Info("Addressables", "Remap " + id + " -> " + result + " (" + exists + ")");
            }

            return result;
        }
    }
}
#endif
