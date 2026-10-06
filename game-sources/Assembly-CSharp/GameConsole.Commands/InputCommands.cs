using GameConsole.CommandTree;
using UnityEngine.InputSystem;
using plog;

namespace GameConsole.Commands;

internal class InputCommands : CommandRoot, IConsoleLogger
{
	public Logger Log { get; } = new Logger("Input");

	public override string Name => "Input";

	public override string Description => "Modify inputs";

	public InputCommands(Console con)
		: base(con)
	{
	}

	protected override Branch BuildTree(Console con)
	{
		return CommandRoot.Branch("input", CommandRoot.Branch("mouse", CommandRoot.Leaf("sensitivity", delegate(float amount)
		{
			Log.Info($"Set mouse sensitivity to {amount}");
			MonoSingleton<PrefsManager>.Instance.SetFloatLocal("mouseSensitivity", amount);
		})), CommandRoot.Leaf("bindings", delegate(string name)
		{
			InputAction inputAction = MonoSingleton<InputManager>.Instance.InputSource.Actions.FindAction(name);
			if (inputAction == null)
			{
				Log.Error("No action found with name or id '" + name + "'");
				return;
			}
			Log.Info("'" + name + "' has the following bindings:");
			foreach (InputBinding binding in inputAction.bindings)
			{
				if (binding.isPartOfComposite)
				{
					Log.Info("-- " + binding.path);
				}
				else
				{
					Log.Info("- " + binding.path);
				}
			}
		}));
	}
}
