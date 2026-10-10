#if ULTRAKILL_FULL_PORT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UltrakillIOS
{
    /// <summary>
    /// Scene dependency warmup only. Shader catalog keys use Windows GUIDs — LoadAssetAsync fails on stub
    /// iOS shaders.bundle (session 172516: Unable to load Shader from location …).
    /// Shaders load via ShadersBundleWarmup.LoadShadersBundleFromDisk after Addressables init.
    /// </summary>
    internal static class RetailAddressablesWarmup
    {
        private const string Area = "AddrWarmup";

        internal static IEnumerator EnsureShaderBundlesLoaded()
        {
            ShadersBundleWarmup.LoadShadersBundleFromDisk();
            yield break;
        }

        internal static IEnumerator EnsureSceneDependencies(string sceneKey)
        {
            if (string.IsNullOrEmpty(sceneKey))
            {
                yield break;
            }

            AsyncOperationHandle<IList<IResourceLocation>> loc = default;
            AsyncOperationHandle dep = default;
            try
            {
                loc = Addressables.LoadResourceLocationsAsync(
                    sceneKey,
                    typeof(UnityEngine.ResourceManagement.ResourceProviders.SceneInstance));
                if (!loc.IsValid())
                {
                    yield break;
                }

                yield return loc;
                if (!loc.IsValid()
                    || loc.Status != AsyncOperationStatus.Succeeded
                    || loc.Result == null
                    || loc.Result.Count == 0)
                {
                    yield break;
                }

                dep = Addressables.DownloadDependenciesAsync(loc.Result, true);
                if (!dep.IsValid())
                {
                    yield break;
                }

                yield return dep;
                if (dep.IsValid() && dep.Status == AsyncOperationStatus.Succeeded)
                {
                    UltrakillLog.Info(Area, "Scene dependencies ready for '" + sceneKey + "'");
                }

                RetailShaderRegistry.RefreshFromMemory();
                RetailContentWarmup.RefreshMaterialIndex();
            }
            finally
            {
                if (dep.IsValid())
                {
                    Addressables.Release(dep);
                }

                if (loc.IsValid())
                {
                    Addressables.Release(loc);
                }
            }
        }
    }
}
#endif
