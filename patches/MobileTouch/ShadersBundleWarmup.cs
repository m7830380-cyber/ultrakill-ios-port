#if ULTRAKILL_FULL_PORT
using System.IO;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Records shaders.bundle size. Shaders load via Addressables (RetailAddressablesWarmup), not LoadFromFile —
    /// duplicate bundle load breaks material→shader GUID resolution (pink InternalError).
    /// </summary>
    internal static class ShadersBundleWarmup
    {
        private const string Area = "ShaderWarmup";
        private static bool _done;

        public static int LoadedShaderCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EarlyProbe()
        {
            TryWarmup();
        }

        internal static void TryWarmup()
        {
            if (_done || !ExternalContentBootstrap.IsReady)
            {
                return;
            }

            _done = true;
            var aa = Path.Combine(ExternalContentBootstrap.ContentStreamingAssetsPath, "aa");
            if (!Directory.Exists(aa))
            {
                UltrakillLog.Warn(Area, "aa folder missing");
                return;
            }

            string bundlePath = null;
            foreach (var f in Directory.GetFiles(aa, "shaders.bundle", SearchOption.AllDirectories))
            {
                bundlePath = f;
                break;
            }

            if (string.IsNullOrEmpty(bundlePath))
            {
                UltrakillLog.Warn(Area, "shaders.bundle not found under aa");
                return;
            }

            RetailShaderRepair.RegisterShadersBundleSize(new FileInfo(bundlePath).Length);
            RetailShaderRegistry.RefreshFromMemory();
            LoadedShaderCount = RetailShaderRegistry.Count;
            UltrakillLog.Info(Area,
                "Shaders via Addressables warmup (no LoadFromFile); registry=" + LoadedShaderCount
                + " bundleBytes=" + new FileInfo(bundlePath).Length);
        }

        internal static void RefreshCounts()
        {
            RetailShaderRegistry.RefreshFromMemory();
            LoadedShaderCount = RetailShaderRegistry.Count;
        }
    }
}
#endif
