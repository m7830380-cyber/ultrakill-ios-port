using UnityEngine;

namespace UltrakillIOS
{
    internal sealed class ContentStatusHud : MonoBehaviour
    {
        private void OnGUI()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var rect = new Rect(16, 16, Screen.width - 32, ExternalContentBootstrap.IsReady ? 200 : 120);
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;

            if (ExternalContentBootstrap.IsReady)
            {
                GUI.Label(
                    rect,
                    "ULTRAKILL data is loaded.\n\n"
                    + "This IPA is the engine shell only — it does not include the game executable (scenes, scripts, Addressables). "
                    + "Gray screen + touch overlay is expected until the full Unity port is built.\n\n"
                    + "Data: " + ExternalContentBootstrap.ContentDataPath);
                return;
            }

            GUI.Label(rect, "Copy " + ExternalContentBootstrap.ZipFileName + " into Files → On My iPhone → ULTRAKILL (Documents), then restart the app.");
#endif
        }
    }
}
