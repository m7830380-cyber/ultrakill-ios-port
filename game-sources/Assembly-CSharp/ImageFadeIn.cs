using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ImageFadeIn : MonoBehaviour
{
	private Image img;

	private TMP_Text text;

	public float speed;

	public float maxAlpha = 1f;

	public bool startAt0;

	public UnityEvent onFull;

	private void Start()
	{
		img = GetComponent<Image>();
		text = GetComponent<TMP_Text>();
		if (maxAlpha == 0f)
		{
			maxAlpha = 1f;
		}
		if (startAt0)
		{
			if ((bool)img)
			{
				img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
			}
			if ((bool)text)
			{
				text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
			}
		}
	}

	private void Update()
	{
		if ((bool)img && img.color.a != maxAlpha)
		{
			Color color = img.color;
			color.a = Mathf.MoveTowards(color.a, maxAlpha, Time.deltaTime * speed);
			img.color = color;
			if (img.color.a == maxAlpha)
			{
				onFull?.Invoke();
			}
		}
		if ((bool)text && text.color.a != maxAlpha)
		{
			Color color2 = text.color;
			color2.a = Mathf.MoveTowards(color2.a, maxAlpha, Time.deltaTime * speed);
			text.color = color2;
			if (text.color.a == maxAlpha)
			{
				onFull?.Invoke();
			}
		}
	}

	public void ResetFade()
	{
		if ((bool)img)
		{
			img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
		}
		if ((bool)text)
		{
			text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
		}
	}

	public void CancelFade()
	{
		ResetFade();
		base.enabled = false;
	}
}
