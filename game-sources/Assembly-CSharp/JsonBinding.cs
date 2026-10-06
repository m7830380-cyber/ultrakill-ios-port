using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

public class JsonBinding
{
	public string path;

	[JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
	public bool isComposite;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public Dictionary<string, string> parts;

	private JsonBinding()
	{
	}

	public static List<JsonBinding> FromAction(InputAction action, string group)
	{
		List<JsonBinding> list = new List<JsonBinding>();
		for (int i = 0; i < action.bindings.Count; i++)
		{
			InputBinding inputBinding = action.bindings[i];
			JsonBinding jsonBinding = new JsonBinding();
			if (!action.BindingHasGroup(i, group))
			{
				continue;
			}
			if (inputBinding.isComposite)
			{
				jsonBinding.path = inputBinding.GetNameOfComposite();
				jsonBinding.isComposite = true;
				jsonBinding.parts = new Dictionary<string, string>();
				while (i + 1 < action.bindings.Count && action.bindings[i + 1].isPartOfComposite)
				{
					i++;
					InputBinding inputBinding2 = action.bindings[i];
					Debug.Log("BLEURHG " + inputBinding2.name);
					Debug.Log(inputBinding2.path);
					Debug.Log(inputBinding2.isPartOfComposite);
					jsonBinding.parts.Add(inputBinding2.name, inputBinding2.path);
				}
			}
			else
			{
				jsonBinding.path = inputBinding.path;
			}
			list.Add(jsonBinding);
		}
		return list;
	}
}
