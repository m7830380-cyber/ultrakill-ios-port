using UnityEngine;

namespace ULTRAKILL.Portal;

public static class PortalDebugUtils
{
	public static void DrawRaycast(Vector3 start, Vector3 end, PortalTraversalV2[] traversals, Color color, float duration)
	{
		for (int i = 0; i < traversals.Length; i++)
		{
			_ = traversals[i];
		}
	}

	public static void DrawPortalSequence(Vector3 start, PortalHandleSequence portalSeq, Color color, float duration)
	{
		foreach (PortalHandle item in portalSeq)
		{
			_ = (Vector3)PortalUtils.GetTransform(item, reverseSide: false).center;
			_ = (Vector3)PortalUtils.GetTransform(item, reverseSide: true).center;
		}
	}
}
