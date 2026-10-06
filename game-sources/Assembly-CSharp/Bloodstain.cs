using UnityEngine;

public class Bloodstain : MonoBehaviour
{
	public int trackedIndex;

	private void OnDestroy()
	{
	}

	private void Update()
	{
		if (base.transform.hasChanged)
		{
			MonoSingleton<BloodsplatterManager>.Instance.props[trackedIndex] = default;
			base.transform.hasChanged = false;
		}
	}
}
