using System;
using System.Collections.Generic;
using SettingsMenu.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SettingsMenu.Components;

public class SettingsDropdown : SettingsBuilderBase
{
	[SerializeField]
	private TMP_Dropdown dropdown;

	private SettingsItem item;

	public TMP_Dropdown.DropdownEvent onValueChanged
	{
		get
		{
			return dropdown.onValueChanged;
		}
		set
		{
			dropdown.onValueChanged = value;
		}
	}

	public override void ConfigureFrom(SettingsItemBuilder itemBuilder, SettingsPageBuilder pageBuilder)
	{
		dropdown.ClearOptions();
		item = itemBuilder.asset;
		List<string> dropdownItems = item.GetDropdownItems();
		dropdown.AddOptions(dropdownItems);
		LoadCurrentValue();
		dropdown.onValueChanged.AddListener(itemBuilder.ValueChanged);
	}

	public override void SetSelected()
	{
		SettingsMenu.SetSelected(dropdown);
	}

	public override void AttachRestoreDefaultButton(SettingsRestoreDefaultButton restoreDefaultButton)
	{
		if (item.valueType != global::SettingsMenu.Models.ValueType.BoolCombination)
		{
			restoreDefaultButton.dropdown = dropdown;
			return;
		}
		DropdownCombinationRestoreDefaultButton dropdownCombinationRestoreDefaultButton = restoreDefaultButton.gameObject.AddComponent<DropdownCombinationRestoreDefaultButton>();
		dropdownCombinationRestoreDefaultButton.buttonContainer = restoreDefaultButton.buttonContainer;
		dropdownCombinationRestoreDefaultButton.dropdown = dropdown;
		dropdownCombinationRestoreDefaultButton.defaultCombination = item.defaultCombination;
		dropdownCombinationRestoreDefaultButton.combinations = item.combinationOptions;
		if (dropdownCombinationRestoreDefaultButton.buttonContainer.TryGetComponent<Button>(out var component))
		{
			component.onClick.RemoveAllListeners();
			component.onClick.AddListener(dropdownCombinationRestoreDefaultButton.RestoreDefault);
		}
		else
		{
			Debug.LogError("Button not found in container. Unable to register onClick event.");
		}
		UnityEngine.Object.Destroy(restoreDefaultButton);
	}

	public void SetDropdownItems(List<string> items, bool reloadValue = true)
	{
		dropdown.ClearOptions();
		dropdown.AddOptions(items);
		if (reloadValue)
		{
			LoadCurrentValue();
		}
	}

	public void SetDropdownValue(int value, bool notify = false)
	{
		if (notify)
		{
			dropdown.value = value;
		}
		else
		{
			dropdown.SetValueWithoutNotify(value);
		}
		dropdown.RefreshShownValue();
	}

	private void LoadCurrentValue()
	{
		switch (item.valueType)
		{
		case global::SettingsMenu.Models.ValueType.Int:
			if (item.preferenceKey.IsValid())
			{
				int intValue = item.preferenceKey.GetIntValue();
				dropdown.SetValueWithoutNotify(intValue);
			}
			break;
		case global::SettingsMenu.Models.ValueType.BoolCombination:
		{
			if (item.combinationOptions.Count != dropdown.options.Count)
			{
				throw new Exception("Dropdown items count does not match the combination options count");
			}
			int valueWithoutNotify = 0;
			for (int i = 0; i < item.combinationOptions.Count; i++)
			{
				DropdownCombinationRestoreDefaultButton.CombinationOption combinationOption = item.combinationOptions[i];
				bool flag = true;
				foreach (DropdownCombinationRestoreDefaultButton.BooleanPrefOption subOption in combinationOption.subOptions)
				{
					if (subOption.preferenceKey.GetBoolValue() != subOption.expectedValue)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					valueWithoutNotify = i;
					break;
				}
			}
			dropdown.SetValueWithoutNotify(valueWithoutNotify);
			break;
		}
		}
	}
}
