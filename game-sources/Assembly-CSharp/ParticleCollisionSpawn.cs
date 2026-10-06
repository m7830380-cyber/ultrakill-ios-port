using System.Collections.Generic;
using UnityEngine;

public class ParticleCollisionSpawn : MonoBehaviour
{
	private ParticleSystem part;

	private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

	public GameObject toSpawn;

	private void OnParticleCollision(GameObject other)
	{
		if (part == null)
		{
			part = GetComponent<ParticleSystem>();
		}
		part.GetCollisionEvents(other, collisionEvents);
		if (collisionEvents.Count > 0)
		{
			Object.Instantiate(toSpawn, collisionEvents[0].intersection, Quaternion.LookRotation(collisionEvents[0].normal)).SetActive(value: true);
		}
	}
}
