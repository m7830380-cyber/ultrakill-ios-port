#if ULTRAKILL_FULL_PORT
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Bridges external Documents data into retail Assembly-CSharp boot (GameBuildSettings, etc.).
    /// </summary>
    internal static class RetailGameHooks
    {
        public static void OnExternalDataBound()
        {
#if UNITY_IOS && !UNITY_EDITOR
            PreloadGameBuildSettings();
#endif
        }

        private static void PreloadGameBuildSettings()
        {
            try
            {
                var streaming = ExternalContentBootstrap.ContentStreamingAssetsPath;
                if (string.IsNullOrEmpty(streaming))
                {
                    return;
                }

                var path = Path.Combine(streaming, "GameBuildSettings.json");
                if (!File.Exists(path))
                {
                    UltrakillLog.Info("Hooks", "No GameBuildSettings.json in external StreamingAssets; using game defaults.");
                    return;
                }

                var gameSettingsType = Type.GetType("GameBuildSettings, Assembly-CSharp");
                if (gameSettingsType == null)
                {
                    UltrakillLog.Warn("Hooks", "GameBuildSettings type not found in Assembly-CSharp.");
                    return;
                }

                var instance = JsonUtility.FromJson(File.ReadAllText(path), gameSettingsType);
                var field = gameSettingsType.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
                field?.SetValue(null, instance);
                UltrakillLog.Info("Hooks", "GameBuildSettings loaded from " + path);
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn("Hooks", "GameBuildSettings preload failed: " + ex.Message);
            }
        }
    }
}
#endif
