using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Uses ULTRAKILL_Data from Documents/ULTRAKILL-Content (you unpack the content archive yourself).
    /// </summary>
    public static class ExternalContentBootstrap
    {
        public const string ContentFolderName = "ULTRAKILL-Content";
        public const string DataFolderName = "ULTRAKILL_Data";

        public static string ContentDataPath { get; private set; }
        public static string ContentStreamingAssetsPath { get; private set; }
        public static bool IsReady { get; private set; }

        public static string ExpectedDocumentsPath =>
            ContentFolderName + "/" + DataFolderName;

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
                BindFromDocumentsFolder();
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

        private static void BindFromDocumentsFolder()
        {
            var documents = GetDocumentsPath();
            var dataPath = Path.Combine(documents, ContentFolderName, DataFolderName);

            if (!Directory.Exists(dataPath))
            {
                Debug.LogWarning(
                    "[UltrakillIOS] Missing " + dataPath
                    + " — unpack game data into Documents/" + ExpectedDocumentsPath
                    + " (Files → On My iPhone → ULTRAKILL).");
                return;
            }

            BindPaths(dataPath);
        }

        private static void BindPaths(string dataPath)
        {
            if (!Directory.Exists(dataPath))
            {
                Debug.LogError("[UltrakillIOS] Data folder not found: " + dataPath);
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
    }
}
