using SettingsMenu.Models;
using UnityEngine;
using UnityEngine.UI;

namespace SettingsMenu.Components;

public class SettingsToggle : SettingsBuilderBase
{
	[SerializeField]
	private Toggle toggle;

	public override void ConfigureFrom(SettingsItemBuilder itemBuilder, SettingsPageBuilder pageBuilder)
	{
		SettingsItem asset = itemBuilder.asset;
		if (asset.preferenceKey.IsValid())
		{
			bool boolValue = asset.preferenceKey.GetBoolValue();
			toggle.SetIsOnWithoutNotify(boolValue);
		}
		toggle.onValueChanged.AddListener(itemBuilder.ValueChanged);
	}

	public override void SetSelected()
	{
		SettingsMenu.SetSelected(toggle);
	}

	public override void AttachRestoreDefaultButton(SettingsRestoreDefaultButton restoreDefaultButton)
	{
		restoreDefaultButton.toggle = toggle;
	}
}
