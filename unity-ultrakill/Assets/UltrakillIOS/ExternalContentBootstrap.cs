using System;
using System.IO;
using System.Linq;
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
                UltrakillLog.Error("Content", "External content bootstrap failed: " + ex);
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
            UltrakillLog.Info("Content", "Documents: " + documents);
            UltrakillLog.Info("Content", "Bundled streamingAssetsPath: " + Application.streamingAssetsPath);

            if (!Directory.Exists(dataPath))
            {
                UltrakillLog.Warn(
                    "Content",
                    "Missing " + dataPath + " — unpack game data into Documents/" + ExpectedDocumentsPath
                    + " (Files → On My iPhone → ULTRAKILL).");
                LogDirectory("Documents", documents);
                LogDirectory(ContentFolderName, Path.Combine(documents, ContentFolderName));
                return;
            }

            BindPaths(dataPath);
        }

        private static void BindPaths(string dataPath)
        {
            ContentDataPath = dataPath;
            ContentStreamingAssetsPath = Path.Combine(dataPath, "StreamingAssets");
            IsReady = true;
            UltrakillLog.Info("Content", "External data ready at " + dataPath);
            LogDirectory(DataFolderName, dataPath);
            if (!Directory.Exists(ContentStreamingAssetsPath))
            {
                UltrakillLog.Warn("Content", "No StreamingAssets folder at " + ContentStreamingAssetsPath);
            }
#if ULTRAKILL_FULL_PORT
            RetailGameHooks.OnExternalDataBound();
#endif
        }

        private static void LogDirectory(string label, string path)
        {
            if (!Directory.Exists(path))
            {
                UltrakillLog.Info("Content", label + " does not exist: " + path);
                return;
            }

            var entries = Directory.GetFileSystemEntries(path).Select(e => Path.GetFileName(e)).Take(40).ToArray();
            UltrakillLog.Info("Content", label + " contains " + entries.Length + " entries: " + string.Join(", ", entries));
        }

        private static void HookAddressablesPathRemap()
        {
            if (!IsReady)
            {
                return;
            }

#if ULTRAKILL_FULL_PORT
            AddressablesContentRemap.Install(ContentStreamingAssetsPath, Application.streamingAssetsPath);
#else
            UltrakillLog.Info("Content", "Engine-only build: Addressables remap skipped (ULTRAKILL_FULL_PORT not defined).");
#endif
        }
    }
}
