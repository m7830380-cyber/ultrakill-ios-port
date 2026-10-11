using System;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UltrakillIOS
{
    // Entry point. Runs before the first scene (level0) loads.
    public static class Bootstrap
    {
        public static string Documents => Application.persistentDataPath;

        // Converted Addressables content, copied by the user into Documents/ULTRAKILL-Content.
        public static string ContentRoot => Path.Combine(Documents, "ULTRAKILL-Content");

        static string s_StreamingAa;
        static string s_DocumentsAa;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Log.Init();
            Log.Info($"UltrakillIOS starting. device={SystemInfo.deviceModel} os={SystemInfo.operatingSystem} " +
                     $"gpu={SystemInfo.graphicsDeviceName} mem={SystemInfo.systemMemorySize}MB screen={Screen.width}x{Screen.height}@{Screen.dpi}dpi");

            Input.simulateMouseWithTouches = false;
            Application.targetFrameRate = Math.Max(60, (int)Math.Round(Screen.currentResolution.refreshRateRatio.value));
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            s_StreamingAa = Path.Combine(Application.streamingAssetsPath, "aa").Replace('\\', '/');
            s_DocumentsAa = Path.Combine(ContentRoot, "aa").Replace('\\', '/');
            if (Directory.Exists(s_DocumentsAa))
            {
                Log.Info("Addressables content: " + s_DocumentsAa);
                Addressables.InternalIdTransformFunc = TransformInternalId;
            }
            else
            {
                Log.Warn("No content found at " + s_DocumentsAa + " - using bundles inside the app (if any).");
            }

            var go = new GameObject("UltrakillIOS");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<TouchHud>();
            go.AddComponent<DebugOverlay>();
        }

        static string TransformInternalId(IResourceLocation location)
        {
            string id = location.InternalId;
            if (id != null && id.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase))
            {
                string norm = id.Replace('\\', '/');
                if (norm.StartsWith(s_StreamingAa, StringComparison.Ordinal))
                {
                    string candidate = s_DocumentsAa + norm.Substring(s_StreamingAa.Length);
                    if (File.Exists(candidate))
                        return candidate;
                    Log.Warn("Bundle missing from Documents content: " + candidate);
                }
            }
            return id;
        }
    }
}
