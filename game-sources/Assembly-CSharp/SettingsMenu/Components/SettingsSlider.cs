using SettingsMenu.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using plog;

namespace SettingsMenu.Components;

public class SettingsSlider : SettingsBuilderBase
{
	private static readonly plog.Logger Log = new plog.Logger("SettingsSlider");

	[SerializeField]
	private Button containerButton;

	[SerializeField]
	private Slider slider;

	[SerializeField]
	private SliderValueToText sliderValueToText;

	private void Awake()
	{
		containerButton.onClick.AddListener(OnContainerButtonClicked);
	}

	private void OnContainerButtonClicked()
	{
		EventSystem.current.SetSelectedGameObject(slider.gameObject);
	}

	public override void ConfigureFrom(SettingsItemBuilder itemBuilder, SettingsPageBuilder pageBuilder)
	{
		if (slider == null)
		{
			return;
		}
		SettingsItem asset = itemBuilder.asset;
		if (asset.sliderConfig != null)
		{
			slider.minValue = asset.sliderConfig.minValue;
			slider.maxValue = asset.sliderConfig.maxValue;
			slider.wholeNumbers = asset.sliderConfig.wholeNumbers;
			if (asset.sliderConfig.textConfig != null)
			{
				sliderValueToText.ConfigureFrom(asset.sliderConfig.textConfig);
			}
			else
			{
				sliderValueToText.gameObject.SetActive(value: false);
				Log.Warning("No textConfig found for slider '" + asset.label + "'");
			}
		}
		if (asset.preferenceKey.IsValid())
		{
			float valueWithoutNotify = asset.preferenceKey.GetFloatValue() * asset.valueDisplayMultiplayer;
			slider.SetValueWithoutNotify(valueWithoutNotify);
		}
		slider.onValueChanged.AddListener(itemBuilder.ValueChanged);
	}

	public void SelectInnerSlider()
	{
		SettingsMenu.SetSelected(slider);
	}

	public override void SetSelected()
	{
		SettingsMenu.SetSelected(containerButton);
	}

	public override void AttachRestoreDefaultButton(SettingsRestoreDefaultButton restoreDefaultButton)
	{
		restoreDefaultButton.slider = slider;
		restoreDefaultButton.integerSlider = slider.wholeNumbers;
	}
}
