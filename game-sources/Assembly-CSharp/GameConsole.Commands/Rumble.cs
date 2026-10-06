using System.Collections.Generic;
using System.Reflection;
using GameConsole.CommandTree;
using plog;

namespace GameConsole.Commands;

public class Rumble : CommandRoot, IConsoleLogger
{
	public Logger Log { get; } = new Logger("Rumble");

	public override string Name => "Rumble";

	public override string Description => "Command for managing ULTRAKILL's controller rumble system";

	public Rumble(Console con)
		: base(con)
	{
	}

	protected override Branch BuildTree(Console con)
	{
		return CommandRoot.Branch("rumble", CommandRoot.Leaf("status", delegate
		{
			Log.Info($"Pending Vibrations ({MonoSingleton<RumbleManager>.Instance.pendingVibrations.Count}):");
			foreach (KeyValuePair<RumbleKey, PendingVibration> pendingVibration in MonoSingleton<RumbleManager>.Instance.pendingVibrations)
			{
				Log.Info($" - {pendingVibration.Key} ({pendingVibration.Value.Intensity}) for {pendingVibration.Value.Duration} seconds");
			}
			Log.Info(string.Empty);
			Log.Info($"Current Intensity: {MonoSingleton<RumbleManager>.Instance.currentIntensity}");
		}), CommandRoot.Leaf("list", delegate
		{
			Log.Info("Available Keys:");
			PropertyInfo[] properties = typeof(RumbleProperties).GetProperties();
			for (int i = 0; i < properties.Length; i++)
			{
				string text = properties[i].GetValue(null) as string;
				Log.Info(" - " + text);
			}
		}), CommandRoot.Leaf("vibrate", delegate(string key)
		{
			MonoSingleton<RumbleManager>.Instance.SetVibration(new RumbleKey(key));
		}), CommandRoot.Leaf("stop", delegate(string key)
		{
			MonoSingleton<RumbleManager>.Instance.StopVibration(new RumbleKey(key));
		}), CommandRoot.Leaf("stop_all", delegate
		{
			MonoSingleton<RumbleManager>.Instance.StopAllVibrations();
		}), CommandRoot.Leaf("toggle_preview", delegate
		{
			DebugUI.previewRumble = !DebugUI.previewRumble;
		}));
	}
}
