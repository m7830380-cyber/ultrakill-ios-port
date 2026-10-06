using ULTRAKILL.Portal.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ULTRAKILL.Portal;

[BurstCompile]
public struct PortalVisionJob : IJobFor
{
	[ReadOnly]
	public NativeArray<PortalHandle> handles;

	[ReadOnly]
	public NativeArray<float4x4> transforms;

	[ReadOnly]
	public NativeArray<PortalVertices> verts;

	[NativeDisableParallelForRestriction]
	[WriteOnly]
	public NativeArray<bool> visionPossible;

	public void Execute(int index)
	{
		int length = handles.Length;
		int num = index * length;
		float4x4 float4x = transforms[index];
		PortalVertices portalVertices = verts[index];
		float4 c = float4x.c2;
		float4 c2 = float4x.c3;
		for (int i = 0; i < length; i++)
		{
			float4x4 float4x2 = transforms[i];
			float4 c3 = float4x2.c2;
			if (math.dot(c, c3) >= 0.99f)
			{
				visionPossible[num + i] = false;
				continue;
			}
			PortalVertices portalVertices2 = verts[i];
			float4 c4 = float4x2.c3;
			bool num2 = portalVertices2.IsBehindPlane(c2.xyz, -c.xyz);
			bool flag = portalVertices.IsBehindPlane(c4.xyz, -c3.xyz);
			if (num2 || flag)
			{
				visionPossible[num + i] = false;
			}
		}
	}
}
