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
    /// Load shaders through Addressables when possible. No yield inside try/catch (CS1626). Never abort boot.
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

            foreach (var key in ShaderCatalogKeys)
            {
                yield return DownloadAndRegisterShader(key);
            }

            RetailShaderRegistry.RefreshFromMemory();
            UltrakillLog.Info(Area, "Shader registry count=" + RetailShaderRegistry.Count);

            if (RetailShaderRegistry.Count < 16)
            {
                ShadersBundleWarmup.LoadShadersBundleFromDisk();
            }
        }

        private static IEnumerator DownloadAndRegisterShader(string key)
        {
            AsyncOperationHandle dep = default;
            AsyncOperationHandle<Shader> load = default;
            try
            {
                dep = Addressables.DownloadDependenciesAsync(key, true);
                if (dep.IsValid())
                {
                    yield return dep;
                    if (dep.IsValid() && dep.Status != AsyncOperationStatus.Succeeded)
                    {
                        UltrakillLog.Warn(Area, "DownloadDependencies failed for " + key + ": " + dep.OperationException);
                    }
                }

                load = Addressables.LoadAssetAsync<Shader>(key);
                if (!load.IsValid())
                {
                    yield break;
                }

                yield return load;
                if (load.IsValid()
                    && load.Status == AsyncOperationStatus.Succeeded
                    && load.Result != null)
                {
                    RetailShaderRegistry.Register(load.Result);
                }
            }
            finally
            {
                if (load.IsValid())
                {
                    Addressables.Release(load);
                }

                if (dep.IsValid())
                {
                    Addressables.Release(dep);
                }
            }
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
