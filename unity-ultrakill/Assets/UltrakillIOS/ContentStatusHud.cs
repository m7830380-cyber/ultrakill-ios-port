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
#if ULTRAKILL_FULL_PORT
                return;
#else
                GUI.Label(
                    rect,
                    "ULTRAKILL data is loaded.\n\n"
                    + "Engine shell only — install the full port IPA for gameplay.\n\n"
                    + "Data: " + ExternalContentBootstrap.ContentDataPath);
                return;
#endif
            }

            GUI.Label(
                rect,
                "Unpack game data into Documents/"
                + ExternalContentBootstrap.ExpectedDocumentsPath
                + " (Files → On My iPhone → ULTRAKILL), then restart the app.");
#endif
        }
    }
}
