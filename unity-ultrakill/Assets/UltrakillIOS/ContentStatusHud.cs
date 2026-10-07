using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Status line plus a toggleable on-screen log console (tap LOG in the top-right corner).
    /// </summary>
    internal sealed class ContentStatusHud : MonoBehaviour
    {
        private const int VisibleLines = 30;

        private bool _showLog = true;
        private Vector2 _scroll;
        private GUIStyle _label;
        private GUIStyle _button;

        private void OnGUI()
        {
#if UNITY_IOS && !UNITY_EDITOR
            EnsureStyles();

            var buttonSize = new Vector2(Screen.width * 0.12f, Screen.height * 0.06f);
            var buttonRect = new Rect(Screen.width - buttonSize.x - 16, 16, buttonSize.x, buttonSize.y);
            var errors = UltrakillLog.ErrorCount;
            if (GUI.Button(buttonRect, errors > 0 ? "LOG (" + errors + ")" : "LOG", _button))
            {
                _showLog = !_showLog;
            }

            if (!_showLog)
            {
                return;
            }

            var panel = new Rect(16, 16, Screen.width - buttonSize.x - 48, Screen.height * 0.6f);
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(panel.x + 8, panel.y + 8, panel.width - 16, panel.height - 16));
            GUILayout.Label(StatusLine(), _label);
            GUILayout.Label("Log file: Documents/" + UltrakillLog.LogFolderName, _label);
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var line in UltrakillLog.GetTail(VisibleLines))
            {
                GUILayout.Label(line, _label);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
#endif
        }

        private static string StatusLine()
        {
            if (!ExternalContentBootstrap.IsReady)
            {
                return "Data: missing — unpack into Documents/" + ExternalContentBootstrap.ExpectedDocumentsPath;
            }
#if ULTRAKILL_FULL_PORT
            return "Data: OK | Boot: " + RetailBootProbe.Status;
#else
            return "Data: OK | Engine shell build (ULTRAKILL_FULL_PORT not defined)";
#endif
        }

        private void EnsureStyles()
        {
            if (_label != null)
            {
                return;
            }

            var fontSize = Mathf.Max(12, Screen.height / 55);
            _label = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true };
            _label.normal.textColor = Color.white;
            _button = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
        }
    }
}
