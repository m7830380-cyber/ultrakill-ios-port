using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuNavigationBuilder : MonoBehaviour
{
	[SerializeField]
	private Selectable topSelectable;

	[SerializeField]
	private Selectable[] selectables;

	[SerializeField]
	private Selectable bottomSelectable;

	[SerializeField]
	private bool loopAround = true;

	private GamepadObjectSelector gamepadObjectSelector;

	private void Start()
	{
		gamepadObjectSelector = GetComponent<GamepadObjectSelector>();
		StartCoroutine(BuildNavigationDelayed());
	}

	private IEnumerator BuildNavigationDelayed()
	{
		yield return null;
		BuildNavigation();
	}

	private void BuildNavigation()
	{
		List<Selectable> list = new List<Selectable>();
		if (topSelectable != null)
		{
			list.Add(topSelectable);
		}
		Selectable[] array = selectables;
		foreach (Selectable selectable in array)
		{
			if (!(selectable == null) && selectable.gameObject.activeInHierarchy)
			{
				list.Add(selectable);
			}
		}
		if (bottomSelectable != null)
		{
			list.Add(bottomSelectable);
		}
		if (list.Count == 0)
		{
			return;
		}
		for (int j = 0; j < list.Count; j++)
		{
			Navigation navigation = list[j].navigation;
			navigation.mode = Navigation.Mode.Explicit;
			if (j > 0)
			{
				navigation.selectOnUp = list[j - 1];
			}
			else if (loopAround)
			{
				navigation.selectOnUp = list[list.Count - 1];
			}
			if (j < list.Count - 1)
			{
				navigation.selectOnDown = list[j + 1];
			}
			else if (loopAround)
			{
				navigation.selectOnDown = list[0];
			}
			list[j].navigation = navigation;
		}
		foreach (Selectable item in list)
		{
			Transform parent = item.transform.parent;
			if (!(parent == null))
			{
				SettingsRestoreDefaultButton componentInChildren = parent.GetComponentInChildren<SettingsRestoreDefaultButton>(includeInactive: true);
				if (!(componentInChildren == null) && !(componentInChildren.transform.parent != parent))
				{
					componentInChildren.SetNavigation(item);
				}
			}
		}
		if (gamepadObjectSelector != null)
		{
			gamepadObjectSelector.SetMainTarget(list[0]);
		}
	}
}
