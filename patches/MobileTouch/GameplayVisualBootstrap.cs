#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Tutorial black screen: many cameras exist but a "Virtual Camera" (higher depth) clears
    /// solid black over the world. Spam: TimeController/AudioMixerController with null mixers.
    /// </summary>
    internal sealed class GameplayVisualBootstrap : MonoBehaviour
    {
        private const string Area = "Visual";

        private static readonly string[] AlwaysMute =
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
            "TimeController",
            "AudioMixerController",
            "MusicManager",
            "OptionsMenuToManager",
            "StainVoxelManager",
            "BloodsplatterManager",
            "BloodstainParent",
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
            FixCameras(forceLog: true);
            if (Time.timeScale <= 0f)
            {
                Time.timeScale = 1f;
            }
        }

        private void LateUpdate()
        {
            if (Time.frameCount % 15 == 0)
            {
                MuteSpam();
                FixCameras(forceLog: Time.frameCount % 60 == 0);
            }
        }

        private static void MuteSpam()
        {
            var n = 0;
            foreach (var name in AlwaysMute)
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

            // Portal namespace
            try
            {
                var asm = Type.GetType("SceneHelper, Assembly-CSharp")?.Assembly;
                if (asm != null)
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t == null || t.Namespace != "ULTRAKILL.Portal" || !typeof(Behaviour).IsAssignableFrom(t))
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
                }
            }
            catch
            {
                /* ignore */
            }

            if (n > 0)
            {
                UltrakillLog.Info(Area, "Muted " + n + " spam/broken behaviours");
            }
        }

        private static void FixCameras(bool forceLog)
        {
            var cams = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            Camera main = null;
            foreach (var cam in cams)
            {
                if (cam == null)
                {
                    continue;
                }

                var n = cam.gameObject.name;
                var isVirtual = n.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("UI Camera", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Overlay", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isVirtual)
                {
                    // These were covering the world with solid black at higher depth.
                    cam.enabled = false;
                    continue;
                }

                cam.enabled = true;
                if (cam.clearFlags == CameraClearFlags.Nothing
                    || cam.clearFlags == CameraClearFlags.Depth
                    || cam.clearFlags == CameraClearFlags.SolidColor
                       && cam.backgroundColor.maxColorComponent < 0.05f)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.45f, 0.45f, 0.5f, 1f);
                }

                if (cam.CompareTag("MainCamera") || n.IndexOf("Main Camera", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    main = cam;
                    cam.depth = 50f;
                    cam.gameObject.tag = "MainCamera";
                }
                else if (main == null)
                {
                    main = cam;
                }
            }

            if (main != null)
            {
                main.enabled = true;
                main.depth = 50f;
                if (main.GetComponent<AudioListener>() == null
                    && UnityEngine.Object.FindObjectOfType<AudioListener>() == null)
                {
                    main.gameObject.AddComponent<AudioListener>();
                }
            }
            else if (cams.Length == 0)
            {
                var go = new GameObject("UltrakillIOS.EmergencyCamera");
                var cam = go.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.5f, 0.2f, 0.2f, 1f);
                cam.depth = 100;
                go.AddComponent<AudioListener>();
                DontDestroyOnLoad(go);
                UltrakillLog.Warn(Area, "No cameras — spawned emergency camera");
                main = cam;
            }

            // CameraController.activated
            try
            {
                var ccType = Type.GetType("CameraController, Assembly-CSharp");
                var cc = ccType != null ? UnityEngine.Object.FindObjectOfType(ccType) as MonoBehaviour : null;
                if (cc != null)
                {
                    cc.enabled = true;
                    ccType.GetField("activated", BindingFlags.Instance | BindingFlags.Public)?.SetValue(cc, true);
                    var camField = ccType.GetField("cam", BindingFlags.Instance | BindingFlags.Public);
                    if (camField != null && main != null)
                    {
                        camField.SetValue(cc, main);
                        if (main.transform != cc.transform && main.transform.IsChildOf(cc.transform.parent ?? cc.transform))
                        {
                            /* keep hierarchy */
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "CameraController wire failed: " + ex.Message);
            }

            var lightsOn = 0;
            foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>(true))
            {
                if (light != null && !light.enabled)
                {
                    light.enabled = true;
                    lightsOn++;
                }
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f, 1f);
            RenderSettings.ambientIntensity = 1f;

            // Brighten remapped materials that ended up near-black.
            var matsFixed = 0;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                if (r == null || !r.enabled)
                {
                    continue;
                }

                var mats = r.sharedMaterials;
                if (mats == null)
                {
                    continue;
                }

                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null)
                    {
                        continue;
                    }

                    if (m.HasProperty("_Color"))
                    {
                        var c = m.GetColor("_Color");
                        if (c.maxColorComponent < 0.08f)
                        {
                            m.SetColor("_Color", Color.white);
                            matsFixed++;
                        }
                    }

                    if (m.HasProperty("_Colorize"))
                    {
                        var c = m.GetColor("_Colorize");
                        if (c.maxColorComponent < 0.08f)
                        {
                            m.SetColor("_Colorize", Color.white);
                        }
                    }
                }
            }

            if (forceLog)
            {
                var enabledCount = 0;
                foreach (var c in cams)
                {
                    if (c != null && c.enabled)
                    {
                        enabledCount++;
                    }
                }

                UltrakillLog.Info(Area, "Cameras enabled=" + enabledCount
                    + " main=" + (main != null ? main.name : "null")
                    + " lightsReenabled=" + lightsOn
                    + " matsBrightened=" + matsFixed
                    + " ambient=" + RenderSettings.ambientLight);
            }
        }
    }
}
#endif
