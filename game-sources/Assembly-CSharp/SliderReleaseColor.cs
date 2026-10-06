using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderReleaseColor : MonoBehaviour, IPointerUpHandler, IEventSystemHandler
{
	[SerializeField]
	private Color releaseColor;

	private Color defaultColor;

	private Selectable slider;

	private float fade;

	private void Awake()
	{
		slider = GetComponent<Selectable>();
		defaultColor = slider.targetGraphic.color;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		fade = slider.colors.fadeDuration;
	}

	private void Update()
	{
		if (fade != 0f)
		{
			fade = Mathf.MoveTowards(fade, 0f, Time.unscaledDeltaTime);
			slider.targetGraphic.color = Color.Lerp(defaultColor, releaseColor, fade / slider.colors.fadeDuration);
		}
	}
}
