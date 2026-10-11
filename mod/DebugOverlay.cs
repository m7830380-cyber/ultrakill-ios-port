using UnityEngine;
using UnityEngine.InputSystem;

namespace UltrakillIOS
{
    // Tiny FPS counter in the top-right corner; tap it to show the last log lines.
    public class DebugOverlay : MonoBehaviour
    {
        bool m_ShowLog;
        float m_Fps, m_Accum;
        int m_Frames;
        GUIStyle m_Style, m_Small;

        void Update()
        {
            m_Accum += Time.unscaledDeltaTime;
            m_Frames++;
            if (m_Accum >= 0.5f)
            {
                m_Fps = m_Frames / m_Accum;
                m_Accum = 0;
                m_Frames = 0;
            }
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 p = ts.primaryTouch.position.ReadValue();
                if (p.x > Screen.width - Screen.height * 0.14f && p.y > Screen.height * 0.9f)
                    m_ShowLog = !m_ShowLog;
            }
        }

        void OnGUI()
        {
            if (m_Style == null)
            {
                m_Style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight };
                m_Small = new GUIStyle(GUI.skin.label) { wordWrap = true };
            }
            m_Style.fontSize = Mathf.RoundToInt(Screen.height * 0.022f);
            m_Small.fontSize = Mathf.RoundToInt(Screen.height * 0.018f);
            float w = Screen.width, h = Screen.height;
            string txt = $"{m_Fps:0} fps" + (Log.ErrorCount > 0 ? $"  {Log.ErrorCount} err" : "");
            GUI.Label(new Rect(w - h * 0.5f, 4, h * 0.5f - 8, h * 0.05f), txt, m_Style);
            if (!m_ShowLog) return;
            GUI.color = new Color(0, 0, 0, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string all;
            all = Log.Snapshot();
            GUI.Label(new Rect(10, h * 0.06f, w - 20, h * 0.92f), all, m_Small);
        }
    }
}
