using Sandbox;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.AddressableAssets;
using plog;

public class SandboxAltar : MonoBehaviour, IAlter, IAlterOptions<bool>, IAlterOptions<int>
{
	private static readonly plog.Logger Log = new plog.Logger("SandboxAltar");

	public AltarType altarType;

	private AssetReference[] altarPrefabs;

	[SerializeField]
	private GameObject skullPrefab;

	[SerializeField]
	private Transform defaultLocation;

	[SerializeField]
	private Collider altarTrigger;

	[SerializeField]
	private Altars altars;

	private bool hasSkull;

	private GameObject skull;

	public string alterKey => "ultrakill.sandbox.altar";

	public string alterCategoryName => "Sandbox";

	AlterOption<bool>[] IAlterOptions<bool>.options => new AlterOption<bool>[1]
	{
		new AlterOption<bool>
		{
			name = "Has Skull",
			key = "hasSkull",
			value = hasSkull,
			callback = SetSkullActive
		}
	};

	AlterOption<int>[] IAlterOptions<int>.options => new AlterOption<int>[1]
	{
		new AlterOption<int>
		{
			name = "Altar Type",
			key = "altarType",
			value = (int)altarType,
			type = typeof(AltarType),
			callback = (int value) =>
			{
				if (value != (int)altarType)
				{
					altarType = (AltarType)value;
					Log.Info($"Changing altar type to {altarType}");
					GameObject gameObject = Object.Instantiate(altars.altarPrefabs[value].ToAsset(), base.transform.position, base.transform.rotation, base.transform.parent);
					SandboxAltar component = gameObject.GetComponent<SandboxAltar>();
					component.altarType = altarType;
					component.SetSkullActive(hasSkull);
					MonoSingleton<SandboxAlterMenu>.Instance.alterInstance.OpenProp(gameObject.GetComponent<SpawnableInstance>());
					Object.Destroy(base.gameObject);
				}
			}
		}
	};

	private void Awake()
	{
		skullPrefab.SetActive(value: false);
	}

	public void CreateSkull()
	{
		if (!hasSkull && !skull)
		{
			GameObject gameObject = Object.Instantiate(skullPrefab, defaultLocation.position, defaultLocation.rotation, defaultLocation.parent);
			gameObject.transform.localScale = skullPrefab.transform.localScale;
			skullPrefab.SetActive(value: false);
			gameObject.SetActive(value: true);
			skull = gameObject;
			hasSkull = true;
			if (altarTrigger != null)
			{
				altarTrigger.enabled = false;
			}
		}
	}

	public void SetSkullActive(bool active)
	{
		Log.Info("Setting skull to " + active);
		skullPrefab.SetActive(value: false);
		if (active)
		{
			CreateSkull();
		}
		else
		{
			RemoveSkull();
		}
	}

	public void RemoveSkull()
	{
		if (hasSkull)
		{
			Log.Info("Deleting skull");
			Object.Destroy(skull);
			hasSkull = false;
			if (altarTrigger != null)
			{
				altarTrigger.enabled = true;
			}
		}
	}
}
