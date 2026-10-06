using System.Collections.Generic;
using GameConsole.CommandTree;
using plog;

namespace GameConsole.Commands;

public class Prefs : CommandRoot, IConsoleLogger
{
	public Logger Log { get; } = new Logger("Prefs");

	public override string Name => "Prefs";

	public override string Description => "Interfaces with the PrefsManager.";

	public Prefs(Console con)
		: base(con)
	{
	}

	protected override Branch BuildTree(Console con)
	{
		return CommandRoot.Branch("prefs", CommandRoot.Branch("get", CommandRoot.Leaf("bool", (string key) =>
		{
			Log.Info($"{key} = {MonoSingleton<PrefsManager>.Instance.GetBool(key)}");
		}), CommandRoot.Leaf("int", (string key) =>
		{
			Log.Info($"{key} = {MonoSingleton<PrefsManager>.Instance.GetInt(key)}");
		}), CommandRoot.Leaf("float", (string key) =>
		{
			Log.Info($"{key} = {MonoSingleton<PrefsManager>.Instance.GetFloat(key)}");
		}), CommandRoot.Leaf("string", (string key) =>
		{
			Log.Info(key + " = " + MonoSingleton<PrefsManager>.Instance.GetString(key));
		})), CommandRoot.Branch("set", CommandRoot.Leaf("bool", (string key, bool value) =>
		{
			Log.Info($"Set {key} to {value}");
			MonoSingleton<PrefsManager>.Instance.SetBool(key, value);
		}), CommandRoot.Leaf("int", (string key, int value) =>
		{
			Log.Info($"Set {key} to {value}");
			MonoSingleton<PrefsManager>.Instance.SetInt(key, value);
		}), CommandRoot.Leaf("float", (string key, float value) =>
		{
			Log.Info($"Set {key} to {value}");
			MonoSingleton<PrefsManager>.Instance.SetFloat(key, value);
		}), CommandRoot.Leaf("string", (string key, string value) =>
		{
			Log.Info("Set " + key + " to " + value);
			MonoSingleton<PrefsManager>.Instance.SetString(key, value);
		})), CommandRoot.Branch("get_local", CommandRoot.Leaf("bool", (string key) =>
		{
			Log.Info($"{key} = {MonoSingleton<PrefsManager>.Instance.GetBoolLocal(key)}");
		}), CommandRoot.Leaf("int", (string key) =>
		{
			Log.Info($"{key} = {MonoSingleton<PrefsManager>.Instance.GetIntLocal(key)}");
		}), CommandRoot.Leaf("float", (string key) =>
		{
			Log.Info($"{key} = {MonoSingleton<PrefsManager>.Instance.GetFloatLocal(key)}");
		}), CommandRoot.Leaf("string", (string key) =>
		{
			Log.Info(key + " = " + MonoSingleton<PrefsManager>.Instance.GetStringLocal(key));
		})), CommandRoot.Branch("set_local", CommandRoot.Leaf("bool", (string key, bool value) =>
		{
			Log.Info($"Set {key} to {value}");
			MonoSingleton<PrefsManager>.Instance.SetBoolLocal(key, value);
		}), CommandRoot.Leaf("int", (string key, int value) =>
		{
			Log.Info($"Set {key} to {value}");
			MonoSingleton<PrefsManager>.Instance.SetIntLocal(key, value);
		}), CommandRoot.Leaf("float", (string key, float value) =>
		{
			Log.Info($"Set {key} to {value}");
			MonoSingleton<PrefsManager>.Instance.SetFloatLocal(key, value);
		}), CommandRoot.Leaf("string", (string key, string value) =>
		{
			Log.Info("Set " + key + " to " + value);
			MonoSingleton<PrefsManager>.Instance.SetStringLocal(key, value);
		})), CommandRoot.Leaf("delete", (string key) =>
		{
			Log.Info("Deleted " + key);
			MonoSingleton<PrefsManager>.Instance.DeleteKey(key);
		}), CommandRoot.Leaf("list_defaults", () =>
		{
			Log.Info("<b>Default Prefs:</b>");
			foreach (KeyValuePair<string, object> defaultValue in MonoSingleton<PrefsManager>.Instance.defaultValues)
			{
				Log.Info($"{defaultValue.Key} = {defaultValue.Value}");
			}
		}), CommandRoot.Leaf("list_cached", () =>
		{
			Log.Info("<b>Cached Prefs:</b>");
			foreach (KeyValuePair<string, object> item in MonoSingleton<PrefsManager>.Instance.prefMap)
			{
				Log.Info($"{item.Key} = {item.Value}");
			}
		}), CommandRoot.Leaf("list_cached_local", () =>
		{
			Log.Info("<b>Local Cached Prefs:</b>");
			foreach (KeyValuePair<string, object> item2 in MonoSingleton<PrefsManager>.Instance.localPrefMap)
			{
				Log.Info($"{item2.Key} = {item2.Value}");
			}
		}), CommandRoot.Leaf("last_played", () =>
		{
			Log.Info($"The game has been played {PrefsManager.monthsSinceLastPlayed} months ago last.\nThis is only valid per session.");
		}));
	}
}
