#if ULTRAKILL_FULL_PORT
using System.IO;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Loads specialscenes_scenes_tutorial_bakedata.bundle (BakedData + atlas-UV mesh + atlases)
    /// that the scene Addressables bundle often omits after AssetRipper export.
    /// </summary>
    internal static class TutorialBakeDataWarmup
    {
        private const string Area = "BakeWarmup";
        private static AssetBundle _bundle;

        public static void EnsureLoaded()
        {
            if (_bundle != null)
            {
                return;
            }

            var root = ExternalContentBootstrap.ContentStreamingAssetsPath;
            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            var path = Path.Combine(root, "aa", "iOS", "specialscenes_scenes_tutorial_bakedata.bundle");
            if (!File.Exists(path))
            {
                UltrakillLog.Warn(Area, "Missing bake companion: " + path);
                return;
            }

            _bundle = AssetBundle.LoadFromFile(path);
            if (_bundle == null)
            {
                UltrakillLog.Warn(Area, "LoadFromFile failed: " + path);
                return;
            }

            var assets = _bundle.LoadAllAssets();
            UltrakillLog.Info(Area, "Loaded bake companion assets=" + (assets != null ? assets.Length : 0)
                + " sizeMB=" + (new FileInfo(path).Length / (1024f * 1024f)).ToString("F2"));
        }
    }
}
#endif
