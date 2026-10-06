using UnityEngine;
using plog;

namespace GameConsole.Commands;

public class Exit : ICommand, IConsoleLogger
{
	public plog.Logger Log { get; } = new plog.Logger("Exit");

	public string Name => "Exit";

	public string Description => "Quits the game.";

	public string Command => Name.ToLower();

	public void Execute(Console con, string[] args)
	{
		Log.Info("Goodbye \ud83d\udc4b");
		Application.Quit();
	}
}
