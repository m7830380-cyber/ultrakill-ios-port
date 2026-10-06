using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlsOptionsKey : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler
{
	public TextMeshProUGUI actionText;

	public Button restoreDefaultsButton;

	public GameObject bindingButtonTemplate;

	public Transform bindingButtonParent;

	public Selectable selectable;

	public GameObject blocker;

	private List<Button> bindingButtons = new List<Button>();

	private bool selected;

	private readonly Color faintTextColor = new Color(1f, 1f, 1f, 0.15f);

	public void OnSelect(BaseEventData eventData)
	{
		selected = true;
	}

	public void OnDeselect(BaseEventData eventData)
	{
		selected = false;
	}

	private void SubmitPressed(InputAction.CallbackContext ctx)
	{
		if (selected && bindingButtons.Count > 0)
		{
			bindingButtons[0].Select();
		}
	}

	private void OnEnable()
	{
		MonoSingleton<InputManager>.Instance.InputSource.Actions.UI.Submit.performed += SubmitPressed;
	}

	private void OnDisable()
	{
		if ((bool)MonoSingleton<InputManager>.Instance)
		{
			MonoSingleton<InputManager>.Instance.InputSource.Actions.UI.Submit.performed -= SubmitPressed;
		}
	}

	private void Update()
	{
		bool active = MonoSingleton<InputManager>.Instance.LastButtonDevice is Gamepad;
		blocker.SetActive(active);
	}

	public void RebuildBindings(InputAction action, InputControlScheme controlScheme)
	{
		foreach (Button bindingButton in bindingButtons)
		{
			Object.Destroy(bindingButton.gameObject);
		}
		bindingButtons.Clear();
		int num = 0;
		int[] bindingsWithGroup = action.GetBindingsWithGroup(controlScheme.bindingGroup);
		foreach (int num2 in bindingsWithGroup)
		{
			InputBinding binding = action.bindings[num2];
			num++;
			string bindingDisplayStringWithoutOverride = action.GetBindingDisplayStringWithoutOverride(binding, InputBinding.DisplayStringOptions.DontIncludeInteractions);
			(Button, TextMeshProUGUI, Image, TooltipOnHover) tuple = BuildBindingButton(bindingDisplayStringWithoutOverride);
			Button item = tuple.Item1;
			TextMeshProUGUI txt = tuple.Item2;
			Image img = tuple.Item3;
			TooltipOnHover item2 = tuple.Item4;
			string text = txt.text + "<br>";
			bool flag = false;
			if (binding.isComposite)
			{
				InputActionSetupExtensions.BindingSyntax bindingSyntax = action.ChangeBinding(binding).NextBinding();
				HashSet<string> hashSet = new HashSet<string>();
				while (bindingSyntax.valid && bindingSyntax.binding.isPartOfComposite)
				{
					InputBinding[] conflicts = MonoSingleton<InputManager>.Instance.InputSource.GetConflicts(bindingSyntax.binding);
					if (conflicts.Length != 0 && !hashSet.Contains(bindingSyntax.binding.path))
					{
						flag = true;
						text = text + "<br>" + GenerateTooltip(action, bindingSyntax.binding, conflicts);
						hashSet.Add(bindingSyntax.binding.path);
					}
					bindingSyntax = bindingSyntax.NextBinding();
				}
			}
			else
			{
				InputBinding[] conflicts2 = MonoSingleton<InputManager>.Instance.InputSource.GetConflicts(binding);
				if (conflicts2.Length != 0)
				{
					flag = true;
					text = text + "<br>" + GenerateTooltip(action, binding, conflicts2);
				}
			}
			item2.text = text;
			item2.enabled = true;
			if (flag)
			{
				txt.color = Color.red;
			}
			int index = num2;
			item.onClick.AddListener(() =>
			{
				_ = img.color;
				img.color = Color.red;
				if (binding.isComposite)
				{
					MonoSingleton<InputManager>.Instance.RebindComposite(action, index, (string part) =>
					{
						txt.text = "PRESS " + part.ToUpper();
					}, () =>
					{
						RebuildBindings(action, controlScheme);
					}, () =>
					{
						action.ChangeBinding(index).Erase();
						MonoSingleton<InputManager>.Instance.actionModified?.Invoke(action);
					}, controlScheme);
				}
				else
				{
					MonoSingleton<InputManager>.Instance.Rebind(action, index, () =>
					{
						RebuildBindings(action, controlScheme);
					}, () =>
					{
						action.ChangeBinding(index).Erase();
						MonoSingleton<InputManager>.Instance.actionModified?.Invoke(action);
					}, controlScheme);
				}
			});
		}
		if (num < 4)
		{
			var (button, txt2, img2) = BuildNewBindButton();
			button.onClick.AddListener(() =>
			{
				img2.color = Color.red;
				txt2.color = Color.white;
				txt2.text = "...";
				if (action.expectedControlType == "Button")
				{
					MonoSingleton<InputManager>.Instance.Rebind(action, null, () =>
					{
						RebuildBindings(action, controlScheme);
					}, () =>
					{
						RebuildBindings(action, controlScheme);
					}, controlScheme);
				}
				else if (action.expectedControlType == "Vector2")
				{
					MonoSingleton<InputManager>.Instance.RebindComposite(action, null, (string part) =>
					{
						txt2.text = "PRESS " + part.ToUpper();
					}, () =>
					{
						RebuildBindings(action, controlScheme);
					}, () =>
					{
						RebuildBindings(action, controlScheme);
					}, controlScheme);
				}
			});
		}
		bool flag2 = action.IsActionEqual(MonoSingleton<InputManager>.Instance.defaultActions.FindAction(action.id), controlScheme.bindingGroup);
		restoreDefaultsButton.gameObject.SetActive(!flag2);
		restoreDefaultsButton.onClick.RemoveAllListeners();
		restoreDefaultsButton.onClick.AddListener(() =>
		{
			MonoSingleton<InputManager>.Instance.ResetToDefault(action, controlScheme);
			RebuildBindings(action, controlScheme);
		});
		Navigation navigation = selectable.navigation;
		navigation.mode = Navigation.Mode.Explicit;
		navigation.selectOnRight = bindingButtons[0];
		selectable.navigation = navigation;
	}

	private (Button, TextMeshProUGUI, Image) BuildNewBindButton()
	{
		(Button, TextMeshProUGUI, Image, TooltipOnHover) tuple = BuildBindingButton("+");
		Button item = tuple.Item1;
		TextMeshProUGUI item2 = tuple.Item2;
		Image item3 = tuple.Item3;
		item2.color = faintTextColor;
		item2.fontSizeMax = 27f;
		return (item, item2, item3);
	}

	private string GenerateTooltip(InputAction action, InputBinding binding, InputBinding[] conflicts)
	{
		string text = action.GetBindingDisplayStringWithoutOverride(binding, InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpper();
		string text2 = "<color=red>" + text + " IS BOUND MULTIPLE TIMES:";
		HashSet<string> hashSet = new HashSet<string>();
		for (int i = 0; i < conflicts.Length; i++)
		{
			InputBinding inputBinding = conflicts[i];
			if (!hashSet.Contains(inputBinding.action))
			{
				text2 += "<br>";
				text2 = text2 + "- " + inputBinding.action.ToUpper();
				hashSet.Add(inputBinding.action);
			}
		}
		return text2 + "</color>";
	}

	private (Button, TextMeshProUGUI, Image, TooltipOnHover) BuildBindingButton(string text)
	{
		GameObject obj = Object.Instantiate(bindingButtonTemplate, bindingButtonParent);
		TextMeshProUGUI componentInChildren = obj.GetComponentInChildren<TextMeshProUGUI>();
		Button component = obj.GetComponent<Button>();
		Image component2 = obj.GetComponent<Image>();
		TooltipOnHover component3 = obj.GetComponent<TooltipOnHover>();
		componentInChildren.text = text;
		bindingButtons.Add(component);
		obj.SetActive(value: true);
		return (component, componentInChildren, component2, component3);
	}
}
