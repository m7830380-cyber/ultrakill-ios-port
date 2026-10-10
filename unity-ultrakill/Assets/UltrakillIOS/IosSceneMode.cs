#if ULTRAKILL_FULL_PORT
using System;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace UltrakillIOS
{
    internal static class IosSceneMode
    {
        public static bool IsMainMenuScene()
        {
            try
            {
                var sh = Type.GetType("SceneHelper, Assembly-CSharp");
                var cur = sh?.GetProperty("CurrentScene", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string;
                if (!string.IsNullOrEmpty(cur))
                {
                    if (cur.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0
                        || cur.IndexOf("b3e7f2f8", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    return false;
                }
            }
            catch
            {
                /* ignore */
            }

            var active = SceneManager.GetActiveScene().name;
            return active.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0
                || active.IndexOf("b3e7f2f8", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
#endif
