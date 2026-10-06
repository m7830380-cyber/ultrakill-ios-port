using ULTRAKILL.Cheats;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class HideUICheatGroup : MonoBehaviour
{
	private CanvasGroup canvasGroup;

	private void Awake()
	{
		if (TryGetComponent<CanvasGroup>(out canvasGroup))
		{
			canvasGroup.alpha = 0f;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
			canvasGroup.enabled = false;
		}
	}

	private void Update()
	{
		if (!(canvasGroup == null))
		{
			canvasGroup.enabled = HideUI.Active;
		}
	}
}
