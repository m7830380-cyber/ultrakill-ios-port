#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Makes Tutorial playable: kill StyleHUD meter, unfreeze player gravity/activation,
    /// repair NewMovement null GroundCheck, force world meshes to Metal-safe Unlit/Color.
    /// </summary>
    internal sealed class PlayerGameplayRepair : MonoBehaviour
    {
        private const string Area = "Playable";

        private static Material _worldMat;
        private static bool _worldForced;
        private static bool _probeSpawned;
        private static int _frames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EarlyMuteIntro()
        {
            // Kill IntroTextController before its Start freezes Rigidbody gravity.
            // Actual disable happens on first scene objects via Install after load.
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindObjectOfType<PlayerGameplayRepair>() != null)
            {
                return;
            }

            var go = new GameObject("UltrakillIOS.Playable");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerGameplayRepair>();
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
            _worldForced = false;
            _probeSpawned = false;
            _frames = 0;
            RunFullRepair(forceWorld: true);
        }

        private void LateUpdate()
        {
            _frames++;
            // Every frame for the first 5s, then every 15 frames — intro/HUD re-enable themselves.
            if (_frames < 300 || _frames % 15 == 0)
            {
                RunFullRepair(forceWorld: !_worldForced || _frames < 120);
            }
        }

        private static void RunFullRepair(bool forceWorld)
        {
            KillStyleMeter();
            KillIntroFreezes();
            RepairPlayer();
            UnlockCameraStates();
            if (forceWorld)
            {
                ForceWorldVisible();
            }
        }

        /// <summary>
        /// StyleHUD child[0] is the style/killstreak meter. Awake NREs on rankImage before
        /// ComboOver(), so the meter stays active on a white clear — exactly the bug report.
        /// </summary>
        private static void KillStyleMeter()
        {
            var t = Type.GetType("StyleHUD, Assembly-CSharp");
            if (t == null)
            {
                return;
            }

            foreach (var obj in UnityEngine.Object.FindObjectsOfType(t, true))
            {
                if (obj is not MonoBehaviour mb)
                {
                    continue;
                }

                try
                {
                    t.GetField("forceMeterOn", BindingFlags.Instance | BindingFlags.Public)
                        ?.SetValue(mb, false);
                    t.GetField("showStyleMeter", BindingFlags.Instance | BindingFlags.Public)
                        ?.SetValue(mb, false);

                    // Hide meter root (child 0) and every other child.
                    var tr = mb.transform;
                    for (var i = 0; i < tr.childCount; i++)
                    {
                        tr.GetChild(i).gameObject.SetActive(false);
                    }

                    mb.enabled = false;
                    // Keep StyleHUD GO alive for singletons, but mute visuals.
                }
                catch (Exception ex)
                {
                    UltrakillLog.Warn(Area, "StyleHUD kill failed: " + ex.Message);
                }
            }
        }

        private static void KillIntroFreezes()
        {
            foreach (var name in new[] { "IntroTextController", "IntroViolenceScreen", "IntroText" })
            {
                var t = Type.GetType(name + ", Assembly-CSharp");
                if (t == null || !typeof(Behaviour).IsAssignableFrom(t))
                {
                    continue;
                }

                foreach (var obj in UnityEngine.Object.FindObjectsOfType(t, true))
                {
                    if (obj is Behaviour b)
                    {
                        b.enabled = false;
                        if (b.gameObject.activeSelf && name != "IntroText")
                        {
                            // Don't destroy; just disable heavy intro canvases.
                            foreach (var canvas in b.GetComponentsInChildren<Canvas>(true))
                            {
                                canvas.enabled = false;
                            }
                        }
                    }
                }
            }

            try
            {
                var omType = Type.GetType("OptionsManager, Assembly-CSharp");
                var om = omType != null ? UnityEngine.Object.FindObjectOfType(omType) as MonoBehaviour : null;
                if (om != null)
                {
                    omType.GetField("inIntro", BindingFlags.Instance | BindingFlags.Public)?.SetValue(om, false);
                    omType.GetField("paused", BindingFlags.Instance | BindingFlags.Public)?.SetValue(om, false);
                    omType.GetField("frozen", BindingFlags.Instance | BindingFlags.Public)?.SetValue(om, false);
                }
            }
            catch
            {
                /* ignore */
            }
        }

        private static void RepairPlayer()
        {
            var nmType = Type.GetType("NewMovement, Assembly-CSharp");
            if (nmType == null)
            {
                return;
            }

            var nm = UnityEngine.Object.FindObjectOfType(nmType) as MonoBehaviour;
            if (nm == null)
            {
                return;
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Awake is Cecil-stubbed — wire essentials here.
            var rb = nm.GetComponent<Rigidbody>();
            nmType.GetField("rb", flags)?.SetValue(nm, rb);
            var ccComp = nm.GetComponentInChildren(Type.GetType("CameraController, Assembly-CSharp"), true);
            if (ccComp != null)
            {
                nmType.GetField("cc", flags)?.SetValue(nm, ccComp);
            }

            nmType.GetField("aud", flags)?.SetValue(nm, nm.GetComponent<AudioSource>());
            nmType.GetField("playerCollider", flags)?.SetValue(nm, nm.GetComponent<CapsuleCollider>());

            // Repair null GroundCheckGroup refs (original Awake NREs on gc.GetComponent).
            RepairComponentRef(nm, nmType, "gc", "GroundCheckGroup, Assembly-CSharp", flags);
            RepairComponentRef(nm, nmType, "slopeCheck", "GroundCheckGroup, Assembly-CSharp", flags);
            RepairComponentRef(nm, nmType, "wcGroup", "WallCheckGroup, Assembly-CSharp", flags);

            var gcObj = nmType.GetField("gc", flags)?.GetValue(nm) as Component;
            if (gcObj != null)
            {
                nmType.GetField("audGround", flags)?.SetValue(nm, gcObj.GetComponent<AudioSource>());
            }

            var wc = nm.GetComponentInChildren(Type.GetType("WallCheck, Assembly-CSharp"), true) as Component;
            if (wc != null)
            {
                nmType.GetField("audWoosh", flags)?.SetValue(nm, wc.GetComponent<AudioSource>());
            }

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
                // IntroTextController.Start called SetGravityMode(false) — undo.
                try
                {
                    var ext = Type.GetType("Gravity.GravityExtensions, Assembly-CSharp")
                        ?? Type.GetType("GravityExtensions, Assembly-CSharp");
                    var set = ext?.GetMethod("SetGravityMode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (set != null)
                    {
                        set.Invoke(null, new object[] { rb, true });
                    }
                    else
                    {
                        rb.useGravity = true;
                    }
                }
                catch
                {
                    rb.useGravity = true;
                }
            }

            nmType.GetField("activated", flags)?.SetValue(nm, true);
            nmType.GetField("dead", flags)?.SetValue(nm, false);
            nmType.GetField("levelOver", flags)?.SetValue(nm, false);

            try
            {
                var inman = Type.GetType("InputManager, Assembly-CSharp");
                if (inman != null)
                {
                    var inst = inman.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                        ?? UnityEngine.Object.FindObjectOfType(inman);
                    nmType.GetField("inman", flags)?.SetValue(nm, inst);
                }

                var ass = Type.GetType("AssistController, Assembly-CSharp");
                if (ass != null)
                {
                    var inst = UnityEngine.Object.FindObjectOfType(ass);
                    nmType.GetField("asscon", flags)?.SetValue(nm, inst);
                }
            }
            catch
            {
                /* ignore */
            }

            if (rb != null)
            {
                nmType.GetField("defaultRBConstraints", flags)?.SetValue(nm, rb.constraints);
                try
                {
                    rb.solverIterations = Math.Max(rb.solverIterations, 30);
                    rb.solverVelocityIterations = Math.Max(rb.solverVelocityIterations, 30);
                }
                catch
                {
                    /* ignore */
                }
            }

            if (gcObj != null)
            {
                nmType.GetField("groundCheckPos", flags)?.SetValue(nm, gcObj.transform.localPosition);
            }

            // frictionlessSurfaceMask default 0 breaks raycasts — set Environment-ish mask.
            var maskField = nmType.GetField("frictionlessSurfaceMask", flags);
            if (maskField != null)
            {
                maskField.SetValue(nm, (LayerMask)(~0));
            }

            try
            {
                var prefs = Type.GetType("PrefsManager, Assembly-CSharp");
                var prefsInst = prefs != null ? UnityEngine.Object.FindObjectOfType(prefs) : null;
                if (prefsInst != null)
                {
                    var getInt = prefs.GetMethod("GetInt", new[] { typeof(string) });
                    if (getInt != null)
                    {
                        var diff = getInt.Invoke(prefsInst, new object[] { "difficulty" });
                        nmType.GetField("difficulty", flags)?.SetValue(nm, diff);
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            var ccType = Type.GetType("CameraController, Assembly-CSharp");
            var cc = ccType != null
                ? (nm.GetComponentInChildren(ccType, true) as MonoBehaviour)
                  ?? (UnityEngine.Object.FindObjectOfType(ccType) as MonoBehaviour)
                : null;
            if (cc != null && ccType != null)
            {
                cc.enabled = true;
                ccType.GetField("activated", flags)?.SetValue(cc, true);
                var cam = cc.GetComponent<Camera>() ?? cc.GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    ccType.GetField("cam", flags)?.SetValue(cc, cam);
                    cam.enabled = true;
                    cam.cullingMask = ~0;
                    cam.useOcclusionCulling = false;
                    cam.targetTexture = null;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.45f, 0.5f, 0.58f, 1f);
                }

                var nmField = ccType.GetField("nm", flags);
                if (nmField != null)
                {
                    nmField.SetValue(cc, nm);
                }

                var playerField = ccType.GetField("player", flags);
                if (playerField != null)
                {
                    playerField.SetValue(cc, nm.gameObject);
                }

                // LateUpdate uses opm.mouseSensitivity — Start stub left opm null.
                var om = Type.GetType("OptionsManager, Assembly-CSharp");
                if (om != null)
                {
                    var omInst = UnityEngine.Object.FindObjectOfType(om);
                    if (omInst != null)
                    {
                        ccType.GetField("opm", flags)?.SetValue(cc, omInst);
                    }
                }

                if (ccType.GetField("defaultFov", flags)?.GetValue(cc) is float fov && fov < 1f && cam != null)
                {
                    ccType.GetField("defaultFov", flags)?.SetValue(cc, cam.fieldOfView > 1f ? cam.fieldOfView : 90f);
                }
            }

            if (Time.timeScale < 0.1f)
            {
                Time.timeScale = 1f;
            }

            if (_frames == 1 || _frames == 60)
            {
                UltrakillLog.Info(Area, "Player repaired activated=true gravity="
                    + (rb != null && !rb.isKinematic)
                    + " cc=" + (cc != null));
            }
        }

        private static void RepairComponentRef(
            MonoBehaviour host,
            Type hostType,
            string fieldName,
            string componentTypeName,
            BindingFlags flags)
        {
            var field = hostType.GetField(fieldName, flags);
            if (field == null)
            {
                return;
            }

            if (field.GetValue(host) != null)
            {
                return;
            }

            var ct = Type.GetType(componentTypeName);
            if (ct == null)
            {
                return;
            }

            var found = host.GetComponentInChildren(ct, true);
            if (found != null)
            {
                field.SetValue(host, found);
                UltrakillLog.Info(Area, "Wired NewMovement." + fieldName + " -> " + found);
            }
        }

        private static void UnlockCameraStates()
        {
            try
            {
                var gsmType = Type.GetType("GameStateManager, Assembly-CSharp");
                if (gsmType == null)
                {
                    return;
                }

                var instProp = gsmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var inst = instProp?.GetValue(null);
                if (inst == null)
                {
                    return;
                }

                var pop = gsmType.GetMethod("PopState", BindingFlags.Public | BindingFlags.Instance);
                // pit-falling locks camera look; intro may leave it stuck.
                pop?.Invoke(inst, new object[] { "pit-falling" });
            }
            catch
            {
                /* already popped / null key */
            }
        }

        private static void ForceWorldVisible()
        {
            var sh = Shader.Find("Unlit/Color")
                ?? Shader.Find("UltrakillIOS/UnlitTexture")
                ?? Shader.Find("Sprites/Default");
            if (sh == null)
            {
                UltrakillLog.Warn(Area, "No unlit shader for world force");
                return;
            }

            if (_worldMat == null || _worldMat.shader != sh)
            {
                _worldMat = new Material(sh);
                if (_worldMat.HasProperty("_Color"))
                {
                    _worldMat.SetColor("_Color", new Color(0.72f, 0.68f, 0.6f, 1f));
                }

                _worldMat.color = new Color(0.72f, 0.68f, 0.6f, 1f);
            }

            var count = 0;
            var enabledR = 0;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (r == null)
                {
                    continue;
                }

                // Skip pure UI / weapon HUD meshes parented under HUD canvases.
                if (r.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                if (!r.enabled)
                {
                    r.enabled = true;
                    enabledR++;
                }

                if (!r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var len = r.sharedMaterials != null ? r.sharedMaterials.Length : 1;
                if (len < 1)
                {
                    len = 1;
                }

                var mats = new Material[len];
                for (var i = 0; i < len; i++)
                {
                    mats[i] = _worldMat;
                }

                r.sharedMaterials = mats;
                count++;
            }

            foreach (var r in UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>(true))
            {
                if (r == null || r.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                if (!r.enabled)
                {
                    r.enabled = true;
                }

                if (!r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var len = r.sharedMaterials != null ? Math.Max(1, r.sharedMaterials.Length) : 1;
                var mats = new Material[len];
                for (var i = 0; i < len; i++)
                {
                    mats[i] = _worldMat;
                }

                r.sharedMaterials = mats;
                count++;
            }

            RenderSettings.fog = false;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.ambientIntensity = 1.5f;

            if (!_probeSpawned)
            {
                SpawnViewProbe();
                _probeSpawned = true;
            }

            _worldForced = true;
            UltrakillLog.Info(Area, "World force Unlit/Color on " + count
                + " renderers (reenabled=" + enabledR + ") shader=" + sh.name);
        }

        /// <summary>Orange cube 2m in front of camera — if you see it, rasterization works.</summary>
        private static void SpawnViewProbe()
        {
            Camera cam = null;
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>())
            {
                if (c != null && c.enabled && c.CompareTag("MainCamera"))
                {
                    cam = c;
                    break;
                }
            }

            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                return;
            }

            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            probe.name = "UltrakillIOS.ViewProbe";
            probe.transform.SetParent(cam.transform, false);
            probe.transform.localPosition = new Vector3(0f, 0f, 2.5f);
            probe.transform.localScale = Vector3.one * 0.35f;
            var col = probe.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.Destroy(col);
            }

            var r = probe.GetComponent<MeshRenderer>();
            if (r != null)
            {
                var sh = Shader.Find("Unlit/Color") ?? Shader.Find("UltrakillIOS/UnlitTexture");
                if (sh != null)
                {
                    var m = new Material(sh);
                    m.color = new Color(1f, 0.4f, 0.05f, 1f);
                    if (m.HasProperty("_Color"))
                    {
                        m.SetColor("_Color", new Color(1f, 0.4f, 0.05f, 1f));
                    }

                    r.sharedMaterial = m;
                }
            }

            UltrakillLog.Info(Area, "Spawned orange view probe on " + cam.name);
        }
    }
}
#endif
