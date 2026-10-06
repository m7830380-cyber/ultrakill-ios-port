using System.Collections.Generic;
using UnityEngine.InputSystem;
using plog;

namespace GameConsole;

public class Binds
{
	public Dictionary<string, InputActionState> registeredBinds;

	public Dictionary<string, string> defaultBinds = new Dictionary<string, string>
	{
		{ "open", "/Keyboard/f8" },
		{ "submit", "/Keyboard/enter" },
		{ "command_history_up", "/Keyboard/upArrow" },
		{ "command_history_down", "/Keyboard/downArrow" },
		{ "scroll_up", "/Keyboard/pageUp" },
		{ "scroll_down", "/Keyboard/pageUp" },
		{ "scroll_to_bottom", "/Keyboard/home" },
		{ "scroll_to_top", "/Keyboard/end" },
		{ "autocomplete", "/Keyboard/tab" }
	};

	private Logger Log { get; } = new Logger("Binds");

	public bool OpenPressed => SafeWasPerformed("open");

	public bool SubmitPressed => SafeWasPerformed("submit");

	public bool AutocompletePressed => SafeWasPerformed("autocomplete");

	public bool CommandHistoryUpPressed => SafeWasPerformed("command_history_up");

	public bool CommandHistoryDownPressed => SafeWasPerformed("command_history_down");

	public bool ScrollUpPressed => SafeWasPerformed("scroll_up");

	public bool ScrollDownPressed => SafeWasPerformed("scroll_down");

	public bool ScrollToBottomPressed => SafeWasPerformed("scroll_to_bottom");

	public bool ScrollToTopPressed => SafeWasPerformed("scroll_to_top");

	public bool ScrollUpHeld => SafeIsHeld("scroll_up");

	public bool ScrollDownHeld => SafeIsHeld("scroll_down");

	private bool SafeWasPerformed(string key)
	{
		if (registeredBinds != null && registeredBinds.TryGetValue(key, out var value))
		{
			return value.WasPerformedThisFrame;
		}
		return false;
	}

	private bool SafeIsHeld(string key)
	{
		if (registeredBinds != null && registeredBinds.TryGetValue(key, out var value))
		{
			return value.IsPressed;
		}
		return false;
	}

	public void Initialize()
	{
		Log.Parent = Console.Log;
		registeredBinds = new Dictionary<string, InputActionState>();
		foreach (KeyValuePair<string, string> defaultBind in defaultBinds)
		{
			InputActionState inputActionState = new InputActionState(new InputAction(defaultBind.Key));
			registeredBinds.Add(defaultBind.Key, inputActionState);
			inputActionState.Action.AddBinding(MonoSingleton<PrefsManager>.Instance.GetString("consoleBinding." + defaultBind.Key, defaultBind.Value)).WithGroup("Keyboard");
			inputActionState.Action.Enable();
		}
	}

	public void Rebind(string key, string bind)
	{
		if (!defaultBinds.ContainsKey(key))
		{
			Log.Info("Invalid console bind key: " + key);
			return;
		}
		string key2 = "consoleBinding." + key;
		MonoSingleton<PrefsManager>.Instance.SetString(key2, bind);
		if (registeredBinds.TryGetValue(key, out var value))
		{
			value.Action.Disable();
			value.Action.Dispose();
		}
		value = new InputActionState(new InputAction(key));
		registeredBinds[key] = value;
		value.Action.AddBinding(bind).WithGroup("Keyboard");
		value.Action.Enable();
		MonoSingleton<Console>.Instance.UpdateDisplayString();
	}
}
