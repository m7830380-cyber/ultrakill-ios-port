using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SaveSlotMenu : MonoBehaviour
{
	public class SlotData
	{
		public bool exists;

		public int highestLvlNumber;

		public int highestDifficulty;

		public override string ToString()
		{
			if (!exists)
			{
				return "EMPTY";
			}
			return GetMissionName.GetMission(highestLvlNumber) + " " + ((highestLvlNumber <= 0) ? string.Empty : ("(" + MonoSingleton<PresenceController>.Instance.diffNames[highestDifficulty] + ")"));
		}
	}

	public const int Slots = 5;

	private static readonly Color ActiveColor = new Color(1f, 0.66f, 0f);

	[SerializeField]
	private SlotRowPanel templateRow;

	[SerializeField]
	private Button closeButton;

	[FormerlySerializedAs("consentWrapper")]
	[SerializeField]
	private GameObject reloadConsentWrapper;

	[SerializeField]
	private TMP_Text wipeConsentContent;

	[SerializeField]
	private GameObject wipeConsentWrapper;

	private int queuedSlot;

	private SlotRowPanel[] slots;

	private void OnEnable()
	{
		if (slots != null)
		{
			return;
		}
		List<SlotRowPanel> list = new List<SlotRowPanel>();
		SlotData[] array = GameProgressSaver.GetSlots();
		for (int i = 0; i < 5; i++)
		{
			SlotRowPanel newRow = Object.Instantiate(templateRow, templateRow.transform.parent);
			newRow.slotIndex = i;
			newRow.gameObject.SetActive(value: true);
			UpdateSlotState(newRow, array[i]);
			newRow.selectButton.onClick.AddListener(() =>
			{
				SelectSlot(newRow.slotIndex);
			});
			newRow.deleteButton.onClick.AddListener(() =>
			{
				ClearSlot(newRow.slotIndex);
			});
			list.Add(newRow);
		}
		for (int num = 0; num < 5; num++)
		{
			list[num].selectButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnUp = ((num > 0) ? list[num - 1].selectButton : null),
				selectOnDown = ((num + 1 < 5) ? list[num + 1].selectButton : closeButton),
				selectOnLeft = list[num].deleteButton,
				selectOnRight = list[num].deleteButton
			};
			list[num].deleteButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnUp = ((num > 0) ? list[num - 1].deleteButton : null),
				selectOnDown = ((num + 1 < 5) ? list[num + 1].deleteButton : closeButton),
				selectOnLeft = list[num].selectButton,
				selectOnRight = list[num].selectButton
			};
		}
		closeButton.navigation = new Navigation
		{
			mode = Navigation.Mode.Explicit,
			selectOnUp = list[4].selectButton,
			selectOnDown = list[0].selectButton
		};
		slots = list.ToArray();
		templateRow.gameObject.SetActive(value: false);
	}

	public void ReloadMenu()
	{
		if (slots != null)
		{
			SlotRowPanel[] array = slots;
			for (int i = 0; i < array.Length; i++)
			{
				Object.Destroy(array[i].gameObject);
			}
			slots = null;
		}
		wipeConsentWrapper.SetActive(value: false);
		reloadConsentWrapper.SetActive(value: false);
		OnEnable();
	}

	private void UpdateSlotState(SlotRowPanel targetPanel, SlotData data)
	{
		bool flag = GameProgressSaver.currentSlot == targetPanel.slotIndex;
		targetPanel.backgroundPanel.color = (flag ? ActiveColor : Color.black);
		targetPanel.slotNumberLabel.color = (flag ? Color.black : (data.exists ? Color.white : Color.red));
		targetPanel.stateLabel.color = (flag ? Color.black : (data.exists ? Color.white : Color.red));
		targetPanel.selectButton.interactable = !flag;
		targetPanel.selectButton.GetComponentInChildren<TMP_Text>().text = (flag ? "SELECTED" : "SELECT");
		targetPanel.deleteButton.interactable = data.exists;
		targetPanel.slotNumberLabel.text = $"Slot {targetPanel.slotIndex + 1}";
		targetPanel.stateLabel.text = data.ToString();
	}

	public void ConfirmReload()
	{
		GameProgressSaver.SetSlot(queuedSlot);
		SceneHelper.LoadScene("Main Menu");
	}

	public void ConfirmWipe()
	{
		int currentSlot = GameProgressSaver.currentSlot;
		GameProgressSaver.WipeSlot(queuedSlot);
		if (currentSlot == queuedSlot)
		{
			SceneHelper.LoadScene("Main Menu");
		}
		else
		{
			ReloadMenu();
		}
	}

	public void CancelConsent()
	{
		reloadConsentWrapper.SetActive(value: false);
		wipeConsentWrapper.SetActive(value: false);
	}

	private void SelectSlot(int slot)
	{
		queuedSlot = slot;
		reloadConsentWrapper.SetActive(value: true);
	}

	private void ClearSlot(int slot)
	{
		queuedSlot = slot;
		wipeConsentContent.text = $"Are you sure you want to <color=red>DELETE SLOT {slot + 1}</color>?";
		wipeConsentWrapper.SetActive(value: true);
	}
}
