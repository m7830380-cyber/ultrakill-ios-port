using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Entry point merged into Assembly-CSharp (via dnSpy or Cecil patcher).
    /// </summary>
    public static class MobileTouchBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
#if UNITY_IOS || UNITY_ANDROID
            EnsureDriver();
#else
            if (Application.isMobilePlatform)
            {
                EnsureDriver();
            }
#endif
        }

        public static void EnsureDriver()
        {
            if (Object.FindObjectOfType<MobileTouchHud>() != null)
            {
                return;
            }

            var go = new GameObject("UltrakillIOS.Touch");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<MobileTouchHud>();
            go.AddComponent<ContentStatusHud>();
            Object.DontDestroyOnLoad(go);
        }
    }
}
