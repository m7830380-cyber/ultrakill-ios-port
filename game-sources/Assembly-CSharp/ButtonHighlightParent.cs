using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlightParent : MonoBehaviour
{
	private Image[] buttons;

	private TMP_Text[] buttonTexts;

	private Sprite[] buttonSprites;

	[SerializeField]
	private Sprite pressedVersion;

	[SerializeField]
	private Image targetOnStart;

	private void Start()
	{
		buttons = GetComponentsInChildren<Image>();
		buttonTexts = new TMP_Text[buttons.Length];
		buttonSprites = new Sprite[buttons.Length];
		for (int i = 0; i < buttons.Length; i++)
		{
			buttonTexts[i] = buttons[i].GetComponentInChildren<TMP_Text>();
			buttonSprites[i] = buttons[i].sprite;
		}
		if ((bool)targetOnStart)
		{
			ChangeButton(targetOnStart);
		}
	}

	public void ChangeButton(Image target)
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			if (!(buttons[i] == null))
			{
				if ((bool)pressedVersion)
				{
					buttons[i].sprite = ((buttons[i] == target) ? pressedVersion : buttonSprites[i]);
				}
				else
				{
					buttons[i].fillCenter = buttons[i] == target;
				}
				if (buttonTexts[i] != null)
				{
					buttonTexts[i].color = ((buttons[i] == target) ? Color.black : Color.white);
				}
			}
		}
	}
}
