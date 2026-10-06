using System;
using System.Collections.Generic;
using System.Linq;
using SettingsMenu.Components;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlsOptions : MonoBehaviour
{
	private InputManager inman;

	[HideInInspector]
	public OptionsManager opm;

	public List<ActionDisplayConfig> actionConfig;

	private Dictionary<Guid, ActionDisplayConfig> idConfigDict;

	public Transform actionParent;

	public GameObject actionTemplate;

	public GameObject sectionTemplate;

	private GameObject currentKey;

	public Color normalColor;

	public Color pressedColor;

	private bool canUnpause;

	public SettingsPageBuilder settingsPageBuilder;

	public TooltipManager tooltipManager;

	private List<GameObject> rebindUIObjects = new List<GameObject>();

	public GameObject modalBackground;

	public void ShowModal()
	{
		modalBackground.SetActive(value: true);
	}

	public void HideModal()
	{
		modalBackground.SetActive(value: false);
	}

	private void Awake()
	{
		inman = MonoSingleton<InputManager>.Instance;
		opm = MonoSingleton<OptionsManager>.Instance;
		idConfigDict = actionConfig.ToDictionary((ActionDisplayConfig config) => config.actionRef.action.id);
	}

	private void OnEnable()
	{
		Rebuild(MonoSingleton<InputManager>.Instance.InputSource.Actions.KeyboardMouseScheme);
		InputManager? instance = MonoSingleton<InputManager>.Instance;
		instance.actionModified = (Action<InputAction>)Delegate.Combine(instance.actionModified, new Action<InputAction>(OnActionChanged));
	}

	private void OnDisable()
	{
		if (currentKey != null)
		{
			if (opm == null)
			{
				opm = MonoSingleton<OptionsManager>.Instance;
			}
			currentKey.GetComponent<Image>().color = normalColor;
			currentKey = null;
			if ((bool)opm)
			{
				opm.dontUnpause = false;
			}
		}
		if ((bool)MonoSingleton<InputManager>.Instance)
		{
			InputManager? instance = MonoSingleton<InputManager>.Instance;
			instance.actionModified = (Action<InputAction>)Delegate.Remove(instance.actionModified, new Action<InputAction>(OnActionChanged));
		}
	}

	public void OnActionChanged(InputAction action)
	{
		Rebuild(MonoSingleton<InputManager>.Instance.InputSource.Actions.KeyboardMouseScheme);
	}

	public void ResetToDefault()
	{
		inman.ResetToDefault();
	}

	private void Rebuild(InputControlScheme controlScheme)
	{
		MonoSingleton<InputManager>.Instance.InputSource.ValidateBindings(MonoSingleton<InputManager>.Instance.InputSource.Actions.KeyboardMouseScheme);
		foreach (GameObject rebindUIObject in rebindUIObjects)
		{
			UnityEngine.Object.Destroy(rebindUIObject);
		}
		rebindUIObjects.Clear();
		InputActionMap[] obj = new InputActionMap[4]
		{
			inman.InputSource.Actions.Movement,
			inman.InputSource.Actions.Weapon,
			inman.InputSource.Actions.Fist,
			inman.InputSource.Actions.HUD
		};
		Debug.Log("Rebuilding");
		Selectable selectable = settingsPageBuilder.GetLastSelectable();
		Selectable firstSelectable = settingsPageBuilder.GetFirstSelectable();
		InputActionMap[] array = obj;
		foreach (InputActionMap inputActionMap in array)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(sectionTemplate, actionParent);
			gameObject.GetComponent<TextMeshProUGUI>().text = "-- " + inputActionMap.name.ToUpper() + " --";
			gameObject.SetActive(value: true);
			rebindUIObjects.Add(gameObject);
			foreach (InputAction item in inputActionMap)
			{
				if (item.expectedControlType != "Button" && item.expectedControlType != "Vector2")
				{
					continue;
				}
				Debug.Log("Building " + item.name);
				if (item == inman.InputSource.Look.Action || item == inman.InputSource.WheelLook.Action)
				{
					continue;
				}
				bool flag = true;
				if (idConfigDict.TryGetValue(item.id, out var value))
				{
					if (value.hidden)
					{
						continue;
					}
					if (!string.IsNullOrEmpty(value.requiredWeapon) && GameProgressSaver.CheckGear(value.requiredWeapon) == 0)
					{
						flag = false;
					}
				}
				GameObject gameObject2 = UnityEngine.Object.Instantiate(actionTemplate, actionParent);
				ControlsOptionsKey component = gameObject2.GetComponent<ControlsOptionsKey>();
				if (selectable != null)
				{
					Navigation navigation = selectable.navigation;
					navigation.mode = Navigation.Mode.Explicit;
					navigation.selectOnDown = component.selectable;
					selectable.navigation = navigation;
					Navigation navigation2 = component.selectable.navigation;
					navigation2.mode = Navigation.Mode.Explicit;
					navigation2.selectOnUp = selectable;
					component.selectable.navigation = navigation2;
				}
				component.actionText.text = (flag ? item.name.ToUpper() : "???");
				component.RebuildBindings(item, controlScheme);
				component.selectable.gameObject.AddComponent<ControllerDisallowedSelection>().fallbackSelectable = firstSelectable;
				Debug.Log("Rebuilt", gameObject2);
				rebindUIObjects.Add(gameObject2);
				gameObject2.SetActive(value: true);
				selectable = component.selectable;
			}
		}
		if (tooltipManager != null)
		{
			TooltipOnHover[] componentsInChildren = actionParent.GetComponentsInChildren<TooltipOnHover>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].tooltipManager = tooltipManager;
			}
		}
	}

	private void LateUpdate()
	{
		if (canUnpause)
		{
			if (opm == null)
			{
				opm = MonoSingleton<OptionsManager>.Instance;
			}
			canUnpause = false;
			opm.dontUnpause = false;
		}
	}

	public void ScrollOn(bool stuff)
	{
		if (inman == null)
		{
			inman = MonoSingleton<InputManager>.Instance;
		}
		if (stuff)
		{
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollEnabled", content: true);
			inman.ScrOn = true;
		}
		else
		{
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollEnabled", content: false);
			inman.ScrOn = false;
		}
	}

	public void ScrollVariations(int stuff)
	{
		if (inman == null)
		{
			inman = MonoSingleton<InputManager>.Instance;
		}
		switch (stuff)
		{
		case 0:
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollWeapons", content: true);
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollVariations", content: false);
			inman.ScrWep = true;
			inman.ScrVar = false;
			break;
		case 1:
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollWeapons", content: false);
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollVariations", content: true);
			inman.ScrWep = false;
			inman.ScrVar = true;
			break;
		default:
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollWeapons", content: true);
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollVariations", content: true);
			inman.ScrWep = true;
			inman.ScrVar = true;
			break;
		}
	}

	public void ScrollReverse(bool stuff)
	{
		if (inman == null)
		{
			inman = MonoSingleton<InputManager>.Instance;
		}
		if (stuff)
		{
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollReversed", content: true);
			inman.ScrRev = true;
		}
		else
		{
			MonoSingleton<PrefsManager>.Instance.SetBool("scrollReversed", content: false);
			inman.ScrRev = false;
		}
	}
}
