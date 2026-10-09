#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UltrakillIOS
{
    /// <summary>
    /// Black-screen root causes (session 135511):
    /// 1) We set RenderSettings.skybox=null while SkyboxEnabler restores CameraClearFlags.Skybox
    ///    → null skybox clear = pure black (worse than before).
    /// 2) We force-enabled 207 disabled Renderers (intro/fade planes covering the view).
    /// 3) SolidColor gray was overwritten before render; need OnPreCull.
    /// Keep Main Camera + HUD Camera only; cyan diagnostic clear; kill opaque fullscreen UI.
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
            "ColorBlindActivator",
            "TimeController",
            "AudioMixerController",
            "MusicManager",
            "OptionsMenuToManager",
            "StainVoxelManager",
            "BloodsplatterManager",
            "BloodstainParent",
            "SkyboxEnabler",
            "IntroTextController",
            "IntroViolenceScreen",
            "LevelStatsEnabler",
            "LevelStats",
            "DifficultyTitle",
            "StyleHUD",
        };

        // Soft sky — cyan was only for proving clear worked.
        private static readonly Color DiagnosticClear = new Color(0.55f, 0.62f, 0.72f, 1f);

        private static Camera _main;
        private static bool _loggedCamNames;
        private static bool _hookedPreCull;
        private static Material _brightSky;

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
            EnsurePreCullHook();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_hookedPreCull)
            {
                Camera.onPreCull -= OnAnyCameraPreCull;
                _hookedPreCull = false;
            }
        }

        private static void EnsurePreCullHook()
        {
            if (_hookedPreCull)
            {
                return;
            }

            Camera.onPreCull += OnAnyCameraPreCull;
            _hookedPreCull = true;
        }

        /// <summary>Do NOT fight clear colour every frame — that was the colour slideshow.</summary>
        private static void OnAnyCameraPreCull(Camera cam)
        {
            if (cam == null)
            {
                return;
            }

            var n = cam.gameObject.name;
            if (n.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Shop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cam.enabled = false;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _loggedCamNames = false;
            MuteSpam();
            HideSpuriousScoreHud();
            FixCameras(forceLog: true);
            if (Time.timeScale <= 0f)
            {
                Time.timeScale = 1f;
            }

            TryEndStuckIntro();
        }

        private void LateUpdate()
        {
            // Rare maintenance only — never recolour materials/clear every frame.
            if (Time.frameCount % 120 == 0)
            {
                MuteSpam();
                HideSpuriousScoreHud();
            }
        }

        /// <summary>
        /// Debug builds auto-open LevelStats (looks like end-of-level scoreboard) with
        /// GetMissionName→"Main Menu" + DifficultyTitle→"STANDARD". Hide until real rank.
        /// </summary>
        private static void HideSpuriousScoreHud()
        {
            PlayerPrefs.SetInt("LevStaOpe", 0);

            var infoSent = false;
            try
            {
                var smType = Type.GetType("StatsManager, Assembly-CSharp");
                var sm = smType != null ? UnityEngine.Object.FindObjectOfType(smType) : null;
                if (sm != null)
                {
                    var f = smType.GetField("infoSent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null && f.FieldType == typeof(bool))
                    {
                        infoSent = (bool)f.GetValue(sm);
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            HideTypeObjects("LevelStatsEnabler, Assembly-CSharp", disableBehaviour: true, deactivateGo: true);
            HideTypeObjects("LevelStats, Assembly-CSharp", disableBehaviour: true, deactivateGo: true);

            // StyleHUD = killstreak / style meter (DOUBLE KILL etc). Hide meter children.
            var styleType = Type.GetType("StyleHUD, Assembly-CSharp");
            if (styleType != null)
            {
                foreach (var obj in UnityEngine.Object.FindObjectsOfType(styleType, true))
                {
                    if (obj is not MonoBehaviour mb)
                    {
                        continue;
                    }

                    styleType.GetField("forceMeterOn", BindingFlags.Instance | BindingFlags.Public)
                        ?.SetValue(mb, false);
                    mb.enabled = false;
                    for (var i = 0; i < mb.transform.childCount; i++)
                    {
                        mb.transform.GetChild(i).gameObject.SetActive(false);
                    }
                }
            }

            // FinalRank only valid after StatsManager.SendInfo (level complete).
            if (!infoSent)
            {
                HideTypeObjects("FinalRank, Assembly-CSharp", disableBehaviour: false, deactivateGo: true);
            }
        }

        private static void HideTypeObjects(string typeName, bool disableBehaviour, bool deactivateGo)
        {
            var t = Type.GetType(typeName);
            if (t == null)
            {
                return;
            }

            foreach (var obj in UnityEngine.Object.FindObjectsOfType(t, true))
            {
                if (obj == null)
                {
                    continue;
                }

                if (disableBehaviour && obj is Behaviour b && b.enabled)
                {
                    b.enabled = false;
                }

                if (deactivateGo && obj is Component c && c.gameObject.activeSelf)
                {
                    c.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Purple UI panels = missing/error shaders or UltrakillIOS/Unlit wrongly on Graphics.</summary>
        private static void FixPurpleUi()
        {
            var uiShader = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
            var fixedN = 0;
            foreach (var g in UnityEngine.Object.FindObjectsOfType<MaskableGraphic>(true))
            {
                if (g == null || g is Text || g is TMPro.TMP_Text)
                {
                    continue;
                }

                var sh = g.material != null ? g.material.shader : null;
                var sn = sh != null ? sh.name : "";
                var bad = sh == null
                    || !sh.isSupported
                    || sn.Contains("InternalError")
                    || sn.StartsWith("UltrakillIOS/", StringComparison.Ordinal)
                    || sn.StartsWith("Hidden/", StringComparison.Ordinal);
                if (bad && uiShader != null)
                {
                    g.material = null; // Graphic default UI material
                    if (g.material != null && g.material.shader != uiShader)
                    {
                        g.material = new Material(uiShader);
                    }

                    fixedN++;
                }

                // Classic missing-sprite magenta → dark translucent panel.
                var c = g.color;
                if (c.a > 0.2f && c.r > 0.85f && c.g < 0.25f && c.b > 0.85f)
                {
                    g.color = new Color(0.12f, 0.12f, 0.14f, Mathf.Min(c.a, 0.85f));
                    fixedN++;
                }
            }

            if (fixedN > 0)
            {
                UltrakillLog.Info(Area, "Fixed " + fixedN + " purple/broken UI graphics");
            }
        }

        private static void ForceBrightSky()
        {
            // Do NOT leave skybox null — SkyboxEnabler / portals flip clearFlags back to Skybox.
            if (_brightSky == null)
            {
                var sh = Shader.Find("Skybox/Procedural")
                    ?? Shader.Find("Skybox/Cubemap")
                    ?? Shader.Find("Unlit/Color")
                    ?? Shader.Find("UltrakillIOS/UnlitTexture");
                if (sh != null)
                {
                    _brightSky = new Material(sh);
                    if (_brightSky.HasProperty("_SkyTint"))
                    {
                        _brightSky.SetColor("_SkyTint", new Color(0.7f, 0.85f, 1f));
                    }

                    if (_brightSky.HasProperty("_GroundColor"))
                    {
                        _brightSky.SetColor("_GroundColor", new Color(0.4f, 0.4f, 0.45f));
                    }

                    if (_brightSky.HasProperty("_Exposure"))
                    {
                        _brightSky.SetFloat("_Exposure", 1.3f);
                    }

                    if (_brightSky.HasProperty("_Color"))
                    {
                        _brightSky.SetColor("_Color", new Color(0.6f, 0.8f, 1f));
                    }
                }
            }

            if (_brightSky != null)
            {
                RenderSettings.skybox = _brightSky;
            }

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.75f, 0.75f, 0.8f, 1f);
            RenderSettings.ambientIntensity = 1.2f;
            // Do not null skybox every call — leave whatever Playable set.
        }

        private static void TryEndStuckIntro()
        {
            try
            {
                var omType = Type.GetType("OptionsManager, Assembly-CSharp");
                var om = omType != null ? UnityEngine.Object.FindObjectOfType(omType) : null;
                if (om != null)
                {
                    var f = omType.GetField("inIntro", BindingFlags.Instance | BindingFlags.Public);
                    if (f != null && f.FieldType == typeof(bool))
                    {
                        f.SetValue(om, false);
                    }
                }
            }
            catch
            {
                /* ignore */
            }
        }

        private static void StripBlackUiOverlays()
        {
            var stripped = 0;
            foreach (var img in UnityEngine.Object.FindObjectsOfType<Image>(true))
            {
                if (img == null || !img.isActiveAndEnabled)
                {
                    continue;
                }

                var rt = img.rectTransform;
                if (rt == null)
                {
                    continue;
                }

                var rect = rt.rect;
                var covers = rect.width >= Screen.width * 0.7f && rect.height >= Screen.height * 0.7f;
                var name = img.gameObject.name;
                var namedFade = name.IndexOf("fade", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("blocker", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("splash", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("black", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("intro", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("violence", StringComparison.OrdinalIgnoreCase) >= 0;

                var dark = img.color.a > 0.6f && img.color.maxColorComponent < 0.25f;
                var opaque = img.color.a > 0.85f;

                if ((covers && (dark || opaque)) || (namedFade && img.color.a > 0.3f))
                {
                    var c = img.color;
                    c.a = 0f;
                    img.color = c;
                    img.enabled = false;
                    stripped++;
                }
            }

            // Also CanvasGroup full-screen fades
            foreach (var cg in UnityEngine.Object.FindObjectsOfType<CanvasGroup>(true))
            {
                if (cg == null || !cg.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var n = cg.gameObject.name;
                if (cg.alpha > 0.5f
                    && (n.IndexOf("fade", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("intro", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("blocker", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("splash", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    cg.alpha = 0f;
                    cg.blocksRaycasts = false;
                    stripped++;
                }
            }

            if (stripped > 0)
            {
                UltrakillLog.Info(Area, "Stripped " + stripped + " black/fullscreen UI overlays");
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
            ForceBrightSky();
            EnsurePreCullHook();

            var cams = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            Camera main = null;
            Camera hud = null;

            foreach (var cam in cams)
            {
                if (cam == null)
                {
                    continue;
                }

                var n = cam.gameObject.name;
                if (cam.CompareTag("MainCamera")
                    || n.IndexOf("Main Camera", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    main = cam;
                }
                else if (n.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hud = cam;
                }
            }

            if (main == null)
            {
                foreach (var cam in cams)
                {
                    if (cam != null
                        && cam.gameObject.name.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) < 0
                        && cam.gameObject.name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) < 0
                        && cam.gameObject.name.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        main = cam;
                        break;
                    }
                }
            }

            _main = main;

            var disabled = 0;
            foreach (var cam in cams)
            {
                if (cam == null)
                {
                    continue;
                }

                if (cam == main || cam == hud)
                {
                    continue;
                }

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
                main.depth = 0f;
                main.cullingMask = ~0;
                main.useOcclusionCulling = false;
                main.clearFlags = CameraClearFlags.SolidColor;
                main.backgroundColor = DiagnosticClear;
                main.allowHDR = false;
                main.targetTexture = null;
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
                }
            }

            if (hud != null)
            {
                hud.enabled = true;
                hud.depth = 10f;
                hud.clearFlags = CameraClearFlags.Depth;
                hud.useOcclusionCulling = false;
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

                    var hudField = ccType.GetField("hudCamera", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (hudField != null && hud != null)
                    {
                        hudField.SetValue(cc, hud);
                    }
                }
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "CameraController wire failed: " + ex.Message);
            }

            // Remap mats only periodically — NEVER re-enable disabled renderers (that covered the view).
            var matsFixed = 0;
            if (forceLog)
            {
                var fallback = Shader.Find("UltrakillIOS/UnlitTexture") ?? Shader.Find("Unlit/Color");
                if (fallback != null)
                {
                    foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(false))
                    {
                        if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                        {
                            continue;
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
                            if (!sn.StartsWith("UltrakillIOS/", StringComparison.Ordinal)
                                && !sn.StartsWith("UI/", StringComparison.Ordinal)
                                && !sn.StartsWith("TextMeshPro/", StringComparison.Ordinal)
                                && !sn.StartsWith("Sprites/", StringComparison.Ordinal)
                                && !sn.StartsWith("Skybox/", StringComparison.Ordinal))
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
                        sb.Append(",cf=").Append(c.clearFlags);
                        sb.Append(",bg=").Append(ColorUtility.ToHtmlStringRGB(c.backgroundColor));
                        sb.Append(") ");
                    }
                }

                UltrakillLog.Info(Area, "Cameras enabled=" + enabledCount
                    + " disabledOthers=" + disabled
                    + " main=" + (main != null ? main.name : "null")
                    + " hud=" + (hud != null ? hud.name : "null")
                    + " matsForced=" + matsFixed
                    + " sky=" + (RenderSettings.skybox != null ? RenderSettings.skybox.shader.name : "NULL")
                    + " clear=" + ColorUtility.ToHtmlStringRGB(DiagnosticClear));

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
