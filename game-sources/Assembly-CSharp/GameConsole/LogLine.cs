using TMPro;
using UnityEngine;
using UnityEngine.UI;
using plog.Helpers;
using plog.Models;
using plog.unity.Extensions;

namespace GameConsole;

public class LogLine : MonoBehaviour
{
	[SerializeField]
	private TMP_Text timestamp;

	[SerializeField]
	private TMP_Text message;

	[SerializeField]
	private TMP_Text context;

	[SerializeField]
	private Image contextPanel;

	[SerializeField]
	private Image mainPanel;

	[Space]
	[SerializeField]
	private CanvasGroup attentionFlashGroup;

	[Space]
	[SerializeField]
	private Color normalLogColor;

	[SerializeField]
	private Color warningLogColor;

	[SerializeField]
	private Color errorLogColor;

	[SerializeField]
	private Color cliLogColor;

	[Space]
	[SerializeField]
	private float normalHeight = 35f;

	[SerializeField]
	private float expandedHeight = 120f;

	private RectTransform rectTransform;

	private Vector2? defaultTextOffsetMin;

	private Vector2? defaultTextOffsetMax;

	private Vector2? defaultTextSizeDelta;

	private ConsoleLog log;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void Wipe()
	{
		log = null;
		timestamp.text = "";
		message.text = "";
		mainPanel.color = normalLogColor;
		RefreshSize();
	}

	public void PopulateLine(ConsoleLog capture)
	{
		log = capture;
		timestamp.text = $"{capture.log.Timestamp:HH:mm:ss.f}";
		RefreshSize();
		if (capture.expanded && !string.IsNullOrEmpty(capture.log.StackTrace))
		{
			string stackTrace = capture.log.StackTrace;
			stackTrace = stackTrace.Replace("\r\n", "\n").Replace("\n", "");
			message.text = $"<b><size={message.fontSizeMax}>{capture.log.Message}</size></b>\n{stackTrace}";
			message.enableAutoSizing = true;
		}
		else
		{
			message.text = capture.log.Message;
			message.fontSize = message.fontSizeMax;
			message.enableAutoSizing = false;
		}
		Image image = mainPanel;
		image.color = capture.log.Level switch
		{
			Level.Info => normalLogColor, 
			Level.Warning => warningLogColor, 
			Level.CommandLine => cliLogColor, 
			Level.Error => errorLogColor, 
			Level.Exception => errorLogColor, 
			_ => normalLogColor, 
		};
		if (capture.source?.Tag != null)
		{
			context.text = capture.source.Tag.ToString();
			var (color, color2) = ColorHelper.GetColorPair(capture.source.Tag.Color);
			context.color = color2.ToUnityColor();
			Color color3 = color.ToUnityColor();
			color3.a = contextPanel.color.a;
			contextPanel.color = color3;
			if (!contextPanel.gameObject.activeSelf)
			{
				contextPanel.gameObject.SetActive(value: true);
				RectTransform rectTransform = message.rectTransform;
				if (defaultTextOffsetMin.HasValue)
				{
					rectTransform.offsetMin = defaultTextOffsetMin.Value;
				}
				if (defaultTextOffsetMax.HasValue)
				{
					rectTransform.offsetMax = defaultTextOffsetMax.Value;
				}
				if (defaultTextSizeDelta.HasValue)
				{
					rectTransform.sizeDelta = defaultTextSizeDelta.Value;
				}
			}
		}
		else if (contextPanel.gameObject.activeSelf)
		{
			contextPanel.gameObject.SetActive(value: false);
			float x = contextPanel.rectTransform.sizeDelta.x;
			RectTransform rectTransform2 = message.rectTransform;
			if (!defaultTextOffsetMin.HasValue)
			{
				defaultTextOffsetMin = rectTransform2.offsetMin;
			}
			if (!defaultTextOffsetMax.HasValue)
			{
				defaultTextOffsetMax = rectTransform2.offsetMax;
			}
			if (!defaultTextSizeDelta.HasValue)
			{
				defaultTextSizeDelta = rectTransform2.sizeDelta;
			}
			rectTransform2.offsetMin = new Vector2(rectTransform2.offsetMin.x - x * 2f, defaultTextOffsetMin.Value.y);
			rectTransform2.offsetMax = new Vector2(rectTransform2.offsetMax.x + x, defaultTextOffsetMax.Value.y);
			rectTransform2.sizeDelta = new Vector2(rectTransform2.sizeDelta.x - x * 2f, defaultTextSizeDelta.Value.y);
		}
		if ((float)capture.timeSinceLogged < 0.5f && base.gameObject.activeInHierarchy)
		{
			attentionFlashGroup.alpha = TimeSinceToFlashAlpha(capture.timeSinceLogged);
		}
	}

	public void ToggleExpand()
	{
		log.expanded = !log.expanded;
		RefreshSize();
		PopulateLine(log);
	}

	private void RefreshSize()
	{
		if (rectTransform == null)
		{
			rectTransform = GetComponent<RectTransform>();
		}
		RectTransform obj = rectTransform;
		ConsoleLog consoleLog = log;
		obj.sizeDelta = ((consoleLog == null || !consoleLog.expanded) ? new Vector2(rectTransform.sizeDelta.x, normalHeight) : new Vector2(rectTransform.sizeDelta.x, expandedHeight));
	}

	private void Update()
	{
		if (log != null)
		{
			if ((float)log.timeSinceLogged > 0.5f)
			{
				attentionFlashGroup.alpha = 0f;
			}
			else
			{
				attentionFlashGroup.alpha = TimeSinceToFlashAlpha(log.timeSinceLogged);
			}
		}
	}

	private float TimeSinceToFlashAlpha(float timeSinceLogged)
	{
		if (timeSinceLogged < 0.2f)
		{
			return timeSinceLogged / 0.2f;
		}
		return 1f - (timeSinceLogged - 0.2f) / 0.3f;
	}
}
