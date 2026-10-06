using Unity.Mathematics;

namespace ULTRAKILL.Portal.Native;

public struct PortalRay
{
	public float3 start;

	public float3 direction;

	public float distanceSq;

	public PortalRay(float3 start, float3 end)
	{
		float3 x = end - start;
		float num = math.lengthsq(x);
		this.start = start;
		direction = math.normalizesafe(x);
		distanceSq = num;
	}

	public PortalRay(float3 start, float3 direction, float distance)
	{
		this.start = start;
		this.direction = direction;
		distanceSq = distance * distance;
	}
}
