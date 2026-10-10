#if ULTRAKILL_FULL_PORT
using System.IO;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Load retail shaders.bundle via LoadFromFile (121 UK stubs). Safe before Addressables — do NOT preload materials/textures bundles.
    /// </summary>
    internal static class ShadersBundleWarmup
    {
        private const string Area = "ShaderWarmup";
        private static bool _done;

        public static int LoadedShaderCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EarlyWarmup()
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

            var ab = AssetBundle.LoadFromFile(bundlePath);
            if (ab == null)
            {
                UltrakillLog.Warn(Area, "LoadFromFile failed: " + bundlePath);
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
            UltrakillLog.Info(Area, "Loaded " + supported + " shaders from bundle (" + supported + " supported)");
        }

        internal static void RefreshCounts()
        {
            RetailShaderRegistry.RefreshFromMemory();
            LoadedShaderCount = RetailShaderRegistry.Count;
        }

        internal static void LoadShadersBundleFromDisk()
        {
            TryWarmup();
        }
    }
}
#endif
