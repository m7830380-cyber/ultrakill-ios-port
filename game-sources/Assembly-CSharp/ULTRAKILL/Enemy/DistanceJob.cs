using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ULTRAKILL.Enemy;

[BurstCompile]
public struct DistanceJob : IJobFor
{
	public int targetCount;

	[ReadOnly]
	public NativeArray<float3> visionOrigins;

	[ReadOnly]
	public NativeArray<VisionTypeFilter> visionFilters;

	[ReadOnly]
	public NativeArray<float3> targetPositions;

	[ReadOnly]
	public NativeArray<TargetType> targetTypes;

	public NativeArray<int> counts;

	[WriteOnly]
	public NativeStream.Writer output;

	public void Execute(int visionIndex)
	{
		output.BeginForEachIndex(visionIndex);
		float3 x = visionOrigins[visionIndex];
		for (int i = 0; i < targetCount; i++)
		{
			if (visionFilters[visionIndex].HasType(targetTypes[i]))
			{
				float distance = math.distancesq(x, targetPositions[i]);
				output.Write(new TargetIndexAndDistance
				{
					index = i,
					distance = distance
				});
				counts[visionIndex]++;
			}
		}
		output.EndForEachIndex();
	}
}
