using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

public class ThreadedParticleCollision : MonoBehaviour
{
	public ParticleSystem particles;

	public Bloodsplatter bloodsplatter;

	public NativeArray<RaycastCommand> raycasts;

	public NativeArray<RaycastHit> results;

	private CommandJob commandJob;

	private JobHandle handle;

	private List<Vector4> customData = new List<Vector4>();

	private BloodsplatterManager bsm;

	private static Matrix4x4 identityMatrix = Matrix4x4.identity;

	public event Action<NativeSlice<RaycastHit>> collisionEvent;

	private void Awake()
	{
		LayerMask layerMask = LayerMaskDefaults.Get(LMD.Environment);
		if (StockMapInfo.Instance.continuousGibCollisions)
		{
			layerMask = (int)layerMask | 0x10;
		}
		QueryTriggerInteraction hitTriggers = ((!StockMapInfo.Instance.continuousGibCollisions) ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
		commandJob.parameters = new QueryParameters(layerMask, hitMultipleFaces: false, hitTriggers);
		results = new NativeArray<RaycastHit>(particles.main.maxParticles, Allocator.Persistent);
		raycasts = new NativeArray<RaycastCommand>(particles.main.maxParticles, Allocator.Persistent);
		commandJob.raycasts = raycasts;
		commandJob.lastFrameHits = results;
	}

	private void OnEnable()
	{
		bsm = MonoSingleton<BloodsplatterManager>.Instance;
	}

	private void OnDisable()
	{
	}

	private void RegisterPortalData()
	{
	}

	private unsafe void Step(float dt)
	{
		if (!handle.IsCompleted)
		{
			return;
		}
		handle.Complete();
		if (results.IsCreated)
		{
			int particleCount = particles.particleCount;
			RaycastHit* unsafeBufferPointerWithoutChecks = (RaycastHit*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results);
			for (int i = 0; i < particleCount; i++)
			{
				RaycastHit hit = unsafeBufferPointerWithoutChecks[i];
				if (hit.colliderInstanceID != 0)
				{
					bloodsplatter.CreateBloodstain(in hit, bsm);
				}
			}
		}
		Transform transform = particles.transform;
		if (transform.hasChanged)
		{
			transform.hasChanged = false;
			if (particles.main.simulationSpace == ParticleSystemSimulationSpace.Local)
			{
				commandJob.transform = transform.localToWorldMatrix;
				commandJob.worldSpace = false;
			}
			else
			{
				commandJob.transform = identityMatrix;
				commandJob.worldSpace = true;
				commandJob.center = transform.position;
			}
		}
	}

	private void OnDestroy()
	{
		raycasts.Dispose(handle);
		results.Dispose(handle);
	}
}
