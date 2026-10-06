using UnityEngine;

namespace UltrakillIOS
{
    internal sealed class ContentStatusHud : MonoBehaviour
    {
        private void OnGUI()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (ExternalContentBootstrap.IsReady)
            {
                return;
            }

            var rect = new Rect(16, 16, Screen.width - 32, 120);
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(rect, "Copy " + ExternalContentBootstrap.ZipFileName + " into Files → On My iPhone → ULTRAKILL (Documents), then restart the app.");
#endif
        }
    }
}
