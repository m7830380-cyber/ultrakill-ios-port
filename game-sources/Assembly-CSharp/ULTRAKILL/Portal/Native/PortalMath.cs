using Unity.Mathematics;

namespace ULTRAKILL.Portal.Native;

public static class PortalMath
{
	public static bool Raycast(in PortalRay ray, in float4x4 mat, in float2 dimensions, out float3 point, out float distance, bool allowBackface = false)
	{
		distance = float.PositiveInfinity;
		point = default;
		float3 xyz = mat.c3.xyz;
		float3 xyz2 = mat.c2.xyz;
		float num = math.dot(xyz2, ray.direction);
		if (!allowBackface && num <= 0f)
		{
			return false;
		}
		float num2 = math.dot(ray.start - xyz, -xyz2);
		if (!allowBackface && num2 < 0f)
		{
			return false;
		}
		float num3 = num2 / num;
		if (num3 < 0f)
		{
			return false;
		}
		if (num3 * num3 > ray.distanceSq)
		{
			return false;
		}
		point = ray.start + ray.direction * num3;
		distance = num3;
		float3 xyz3 = mat.c0.xyz;
		float3 xyz4 = mat.c1.xyz;
		float3 x = point - xyz;
		float2 float5 = dimensions * 0.5f;
		bool result = true;
		if (math.abs(math.dot(x, xyz3)) > float5.x || math.abs(math.dot(x, xyz4)) > float5.y)
		{
			result = false;
		}
		return result;
	}
}
