#if ULTRAKILL_FULL_PORT
using System.IO;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Materials reference textures in separate Addressables bundles; without loading them, _MainTex stays null (white).
    /// Session 104451: withMainTex=12/1362 after shader recovery.
    /// </summary>
    internal static class RetailContentWarmup
    {
        private const string Area = "ContentWarmup";
        private static bool _done;

        public static int TexturesLoaded { get; private set; }
        public static int MaterialsLoaded { get; private set; }

        internal static void WarmupRetailBundles()
        {
            if (_done || !ExternalContentBootstrap.IsReady)
            {
                return;
            }

            _done = true;
            var aa = Path.Combine(ExternalContentBootstrap.ContentStreamingAssetsPath, "aa");
            if (!Directory.Exists(aa))
            {
                return;
            }

            WarmupBundleFile(aa, "textures.bundle", ref TexturesLoaded, ref MaterialsLoaded);
            WarmupBundleFile(aa, "materials.bundle", ref TexturesLoaded, ref MaterialsLoaded);
        }

        private static void WarmupBundleFile(string aaRoot, string fileName, ref int texCount, ref int matCount)
        {
            foreach (var path in Directory.GetFiles(aaRoot, fileName, SearchOption.AllDirectories))
            {
                var ab = AssetBundle.LoadFromFile(path);
                if (ab == null)
                {
                    UltrakillLog.Warn(Area, "LoadFromFile failed: " + path);
                    continue;
                }

                var tex = ab.LoadAllAssets<Texture>();
                var mats = ab.LoadAllAssets<Material>();
                texCount += tex != null ? tex.Length : 0;
                matCount += mats != null ? mats.Length : 0;
                ab.Unload(false);

                UltrakillLog.Info(Area,
                    fileName + " from " + Path.GetFileName(Path.GetDirectoryName(path))
                    + " textures=" + (tex != null ? tex.Length : 0)
                    + " materials=" + (mats != null ? mats.Length : 0));
            }
        }
    }
}
#endif
