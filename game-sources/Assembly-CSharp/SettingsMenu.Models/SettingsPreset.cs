using UnityEngine;
using plog;

namespace SettingsMenu.Models;

[CreateAssetMenu(fileName = "SettingsPreset", menuName = "ULTRAKILL/Settings/Preset")]
public class SettingsPreset : ScriptableObject
{
	private static readonly plog.Logger Log = new plog.Logger("SettingsPreset");

	public PreferenceEntry[] preferences;

	public void Apply()
	{
		Log.Info("Applying settings preset " + base.name);
		PreferenceEntry[] array = preferences;
		foreach (PreferenceEntry preferenceEntry in array)
		{
			Log.Info($"Applying preference {preferenceEntry}");
			preferenceEntry.Apply();
		}
	}
}
