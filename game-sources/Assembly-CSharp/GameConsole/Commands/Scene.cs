using UnityEngine;
using plog;

namespace GameConsole.Commands;

public class Scene : ICommand, IConsoleLogger
{
	public plog.Logger Log { get; } = new plog.Logger("Scene");

	public string Name => "Scene";

	public string Description => "Loads a scene.";

	public string Command => "scene";

	public void Execute(Console con, string[] args)
	{
		if (con.CheatBlocker())
		{
			return;
		}
		if (args.Length == 0)
		{
			Log.Info("Usage: scene <scene name>");
			return;
		}
		string sceneName = string.Join(" ", args);
		if (!UnityEngine.Debug.isDebugBuild && MonoSingleton<SceneHelper>.Instance.IsSceneSpecial(sceneName))
		{
			Log.Info("Scene is special and cannot be loaded in release mode. \ud83e\udd7a");
		}
		else
		{
			SceneHelper.LoadScene(sceneName);
		}
	}
}
