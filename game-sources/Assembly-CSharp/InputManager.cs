using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewBlood;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class InputManager : MonoSingleton<InputManager>
{
	private sealed class ButtonPressListener : IObserver<InputControl>
	{
		public static ButtonPressListener Instance { get; } = new ButtonPressListener();

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(InputControl value)
		{
			if (!(value.device is LegacyInput))
			{
				MonoSingleton<InputManager>.Instance.LastButtonDevice = value.device;
				if (MonoSingleton<InputManager>.Instance.IsRebinding && value.device is Gamepad)
				{
					MonoSingleton<InputManager>.Instance.CancelRebinding();
				}
			}
		}
	}

	private class BindingInfo
	{
		public InputAction Action;

		public string Name;

		public int Offset;

		public KeyCode DefaultKey;

		public string PrefName => "keyBinding." + Name;
	}

	public Dictionary<string, KeyCode> inputsDictionary = new Dictionary<string, KeyCode>();

	public InputActionAsset defaultActions;

	private IDisposable anyButtonListener;

	public bool ScrOn;

	public bool ScrWep;

	public bool ScrVar;

	public bool ScrRev;

	public Action<InputAction> actionModified;

	private BindingInfo[] legacyBindings;

	private InputActionRebindingExtensions.RebindingOperation rebinding;

	private Action currentCancelCallback;

	public PlayerInput InputSource { get; private set; }

	public InputDevice LastButtonDevice { get; private set; }

	public bool IsRebinding { get; private set; }

	private static IObservable<InputControl> onAnyInput => from e in InputSystem.onEvent
		select (e.type != 1398030676 && e.type != 1145852993) ? null : e.GetFirstButtonPressOrNull(-1f, buttonControlsOnly: false) into c
		where c != null && !c.noisy
		select c;

	public Dictionary<string, KeyCode> Inputs => inputsDictionary;

	private FileInfo savedBindingsFile => new FileInfo(Path.Combine(PrefsManager.PrefsPath, "Binds.json"));

	private void Awake()
	{
		InputSource = new PlayerInput();
		defaultActions = InputActionAsset.FromJson(InputSource.Actions.asset.ToJson());
		if (savedBindingsFile.Exists)
		{
			JsonConvert.DeserializeObject<JsonBindingMap>(File.ReadAllText(savedBindingsFile.FullName)).ApplyTo(InputSource.Actions.asset);
		}
		legacyBindings = new BindingInfo[20]
		{
			new BindingInfo
			{
				Action = InputSource.Move.Action,
				DefaultKey = KeyCode.W,
				Name = "W"
			},
			new BindingInfo
			{
				Action = InputSource.Move.Action,
				Offset = 1,
				DefaultKey = KeyCode.S,
				Name = "S"
			},
			new BindingInfo
			{
				Action = InputSource.Move.Action,
				Offset = 2,
				DefaultKey = KeyCode.A,
				Name = "A"
			},
			new BindingInfo
			{
				Action = InputSource.Move.Action,
				Offset = 3,
				DefaultKey = KeyCode.D,
				Name = "D"
			},
			new BindingInfo
			{
				Action = InputSource.Jump.Action,
				DefaultKey = KeyCode.Space,
				Name = "Jump"
			},
			new BindingInfo
			{
				Action = InputSource.Dodge.Action,
				DefaultKey = KeyCode.LeftShift,
				Name = "Dodge"
			},
			new BindingInfo
			{
				Action = InputSource.Slide.Action,
				DefaultKey = KeyCode.LeftControl,
				Name = "Slide"
			},
			new BindingInfo
			{
				Action = InputSource.Fire1.Action,
				DefaultKey = KeyCode.Mouse0,
				Name = "Fire1"
			},
			new BindingInfo
			{
				Action = InputSource.Fire2.Action,
				DefaultKey = KeyCode.Mouse1,
				Name = "Fire2"
			},
			new BindingInfo
			{
				Action = InputSource.Punch.Action,
				DefaultKey = KeyCode.F,
				Name = "Punch"
			},
			new BindingInfo
			{
				Action = InputSource.Hook.Action,
				DefaultKey = KeyCode.R,
				Name = "Hook"
			},
			new BindingInfo
			{
				Action = InputSource.LastWeapon.Action,
				DefaultKey = KeyCode.Q,
				Name = "LastUsedWeapon"
			},
			new BindingInfo
			{
				Action = InputSource.NextVariation.Action,
				DefaultKey = KeyCode.E,
				Name = "ChangeVariation"
			},
			new BindingInfo
			{
				Action = InputSource.ChangeFist.Action,
				DefaultKey = KeyCode.G,
				Name = "ChangeFist"
			},
			new BindingInfo
			{
				Action = InputSource.Slot1.Action,
				DefaultKey = KeyCode.Alpha1,
				Name = "Slot1"
			},
			new BindingInfo
			{
				Action = InputSource.Slot2.Action,
				DefaultKey = KeyCode.Alpha2,
				Name = "Slot2"
			},
			new BindingInfo
			{
				Action = InputSource.Slot3.Action,
				DefaultKey = KeyCode.Alpha3,
				Name = "Slot3"
			},
			new BindingInfo
			{
				Action = InputSource.Slot4.Action,
				DefaultKey = KeyCode.Alpha4,
				Name = "Slot4"
			},
			new BindingInfo
			{
				Action = InputSource.Slot5.Action,
				DefaultKey = KeyCode.Alpha5,
				Name = "Slot5"
			},
			new BindingInfo
			{
				Action = InputSource.Slot6.Action,
				DefaultKey = KeyCode.Alpha6,
				Name = "Slot6"
			}
		};
		UpgradeBindings();
		InputSource.Enable();
		ScrOn = MonoSingleton<PrefsManager>.Instance.GetBool("scrollEnabled");
		ScrWep = MonoSingleton<PrefsManager>.Instance.GetBool("scrollWeapons");
		ScrVar = MonoSingleton<PrefsManager>.Instance.GetBool("scrollVariations");
		ScrRev = MonoSingleton<PrefsManager>.Instance.GetBool("scrollReversed");
	}

	private void OnEnable()
	{
		anyButtonListener = onAnyInput.Subscribe(ButtonPressListener.Instance);
		PrefsManager.onPrefChanged = (Action<string, object>)Delegate.Combine(PrefsManager.onPrefChanged, new Action<string, object>(OnPrefChanged));
	}

	private void OnDisable()
	{
		anyButtonListener?.Dispose();
		SaveBindings(InputSource.Actions.asset);
		PrefsManager.onPrefChanged = (Action<string, object>)Delegate.Remove(PrefsManager.onPrefChanged, new Action<string, object>(OnPrefChanged));
	}

	private void OnPrefChanged(string key, object value)
	{
		switch (key)
		{
		case "scrollEnabled":
			ScrOn = (bool)value;
			break;
		case "scrollWeapons":
			ScrWep = (bool)value;
			break;
		case "scrollVariations":
			ScrVar = (bool)value;
			break;
		case "scrollReversed":
			ScrRev = (bool)value;
			break;
		}
	}

	public void ResetToDefault()
	{
		JsonBindingMap.From(defaultActions, InputSource.Actions.KeyboardMouseScheme).ApplyTo(InputSource.Actions.asset);
		InputSource.ValidateBindings(InputSource.Actions.KeyboardMouseScheme);
	}

	public void ResetToDefault(InputAction action, InputControlScheme controlScheme)
	{
		InputAction inputAction = defaultActions.FindAction(action.name);
		InputSource.Disable();
		action.WipeAction(controlScheme.bindingGroup);
		for (int i = 0; i < inputAction.bindings.Count; i++)
		{
			if (!inputAction.BindingHasGroup(i, controlScheme.bindingGroup))
			{
				continue;
			}
			InputBinding binding = inputAction.bindings[i];
			if (binding.isPartOfComposite)
			{
				continue;
			}
			if (binding.isComposite)
			{
				InputActionSetupExtensions.CompositeSyntax compositeSyntax = action.AddCompositeBinding("2DVector");
				for (int j = i + 1; j < inputAction.bindings.Count && inputAction.bindings[j].isPartOfComposite; j++)
				{
					InputBinding inputBinding = inputAction.bindings[j];
					compositeSyntax.With(inputBinding.name, inputBinding.path, controlScheme.bindingGroup);
				}
			}
			else
			{
				action.AddBinding(binding).WithGroup(controlScheme.bindingGroup);
			}
		}
		actionModified?.Invoke(action);
		SaveBindings(InputSource.Actions.asset);
		InputSource.Enable();
	}

	public bool PerformingCheatMenuCombo()
	{
		if (!MonoSingleton<CheatsController>.Instance.cheatsEnabled)
		{
			return false;
		}
		if (!(LastButtonDevice is Gamepad))
		{
			return false;
		}
		if (Gamepad.current == null)
		{
			return false;
		}
		return Gamepad.current.selectButton.isPressed;
	}

	public void SaveBindings(InputActionAsset asset)
	{
		JsonBindingMap value = JsonBindingMap.From(asset, defaultActions, InputSource.Actions.KeyboardMouseScheme);
		File.WriteAllText(savedBindingsFile.FullName, JsonConvert.SerializeObject(value, Formatting.Indented));
	}

	public void UpgradeBindings()
	{
		BindingInfo[] array = legacyBindings;
		foreach (BindingInfo bindingInfo in array)
		{
			InputBinding bindingMask = InputBinding.MaskByGroup("Keyboard & Mouse");
			int bindingIndex = bindingInfo.Action.GetBindingIndex(bindingMask);
			Inputs[bindingInfo.Name] = (KeyCode)MonoSingleton<PrefsManager>.Instance.GetInt(bindingInfo.PrefName, (int)bindingInfo.DefaultKey);
			if (bindingIndex != -1 && MonoSingleton<PrefsManager>.Instance.HasKey(bindingInfo.PrefName))
			{
				KeyCode key = (KeyCode)MonoSingleton<PrefsManager>.Instance.GetInt(bindingInfo.PrefName);
				MonoSingleton<PrefsManager>.Instance.DeleteKey(bindingInfo.PrefName);
				ButtonControl button = LegacyInput.current.GetButton(key);
				bindingInfo.Action.ChangeBinding(bindingIndex + bindingInfo.Offset).WithPath(button.path).WithGroup(InputSource.Actions.KeyboardMouseScheme.bindingGroup);
			}
		}
		foreach (InputAction action in InputSource.Actions)
		{
			foreach (InputBinding binding in action.bindings)
			{
				if (binding.path.Contains("LegacyInput"))
				{
					string path = binding.path.Replace("/LegacyInput/", "<Keyboard>/").Replace("alpha", "");
					if (InputSystem.FindControl(path) != null)
					{
						action.ChangeBinding(binding).WithPath(path);
					}
					else
					{
						action.ChangeBinding(binding).Erase();
					}
				}
			}
		}
	}

	public void CancelRebinding()
	{
		if (IsRebinding)
		{
			rebinding?.Cancel();
			rebinding?.Dispose();
			rebinding = null;
			currentCancelCallback?.Invoke();
			currentCancelCallback = null;
			IsRebinding = false;
			InputSource.Enable();
		}
	}

	public void WaitForButton(Action<string> onComplete, Action onCancel, List<string> allowedPaths = null)
	{
		InputSource.Disable();
		rebinding?.Cancel();
		rebinding?.Dispose();
		currentCancelCallback = onCancel;
		IsRebinding = true;
		rebinding = new InputActionRebindingExtensions.RebindingOperation().OnApplyBinding(delegate(InputActionRebindingExtensions.RebindingOperation op, string path)
		{
			rebinding = null;
			op.Dispose();
			IsRebinding = false;
			currentCancelCallback = null;
			if (InputControlPath.TryFindControl(Keyboard.current, path) == Keyboard.current.escapeKey)
			{
				onCancel?.Invoke();
			}
			else
			{
				onComplete?.Invoke(path);
			}
			InputSource.Enable();
		}).WithControlsExcluding(LegacyInput.current.path).WithExpectedControlType<ButtonControl>()
			.WithMatchingEventsBeingSuppressed();
		if (allowedPaths != null)
		{
			foreach (string allowedPath in allowedPaths)
			{
				rebinding.WithControlsHavingToMatchPath(allowedPath);
			}
		}
		rebinding.Start();
	}

	public void WaitForButtonSequence(Queue<string> partNames, Action<string> onBeginPart, Action<string, string> onCompletePart, Action onComplete, Action onCancel, List<string> allowedPaths = null)
	{
		if (partNames.Count == 0)
		{
			onComplete?.Invoke();
			return;
		}
		string part = partNames.Dequeue();
		onBeginPart?.Invoke(part);
		WaitForButton(delegate(string path)
		{
			onCompletePart?.Invoke(part, path);
			WaitForButtonSequence(partNames, onBeginPart, onCompletePart, onComplete, onCancel);
		}, onCancel, allowedPaths);
	}

	public void ClearOtherActions(InputAction action, string path)
	{
		foreach (InputAction action2 in InputSource.Actions)
		{
			if (action2 == action)
			{
				continue;
			}
			int bindingIndex = action2.GetBindingIndex(null, path);
			if (bindingIndex != -1)
			{
				InputActionSetupExtensions.BindingSyntax bindingSyntax = action2.ChangeBinding(bindingIndex);
				if (bindingSyntax.binding.isPartOfComposite)
				{
					bindingSyntax = bindingSyntax.PreviousCompositeBinding();
				}
				bindingSyntax.Erase();
			}
		}
	}

	public void Rebind(InputAction action, int? existingIndex, Action onComplete, Action onCancel, InputControlScheme scheme)
	{
		List<string> allowedPaths = scheme.deviceRequirements.Select((InputControlScheme.DeviceRequirement requirement) => requirement.controlPath).ToList();
		WaitForButton(delegate(string path)
		{
			foreach (InputBinding binding in action.bindings)
			{
				if (InputSystem.FindControl(binding.path) == InputSystem.FindControl(path))
				{
					onComplete?.Invoke();
					return;
				}
			}
			InputActionSetupExtensions.BindingSyntax bindingSyntax;
			if (existingIndex.HasValue)
			{
				int valueOrDefault = existingIndex.GetValueOrDefault();
				bindingSyntax = action.ChangeBinding(valueOrDefault);
			}
			else
			{
				bindingSyntax = action.AddBinding();
			}
			InputActionSetupExtensions.BindingSyntax bindingSyntax2 = bindingSyntax;
			bindingSyntax2.WithPath(path).WithGroup(scheme.bindingGroup);
			actionModified?.Invoke(action);
			onComplete?.Invoke();
		}, delegate
		{
			onCancel?.Invoke();
		}, allowedPaths);
	}

	public void RebindComposite(InputAction action, int? existingIndex, Action<string> onBeginPart, Action onComplete, Action onCancel, InputControlScheme scheme)
	{
		List<string> allowedPaths = scheme.deviceRequirements.Select((InputControlScheme.DeviceRequirement requirement) => requirement.controlPath).ToList();
		if (action.expectedControlType == "Vector2")
		{
			string[] collection = new string[4] { "Up", "Down", "Left", "Right" };
			Dictionary<string, string> partPathDict = new Dictionary<string, string>();
			WaitForButtonSequence(new Queue<string>(collection), onBeginPart, delegate(string part, string path)
			{
				partPathDict.Add(part, path);
			}, delegate
			{
				if (existingIndex.HasValue)
				{
					int valueOrDefault = existingIndex.GetValueOrDefault();
					InputActionSetupExtensions.BindingSyntax bindingSyntax = action.ChangeBinding(valueOrDefault);
					foreach (KeyValuePair<string, string> item in partPathDict)
					{
						bindingSyntax.NextPartBinding(item.Key).WithPath(item.Value).WithGroup(scheme.bindingGroup);
					}
				}
				else
				{
					InputActionSetupExtensions.CompositeSyntax compositeSyntax = action.AddCompositeBinding("2DVector");
					foreach (KeyValuePair<string, string> item2 in partPathDict)
					{
						compositeSyntax.With(item2.Key, item2.Value, scheme.bindingGroup);
					}
					action.AddBinding().Erase();
				}
				actionModified?.Invoke(action);
				onComplete?.Invoke();
			}, onCancel, allowedPaths);
		}
		else
		{
			Debug.LogError("Attempted to call RebindComposite on action with unsupported control type: '" + action.expectedControlType + "'");
		}
	}

	public string GetBindingString(Guid actionId)
	{
		return GetBindingString(actionId.ToString());
	}

	public string GetBindingString(string nameOrId)
	{
		ReadOnlyArray<InputBinding> bindings = InputSource.Actions.FindAction(nameOrId).bindings;
		string text = string.Empty;
		int num = 0;
		Queue<string> queue = new Queue<string>();
		InputControlScheme inputControlScheme = InputSource.Actions.KeyboardMouseScheme;
		foreach (InputControlScheme controlScheme in InputSource.Actions.controlSchemes)
		{
			if (controlScheme.SupportsDevice(LastButtonDevice))
			{
				inputControlScheme = controlScheme;
				break;
			}
		}
		for (int i = 0; i < bindings.Count; i++)
		{
			if (bindings[i].isComposite)
			{
				num = i;
				continue;
			}
			InputControl inputControl = InputSystem.FindControl(bindings[i].path);
			if (inputControl == null)
			{
				continue;
			}
			if (bindings[i].isPartOfComposite)
			{
				for (int j = num + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
				{
					if (j > num + 1)
					{
						text += " + ";
					}
					text += bindings[j].ToDisplayString() ?? "?";
				}
				return text;
			}
			if (inputControlScheme.SupportsDevice(inputControl.device))
			{
				return bindings[i].ToDisplayString();
			}
		}
		if (queue.Count == 0)
		{
			return "";
		}
		Debug.Log(queue.Count);
		string text2 = queue.Dequeue() ?? "";
		while (queue.Count > 0)
		{
			text2 = text2 + "/" + queue.Dequeue();
		}
		return text2;
	}
}
