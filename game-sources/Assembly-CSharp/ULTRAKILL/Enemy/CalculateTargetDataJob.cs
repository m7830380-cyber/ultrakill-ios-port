using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ULTRAKILL.Enemy;

[BurstCompile]
public struct CalculateTargetDataJob : IJobFor
{
	public int targetCount;

	[ReadOnly]
	public NativeArray<float4x4> matrices;

	public TargetDataArrays arrays;

	public void Execute(int index)
	{
		int index2 = index / targetCount;
		float4x4 a = matrices[index2];
		float3 value = math.transform(a, arrays.positions[index]);
		float3 value2 = math.transform(a, arrays.headPositions[index]);
		arrays.positions[index] = value;
		arrays.headPositions[index] = value2;
		arrays.velocities[index] = math.rotate(a, arrays.velocities[index]);
	}
}
