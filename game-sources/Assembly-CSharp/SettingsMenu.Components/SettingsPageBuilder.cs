using System;
using System.Collections.Generic;
using SettingsMenu.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SettingsMenu.Components;

[DefaultExecutionOrder(-100)]
public class SettingsPageBuilder : MonoBehaviour
{
	[SerializeField]
	public SettingsMenuAssets assets;

	[Space]
	[SerializeField]
	private SettingsPage page;

	[SerializeField]
	private Transform targetContainer;

	public Selectable navigationButtonSelectable;

	public Selectable[] customTopSelectables;

	public Selectable[] customBottomSelectables;

	[Space]
	[FormerlySerializedAs("buttonCallbacks")]
	public List<SettingsButtonEvent> buttonEvents;

	public List<SettingsGroupInterrupt> groupInterrupts;

	private bool pageBuilt;

	private bool selectAfterBuild;

	private GamepadObjectSelector gamepadObjectSelector;

	private Dictionary<SettingsItem, SettingsBuilderBase> createdInstances;

	private Dictionary<SettingsGroup, List<ISettingsGroupUser>> groups;

	private List<Selectable> selectableRows;

	private void Awake()
	{
		gamepadObjectSelector = GetComponent<GamepadObjectSelector>();
		if (!(page == null))
		{
			BuildPage(page);
		}
	}

	private void Start()
	{
		PrefsManager.onPrefChanged = (Action<string, object>)Delegate.Combine(PrefsManager.onPrefChanged, new Action<string, object>(OnPrefChanged));
	}

	private void OnDestroy()
	{
		PrefsManager.onPrefChanged = (Action<string, object>)Delegate.Remove(PrefsManager.onPrefChanged, new Action<string, object>(OnPrefChanged));
	}

	private void OnPrefChanged(string key, object value)
	{
		foreach (KeyValuePair<SettingsGroup, List<ISettingsGroupUser>> group in groups)
		{
			if (group.Key.preferenceKey.key == key)
			{
				bool groupEnabled = group.Key.GetEnabled();
				UpdateGroupUsers(group.Key, groupEnabled);
			}
		}
	}

	private void OnValidate()
	{
		if (page == null)
		{
			return;
		}
		if (buttonEvents == null)
		{
			buttonEvents = new List<SettingsButtonEvent>();
		}
		List<SettingsItem> buttonItems = new List<SettingsItem>();
		SettingsCategory[] categories = page.categories;
		for (int i = 0; i < categories.Length; i++)
		{
			foreach (SettingsItem item2 in categories[i].items)
			{
				if (item2.itemType == SettingsItemType.Button)
				{
					buttonItems.Add(item2);
				}
			}
		}
		buttonEvents.RemoveAll((SettingsButtonEvent x) => !buttonItems.Contains(x.buttonItem));
		foreach (SettingsItem item in buttonItems)
		{
			if (buttonEvents.Find((SettingsButtonEvent x) => x.buttonItem == item).buttonItem == null)
			{
				buttonEvents.Add(new SettingsButtonEvent
				{
					buttonItem = item,
					onClickEvent = new UnityEvent()
				});
			}
		}
	}

	private void BuildPage(SettingsPage settingsPage)
	{
		if (targetContainer == null)
		{
			return;
		}
		createdInstances = new Dictionary<SettingsItem, SettingsBuilderBase>();
		groups = new Dictionary<SettingsGroup, List<ISettingsGroupUser>>();
		selectableRows = new List<Selectable>();
		foreach (Transform item in targetContainer)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		SettingsCategory[] categories = settingsPage.categories;
		foreach (SettingsCategory settingsCategory in categories)
		{
			SettingsCategoryBuilder settingsCategoryBuilder = UnityEngine.Object.Instantiate(assets.categoryTitlePrefab, targetContainer);
			string label = settingsCategory.GetLabel(capitalize: false);
			settingsCategoryBuilder.gameObject.name = label;
			settingsCategoryBuilder.ConfigureFrom(settingsCategory, this);
			foreach (SettingsItem item2 in settingsCategory.items)
			{
				if (item2.platformRequirements == null || item2.platformRequirements.Check())
				{
					SettingsItemBuilder settingsItemBuilder = UnityEngine.Object.Instantiate(assets.itemPrefab, targetContainer);
					settingsItemBuilder.ConfigureFrom(item2, settingsCategory, this);
					settingsItemBuilder.name = item2.GetLabel(capitalize: false);
				}
			}
		}
		foreach (KeyValuePair<SettingsGroup, List<ISettingsGroupUser>> group in groups)
		{
			bool flag = group.Key.GetEnabled();
			foreach (ISettingsGroupUser item3 in group.Value)
			{
				item3.UpdateGroupStatus(flag, group.Key.togglingMode);
			}
		}
		RefreshSelectableNavigation();
		pageBuilt = true;
		if (selectAfterBuild)
		{
			SetSelected();
			selectAfterBuild = false;
		}
	}

	public void SetSelected()
	{
		if (!pageBuilt)
		{
			selectAfterBuild = true;
		}
		else if ((bool)gamepadObjectSelector)
		{
			gamepadObjectSelector.Activate();
			gamepadObjectSelector.SetTop();
		}
	}

	public void RefreshSelectableNavigation()
	{
		List<Selectable> list = new List<Selectable>(selectableRows);
		list.InsertRange(0, customTopSelectables);
		list.AddRange(customBottomSelectables);
		if (list.Count == 0)
		{
			return;
		}
		Selectable selectable = null;
		Selectable selectable2 = null;
		foreach (Selectable item in list)
		{
			if (!(item == null) && item.gameObject.activeInHierarchy && item.IsInteractable())
			{
				if (selectable == null)
				{
					selectable = item;
				}
				if (selectable2 != null)
				{
					Navigation navigation = selectable2.navigation;
					navigation.selectOnDown = item;
					selectable2.navigation = navigation;
					navigation.mode = Navigation.Mode.Explicit;
					Navigation navigation2 = item.navigation;
					navigation2.mode = Navigation.Mode.Explicit;
					navigation2.selectOnUp = selectable2;
					item.navigation = navigation2;
				}
				selectable2 = item;
			}
		}
		if (selectable != null && selectable2 != null)
		{
			Navigation navigation3 = selectable.navigation;
			navigation3.selectOnUp = selectable2;
			selectable.navigation = navigation3;
			Navigation navigation4 = selectable2.navigation;
			navigation4.selectOnDown = selectable;
			selectable2.navigation = navigation4;
		}
		if (gamepadObjectSelector != null && selectable != null)
		{
			gamepadObjectSelector.SetMainTarget(selectable);
		}
	}

	public void AddBuilderInstance(SettingsBuilderBase builder, SettingsItem item)
	{
		if (createdInstances == null)
		{
			createdInstances = new Dictionary<SettingsItem, SettingsBuilderBase>();
		}
		createdInstances[item] = builder;
	}

	public void AddToGroup(SettingsGroup group, ISettingsGroupUser builder)
	{
		if (groups == null)
		{
			groups = new Dictionary<SettingsGroup, List<ISettingsGroupUser>>();
		}
		if (!groups.ContainsKey(group))
		{
			groups[group] = new List<ISettingsGroupUser>();
		}
		groups[group].Add(builder);
	}

	public void AddSelectableRow(Selectable selectable)
	{
		if (selectableRows == null)
		{
			selectableRows = new List<Selectable>();
		}
		selectableRows.Add(selectable);
	}

	public Selectable GetFirstSelectable()
	{
		if (selectableRows == null)
		{
			return null;
		}
		foreach (Selectable selectableRow in selectableRows)
		{
			if (!(selectableRow == null) && selectableRow.gameObject.activeInHierarchy && selectableRow.IsInteractable())
			{
				return selectableRow;
			}
		}
		return null;
	}

	public Selectable GetLastSelectable()
	{
		if (selectableRows == null)
		{
			return null;
		}
		for (int num = selectableRows.Count - 1; num >= 0; num--)
		{
			Selectable selectable = selectableRows[num];
			if (!(selectable == null) && selectable.gameObject.activeInHierarchy && selectable.IsInteractable())
			{
				return selectable;
			}
		}
		return null;
	}

	public void ConfirmGroupEnabled(SettingsGroup group)
	{
		SetGroupEnabled(group, groupEnabled: true, noInterrupts: true);
	}

	public void SetGroupEnabled(SettingsGroup group, bool groupEnabled, bool noInterrupts = false)
	{
		List<SettingsGroupInterrupt> list = groupInterrupts;
		if (list != null && list.Count > 0 && groupEnabled && !noInterrupts)
		{
			foreach (SettingsGroupInterrupt groupInterrupt in groupInterrupts)
			{
				if (!(groupInterrupt.group != group))
				{
					groupInterrupt.onEnableEvent.Invoke();
					if (groupInterrupt.suppressDefaultEnable)
					{
						return;
					}
				}
			}
		}
		group.SetEnabledBool(groupEnabled);
	}

	private void UpdateGroupUsers(SettingsGroup group, bool groupEnabled)
	{
		if (groups == null || !groups.TryGetValue(group, out var value))
		{
			return;
		}
		foreach (ISettingsGroupUser item in value)
		{
			item.UpdateGroupStatus(groupEnabled, group.togglingMode);
		}
		RefreshSelectableNavigation();
	}

	public bool TryGetItemBuilderInstance<T>(SettingsItem item, out T builder) where T : SettingsBuilderBase
	{
		builder = null;
		if (createdInstances == null)
		{
			return false;
		}
		if (createdInstances.TryGetValue(item, out var value))
		{
			builder = value as T;
			return builder != null;
		}
		return false;
	}

	public void SetSelectedItem(SettingsItem item)
	{
		if (createdInstances != null && createdInstances.TryGetValue(item, out var value))
		{
			value.SetSelected();
		}
	}
}
