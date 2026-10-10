#if ULTRAKILL_FULL_PORT
using System.IO;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Prefer Addressables shader warmup; LoadFromFile fallback if registry stays empty (boot must not hang).
    /// </summary>
    internal static class ShadersBundleWarmup
    {
        private const string Area = "ShaderWarmup";
        private static bool _sizeProbeDone;
        private static bool _diskLoadDone;

        public static int LoadedShaderCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EarlyProbe()
        {
            ProbeBundleSizeOnly();
        }

        internal static void TryWarmup()
        {
            ProbeBundleSizeOnly();
            RetailShaderRegistry.RefreshFromMemory();
            LoadedShaderCount = RetailShaderRegistry.Count;
        }

        internal static void RefreshCounts()
        {
            RetailShaderRegistry.RefreshFromMemory();
            LoadedShaderCount = RetailShaderRegistry.Count;
        }

        internal static void LoadShadersBundleFromDisk()
        {
            if (_diskLoadDone || !ExternalContentBootstrap.IsReady)
            {
                return;
            }

            _diskLoadDone = true;
            var aa = Path.Combine(ExternalContentBootstrap.ContentStreamingAssetsPath, "aa");
            if (!Directory.Exists(aa))
            {
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
                UltrakillLog.Warn(Area, "LoadFromFile fallback: shaders.bundle not found");
                return;
            }

            var ab = AssetBundle.LoadFromFile(bundlePath);
            if (ab == null)
            {
                UltrakillLog.Warn(Area, "LoadFromFile fallback failed: " + bundlePath);
                return;
            }

            var shaders = ab.LoadAllAssets<Shader>();
            var supported = 0;
            if (shaders != null)
            {
                foreach (var s in shaders)
                {
                    if (s != null && s.isSupported)
                    {
                        RetailShaderRegistry.Register(s);
                        supported++;
                    }
                }
            }

            ab.Unload(false);
            LoadedShaderCount = RetailShaderRegistry.Count;
            UltrakillLog.Info(Area,
                "LoadFromFile fallback: loaded " + supported + " shaders; registry=" + LoadedShaderCount);
        }

        private static void ProbeBundleSizeOnly()
        {
            if (_sizeProbeDone || !ExternalContentBootstrap.IsReady)
            {
                return;
            }

            _sizeProbeDone = true;
            var aa = Path.Combine(ExternalContentBootstrap.ContentStreamingAssetsPath, "aa");
            if (!Directory.Exists(aa))
            {
                UltrakillLog.Warn(Area, "aa folder missing");
                return;
            }

            foreach (var f in Directory.GetFiles(aa, "shaders.bundle", SearchOption.AllDirectories))
            {
                RetailShaderRepair.RegisterShadersBundleSize(new FileInfo(f).Length);
                UltrakillLog.Info(Area,
                    "Shaders bundle on disk " + (new FileInfo(f).Length / 1024) + " KB (registry="
                    + RetailShaderRegistry.Count + ")");
                return;
            }

            UltrakillLog.Warn(Area, "shaders.bundle not found under aa");
        }
    }
}
#endif
