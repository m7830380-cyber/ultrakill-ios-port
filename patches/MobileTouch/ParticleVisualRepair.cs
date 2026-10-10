#if ULTRAKILL_FULL_PORT
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    /// <summary>
    /// Stub shaders.bundle leaves particle materials as InternalError → permanent purple quads.
    /// Disable broken particle renderers and clear emitters (session 233714).
    /// </summary>
    internal static class ParticleVisualRepair
    {
        private const string Area = "Particles";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("UltrakillIOS.ParticleRepair");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Host>();
        }

        internal static int SuppressBroken()
        {
            var n = 0;
            foreach (var psr in UnityEngine.Object.FindObjectsOfType<ParticleSystemRenderer>(true))
            {
                if (psr == null || !psr.enabled)
                {
                    continue;
                }

                if (!IsBroken(psr))
                {
                    continue;
                }

                psr.enabled = false;
                var ps = psr.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                }

                n++;
            }

            if (n > 0)
            {
                UltrakillLog.Info(Area, "Suppressed broken particle renderers=" + n);
            }

            return n;
        }

        private static bool IsBroken(ParticleSystemRenderer psr)
        {
            var mats = psr.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || m.shader == null)
                {
                    return true;
                }

                var sn = m.shader.name ?? "";
                if (sn.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0
                    || !m.shader.isSupported)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class Host : MonoBehaviour
        {
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
                if (!scene.IsValid())
                {
                    return;
                }

                StartCoroutine(Deferred());
            }

            private System.Collections.IEnumerator Deferred()
            {
                yield return null;
                SuppressBroken();
                yield return new WaitForSecondsRealtime(0.4f);
                SuppressBroken();
                yield return new WaitForSecondsRealtime(1f);
                SuppressBroken();
            }
        }
    }
}
#endif
