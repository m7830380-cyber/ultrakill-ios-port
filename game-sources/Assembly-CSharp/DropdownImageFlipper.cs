using UnityEngine;
using UnityEngine.UI;

public class DropdownImageFlipper : MonoBehaviour
{
	[SerializeField]
	private Image targetImage;

	[SerializeField]
	private Sprite flippedSprite;

	private RectTransform rect;

	private void Awake()
	{
		if (!targetImage)
		{
			targetImage = GetComponent<Image>();
		}
		rect = GetComponent<RectTransform>();
	}

	private void Update()
	{
		if (rect.pivot.y < 0.5f)
		{
			targetImage.sprite = flippedSprite;
			float y = targetImage.rectTransform.offsetMin.y;
			targetImage.rectTransform.offsetMin = new Vector2(targetImage.rectTransform.offsetMin.x, 0f - targetImage.rectTransform.offsetMax.y);
			targetImage.rectTransform.offsetMax = new Vector2(targetImage.rectTransform.offsetMax.x, y);
			base.enabled = false;
		}
	}
}
