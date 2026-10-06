using ULTRAKILL.Portal;
using plog;

namespace GameConsole.Commands;

public class PortalOcclusion : ICommand, IConsoleLogger
{
	public Logger Log { get; } = new Logger("PortalOcclusion");

	public string Name => "PortalOcclusion";

	public string Description => "Enables or disables portal occlusion culling.";

	public string Command => "portalocclusion";

	public void Execute(Console con, string[] args)
	{
		if (con.CheatBlocker())
		{
			return;
		}
		if (args.Length != 1 || !TryParseBool(args[0], out var value))
		{
			Log.Info("Usage: portalocclusion <true|false>");
			return;
		}
		PortalManagerV2 instance = MonoSingleton<PortalManagerV2>.Instance;
		if (instance == null)
		{
			Log.Warning("PortalManagerV2 instance not found.");
			return;
		}
		instance.SetPortalOcclusion(value);
		Log.Info("Portal occlusion " + (value ? "enabled" : "disabled") + ".");
	}

	private static bool TryParseBool(string s, out bool value)
	{
		switch (s.Trim().ToLowerInvariant())
		{
		case "1":
		case "true":
		case "on":
		case "enable":
		case "enabled":
			value = true;
			return true;
		case "0":
		case "false":
		case "off":
		case "disable":
		case "disabled":
			value = false;
			return true;
		default:
			return bool.TryParse(s, out value);
		}
	}
}
