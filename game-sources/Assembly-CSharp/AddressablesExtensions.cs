using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using plog;

public static class AddressablesExtensions
{
	private static readonly plog.Logger Log = new plog.Logger("AddressablesExtensions");

	public static GameObject ToAsset(this AssetReference reference)
	{
		return AssetHelper.LoadPrefab(reference);
	}

	public static GameObject[] ToAssets(this AssetReference[] references)
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < references.Length; i++)
		{
			if (references[i] == null || !references[i].RuntimeKeyIsValid())
			{
				Log.Warning($"Invalid asset reference at index {i}.");
				continue;
			}
			GameObject gameObject = references[i].ToAsset();
			if (gameObject == null || gameObject.Equals(null))
			{
				Log.Warning($"Failed to load asset at index {i}.\nRuntime key: {references[i].RuntimeKey}");
			}
			else
			{
				list.Add(gameObject);
			}
		}
		return list.ToArray();
	}
}
