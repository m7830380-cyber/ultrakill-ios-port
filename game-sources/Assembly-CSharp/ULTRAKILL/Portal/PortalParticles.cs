using System;
using System.Collections.Generic;
using Interop;
using PrivateAPIBridge;
using ULTRAKILL.Portal.Native;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

namespace ULTRAKILL.Portal;

public class PortalParticles : IDisposable
{
	public PortalScene scene;

	public readonly System.Collections.Generic.List<PortalAwareParticleSystem> systems = new System.Collections.Generic.List<PortalAwareParticleSystem>();

	public NativeList<RaycastCommand> commands;

	public NativeList<IntersectionAndIndex> intersections;

	public NativeList<RaycastHit> hits;

	public NativeList<JobHandle> commandJobs;

	public NativeList<BloodsplatterMetadata> bloodMeta;

	public JobHandle createBloodHandle;

	private bool disposed;

	public void Initialize(PortalScene scene)
	{
		this.scene = scene;
		systems.Clear();
		commands = new NativeList<RaycastCommand>(Allocator.Persistent);
		intersections = new NativeList<IntersectionAndIndex>(Allocator.Persistent);
		hits = new NativeList<RaycastHit>(Allocator.Persistent);
		commandJobs = new NativeList<JobHandle>(Allocator.Persistent);
		bloodMeta = new NativeList<BloodsplatterMetadata>(Allocator.Persistent);
	}

	public unsafe void ScheduleJobs(ref NativeList<JobHandle> portalDependent)
	{
		CompleteJobs();
		if (MonoSingleton<BloodsplatterManager>.TryGetInstance(out BloodsplatterManager instance) && instance.stainCreateQueue.IsCreated && !(StockMapInfo.Instance == null) && scene != null && !disposed)
		{
			ref NativePortalScene nativeScene = ref scene.nativeScene;
			int count = systems.Count;
			int num = 0;
			for (int i = 0; i < count; i++)
			{
				PortalAwareParticleSystem portalAwareParticleSystem = systems[i];
				portalAwareParticleSystem.responseHandle.Complete();
				int particleCount = portalAwareParticleSystem._system.particleCount;
				((Interop.ParticleSystem*)(void*)portalAwareParticleSystem._system.GetCachedPtr())->m_UpdateFence.Complete();
				particleCount = portalAwareParticleSystem._system.particleCount;
				num += particleCount;
			}
			commands.Resize(num, NativeArrayOptions.ClearMemory);
			intersections.Resize(num, NativeArrayOptions.ClearMemory);
			hits.Resize(num, NativeArrayOptions.ClearMemory);
			bloodMeta.Resize(num, NativeArrayOptions.ClearMemory);
			NativeArray<RaycastCommand> nativeArray = commands.AsArray();
			NativeArray<IntersectionAndIndex> nativeArray2 = intersections.AsArray();
			NativeArray<RaycastHit> nativeArray3 = hits.AsArray();
			LayerMask layerMask = LayerMaskDefaults.Get(LMD.Environment);
			bool continuousGibCollisions = StockMapInfo.Instance.continuousGibCollisions;
			if (continuousGibCollisions)
			{
				layerMask = (int)layerMask | 0x10;
			}
			QueryParameters parameters = new QueryParameters(layerMask, hitMultipleFaces: false, (!continuousGibCollisions) ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide, hitBackfaces: true);
			int num2 = 0;
			ParticleCommandJob jobData = new ParticleCommandJob
			{
				parameters = parameters
			};
			ParticleResponseJob jobData2 = new ParticleResponseJob
			{
				scene = nativeScene,
				intersections = nativeArray2,
				hits = nativeArray3
			};
			Span<BloodsplatterMetadata> span = bloodMeta.AsArray().AsSpan();
			for (int j = 0; j < count; j++)
			{
				PortalAwareParticleSystem portalAwareParticleSystem2 = systems[j];
				portalAwareParticleSystem2._system.AllocateCustomDataAttribute(ParticleSystemCustomData.Custom1);
				int particleCount2 = portalAwareParticleSystem2._system.particleCount;
				NativeSlice<RaycastCommand> raycasts = nativeArray.Slice(num2, particleCount2);
				bool flag = portalAwareParticleSystem2.blood;
				span.Slice(num2, particleCount2).Fill(new BloodsplatterMetadata
				{
					exists = flag,
					halfChance = (flag && portalAwareParticleSystem2.blood.halfChance),
					instanceId = (flag ? portalAwareParticleSystem2.blood.GetInstanceID() : (-1))
				});
				bool flag2 = portalAwareParticleSystem2._system.main.simulationSpace == ParticleSystemSimulationSpace.World;
				jobData.toWorld = (flag2 ? ((float4x4)Matrix4x4.identity) : portalAwareParticleSystem2.toWorld);
				jobData.raycasts = raycasts;
				JobHandle value = jobData.Schedule(portalAwareParticleSystem2._system);
				commandJobs.Add(in value);
				num2 += particleCount2;
			}
			JobHandle.ScheduleBatchedJobs();
			JobHandle value2 = JobHandle.CombineDependencies(commandJobs.AsArray());
			portalDependent.Add(in value2);
			JobHandle jobHandle = RaycastCommand.ScheduleBatch(commands.AsArray(), hits.AsArray(), 64, 1, value2);
			ParticleIntersectionJob jobData3 = new ParticleIntersectionJob
			{
				scene = nativeScene,
				intersections = nativeArray2,
				rays = nativeArray
			};
			JobHandle value3 = IJobForExtensions.ScheduleParallelByRef(ref jobData3, commands.Length, 64, value2);
			portalDependent.Add(in value3);
			CreateBloodJob jobData4 = new CreateBloodJob
			{
				queue = instance.stainCreateQueue.AsParallelWriter(),
				hits = nativeArray3,
				intersections = nativeArray2,
				shouldCreate = bloodMeta.AsArray()
			};
			createBloodHandle = IJobForExtensions.ScheduleParallelByRef(ref jobData4, commands.Length, 128, jobHandle);
			JobHandle dependsOn = JobHandle.CombineDependencies(value3, jobHandle);
			JobHandle.ScheduleBatchedJobs();
			num2 = 0;
			for (int k = 0; k < count; k++)
			{
				PortalAwareParticleSystem portalAwareParticleSystem3 = systems[k];
				int particleCount3 = portalAwareParticleSystem3._system.particleCount;
				NativeSlice<IntersectionAndIndex> nativeSlice = nativeArray2.Slice(num2, particleCount3);
				NativeSlice<RaycastHit> nativeSlice2 = nativeArray3.Slice(num2, particleCount3);
				bool flag3 = portalAwareParticleSystem3._system.main.simulationSpace == ParticleSystemSimulationSpace.World;
				jobData2.toWorld = (flag3 ? ((float4x4)Matrix4x4.identity) : portalAwareParticleSystem3.toWorld);
				jobData2.toLocal = (flag3 ? ((float4x4)Matrix4x4.identity) : portalAwareParticleSystem3.toLocal);
				jobData2.intersections = nativeSlice;
				jobData2.hits = nativeSlice2;
				jobData2.trails = &((Interop.ParticleSystem*)(void*)portalAwareParticleSystem3._system.GetCachedPtr())->m_Particles[0].Value->trails;
				portalAwareParticleSystem3.responseHandle = jobData2.Schedule(portalAwareParticleSystem3._system, dependsOn);
				portalDependent.Add(in portalAwareParticleSystem3.responseHandle);
				num2 += particleCount3;
			}
			JobHandle.ScheduleBatchedJobs();
		}
	}

	public void CompleteJobs()
	{
		createBloodHandle.Complete();
		if (commandJobs.IsCreated)
		{
			JobHandle.CompleteAll(commandJobs.AsArray());
			commandJobs.Clear();
		}
	}

	public void Register(PortalAwareParticleSystem system)
	{
		systems.Add(system);
	}

	public void Deregister(PortalAwareParticleSystem system)
	{
		systems.Remove(system);
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			CompleteJobs();
			if (commands.IsCreated)
			{
				commands.Dispose();
			}
			if (intersections.IsCreated)
			{
				intersections.Dispose();
			}
			if (hits.IsCreated)
			{
				hits.Dispose();
			}
			if (commandJobs.IsCreated)
			{
				commandJobs.Dispose();
			}
			if (bloodMeta.IsCreated)
			{
				bloodMeta.Dispose();
			}
		}
	}
}
