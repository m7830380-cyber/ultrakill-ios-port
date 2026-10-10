#if ULTRAKILL_FULL_PORT
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Never LoadFromFile materials/textures.bundle — Addressables owns those paths (session 122619:
    /// "another AssetBundle with the same files is already loaded" → Main Menu load failed).
    /// Build a relink index from materials already loaded into memory after Addressables runs.
    /// </summary>
    internal static class RetailContentWarmup
    {
        private const string Area = "ContentWarmup";

        private static readonly System.Collections.Generic.Dictionary<string, Material> MaterialsByName =
            new System.Collections.Generic.Dictionary<string, Material>();

        private static string _lastIndexScene = "";
        private static int _lastIndexFrame = -9999;

        public static int TexturesLoaded { get; private set; }
        public static int MaterialsLoaded { get; private set; }

        /// <summary>Index textured materials already resident (post-Addressables).</summary>
        internal static void RefreshMaterialIndex()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? "";
            if (scene == _lastIndexScene && Time.frameCount - _lastIndexFrame < 90)
            {
                return;
            }

            _lastIndexScene = scene;
            _lastIndexFrame = Time.frameCount;

            MaterialsByName.Clear();
            MaterialsLoaded = 0;

            var textures = Resources.FindObjectsOfTypeAll<Texture>();
            TexturesLoaded = textures != null ? textures.Length : 0;

            var withTex = 0;
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat == null || string.IsNullOrEmpty(mat.name))
                {
                    continue;
                }

                if (RetailMaterialVisuals.IsBrokenShader(mat.shader))
                {
                    continue;
                }

                if (!MaterialHasAnyTexture(mat))
                {
                    continue;
                }

                withTex++;
                if (!MaterialsByName.TryGetValue(mat.name, out var existing)
                    || MaterialQuality(mat) > MaterialQuality(existing))
                {
                    MaterialsByName[mat.name] = mat;
                }
            }

            RetailMaterialVisuals.RebuildTextureIndex();

            MaterialsLoaded = MaterialsByName.Count;
            UltrakillLog.Info(Area,
                "Material index from loaded assets: unique=" + MaterialsByName.Count
                + " withAnyTexProp=" + withTex
                + " texturesInMemory=" + TexturesLoaded);
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
                    if (m == null)
                    {
                        continue;
                    }

                    var needsRelink = RetailMaterialVisuals.IsBrokenShader(m.shader) || !MaterialHasAnyTexture(m);
                    if (!needsRelink)
                    {
                        continue;
                    }

                    var src = TryGetBundleMaterial(m.name);
                    if (src == null || RetailMaterialVisuals.IsBrokenShader(src.shader) || !MaterialHasAnyTexture(src))
                    {
                        continue;
                    }

                    RetailMaterialVisuals.CopyTexturesAndShader(m, src);
                    if (!MaterialHasAnyTexture(m) && MaterialHasAnyTexture(src))
                    {
                        shared[i] = src;
                    }

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
                UltrakillLog.Info(Area, "Relinked " + relinked + " material slots from loaded retail materials");
            }

            return relinked;
        }

        internal static bool MaterialHasAnyTexture(Material m)
        {
            return TexturePropertyCount(m) > 0;
        }

        private static int TexturePropertyCount(Material m)
        {
            if (m == null)
            {
                return 0;
            }

            var n = 0;
            try
            {
                foreach (var prop in m.GetTexturePropertyNames())
                {
                    if (m.GetTexture(prop) != null)
                    {
                        n++;
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            return n;
        }

        private static int MaterialQuality(Material m)
        {
            if (m == null)
            {
                return 0;
            }

            var score = TexturePropertyCount(m) * 10;
            var sn = m.shader != null ? m.shader.name : "";
            if (sn.StartsWith("ULTRAKILL", System.StringComparison.OrdinalIgnoreCase))
            {
                score += 100;
            }

            return score;
        }
    }
}
#endif
