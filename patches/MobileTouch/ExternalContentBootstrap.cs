using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Loads ULTRAKILL_Data from Documents/ULTRAKILL-Content.zip (sidestep huge IPA).
    /// </summary>
    public static class ExternalContentBootstrap
    {
        public const string ZipFileName = "ULTRAKILL-Content.zip";
        public const string DataFolderName = "ULTRAKILL_Data";

        public static string ContentDataPath { get; private set; }
        public static string ContentStreamingAssetsPath { get; private set; }
        public static bool IsReady { get; private set; }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void UltrakillGetDocumentsPath(IntPtr buffer, int size);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                InstallFromDocumentsZip();
                HookAddressablesPathRemap();
            }
            catch (Exception ex)
            {
                Debug.LogError("[UltrakillIOS] External content bootstrap failed: " + ex);
            }
#endif
        }

        public static string GetDocumentsPath()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var ptr = Marshal.AllocHGlobal(1024);
            try
            {
                UltrakillGetDocumentsPath(ptr, 1024);
                return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
#else
            return Application.persistentDataPath;
#endif
        }

        private static void InstallFromDocumentsZip()
        {
            var documents = GetDocumentsPath();
            var zipPath = Path.Combine(documents, ZipFileName);
            var cacheRoot = Path.Combine(Application.persistentDataPath, "ultrakill-content");
            var dataPath = Path.Combine(cacheRoot, DataFolderName);
            var markerPath = Path.Combine(cacheRoot, "installed.sha256");

            if (!File.Exists(zipPath))
            {
                Debug.LogWarning("[UltrakillIOS] Missing " + zipPath + " — copy " + ZipFileName + " into the app Documents folder (Files → On My iPhone → ULTRAKILL).");
                return;
            }

            var zipHash = Sha256File(zipPath);
            if (File.Exists(markerPath) && File.ReadAllText(markerPath) == zipHash && Directory.Exists(dataPath))
            {
                BindPaths(dataPath);
                return;
            }

            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, true);
            }

            Directory.CreateDirectory(cacheRoot);
            Debug.Log("[UltrakillIOS] Extracting " + zipPath + " …");
            ZipFile.ExtractToDirectory(zipPath, cacheRoot);
            File.WriteAllText(markerPath, zipHash);
            BindPaths(dataPath);
        }

        private static void BindPaths(string dataPath)
        {
            if (!Directory.Exists(dataPath))
            {
                Debug.LogError("[UltrakillIOS] Extracted zip did not contain " + DataFolderName);
                return;
            }

            ContentDataPath = dataPath;
            ContentStreamingAssetsPath = Path.Combine(dataPath, "StreamingAssets");
            IsReady = true;
            Debug.Log("[UltrakillIOS] External data ready at " + dataPath);
        }

        private static void HookAddressablesPathRemap()
        {
            if (!IsReady)
            {
                return;
            }

            var addrType = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
            if (addrType == null)
            {
                return;
            }

            var prop = addrType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.Static);
            var rm = prop?.GetValue(null);
            if (rm == null)
            {
                return;
            }

#if ULTRAKILL_FULL_PORT
            AddressablesContentRemap.Install(ContentStreamingAssetsPath, Application.streamingAssetsPath);
#else
            Debug.Log("[UltrakillIOS] Engine-only build: Addressables remap skipped (use unity-ultrakill + ULTRAKILL_FULL_PORT).");
#endif
        }

        private static string Sha256File(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
