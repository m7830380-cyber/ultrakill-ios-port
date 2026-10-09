#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Tutorial shaft black: many secondary cameras stay enabled and clear/cover the world.
    /// Keep only Main Camera, solid clear, no occlusion; force bright unlit world mats.
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

        private static bool _loggedCamNames;

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
            _loggedCamNames = false;
            MuteSpam();
            FixCameras(forceLog: true);
            if (Time.timeScale <= 0f)
            {
                Time.timeScale = 1f;
            }
        }

        private void LateUpdate()
        {
            // Every frame while in gameplay — secondary cams re-enable themselves.
            var inMenu = false;
            try
            {
                var sh = Type.GetType("SceneHelper, Assembly-CSharp");
                var inst = sh?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var cur = inst != null
                    ? sh.GetProperty("CurrentScene", BindingFlags.Public | BindingFlags.Instance)?.GetValue(inst) as string
                    : null;
                inMenu = string.IsNullOrEmpty(cur)
                    || cur.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0
                    || cur.IndexOf("b3e7f2f8", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                /* ignore */
            }

            if (!inMenu)
            {
                FixCameras(forceLog: Time.frameCount % 60 == 0);
            }
            else if (Time.frameCount % 15 == 0)
            {
                MuteSpam();
                FixCameras(forceLog: Time.frameCount % 60 == 0);
            }

            if (Time.frameCount % 15 == 0)
            {
                MuteSpam();
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
                if (cam.CompareTag("MainCamera")
                    || n.Equals("Main Camera", StringComparison.OrdinalIgnoreCase)
                    || n.IndexOf("Main Camera", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    main = cam;
                    break;
                }
            }

            if (main == null)
            {
                foreach (var cam in cams)
                {
                    if (cam != null && cam.gameObject.name.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        main = cam;
                        break;
                    }
                }
            }

            var disabled = 0;
            foreach (var cam in cams)
            {
                if (cam == null)
                {
                    continue;
                }

                if (main != null && cam == main)
                {
                    continue;
                }

                // Kill every secondary camera — they clear black over the Tutorial shaft.
                if (cam.enabled)
                {
                    cam.enabled = false;
                    disabled++;
                }
            }

            if (main != null)
            {
                main.enabled = true;
                main.gameObject.tag = "MainCamera";
                main.depth = 100f;
                main.cullingMask = ~0;
                main.useOcclusionCulling = false;
                main.clearFlags = CameraClearFlags.SolidColor;
                main.backgroundColor = new Color(0.62f, 0.62f, 0.68f, 1f);
                main.allowHDR = false;
                main.allowMSAA = false;
                if (main.nearClipPlane > 0.05f)
                {
                    main.nearClipPlane = 0.05f;
                }

                if (main.farClipPlane < 500f)
                {
                    main.farClipPlane = 2000f;
                }

                if (main.GetComponent<AudioListener>() == null
                    && UnityEngine.Object.FindObjectOfType<AudioListener>() == null)
                {
                    main.gameObject.AddComponent<AudioListener>();
                }

                if (main.GetComponent<Light>() == null)
                {
                    var flash = main.gameObject.AddComponent<Light>();
                    flash.type = LightType.Point;
                    flash.range = 80f;
                    flash.intensity = 4f;
                    flash.color = Color.white;
                    flash.shadows = LightShadows.None;
                    UltrakillLog.Info(Area, "Attached camera flashlight");
                }
            }
            else if (cams.Length == 0)
            {
                var go = new GameObject("UltrakillIOS.EmergencyCamera");
                main = go.AddComponent<Camera>();
                main.tag = "MainCamera";
                main.clearFlags = CameraClearFlags.SolidColor;
                main.backgroundColor = new Color(0.5f, 0.2f, 0.2f, 1f);
                main.depth = 100;
                main.cullingMask = ~0;
                main.useOcclusionCulling = false;
                go.AddComponent<AudioListener>();
                DontDestroyOnLoad(go);
                UltrakillLog.Warn(Area, "No cameras — spawned emergency camera");
            }

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

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.ambientIntensity = 1.5f;
            RenderSettings.skybox = null;

            // Material force only on scene load / periodic log — avoid .materials instance leak.
            var matsFixed = 0;
            var renderersOn = 0;
            if (forceLog)
            {
                var fallback = Shader.Find("UltrakillIOS/UnlitTexture") ?? Shader.Find("Unlit/Color");
                if (fallback != null)
                {
                    foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
                    {
                        if (r == null)
                        {
                            continue;
                        }

                        if (!r.enabled)
                        {
                            r.enabled = true;
                            renderersOn++;
                        }

                        var mats = r.sharedMaterials;
                        if (mats == null)
                        {
                            continue;
                        }

                        var changed = false;
                        for (var i = 0; i < mats.Length; i++)
                        {
                            var m = mats[i];
                            if (m == null)
                            {
                                continue;
                            }

                            var sn = m.shader != null ? m.shader.name : "";
                            if (m.shader != fallback
                                && !sn.StartsWith("UI/", StringComparison.Ordinal)
                                && !sn.StartsWith("TextMeshPro/", StringComparison.Ordinal)
                                && !sn.StartsWith("Sprites/", StringComparison.Ordinal)
                                && !sn.StartsWith("UltrakillIOS/", StringComparison.Ordinal))
                            {
                                m.shader = fallback;
                                matsFixed++;
                                changed = true;
                            }

                            if (m.HasProperty("_Color"))
                            {
                                m.SetColor("_Color", Color.white);
                            }

                            if (m.HasProperty("_Colorize"))
                            {
                                m.SetColor("_Colorize", Color.white);
                            }
                        }

                        if (changed)
                        {
                            r.sharedMaterials = mats;
                        }
                    }
                }
            }

            if (forceLog)
            {
                var enabledCount = 0;
                var sb = new StringBuilder();
                foreach (var c in cams)
                {
                    if (c == null)
                    {
                        continue;
                    }

                    if (c.enabled)
                    {
                        enabledCount++;
                    }

                    if (!_loggedCamNames)
                    {
                        sb.Append(c.enabled ? '+' : '-');
                        sb.Append(c.name);
                        sb.Append("(d=").Append(c.depth.ToString("0.#"));
                        sb.Append(",cf=").Append(c.clearFlags).Append(") ");
                    }
                }

                UltrakillLog.Info(Area, "Cameras enabled=" + enabledCount
                    + " disabledOthers=" + disabled
                    + " main=" + (main != null ? main.name : "null")
                    + " lightsReenabled=" + lightsOn
                    + " matsForced=" + matsFixed
                    + " renderersOn=" + renderersOn
                    + " ambient=" + RenderSettings.ambientLight);

                if (!_loggedCamNames && sb.Length > 0)
                {
                    UltrakillLog.Info(Area, "CamList " + sb);
                    _loggedCamNames = true;
                }
            }
        }
    }
}
#endif
