using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using plog;

[ConfigureSingleton(SingletonFlags.NoAutoInstance | SingletonFlags.PersistAutoInstance | SingletonFlags.DestroyDuplicates)]
public class AssetHelper : MonoSingleton<AssetHelper>
{
	private static readonly plog.Logger Log = new plog.Logger("AssetHelper");

	private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

	private void OnEnable()
	{
		Object.DontDestroyOnLoad(base.gameObject);
	}

	public static GameObject LoadPrefab(string address)
	{
		if (MonoSingleton<AssetHelper>.Instance.prefabCache.ContainsKey(address))
		{
			if (!(MonoSingleton<AssetHelper>.Instance.prefabCache[address] == null) && !MonoSingleton<AssetHelper>.Instance.prefabCache[address].Equals(null))
			{
				return MonoSingleton<AssetHelper>.Instance.prefabCache[address];
			}
			MonoSingleton<AssetHelper>.Instance.prefabCache.Remove(address);
		}
		GameObject gameObject = Addressables.LoadAssetAsync<GameObject>(address).WaitForCompletion();
		MonoSingleton<AssetHelper>.Instance.prefabCache.Add(address, gameObject);
		return gameObject;
	}

	public static GameObject LoadPrefab(AssetReference reference)
	{
		if (reference == null || reference.Equals(null) || reference.RuntimeKey == null || !reference.RuntimeKeyIsValid())
		{
			Log.Warning($"Missing asset reference.\nRuntime key: {reference.RuntimeKey}");
			return null;
		}
		string key = reference.RuntimeKey.ToString();
		if (MonoSingleton<AssetHelper>.Instance.prefabCache.ContainsKey(key))
		{
			if (!(MonoSingleton<AssetHelper>.Instance.prefabCache[key] == null) && !MonoSingleton<AssetHelper>.Instance.prefabCache[key].Equals(null))
			{
				return MonoSingleton<AssetHelper>.Instance.prefabCache[key];
			}
			MonoSingleton<AssetHelper>.Instance.prefabCache.Remove(key);
		}
		GameObject gameObject = reference.LoadAssetAsync<GameObject>().WaitForCompletion();
		MonoSingleton<AssetHelper>.Instance.prefabCache.Add(key, gameObject);
		return gameObject;
	}

	public static void SpawnPrefabAsync(string prefab, Vector3 position, Quaternion rotation)
	{
		MonoSingleton<AssetHelper>.Instance.StartCoroutine(MonoSingleton<AssetHelper>.Instance.LoadPrefab(prefab, position, rotation));
	}

	public IEnumerator LoadPrefab(string prefab, Vector3 position, Quaternion rotation)
	{
		AsyncOperationHandle<GameObject> loadOperation = Addressables.LoadAssetAsync<GameObject>(prefab);
		yield return loadOperation;
		Object.Instantiate(loadOperation.Result, position, rotation);
	}
}
