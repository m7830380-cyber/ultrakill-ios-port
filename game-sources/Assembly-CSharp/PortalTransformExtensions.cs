using ULTRAKILL.Portal.Geometry;
using ULTRAKILL.Portal.Native;
using Unity.Mathematics;
using UnityEngine;

public static class PortalTransformExtensions
{
	public static bool IsTilted(this PortalTransform portalTrans)
	{
		return portalTrans.back.y != 0f;
	}

	public static bool IsRotated(this PortalTransform portalTrans)
	{
		return portalTrans.left.y != 0f;
	}

	public static bool IsFacingDown(this PortalTransform portalTrans)
	{
		return Vector3.Dot(portalTrans.back, Vector3.down) > 0f;
	}

	public static bool IsFloor(this PortalTransform transform)
	{
		return transform.back.y == 1f;
	}

	public static Vector3 GetPositionInFrontOfLocalIntersect(this PortalTransform transform, Vector3 localPortalIntersect, float distance)
	{
		return transform.GetPositionInFront(transform.LocalToWorld(localPortalIntersect), distance);
	}

	public static Vector3 GetPositionInFront(this PortalTransform transform, Vector3 worldPortalIntersect, float distance)
	{
		return worldPortalIntersect + transform.back * distance;
	}

	public static bool IsTilted(this NativePortalTransform portalTrans)
	{
		return portalTrans.back.y != 0f;
	}

	public static bool IsRotated(this NativePortalTransform portalTrans)
	{
		return portalTrans.left.y != 0f;
	}

	public static bool IsFacingDown(this NativePortalTransform portalTrans)
	{
		return Vector3.Dot(portalTrans.back, Vector3.down) > 0f;
	}

	public static bool IsFloor(this NativePortalTransform transform)
	{
		return transform.back.y == 1f;
	}

	public static Vector3 GetPositionInFrontOfLocalIntersect(this NativePortalTransform transform, Vector3 localPortalIntersect, float distance)
	{
		return transform.GetPositionInFront(transform.LocalToWorld((float3)localPortalIntersect), distance);
	}

	public static Vector3 GetPositionInFront(this NativePortalTransform transform, Vector3 worldPortalIntersect, float distance)
	{
		return worldPortalIntersect + transform.backManaged * distance;
	}
}
