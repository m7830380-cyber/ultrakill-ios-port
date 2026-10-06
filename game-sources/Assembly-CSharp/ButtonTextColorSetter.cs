using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonTextColorSetter : MonoBehaviour
{
	public bool onlyDisabledState;

	private Button button;

	private Graphic originalGraphic;

	private TMP_Text[] texts;

	private void Awake()
	{
		button = GetComponent<Button>();
		texts = GetComponentsInChildren<TMP_Text>();
		GameObject obj = new GameObject("CrossFadeColorProxy");
		obj.SetActive(value: false);
		obj.transform.SetParent(base.gameObject.transform, worldPositionStays: false);
		obj.transform.hideFlags = HideFlags.HideInHierarchy;
		CrossFadeColorProxy crossFadeColorProxy = obj.AddComponent<CrossFadeColorProxy>();
		obj.GetComponent<RectTransform>().sizeDelta = GetComponent<RectTransform>().sizeDelta;
		crossFadeColorProxy.setter = this;
		originalGraphic = button.targetGraphic;
		button.targetGraphic = crossFadeColorProxy;
	}

	public void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)
	{
		originalGraphic.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, useRGB);
		if (onlyDisabledState)
		{
			targetColor = (button.interactable ? button.colors.normalColor : button.colors.disabledColor);
			TMP_Text[] array = texts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].color = targetColor;
			}
		}
		else
		{
			TMP_Text[] array = texts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, useRGB);
			}
		}
	}
}
