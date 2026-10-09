#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Master shaders missing from stub shaders.bundle → InternalError. Use IPA Resources stubs only —
    /// never Addressables.WaitForCompletion (deadlocks boot; session 002321).
    /// </summary>
    internal static class UkMasterShaderBootstrap
    {
        private const string Area = "UkShader";
        private static Shader _master;
        private static Shader _stationary;

        private static readonly string[] MasterShaderFindNames =
        {
            "ULTRAKILL-Standard",
            "ULTRAKILL/Standard",
            "Custom/ULTRAKILL-Standard",
            "ULTRAKILL-Stationary",
            "ULTRAKILL/Stationary",
        };

        internal static Shader Master => _master;
        internal static Shader Stationary => _stationary ?? _master;

        internal static void EnsureReady()
        {
            if (_master != null)
            {
                return;
            }

            ShadersBundleWarmup.TryWarmup();

            foreach (var name in MasterShaderFindNames)
            {
                var sh = Shader.Find(name);
                if (sh == null || !sh.isSupported || IsError(sh))
                {
                    continue;
                }

                if (name.IndexOf("Stationary", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _stationary ??= sh;
                }
                else
                {
                    _master ??= sh;
                }
            }

            _master ??= Resources.Load<Shader>("Shaders/UK_MasterStandard")
                ?? Resources.Load<Shader>("Shaders/UltrakillIOS_UnlitTexture");

            _stationary ??= Resources.Load<Shader>("Shaders/UK_MasterStationary") ?? _master;

            if (_master != null)
            {
                UltrakillLog.Info(Area, "Using master fallback: " + _master.name);
                PatchDefaultReferenceManager(_master);
            }
            else
            {
                UltrakillLog.Warn(Area, "No master shader available");
            }
        }

        private static bool IsError(Shader sh)
        {
            var n = sh != null ? sh.name : "";
            return n.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void PatchDefaultReferenceManager(Shader master)
        {
            try
            {
                var drmType = Type.GetType("DefaultReferenceManager, Assembly-CSharp");
                if (drmType == null)
                {
                    return;
                }

                var instProp = drmType.BaseType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var inst = instProp?.GetValue(null);
                if (inst == null)
                {
                    foreach (var obj in UnityEngine.Object.FindObjectsOfType(drmType, true))
                    {
                        inst = obj;
                        break;
                    }
                }

                if (inst == null)
                {
                    return;
                }

                var f = drmType.GetField("masterShader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && (f.GetValue(inst) == null || IsError((Shader)f.GetValue(inst))))
                {
                    f.SetValue(inst, master);
                    UltrakillLog.Info(Area, "Patched DefaultReferenceManager.masterShader");
                }
            }
            catch (Exception ex)
            {
                UltrakillLog.Warn(Area, "DRM patch: " + ex.Message);
            }
        }

        internal static int RecoverInternalErrorMaterials(bool includeInactive)
        {
            EnsureReady();
            if (_master == null)
            {
                return 0;
            }

            var fixedN = 0;
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(includeInactive))
            {
                if (r == null || r is ParticleSystemRenderer || r.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                var shared = r.sharedMaterials;
                if (shared == null || shared.Length == 0)
                {
                    continue;
                }

                var dirty = false;
                for (var i = 0; i < shared.Length; i++)
                {
                    var m = shared[i];
                    if (m == null || m.shader == null)
                    {
                        continue;
                    }

                    var sn = m.shader.name;
                    if (sn.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    var pick = r.lightmapIndex >= 0 || r.realtimeLightmapIndex >= 0 ? Stationary : Master;
                    m.shader = pick;
                    RetailShaderRepair.HydrateMainTexPublic(m, r, i);
                    dirty = true;
                    fixedN++;
                }

                if (dirty)
                {
                    r.sharedMaterials = shared;
                }
            }

            if (fixedN > 0)
            {
                UltrakillLog.Info(Area, "Recovered " + fixedN + " InternalError material slots -> " + _master.name);
            }

            return fixedN;
        }
    }
}
#endif
