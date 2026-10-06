using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ULTRAKILL.Enemy;

[BurstCompile]
public struct CopyDefaultDataJob : IJobFor
{
	public int stride;

	[ReadOnly]
	public TargetDataArrays defaults;

	public TargetDataArrays targets;

	public void Execute(int index)
	{
		int index2 = index % stride;
		float3 value = defaults.positions[index2];
		targets.positions[index] = value;
		targets.headPositions[index] = defaults.headPositions[index2];
		targets.velocities[index] = defaults.velocities[index2];
		targets.rotations[index] = defaults.rotations[index2];
		targets.types[index] = defaults.types[index2];
	}
}
