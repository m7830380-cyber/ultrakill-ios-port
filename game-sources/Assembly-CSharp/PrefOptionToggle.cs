using UnityEngine;
using UnityEngine.UI;

public class PrefOptionToggle : MonoBehaviour
{
	public string prefName;

	public bool isLocal;

	public bool fallbackValue;

	public Toggle toggle;

	private void Awake()
	{
		toggle.SetIsOnWithoutNotify(GetPref());
		toggle.onValueChanged.AddListener(OnToggle);
	}

	private bool GetPref()
	{
		if (MonoSingleton<PrefsManager>.Instance == null)
		{
			return fallbackValue;
		}
		if (isLocal)
		{
			return MonoSingleton<PrefsManager>.Instance.GetBoolLocal(prefName, fallbackValue);
		}
		return MonoSingleton<PrefsManager>.Instance.GetBool(prefName, fallbackValue);
	}

	private void OnToggle(bool value)
	{
		if (!(MonoSingleton<PrefsManager>.Instance == null))
		{
			if (isLocal)
			{
				MonoSingleton<PrefsManager>.Instance.SetBoolLocal(prefName, value);
			}
			else
			{
				MonoSingleton<PrefsManager>.Instance.SetBool(prefName, value);
			}
		}
	}
}
