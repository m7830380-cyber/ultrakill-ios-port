using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

namespace ULTRAKILL.Portal;

[BurstCompile]
public struct ParticleCommandJob : IJobParticleSystem
{
	private PhysicsScene scene;

	public float4x4 toWorld;

	public QueryParameters parameters;

	[NativeDisableContainerSafetyRestriction]
	[WriteOnly]
	public NativeSlice<RaycastCommand> raycasts;

	public void Execute(ParticleSystemJobData jobData)
	{
		ParticleSystemNativeArray4 customData = jobData.customData1;
		int count = jobData.count;
		for (int i = 0; i < count; i++)
		{
			float3 xyz = math.float4(jobData.customData1[i]).xyz;
			float3 @float = math.transform(toWorld, jobData.positions[i]);
			if (math.all(xyz == new float3(0f, 0f, 0f)))
			{
				xyz = toWorld.c3.xyz;
			}
			customData[i] = (Vector3)@float;
			float3 x = @float - xyz;
			raycasts[i] = new RaycastCommand(xyz, math.normalizesafe(x, new float3(0f, 1f, 0f)), parameters, math.length(x));
		}
	}
}
