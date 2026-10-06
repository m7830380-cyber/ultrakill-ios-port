using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

[BurstCompile]
internal struct CommandJob : IJobParticleSystemParallelFor
{
	public float4x4 transform;

	public NativeArray<RaycastCommand> raycasts;

	[ReadOnly]
	public NativeArray<RaycastHit> lastFrameHits;

	public QueryParameters parameters;

	public float deltaTime;

	public bool worldSpace;

	public Vector3 center;

	public void Execute(ParticleSystemJobData jobData, int i)
	{
		ParticleSystemNativeArray4 customData = jobData.customData1;
		Vector4 vector = customData[i];
		if (worldSpace && vector == Vector4.zero)
		{
			vector = center;
		}
		int index = (int)vector.w;
		Vector3 point = lastFrameHits[index].point;
		if (point.x != 0f && point.y != 0f && point.z != 0f)
		{
			NativeArray<float> aliveTimePercent = jobData.aliveTimePercent;
			aliveTimePercent[i] = 100f;
		}
		float3 xyz = math.mul(transform, new float4(vector.x, vector.y, vector.z, 1f)).xyz;
		float3 x = math.mul(transform, new float4(jobData.positions[i], 1f)).xyz - xyz;
		float distance = math.length(x);
		raycasts[i] = new RaycastCommand(xyz, math.normalizesafe(x), parameters, distance);
		customData[i] = new float4(jobData.positions[i], i);
	}
}
