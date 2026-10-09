#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Session 154618 proof:
    /// - "Colour slideshow" = we reassigned 526 materials EVERY frame.
    /// - ClimbStep×1008 = player collides with Tutorial geometry (world exists).
    /// - NewMovement.Update NRE on null windStateParticle (Awake/Start stubbed).
    /// - CameraController.LateUpdate NRE on null player/opm before look code.
    /// Fix: one-shot world materials, hide StyleHUD, stub broken Update/LateUpdate in DLL,
    /// drive move+look ourselves.
    /// </summary>
    internal sealed class PlayerGameplayRepair : MonoBehaviour
    {
        private const string Area = "Playable";
        private const float MoveSpeed = 12f;
        private const float LookSens = 0.12f;

        private static Material _worldMat;
        private static bool _worldDone;
        private static bool _hudDone;
        private static string _lastScene;

        /// <summary>Written by MobileTouchHud each frame.</summary>
        public static Vector2 TouchMove;

        /// <summary>Written by MobileTouchHud each frame (pixels).</summary>
        public static Vector2 TouchLook;

        private Rigidbody _rb;
        private Transform _camTr;
        private float _yaw;
        private float _pitch;
        private bool _bound;

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
            _worldDone = false;
            _hudDone = false;
            _bound = false;
            _lastScene = scene.name;
            // Defer one frame so addressable content finishes instantiating.
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
            ForceWorldOnce();
            UltrakillLog.Info(Area, "Deferred repair done scene=" + _lastScene
                + " bound=" + _bound + " worldDone=" + _worldDone);
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

            if (!_worldDone && IsGameplayScene())
            {
                ForceWorldOnce();
            }

            DriveMoveLook();
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

            // Disable broken Update path (DLL stub) — we drive movement.
            nm.enabled = false;

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

                // Freeze rotation — we rotate camera only.
                _rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            var ccType = Type.GetType("CameraController, Assembly-CSharp");
            var cc = ccType != null
                ? nm.GetComponentInChildren(ccType, true) as MonoBehaviour
                : null;
            if (cc != null)
            {
                // LateUpdate stubbed in DLL — disable leftover behaviour noise.
                cc.enabled = false;
                var cam = cc.GetComponent<Camera>() ?? cc.GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    _camTr = cam.transform;
                    cam.enabled = true;
                    cam.cullingMask = ~0;
                    cam.useOcclusionCulling = false;
                    cam.targetTexture = null;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.35f, 0.4f, 0.48f, 1f);
                    cam.depth = 0f;
                    if (!cam.CompareTag("MainCamera"))
                    {
                        cam.tag = "MainCamera";
                    }
                }

                var e = _camTr != null ? _camTr.eulerAngles : nm.transform.eulerAngles;
                _yaw = e.y;
                _pitch = e.x > 180f ? e.x - 360f : e.x;
            }
            else
            {
                var cam = nm.GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    _camTr = cam.transform;
                    cam.enabled = true;
                    cam.cullingMask = ~0;
                }
            }

            // Only Main Camera for world; keep HUD Camera if present.
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

        private void DriveMoveLook()
        {
            if (!_bound || _rb == null || _camTr == null)
            {
                return;
            }

            var look = TouchLook;
            TouchLook = Vector2.zero;
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

            var move = TouchMove;
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
            // Keep vertical velocity (gravity / fall).
            _rb.velocity = v;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
            {
                if (Physics.Raycast(_rb.position + Vector3.up * 0.1f, Vector3.down, 1.2f))
                {
                    v = _rb.velocity;
                    v.y = 8f;
                    _rb.velocity = v;
                }
            }
        }

        private static void ForceWorldOnce()
        {
            if (_worldDone)
            {
                return;
            }

            var sh = Shader.Find("Unlit/Color") ?? Shader.Find("UltrakillIOS/UnlitTexture");
            if (sh == null)
            {
                UltrakillLog.Warn(Area, "No Unlit shader");
                return;
            }

            if (_worldMat == null)
            {
                _worldMat = new Material(sh);
                var col = new Color(0.65f, 0.62f, 0.58f, 1f);
                _worldMat.color = col;
                if (_worldMat.HasProperty("_Color"))
                {
                    _worldMat.SetColor("_Color", col);
                }
            }

            var n = 0;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer)
                {
                    continue;
                }

                if (r.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                if (!r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                r.enabled = true;
                var len = r.sharedMaterials != null ? Math.Max(1, r.sharedMaterials.Length) : 1;
                var mats = new Material[len];
                for (var i = 0; i < len; i++)
                {
                    mats[i] = _worldMat;
                }

                r.sharedMaterials = mats;
                n++;
            }

            RenderSettings.fog = false;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.skybox = null;

            _worldDone = n > 0;
            UltrakillLog.Info(Area, "ONE-SHOT world Unlit/Color on " + n + " renderers");
        }
    }
}
#endif
