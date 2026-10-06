using System.Collections.Generic;
using System.Linq;
using GameConsole.CommandTree;
using plog;

namespace GameConsole.Commands;

public class ConsoleCmd : CommandRoot, IConsoleLogger
{
	public Logger Log { get; } = new Logger("ConsoleCmd");

	public override string Name => "Console";

	public override string Description => "Used for configuring the console";

	public ConsoleCmd(Console con)
		: base(con)
	{
	}

	protected override Branch BuildTree(Console con)
	{
		return CommandRoot.Branch("console", BoolMenu("hide_badge", () => con.errorBadge.hidden, delegate(bool value)
		{
			con.errorBadge.SetEnabled(value);
		}), BoolMenu("force_stacktrace_extraction", () => con.ExtractStackTraces, con.SetForceStackTraceExtraction), CommandRoot.Leaf("change_bind", delegate(string bind, string key)
		{
			if (con.binds.defaultBinds.ContainsKey(bind.ToLower()))
			{
				con.binds.Rebind(bind.ToLower(), key);
			}
			else
			{
				Log.Error(bind.ToLower() + " is not a valid bind.");
				Log.Info("Listing valid binds:");
				ListDefaults(con);
			}
		}, requireCheats: true), CommandRoot.Leaf("list_binds", delegate
		{
			Log.Info("Listing binds:");
			foreach (KeyValuePair<string, InputActionState> registeredBind in con.binds.registeredBinds)
			{
				Log.Info(registeredBind.Key + "  -  " + registeredBind.Value.Action.bindings.First().path);
			}
		}), CommandRoot.Leaf("reset", delegate
		{
			MonoSingleton<Console>.Instance.consoleWindow.ResetWindow();
		}));
	}

	private void ListDefaults(Console con)
	{
		foreach (KeyValuePair<string, string> defaultBind in con.binds.defaultBinds)
		{
			Log.Info(defaultBind.Key + "  -  " + defaultBind.Value);
		}
	}
}
