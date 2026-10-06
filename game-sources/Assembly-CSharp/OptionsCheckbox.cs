using UnityEngine;
using UnityEngine.UI;

public class OptionsCheckbox : MonoBehaviour
{
	public Toggle toggle;

	public string prefsKey;

	private void Awake()
	{
		toggle.SetIsOnWithoutNotify(MonoSingleton<PrefsManager>.Instance.GetBool(prefsKey, toggle.isOn));
		toggle.onValueChanged.AddListener(OnChanged);
	}

	private void OnChanged(bool value)
	{
		MonoSingleton<PrefsManager>.Instance.SetBool(prefsKey, value);
	}
}
