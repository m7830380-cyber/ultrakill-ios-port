#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Movement/camera repair only. World materials use retail shaders from shaders.bundle;
    /// RetailShaderRepair fixes only unsupported shaders (with albedo preserved).
    /// </summary>
    internal sealed class PlayerGameplayRepair : MonoBehaviour
    {
        private const string Area = "Playable";
        private const float MoveSpeed = 12f;
        private const float LookSens = 0.12f;

        private static Material _skyMat;
        private static bool _lightingDone;
        private static bool _hudDone;
        private static bool _preCullHooked;
        private static string _lastScene;
        private static Camera _mainCam;
        private static readonly Color WorldClear = new Color(0.35f, 0.4f, 0.48f, 1f);

        private Rigidbody _rb;
        private Transform _camTr;
        private AudioSource _fallWhoosh;
        private float _yaw;
        private float _pitch;
        private bool _bound;
        private float _nextDiag;

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
            EnsurePreCullHook();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_preCullHooked)
            {
                Camera.onPreCull -= LockMainClearFlags;
                _preCullHooked = false;
            }
        }

        private static void EnsurePreCullHook()
        {
            if (_preCullHooked)
            {
                return;
            }

            Camera.onPreCull += LockMainClearFlags;
            _preCullHooked = true;
        }

        /// <summary>
        /// Skybox clear + missing/stub skybox = black. Lock Main to SolidColor every pre-cull.
        /// </summary>
        private static void LockMainClearFlags(Camera cam)
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
                return;
            }

            if (_mainCam != null && cam != _mainCam)
            {
                return;
            }

            if (!cam.enabled)
            {
                return;
            }

            if (n.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            if (cam.clearFlags == CameraClearFlags.Skybox || cam.clearFlags == CameraClearFlags.Nothing)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            if (cam.clearFlags == CameraClearFlags.SolidColor
                && cam.backgroundColor.maxColorComponent < 0.15f)
            {
                cam.backgroundColor = new Color(0.35f, 0.4f, 0.48f, 1f);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _lightingDone = false;
            _hudDone = false;
            _bound = false;
            _lastScene = scene.name;
            StartCoroutine(DeferredRepair());
        }

        private System.Collections.IEnumerator DeferredRepair()
        {
            yield return null;
            yield return null;
            KillStyleMeter();
            KillIntro();
            MuteClimbStep();
            BindPlayer();
            EnsureRetailLighting();
            var fixedMats = RetailShaderRepair.RemapBrokenMaterialsOnRenderers(includeInactive: false);
            UltrakillLog.Info(Area, "Deferred repair done scene=" + _lastScene
                + " bound=" + _bound + " brokenMatsFixed=" + fixedMats
                + " sky=" + (RenderSettings.skybox != null ? RenderSettings.skybox.shader.name : "NULL"));
            yield return new WaitForSecondsRealtime(0.75f);
            RetailShaderRepair.RemapBrokenMaterialsOnRenderers(includeInactive: false);
            LevelLookHorizon();
        }

        private void Update()
        {
            if (!_bound)
            {
                BindPlayer();
            }

            if (!_hudDone)
            {
                KillStyleMeter();
                KillIntro();
                MuteClimbStep();
                _hudDone = true;
            }

            if (!_lightingDone && IsGameplayScene())
            {
                EnsureRetailLighting();
            }

            DriveMoveLook();
            MaybeDiag();
        }

        private void LevelLookHorizon()
        {
            if (!_bound || _camTr == null)
            {
                return;
            }

            _pitch = 0f;
            _camTr.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void MaybeDiag()
        {
            if (Time.unscaledTime < _nextDiag)
            {
                return;
            }

            _nextDiag = Time.unscaledTime + 2f;
            if (!_bound || _rb == null || _camTr == null)
            {
                return;
            }

            var cam = _camTr.GetComponent<Camera>();
            var hitInfo = "none";
            if (Physics.Raycast(_camTr.position, _camTr.forward, out var hit, 80f))
            {
                hitInfo = hit.collider.name + "@" + hit.distance.ToString("F1")
                    + " layer=" + hit.collider.gameObject.layer;
                var r = hit.collider.GetComponent<Renderer>()
                    ?? hit.collider.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null)
                {
                    hitInfo += " sh=" + r.sharedMaterial.shader.name;
                }
            }

            UltrakillLog.Info(Area, "DIAG pos=" + _rb.position.ToString("F1")
                + " vy=" + _rb.velocity.y.ToString("F1")
                + " clear=" + (cam != null ? cam.clearFlags.ToString() : "?")
                + " bg=" + (cam != null ? ColorUtility.ToHtmlStringRGB(cam.backgroundColor) : "?")
                + " sky=" + (RenderSettings.skybox != null ? RenderSettings.skybox.shader.name : "NULL")
                + " look=" + hitInfo);
        }

        private static bool IsGameplayScene()
        {
            try
            {
                var sh = Type.GetType("SceneHelper, Assembly-CSharp");
                var cur = sh?.GetProperty("CurrentScene", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string;
                if (string.IsNullOrEmpty(cur))
                {
                    return false;
                }

                if (cur.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }

                if (cur.IndexOf("b3e7f2f8", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

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

                t.GetField("forceMeterOn", BindingFlags.Instance | BindingFlags.Public)?.SetValue(mb, false);
                mb.enabled = false;
                for (var i = 0; i < mb.transform.childCount; i++)
                {
                    mb.transform.GetChild(i).gameObject.SetActive(false);
                }
            }
        }

        private static void KillIntro()
        {
            foreach (var name in new[] { "IntroTextController", "IntroViolenceScreen" })
            {
                var t = Type.GetType(name + ", Assembly-CSharp");
                if (t == null)
                {
                    continue;
                }

                foreach (var obj in UnityEngine.Object.FindObjectsOfType(t, true))
                {
                    if (obj is Behaviour b)
                    {
                        b.enabled = false;
                    }
                }
            }

            try
            {
                var omType = Type.GetType("OptionsManager, Assembly-CSharp");
                var om = omType != null ? UnityEngine.Object.FindObjectOfType(omType) : null;
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

        private static void MuteClimbStep()
        {
            var t = Type.GetType("ClimbStep, Assembly-CSharp");
            if (t == null)
            {
                return;
            }

            foreach (var obj in UnityEngine.Object.FindObjectsOfType(t, true))
            {
                if (obj is Behaviour b)
                {
                    b.enabled = false;
                }
            }
        }

        private void BindPlayer()
        {
            var nmType = Type.GetType("NewMovement, Assembly-CSharp");
            var nm = nmType != null ? UnityEngine.Object.FindObjectOfType(nmType) as MonoBehaviour : null;
            if (nm == null)
            {
                return;
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _rb = nm.GetComponent<Rigidbody>();
            nmType.GetField("rb", flags)?.SetValue(nm, _rb);
            nmType.GetField("activated", flags)?.SetValue(nm, true);
            nmType.GetField("dead", flags)?.SetValue(nm, false);

            // NM Update/FixedUpdate are DLL-stubbed (windStateParticle NRE). Keep component
            // enabled for other messages, but we drive move. Fall whoosh is synthesised below.
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.detectCollisions = true;
                _rb.useGravity = true;
                try
                {
                    var ext = Type.GetType("Gravity.GravityExtensions, Assembly-CSharp");
                    ext?.GetMethod("SetGravityMode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.Invoke(null, new object[] { _rb, true });
                }
                catch
                {
                    _rb.useGravity = true;
                }

                _rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            EnsureFallWhoosh(nm.transform);

            var ccType = Type.GetType("CameraController, Assembly-CSharp");
            var cc = ccType != null
                ? nm.GetComponentInChildren(ccType, true) as MonoBehaviour
                : null;
            Camera cam = null;
            if (cc != null)
            {
                // LateUpdate stubbed — look is driven here. Leave enabled so PlayerActivator
                // can set activated without fighting a disabled component.
                cam = cc.GetComponent<Camera>() ?? cc.GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    ccType.GetField("cam", flags)?.SetValue(cc, cam);
                    ccType.GetField("activated", flags)?.SetValue(cc, true);
                    ccType.GetField("player", flags)?.SetValue(cc, nm.gameObject);
                    ccType.GetField("nm", flags)?.SetValue(cc, nm);
                }
            }

            if (cam == null)
            {
                cam = nm.GetComponentInChildren<Camera>(true);
            }

            if (cam != null)
            {
                _camTr = cam.transform;
                _mainCam = cam;
                cam.enabled = true;
                cam.cullingMask = ~0;
                cam.useOcclusionCulling = false;
                cam.targetTexture = null;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = WorldClear;
                cam.depth = 0f;
                if (!cam.CompareTag("MainCamera"))
                {
                    cam.tag = "MainCamera";
                }

                var e = _camTr.eulerAngles;
                _yaw = e.y;
                _pitch = e.x > 180f ? e.x - 360f : e.x;
            }

            foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>(true))
            {
                if (c == null)
                {
                    continue;
                }

                var n = c.gameObject.name;
                if (_camTr != null && c.transform == _camTr)
                {
                    c.enabled = true;
                    continue;
                }

                if (n.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.enabled = true;
                    c.clearFlags = CameraClearFlags.Depth;
                    continue;
                }

                c.enabled = false;
            }

            Time.timeScale = 1f;
            _bound = _rb != null && _camTr != null;
            if (_bound)
            {
                UltrakillLog.Info(Area, "Bound player rb+cam for manual move/look");
            }
        }

        private void EnsureFallWhoosh(Transform player)
        {
            if (_fallWhoosh != null)
            {
                return;
            }

            // Retail fall whoosh lives on WallCheck; NM Awake (stubbed) never wired audWoosh.
            var wall = player.GetComponentInChildren<AudioSource>(true);
            foreach (var a in player.GetComponentsInChildren<AudioSource>(true))
            {
                if (a != null && a.gameObject.name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    wall = a;
                    break;
                }
            }

            if (wall != null)
            {
                _fallWhoosh = wall;
                return;
            }

            var go = new GameObject("UltrakillIOS.FallWhoosh");
            go.transform.SetParent(player, false);
            _fallWhoosh = go.AddComponent<AudioSource>();
            _fallWhoosh.loop = true;
            _fallWhoosh.playOnAwake = false;
            _fallWhoosh.spatialBlend = 0f;
            _fallWhoosh.volume = 0f;
        }

        private void DriveMoveLook()
        {
            if (!_bound || _rb == null || _camTr == null)
            {
                return;
            }

            var look = TouchGameplayBridge.Look;
            TouchGameplayBridge.Look = Vector2.zero;
            if (look.sqrMagnitude < 0.0001f)
            {
                try
                {
                    var mouse = UnityEngine.InputSystem.Mouse.current;
                    if (mouse != null)
                    {
                        look = mouse.delta.ReadValue();
                    }
                }
                catch
                {
                    look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
                }
            }

            if (look.sqrMagnitude > 0.0001f)
            {
                _yaw += look.x * LookSens;
                _pitch -= look.y * LookSens;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            }

            _rb.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _camTr.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            var move = TouchGameplayBridge.Move;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                move.y += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                move.y -= 1f;
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                move.x -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                move.x += 1f;
            }

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            var forward = Vector3.ProjectOnPlane(_camTr.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(_camTr.right, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = _rb.transform.forward;
            }

            var wish = (forward * move.y + right * move.x) * MoveSpeed;
            var v = _rb.velocity;
            v.x = wish.x;
            v.z = wish.z;
            _rb.velocity = v;

            // Synthesise fall whoosh — retail path is inside stubbed NewMovement.Update.
            if (_fallWhoosh != null)
            {
                var downSpeed = Mathf.Max(0f, -_rb.velocity.y);
                if (downSpeed > 8f)
                {
                    if (!_fallWhoosh.isPlaying && _fallWhoosh.clip != null)
                    {
                        _fallWhoosh.Play();
                    }

                    _fallWhoosh.volume = Mathf.Clamp01(downSpeed / 80f);
                    _fallWhoosh.pitch = Mathf.Clamp(downSpeed / 120f, 0.1f, 2f);
                }
                else
                {
                    _fallWhoosh.volume = Mathf.MoveTowards(_fallWhoosh.volume, 0f, Time.deltaTime);
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
            {
                if (Physics.Raycast(_rb.position + Vector3.up * 0.1f, Vector3.down, 1.2f))
                {
                    v = _rb.velocity;
                    v.y = 8f;
                    _rb.velocity = v;
                }
            }

            // Keep clear flags honest even if something re-enables Skybox mid-frame.
            if (_mainCam != null)
            {
                if (_mainCam.clearFlags != CameraClearFlags.SolidColor)
                {
                    _mainCam.clearFlags = CameraClearFlags.SolidColor;
                }

                _mainCam.backgroundColor = WorldClear;
                EnsureSkyMaterial();
            }
        }

        private static void EnsureSkyMaterial()
        {
            if (RenderSettings.skybox != null)
            {
                return;
            }

            if (_skyMat == null)
            {
                var sh = Shader.Find("Skybox/Procedural")
                    ?? Shader.Find("Unlit/Color")
                    ?? Shader.Find("UltrakillIOS/UnlitTexture");
                if (sh == null)
                {
                    return;
                }

                _skyMat = new Material(sh);
                if (_skyMat.HasProperty("_SkyTint"))
                {
                    _skyMat.SetColor("_SkyTint", new Color(0.55f, 0.65f, 0.8f));
                }

                if (_skyMat.HasProperty("_Color"))
                {
                    _skyMat.SetColor("_Color", new Color(0.45f, 0.55f, 0.7f));
                }
            }

            RenderSettings.skybox = _skyMat;
        }

        private static void EnsureRetailLighting()
        {
            if (_lightingDone)
            {
                return;
            }

            EnsureSkyMaterial();
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.75f, 0.75f, 0.8f, 1f);
            RenderSettings.ambientIntensity = 1.1f;
            _lightingDone = true;
        }
    }
}
#endif
