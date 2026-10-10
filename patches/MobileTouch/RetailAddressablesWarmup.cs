#if ULTRAKILL_FULL_PORT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UltrakillIOS
{
    /// <summary>
    /// Load shaders.bundle through Addressables before scenes/materials (Unity manual: load shader AssetBundle first).
    /// Avoids duplicate LoadFromFile + mismatched shader instances that cause InternalError/pink materials.
    /// </summary>
    internal static class RetailAddressablesWarmup
    {
        private const string Area = "AddrWarmup";
        private static bool _shaderDepsDone;

        private static readonly string[] ShaderCatalogKeys =
        {
            "Assets/Shaders/MasterShader/ULTRAKILL-Standard.shader",
            "Assets/Shaders/MasterShader/ULTRAKILL-Stationary.shader",
        };

        internal static IEnumerator EnsureShaderBundlesLoaded()
        {
            if (_shaderDepsDone)
            {
                yield break;
            }

            _shaderDepsDone = true;
            var keys = new List<object>();
            foreach (var key in ShaderCatalogKeys)
            {
                keys.Add(key);
            }

            var locHandle = Addressables.LoadResourceLocationsAsync(keys, Addressables.MergeMode.Union, typeof(Shader));
            yield return locHandle;
            if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result == null || locHandle.Result.Count == 0)
            {
                UltrakillLog.Warn(Area, "Shader catalog keys not in locator; materials may stay pink until scene deps load");
                Addressables.Release(locHandle);
                yield break;
            }

            var depHandle = Addressables.DownloadDependenciesAsync(locHandle.Result, true);
            yield return depHandle;
            if (depHandle.Status != AsyncOperationStatus.Succeeded)
            {
                UltrakillLog.Warn(Area, "DownloadDependenciesAsync(shaders) failed: " + depHandle.OperationException);
            }
            else
            {
                UltrakillLog.Info(Area, "Shader bundle dependencies downloaded (" + locHandle.Result.Count + " locations)");
            }

            Addressables.Release(depHandle);
            Addressables.Release(locHandle);

            foreach (var key in ShaderCatalogKeys)
            {
                var load = Addressables.LoadAssetAsync<Shader>(key);
                yield return load;
                if (load.Status == AsyncOperationStatus.Succeeded && load.Result != null)
                {
                    RetailShaderRegistry.Register(load.Result);
                }

                Addressables.Release(load);
            }

            RetailShaderRegistry.RefreshFromMemory();
            UltrakillLog.Info(Area, "Shader registry count=" + RetailShaderRegistry.Count);
        }

        internal static IEnumerator EnsureSceneDependencies(string sceneKey)
        {
            if (string.IsNullOrEmpty(sceneKey))
            {
                yield break;
            }

            var loc = Addressables.LoadResourceLocationsAsync(sceneKey, typeof(UnityEngine.ResourceManagement.ResourceProviders.SceneInstance));
            yield return loc;
            if (loc.Status != AsyncOperationStatus.Succeeded || loc.Result == null || loc.Result.Count == 0)
            {
                Addressables.Release(loc);
                yield break;
            }

            var dep = Addressables.DownloadDependenciesAsync(loc.Result, true);
            yield return dep;
            if (dep.Status == AsyncOperationStatus.Succeeded)
            {
                UltrakillLog.Info(Area, "Scene dependencies ready for '" + sceneKey + "'");
            }

            Addressables.Release(dep);
            Addressables.Release(loc);
            RetailShaderRegistry.RefreshFromMemory();
            RetailContentWarmup.RefreshMaterialIndex();
        }
    }
}
#endif
