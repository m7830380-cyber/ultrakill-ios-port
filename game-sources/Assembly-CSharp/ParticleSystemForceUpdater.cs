using System;
using UnityEngine;

public class ParticleSystemForceUpdater : MonoBehaviour
{
	private ParticleSystem ps;

	private ParticleSystem.Particle[] particles;

	private void Awake()
	{
		ps = GetComponent<ParticleSystem>();
		if (ps == null)
		{
			throw new NullReferenceException("ParticleSystemForceUpdater has no ParticleSystem component!");
		}
		particles = new ParticleSystem.Particle[ps.main.maxParticles];
	}

	public void ForceUpdate()
	{
		if ((bool)ps)
		{
			int size = ps.GetParticles(particles);
			ps.SetParticles(particles, size);
		}
	}
}
