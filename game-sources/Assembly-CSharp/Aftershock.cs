using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aftershock : MonoBehaviour
{
	public float delay;

	public float extraDamage;

	public bool waterOnly;

	public GameObject sourceWeapon;

	private List<GameObject> hitObjects = new List<GameObject>();

	public void ZapFrom(EnemyIdentifier eid)
	{
		hitObjects.Add(eid.gameObject);
		StartCoroutine(ZapCoroutine(eid));
	}

	private IEnumerator ZapCoroutine(EnemyIdentifier eid)
	{
		yield return new WaitForSeconds(delay);
		if ((bool)eid)
		{
			eid.PreShockBleed();
		}
		EnemyIdentifier.Zap(base.transform.position, extraDamage, hitObjects, sourceWeapon, eid, null, waterOnly);
		if ((bool)eid)
		{
			eid.AfterShock(extraDamage);
		}
		Object.Destroy(base.gameObject);
	}
}
