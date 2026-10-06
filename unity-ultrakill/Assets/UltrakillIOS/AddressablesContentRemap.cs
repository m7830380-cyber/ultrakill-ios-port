#if ULTRAKILL_FULL_PORT
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UltrakillIOS
{
    internal static class AddressablesContentRemap
    {
        public static void Install(string externalStreamingAssetsPath, string bundledStreamingAssetsPath)
        {
            if (string.IsNullOrEmpty(externalStreamingAssetsPath))
            {
                return;
            }

            var bundled = bundledStreamingAssetsPath ?? Application.streamingAssetsPath;
            Addressables.ResourceManager.InternalIdTransformFunc = location => Remap(location, bundled, externalStreamingAssetsPath);
            Debug.Log("[UltrakillIOS] Addressables paths remapped to external StreamingAssets.");
        }

        private static string Remap(IResourceLocation location, string bundled, string external)
        {
            var id = location?.InternalId;
            if (string.IsNullOrEmpty(id))
            {
                return id;
            }

            if (id.StartsWith(bundled, System.StringComparison.Ordinal))
            {
                return external + id.Substring(bundled.Length);
            }

            var marker = "StreamingAssets";
            var idx = id.IndexOf(marker, System.StringComparison.Ordinal);
            if (idx >= 0)
            {
                var tail = id.Substring(idx + marker.Length).TrimStart('/', '\\');
                return Path.Combine(external, tail);
            }

            return id;
        }
    }
}
#endif
