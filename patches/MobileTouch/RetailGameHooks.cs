#if ULTRAKILL_FULL_PORT
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
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
                    Debug.Log("[UltrakillIOS] No GameBuildSettings.json in external StreamingAssets; using game defaults.");
                    return;
                }

                var gameSettingsType = Type.GetType("GameBuildSettings, Assembly-CSharp");
                if (gameSettingsType == null)
                {
                    Debug.LogWarning("[UltrakillIOS] GameBuildSettings type not found.");
                    return;
                }

                var instance = JsonConvert.DeserializeObject(File.ReadAllText(path), gameSettingsType);
                var field = gameSettingsType.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
                field?.SetValue(null, instance);
                Debug.Log("[UltrakillIOS] GameBuildSettings loaded from " + path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UltrakillIOS] GameBuildSettings preload failed: " + ex.Message);
            }
        }
    }
}
#endif
