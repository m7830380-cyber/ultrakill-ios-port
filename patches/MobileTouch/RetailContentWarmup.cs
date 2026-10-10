#if ULTRAKILL_FULL_PORT
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Session 112439: warmupTex=2644 but scene anyTexProp=12 — textures in memory, scene materials not linked.
    /// Keep bundle materials indexed by name so renderers can swap broken instances for retail assets.
    /// </summary>
    internal static class RetailContentWarmup
    {
        private const string Area = "ContentWarmup";
        private static bool _done;

        private static readonly Dictionary<string, Material> MaterialsByName = new Dictionary<string, Material>();

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

            // Load textures first and keep them resident so materials.bundle can resolve cross-refs.
            var heldBundles = new List<AssetBundle>();
            WarmupBundleFile(aa, "textures.bundle", heldBundles);
            WarmupBundleFile(aa, "materials.bundle", heldBundles);

            var withTex = 0;
            foreach (var m in MaterialsByName.Values)
            {
                if (MaterialHasAnyTexture(m))
                {
                    withTex++;
                }
            }

            UltrakillLog.Info(Area,
                "Bundle material index: unique=" + MaterialsByName.Count
                + " withAnyTexProp=" + withTex
                + " heldBundles=" + heldBundles.Count);
        }

        internal static Material TryGetBundleMaterial(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
            {
                return null;
            }

            if (MaterialsByName.TryGetValue(materialName, out var m))
            {
                return m;
            }

            foreach (var kv in MaterialsByName)
            {
                if (string.Equals(kv.Key, materialName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Value;
                }
            }

            return null;
        }

        /// <summary>Replace scene material instances that have no texture refs with matching bundle materials.</summary>
        internal static int RelinkSceneMaterials(bool includeInactive)
        {
            if (MaterialsByName.Count == 0)
            {
                return 0;
            }

            var relinked = 0;
            foreach (var r in Object.FindObjectsOfType<Renderer>(includeInactive))
            {
                if (r == null || r is ParticleSystemRenderer || r.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                var shared = r.sharedMaterials;
                if (shared == null || shared.Length == 0)
                {
                    continue;
                }

                var changed = false;
                for (var i = 0; i < shared.Length; i++)
                {
                    var m = shared[i];
                    if (m == null || MaterialHasAnyTexture(m))
                    {
                        continue;
                    }

                    var src = TryGetBundleMaterial(m.name);
                    if (src == null || !MaterialHasAnyTexture(src))
                    {
                        continue;
                    }

                    shared[i] = src;
                    changed = true;
                    relinked++;
                }

                if (changed)
                {
                    r.sharedMaterials = shared;
                }
            }

            if (relinked > 0)
            {
                UltrakillLog.Info(Area, "Relinked " + relinked + " material slots from materials.bundle");
            }

            return relinked;
        }

        internal static bool MaterialHasAnyTexture(Material m)
        {
            if (m == null)
            {
                return false;
            }

            try
            {
                foreach (var prop in m.GetTexturePropertyNames())
                {
                    if (m.GetTexture(prop) != null)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            return false;
        }

        private static void WarmupBundleFile(string aaRoot, string fileName, List<AssetBundle> holdOpen)
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
                TexturesLoaded += tex != null ? tex.Length : 0;
                if (mats != null)
                {
                    foreach (var mat in mats)
                    {
                        if (mat == null || string.IsNullOrEmpty(mat.name))
                        {
                            continue;
                        }

                        MaterialsByName[mat.name] = mat;
                        MaterialsLoaded++;
                    }
                }

                if (holdOpen != null)
                {
                    holdOpen.Add(ab);
                }
                else
                {
                    ab.Unload(false);
                }

                UltrakillLog.Info(Area,
                    fileName + " from " + Path.GetFileName(Path.GetDirectoryName(path))
                    + " textures=" + (tex != null ? tex.Length : 0)
                    + " materials=" + (mats != null ? mats.Length : 0)
                    + " index=" + MaterialsByName.Count);
            }
        }
    }
}
#endif
