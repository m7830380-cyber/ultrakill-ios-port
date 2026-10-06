using TMPro;
using UnityEngine;

public class OptionsDropdown : MonoBehaviour
{
	public TMP_Dropdown dropdown;

	public string prefName;

	private void Awake()
	{
		dropdown.SetValueWithoutNotify(MonoSingleton<PrefsManager>.Instance.GetInt(prefName, dropdown.value));
		dropdown.onValueChanged.AddListener(OnValueChanged);
	}

	private void OnValueChanged(int value)
	{
		MonoSingleton<PrefsManager>.Instance.SetInt(prefName, value);
	}
}
