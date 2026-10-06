using System.Collections.Generic;
using UnityEngine;

public class SpinFromForce : MonoBehaviour
{
	public Vector3 rotationAxis = Vector3.up;

	public float angularDrag = 0.01f;

	public float mass = 1f;

	private Vector3 angularVelocity = Vector3.zero;

	private void Update()
	{
		angularVelocity *= 1f - angularDrag;
		Vector3 vector = base.transform.TransformDirection(rotationAxis);
		Quaternion quaternion = Quaternion.AngleAxis(Vector3.Dot(angularVelocity, vector) * Time.deltaTime, vector);
		base.transform.rotation = quaternion * base.transform.rotation;
	}

	public void AddSpin(ref List<ParticleCollisionEvent> pEvents)
	{
		foreach (ParticleCollisionEvent pEvent in pEvents)
		{
			Vector3 vector = Vector3.Cross(pEvent.intersection - base.transform.position, pEvent.velocity);
			angularVelocity += vector / mass;
			angularVelocity = Vector3.Project(angularVelocity, base.transform.TransformDirection(rotationAxis));
		}
	}
}
