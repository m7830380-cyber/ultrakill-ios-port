using UnityEngine;

[DefaultExecutionOrder(11000)]
public class LateUpdateParticles : MonoBehaviour
{
	private ParticleSystem part;

	private bool beenStarted;

	private void Awake()
	{
		part = GetComponent<ParticleSystem>();
	}

	private void LateUpdate()
	{
		if (part.isPlaying)
		{
			beenStarted = true;
		}
		if (beenStarted)
		{
			part.Simulate(Time.deltaTime, withChildren: true, restart: false, fixedTimeStep: false);
		}
	}
}
