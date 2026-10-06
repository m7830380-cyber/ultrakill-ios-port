using System.Collections.Generic;
using UnityEngine.InputSystem;
using plog;

public class JsonBindingMap
{
	public static readonly Logger Log = new Logger("JsonBindingMap");

	public string controlScheme;

	public static Dictionary<string, string> bindAliases = new Dictionary<string, string>
	{
		{ "Slot 1", "Revolver" },
		{ "Slot 2", "Shotgun" },
		{ "Slot 3", "Nailgun" },
		{ "Slot 4", "Railcannon" },
		{ "Slot 5", "Rocket Launcher" },
		{ "Change Variation", "Next Variation" },
		{ "Last Weapon", "Last Used Weapon" }
	};

	public Dictionary<string, List<JsonBinding>> modifiedActions = new Dictionary<string, List<JsonBinding>>();

	public static JsonBindingMap From(InputActionAsset asset, InputControlScheme scheme)
	{
		JsonBindingMap jsonBindingMap = new JsonBindingMap
		{
			controlScheme = scheme.bindingGroup
		};
		foreach (InputAction item in asset)
		{
			jsonBindingMap.AddAction(item);
		}
		return jsonBindingMap;
	}

	public static JsonBindingMap From(InputActionAsset asset, InputActionAsset baseAsset, InputControlScheme scheme)
	{
		JsonBindingMap jsonBindingMap = new JsonBindingMap
		{
			controlScheme = scheme.bindingGroup
		};
		foreach (InputAction item in asset)
		{
			InputAction baseAction = baseAsset.FindAction(item.id);
			if (!item.IsActionEqual(baseAction, scheme.bindingGroup))
			{
				jsonBindingMap.AddAction(item);
			}
		}
		return jsonBindingMap;
	}

	public void ApplyTo(InputActionAsset asset)
	{
		foreach (KeyValuePair<string, List<JsonBinding>> modifiedAction in modifiedActions)
		{
			string text = modifiedAction.Key;
			List<JsonBinding> value = modifiedAction.Value;
			if (bindAliases.TryGetValue(text, out var value2))
			{
				text = value2;
			}
			InputAction inputAction = asset.FindAction(text);
			if (inputAction == null)
			{
				Log.Warning("Action " + text + " was found in saved bindings, but does not exist (action == null). Ignoring...");
				break;
			}
			inputAction.WipeAction(controlScheme);
			foreach (JsonBinding item in value)
			{
				if (item.isComposite)
				{
					if (item.parts.Count == 0)
					{
						continue;
					}
					InputActionSetupExtensions.CompositeSyntax compositeSyntax = inputAction.AddCompositeBinding(item.path);
					foreach (KeyValuePair<string, string> part in item.parts)
					{
						compositeSyntax.With(part.Key, part.Value, controlScheme);
					}
					inputAction.ChangeBinding(compositeSyntax.bindingIndex).WithGroup(controlScheme);
				}
				else
				{
					inputAction.AddBinding(item.path, null, null, controlScheme);
				}
			}
		}
	}

	public void AddAction(InputAction action)
	{
		modifiedActions.Add(action.name, JsonBinding.FromAction(action, controlScheme));
	}
}
