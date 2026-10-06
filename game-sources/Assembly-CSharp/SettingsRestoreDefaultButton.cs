using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsRestoreDefaultButton : MonoBehaviour
{
	public GameObject buttonContainer;

	public string settingKey;

	[Header("Float")]
	public Slider slider;

	public float valueToPrefMultiplier = 1f;

	public float sliderTolerance = 0.01f;

	public bool integerSlider;

	[Header("Integer")]
	public TMP_Dropdown dropdown;

	[Header("Boolean")]
	public Toggle toggle;

	[SerializeField]
	private UnityEvent customToggleEvent;

	private float? defaultFloat;

	private int? defaultInt;

	private bool? defaultBool;

	public void RestoreDefault()
	{
		customToggleEvent?.Invoke();
		if (defaultFloat.HasValue)
		{
			slider.value = defaultFloat.Value / valueToPrefMultiplier;
		}
		else if (defaultBool.HasValue)
		{
			toggle.isOn = defaultBool.Value;
		}
		else if (defaultInt.HasValue)
		{
			if (dropdown != null)
			{
				dropdown.value = defaultInt.Value;
			}
			if (integerSlider && slider != null)
			{
				slider.value = defaultInt.Value;
			}
		}
	}

	public void SetNavigation(Selectable mainSelectable)
	{
		Navigation navigation = mainSelectable.navigation;
		Selectable component = buttonContainer.GetComponent<Selectable>();
		navigation.mode = Navigation.Mode.Explicit;
		navigation.selectOnRight = component;
		mainSelectable.navigation = navigation;
		Navigation navigation2 = component.navigation;
		navigation2.mode = Navigation.Mode.Explicit;
		navigation2.selectOnLeft = mainSelectable;
		component.navigation = navigation2;
	}

	private void Start()
	{
		if (MonoSingleton<PrefsManager>.Instance.defaultValues.ContainsKey(settingKey))
		{
			object obj = MonoSingleton<PrefsManager>.Instance.defaultValues[settingKey];
			if (!(obj is float value))
			{
				if (!(obj is bool value2))
				{
					if (obj is int value3)
					{
						defaultInt = value3;
					}
				}
				else
				{
					defaultBool = value2;
				}
			}
			else
			{
				defaultFloat = value;
			}
		}
		if (slider != null)
		{
			if (integerSlider)
			{
				if (!defaultInt.HasValue)
				{
					defaultInt = 0;
				}
			}
			else if (!defaultFloat.HasValue)
			{
				defaultFloat = 0f;
			}
			slider.onValueChanged.AddListener((float _) =>
			{
				UpdateSelf();
			});
		}
		if (toggle != null)
		{
			if (!defaultBool.HasValue)
			{
				defaultBool = false;
			}
			toggle.onValueChanged.AddListener((bool _) =>
			{
				UpdateSelf();
			});
		}
		if (dropdown != null)
		{
			if (!defaultInt.HasValue)
			{
				defaultInt = 0;
			}
			dropdown.onValueChanged.AddListener((int _) =>
			{
				UpdateSelf();
			});
		}
		UpdateSelf();
	}

	private void UpdateSelf()
	{
		if (!defaultInt.HasValue && !defaultBool.HasValue && !defaultFloat.HasValue)
		{
			buttonContainer.SetActive(value: false);
		}
		else if (defaultFloat.HasValue && slider != null)
		{
			if (Math.Abs(defaultFloat.Value - slider.value * valueToPrefMultiplier) < sliderTolerance)
			{
				buttonContainer.SetActive(value: false);
			}
			else
			{
				buttonContainer.SetActive(value: true);
			}
		}
		else if (defaultBool.HasValue && toggle != null)
		{
			if (defaultBool.Value == toggle.isOn)
			{
				buttonContainer.SetActive(value: false);
			}
			else
			{
				buttonContainer.SetActive(value: true);
			}
		}
		else if (defaultInt.HasValue && (dropdown != null || (integerSlider && slider != null)))
		{
			int? num = ReadCurrentInt();
			if (!num.HasValue || defaultInt.Value == num)
			{
				buttonContainer.SetActive(value: false);
			}
			else
			{
				buttonContainer.SetActive(value: true);
			}
		}
	}

	private int? ReadCurrentInt()
	{
		if (dropdown != null)
		{
			return dropdown.value;
		}
		if (slider != null && integerSlider)
		{
			return (int)slider.value;
		}
		return null;
	}
}
