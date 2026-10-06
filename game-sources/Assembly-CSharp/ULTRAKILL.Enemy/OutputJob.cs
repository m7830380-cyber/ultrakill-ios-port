using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace ULTRAKILL.Enemy;

[BurstCompile]
public struct OutputJob : IJobFor
{
	public NativeStream.Reader output;

	[WriteOnly]
	public NativeList<TargetIndexAndDistance> array;

	public void Execute(int index)
	{
		output.BeginForEachIndex(index);
		int remainingItemCount = output.RemainingItemCount;
		for (int i = 0; i < remainingItemCount; i++)
		{
			array.AddNoResize(output.Read<TargetIndexAndDistance>());
		}
		output.EndForEachIndex();
	}
}
