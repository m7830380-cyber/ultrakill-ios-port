#if ULTRAKILL_FULL_PORT
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Boots retail content step by step and logs exactly where it stops.
    /// </summary>
    internal sealed class RetailBootProbe : MonoBehaviour
    {
        private const string Area = "Boot";
        private static readonly string[] BootSceneKeys = { "Main Menu", "Intro", "Tutorial" };

        public static string Status { get; private set; } = "Waiting";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("UltrakillIOS.BootProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<RetailBootProbe>();
        }

        private IEnumerator Start()
        {
            SceneManager.sceneLoaded += (scene, mode) => UltrakillLog.Info("Scene", "Loaded '" + scene.name + "' (" + mode + "), roots=" + scene.rootCount);
            UltrakillLog.Info(Area, "Active scene: '" + SceneManager.GetActiveScene().name + "', build scenes=" + SceneManager.sceneCountInBuildSettings);
            UltrakillLog.Info(Area, "Overlay assembly: " + typeof(RetailBootProbe).Assembly.GetName().Name);
            UltrakillLog.Info(Area, "Assembly-CSharp Bootstrap type: " + (Type.GetType("Bootstrap, Assembly-CSharp") != null ? "found" : "MISSING"));
            UltrakillLog.Info(Area, "Assembly-CSharp SceneHelper type: " + (Type.GetType("SceneHelper, Assembly-CSharp") != null ? "found" : "MISSING"));

            if (!ExternalContentBootstrap.IsReady)
            {
                Fail("External data not bound; see earlier [Content] lines.");
                yield break;
            }

            var aa = Path.Combine(ExternalContentBootstrap.ContentStreamingAssetsPath, "aa");
            if (!InspectAddressablesFolder(aa))
            {
                yield break;
            }

            ProbeOneBundle(aa);
            LogTutorialBundleSize(aa);

            SetStatus("Initializing Addressables");
            var init = Addressables.InitializeAsync(false);
            yield return init;
            if (init.Status != AsyncOperationStatus.Succeeded)
            {
                Fail("Addressables.InitializeAsync failed: " + init.OperationException);
                Addressables.Release(init);
                yield break;
            }

            var locator = init.Result;
            UltrakillLog.Info(Area, "Catalog loaded: locator '" + locator.LocatorId + "', keys=" + locator.Keys.Count());
            Addressables.Release(init);
            ShadersBundleWarmup.TryWarmup();
            ShadersBundleWarmup.RefreshCounts();
            UkMasterShaderBootstrap.EnsureReady();

            var sceneKey = BootSceneKeys.FirstOrDefault(k => locator.Locate(k, typeof(SceneInstance), out _));
            if (sceneKey == null)
            {
                Fail("None of the boot scene keys (" + string.Join(", ", BootSceneKeys) + ") exist in the catalog.");
                yield break;
            }

            SetStatus("Loading scene '" + sceneKey + "'");
            var load = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Single);
            while (!load.IsDone)
            {
                Status = "Loading scene '" + sceneKey + "' " + Mathf.RoundToInt(load.PercentComplete * 100f) + "%";
                yield return null;
            }

            if (load.Status != AsyncOperationStatus.Succeeded)
            {
                Fail("LoadSceneAsync('" + sceneKey + "') failed: " + load.OperationException);
                yield break;
            }

            SetStatus("Scene '" + sceneKey + "' loaded");
        }

        private static bool InspectAddressablesFolder(string aa)
        {
            var settings = Path.Combine(aa, "settings.json");
            var catalog = Path.Combine(aa, "catalog.json");
            UltrakillLog.Info(Area, "settings.json " + (File.Exists(settings) ? "present" : "MISSING") + ", catalog.json " + (File.Exists(catalog) ? "present" : "MISSING"));
            if (!File.Exists(settings) || !File.Exists(catalog))
            {
                Fail("Addressables runtime data missing under " + aa);
                return false;
            }

            var json = File.ReadAllText(settings);
            var marker = "\"m_buildTarget\":\"";
            var start = json.IndexOf(marker, StringComparison.Ordinal);
            if (start >= 0)
            {
                start += marker.Length;
                var target = json.Substring(start, json.IndexOf('"', start) - start);
                if (target == "iOS")
                {
                    UltrakillLog.Info(Area, "Addressables build target: iOS");
                }
                else
                {
                    UltrakillLog.Warn(Area, "Addressables build target is '" + target + "', not iOS. Bundles built for another platform usually fail to load or render on iOS.");
                }
            }

            foreach (var dir in Directory.GetDirectories(aa))
            {
                var bundles = Directory.GetFiles(dir, "*.bundle", SearchOption.AllDirectories);
                if (bundles.Length > 0)
                {
                    var mb = bundles.Sum(b => new FileInfo(b).Length) / (1024 * 1024);
                    UltrakillLog.Info(Area, "Bundle folder '" + Path.GetFileName(dir) + "': " + bundles.Length + " bundles, " + mb + " MB");
                }
            }

            var shaderBundle = Directory.GetFiles(aa, "shaders.bundle", SearchOption.AllDirectories);
            foreach (var sb in shaderBundle)
            {
                var len = new FileInfo(sb).Length;
                var mb = Math.Round(len / (1024.0 * 1024.0), 2);
                var rel = sb.Replace(aa, "").TrimStart('\\', '/');
                RetailShaderRepair.RegisterShadersBundleSize(len);
                if (len < 5_000_000)
                {
                    UltrakillLog.Warn(Area, "shaders.bundle tiny (" + mb + " MB) at " + rel
                        + " — replace with a real iOS shaders.bundle build (scripts/Build-IosShaders.ps1). Runtime no longer swaps all materials (caused tex=0 white).");
                }
                else
                {
                    UltrakillLog.Info(Area, "shaders.bundle " + mb + " MB at " + rel);
                }
            }

            return true;
        }

        private static void LogTutorialBundleSize(string aa)
        {
            var files = Directory.GetFiles(aa, "*tutorial*.bundle", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                UltrakillLog.Warn(Area, "No *tutorial*.bundle under aa — StaticSceneOptimizer bake likely missing");
                return;
            }

            foreach (var path in files)
            {
                var len = new FileInfo(path).Length;
                var mb = Math.Round(len / (1024.0 * 1024.0), 2);
                var rel = path.Replace(aa, "").TrimStart('\\', '/');
                if (len < 8_000_000)
                {
                    UltrakillLog.Warn(Area, "Tutorial bundle small (" + mb + " MB) at " + rel
                        + " — retail ~8+ MB; legacy rebuild ~50 MB. Small bundle = empty StaticSceneData on device.");
                }
                else
                {
                    UltrakillLog.Info(Area, "Tutorial bundle " + mb + " MB at " + rel);
                }
            }
        }

        private static void ProbeOneBundle(string aa)
        {
            var bundle = Directory.GetFiles(aa, "*.bundle", SearchOption.AllDirectories)
                .OrderBy(b => new FileInfo(b).Length)
                .FirstOrDefault();
            if (bundle == null)
            {
                UltrakillLog.Warn(Area, "No .bundle files found to probe.");
                return;
            }

            UltrakillLog.Info(Area, "Probing AssetBundle.LoadFromFile: " + bundle);
            var ab = AssetBundle.LoadFromFile(bundle);
            if (ab == null)
            {
                UltrakillLog.Error(Area, "Probe bundle failed to load (see Unity error above for the reason).");
                return;
            }

            UltrakillLog.Info(Area, "Probe bundle loaded OK, assets=" + ab.GetAllAssetNames().Length);
            ab.Unload(true);
        }

        private static void SetStatus(string status)
        {
            Status = status;
            UltrakillLog.Info(Area, status);
        }

        private static void Fail(string message)
        {
            Status = "FAILED: " + message.Split('\n')[0];
            UltrakillLog.Error(Area, message);
        }
    }
}
#endif
