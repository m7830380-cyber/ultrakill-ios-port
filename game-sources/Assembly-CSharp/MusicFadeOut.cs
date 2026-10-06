using UnityEngine;

public class MusicFadeOut : MonoBehaviour
{
	public bool forceOff;

	public bool oneTime = true;

	private bool colliderless = true;

	private void Awake()
	{
		if (TryGetComponent<Collider>(out var _))
		{
			colliderless = false;
		}
	}

	private void Start()
	{
		if (colliderless)
		{
			Activate();
		}
	}

	private void OnEnable()
	{
		if (colliderless)
		{
			Activate();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject == MonoSingleton<NewMovement>.Instance.gameObject)
		{
			Activate();
		}
	}

	public void Activate()
	{
		MonoSingleton<MusicManager>.Instance.off = true;
		if (forceOff)
		{
			MonoSingleton<MusicManager>.Instance.forcedOff = true;
		}
		if (oneTime)
		{
			Object.Destroy(this);
		}
	}
}
