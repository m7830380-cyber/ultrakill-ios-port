using System;
using UnityEngine;
using UnityEngine.AI;
using Vertx.Debugging;

namespace Utilities;

public static class NavMeshPathExtensions
{
	public static bool TryGetDistance(this NavMeshPath path, out float distance)
	{
		distance = 0f;
		if (path.corners.Length <= 1)
		{
			return false;
		}
		Vector3 b = path.corners[0];
		for (int i = 1; i < path.corners.Length; i++)
		{
			distance += Vector3.Distance(path.corners[i], b);
			b = path.corners[i];
		}
		return true;
	}

	public static float GetDistance(this NavMeshPath path)
	{
		if (!path.TryGetDistance(out var distance))
		{
			throw new ArgumentException($"Could not get distance from path with {path.corners.Length} corners");
		}
		return distance;
	}

	public static void DrawDebug(this NavMeshPath path, Color color, float duration)
	{
		if (path.corners.Length == 0)
		{
			Debug.LogWarning("Attempted to debug draw empty path");
		}
	}

	public static Shape.LineStrip GetLineStrip(this NavMeshPath path)
	{
		return new Shape.LineStrip(path.corners);
	}
}
