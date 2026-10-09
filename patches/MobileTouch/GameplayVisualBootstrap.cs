#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// After Tutorial loads: CameraController.Start NREs (null Prefs/mixers) leaves a black screen.
    /// Force-enable cameras, unstick time, and mute per-frame spam that fills the log.
    /// </summary>
    internal sealed class GameplayVisualBootstrap : MonoBehaviour
    {
        private const string Area = "Visual";

        private static readonly string[] SpamMute =
        {
            "AnimatedTexture",
            "GunControl",
            "MenuEsc",
            "StaticSceneOptimizer",
            "Flicker",
            "ZombieMelee",
            "ElectricityLine",
            "CheatsManager",
            "LevelNamePopup",
            "GroundCheck",
            "ColorBlindActivator",
            "ColorBlindSetter",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindObjectOfType<GameplayVisualBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("UltrakillIOS.Visual");
            DontDestroyOnLoad(go);
            go.AddComponent<GameplayVisualBootstrap>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            MuteSpam();
            FixCameras();
            EnsureAudioListener();
            if (Time.timeScale <= 0f)
            {
                Time.timeScale = 1f;
            }
        }

        private void LateUpdate()
        {
            // Keep camera alive if something disables it mid-frame.
            if (Time.frameCount % 30 == 0)
            {
                FixCameras();
            }
        }

        private static void MuteSpam()
        {
            var n = 0;
            foreach (var name in SpamMute)
            {
                var t = Type.GetType(name + ", Assembly-CSharp");
                if (t == null || !typeof(Behaviour).IsAssignableFrom(t))
                {
                    continue;
                }

                foreach (var obj in UnityEngine.Object.FindObjectsOfType(t, true))
                {
                    if (obj is Behaviour b && b.enabled)
                    {
                        b.enabled = false;
                        n++;
                    }
                }
            }

            if (n > 0)
            {
                UltrakillLog.Info(Area, "Muted " + n + " per-frame spam behaviours");
            }
        }

        private static void FixCameras()
        {
            var cams = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            var enabled = 0;
            Camera best = null;
            foreach (var cam in cams)
            {
                if (cam == null)
                {
                    continue;
                }

                // Skip UI overlay cameras that only draw UI layer if we have a world camera.
                cam.enabled = true;
                if (cam.clearFlags == CameraClearFlags.Nothing || cam.clearFlags == CameraClearFlags.Depth)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 1f);
                }

                enabled++;
                if (best == null || cam.depth > best.depth)
                {
                    best = cam;
                }
            }

            // CameraController.activated stuck false after failed Start
            try
            {
                var ccType = Type.GetType("CameraController, Assembly-CSharp");
                if (ccType != null)
                {
                    var cc = UnityEngine.Object.FindObjectOfType(ccType) as MonoBehaviour;
                    if (cc != null)
                    {
                        cc.enabled = true;
                        var activated = ccType.GetField("activated", BindingFlags.Instance | BindingFlags.Public);
                        activated?.SetValue(cc, true);
                        var camField = ccType.GetField("cam", BindingFlags.Instance | BindingFlags.Public);
                        if (camField != null && camField.GetValue(cc) == null && best != null)
                        {
                            camField.SetValue(cc, best);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "CameraController fix failed: " + ex.Message);
            }

            if (enabled == 0)
            {
                // Absolute fallback — create a camera so we aren't stuck on black.
                var go = new GameObject("UltrakillIOS.EmergencyCamera");
                var cam = go.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.05f, 0.05f, 1f);
                cam.depth = 100;
                go.AddComponent<AudioListener>();
                DontDestroyOnLoad(go);
                UltrakillLog.Warn(Area, "No cameras in scene — spawned emergency camera");
            }
            else if (Time.frameCount < 5 || Time.frameCount % 60 == 0)
            {
                UltrakillLog.Info(Area, "Cameras enabled=" + enabled
                    + " main=" + (Camera.main != null ? Camera.main.name : "null")
                    + " best=" + (best != null ? best.name : "null"));
            }

            // Lights often off after broken Start chains
            var lights = UnityEngine.Object.FindObjectsOfType<Light>(true);
            var lit = 0;
            foreach (var light in lights)
            {
                if (light != null && !light.enabled)
                {
                    light.enabled = true;
                    lit++;
                }
            }

            if (lit > 0)
            {
                UltrakillLog.Info(Area, "Re-enabled " + lit + " lights");
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            if (RenderSettings.ambientLight.maxColorComponent < 0.05f)
            {
                RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.4f, 1f);
            }
        }

        private static void EnsureAudioListener()
        {
            if (UnityEngine.Object.FindObjectOfType<AudioListener>() != null)
            {
                return;
            }

            var cam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
        }
    }
}
#endif
