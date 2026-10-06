using UnityEngine;
using UnityEngine.UI;

public class ShopButtonListController : MonoBehaviour
{
	[SerializeField]
	private bool resetOnEnable = true;

	[SerializeField]
	private Button[] buttons;

	private void Start()
	{
		Button[] array = buttons;
		foreach (Button button in array)
		{
			button.GetComponent<ShopButton>().PointerClickSuccess += delegate
			{
				SetActiveButton(button);
			};
		}
	}

	private void SetActiveButton(Button specButton)
	{
		Button[] array = buttons;
		foreach (Button button in array)
		{
			if (button == specButton)
			{
				button.interactable = false;
				if (button.TryGetComponent<ShopButton>(out var component))
				{
					component.deactivated = true;
				}
			}
			else
			{
				button.interactable = true;
				if (button.TryGetComponent<ShopButton>(out var component2))
				{
					component2.deactivated = false;
				}
			}
		}
	}

	public void ResetButtons()
	{
		SetActiveButton(null);
	}
}
