using ULTRAKILL.Enemy;
using ULTRAKILL.Portal.Native;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace ULTRAKILL.Portal;

public static class PortalEnemyUtils
{
	public static bool GetPortalVisionPos(this NavMeshAgent nma, AcquirePortalVisionState state, Vector3 currentPos, out Vector3 closestPos)
	{
		switch (state.type)
		{
		case AcquirePortalVisionType.PROJECT_W_NORMAL:
			return CalcNavPosToPortalProjection_TargetWithPortalNormal(currentPos, state.target, nma.areaMask, out closestPos);
		case AcquirePortalVisionType.PROJECT_W_CENTER:
			return CalcNavPosToPortalProjection_TargetToPortalCenter(currentPos, state.target, nma.areaMask, out closestPos);
		default:
			closestPos = default;
			return false;
		}
	}

	public static bool CalcNavPosToPortalProjection_TargetWithPortalNormal(Vector3 currentPos, TargetData target, int areaMask, out Vector3 navPos)
	{
		if (target.handle.portals.Count == 0)
		{
			navPos = default;
			return false;
		}
		PortalHandleSequence portals = target.handle.portals;
		PortalHandle handle = portals[portals.Count - 1].Reverse();
		Vector3 normalized = PortalUtils.GetPortalObject(handle).GetTransform(handle.side).backManaged.normalized;
		return FindClosestNavMeshPoint(normalized * Vector3.Dot(normalized, currentPos - target.position) + target.position, areaMask, out navPos);
	}

	public static bool CalcNavPosToPortalProjection_TargetToPortalCenter(Vector3 currentPos, TargetData target, int areaMask, out Vector3 navPos)
	{
		if (target.handle.portals.Count == 0)
		{
			navPos = default;
			return false;
		}
		PortalHandleSequence portals = target.handle.portals;
		PortalHandle handle = portals[portals.Count - 1].Reverse();
		NativePortalTransform transform = PortalUtils.GetPortalObject(handle).GetTransform(handle.side);
		Vector3 normalized = (transform.centerManaged - target.position).normalized;
		return FindClosestNavMeshPoint(normalized * Vector3.Dot(normalized, currentPos - transform.centerManaged) + transform.centerManaged, areaMask, out navPos);
	}

	private static bool FindClosestNavMeshPoint(Vector3 point, int areaMask, out Vector3 navMeshPos)
	{
		if (Physics.Raycast(point, Vector3.down, out var hitInfo, 20f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
		{
			navMeshPos = hitInfo.point;
			return true;
		}
		if (NavMesh.SamplePosition(point, out var hit, 10f, areaMask))
		{
			navMeshPos = hit.position;
			return true;
		}
		navMeshPos = point;
		return false;
	}

	public static bool IsTargetInPortalOrth(this TargetData target)
	{
		if (target.handle.portals.Count == 0)
		{
			return false;
		}
		PortalHandleSequence portals = target.handle.portals;
		PortalHandle handle = portals[portals.Count - 1].Reverse();
		Portal portalObject = PortalUtils.GetPortalObject(handle);
		NativePortalTransform transform = portalObject.GetTransform(handle.side);
		Vector3 vector = 2f * Vector3.Distance(target.position, transform.centerManaged) * transform.backManaged;
		Vector3 position = target.position;
		Vector3 vector2 = position + vector;
		Vector3 intersection;
		return portalObject.shape.DidCross(transform.WorldToLocal((float3)vector2), transform.WorldToLocal((float3)position), out intersection);
	}

	public static bool IsRayObstructedByHitOrPortal(Vector3 origin, Vector3 direction, float distance, int collissionMask, out Vector3 endPos, out Vector3 endDirection, out PortalTraversalV2[] portalTraversals, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
	{
		bool flag = PortalPhysicsV2.Raycast(origin, direction, distance, collissionMask, out var hitInfo, out portalTraversals, out var endPoint, queryTriggerInteraction);
		if (portalTraversals.Length == 0)
		{
			endDirection = direction;
			if (flag)
			{
				endPos = hitInfo.point;
				return true;
			}
			endPos = endPoint;
			return false;
		}
		PortalTraversalV2 portalTraversalV = portalTraversals[0];
		endPos = portalTraversalV.entrancePoint;
		endDirection = portalTraversalV.entranceDirection;
		return true;
	}
}
