using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace BeamHitInterpolation;

[BurstCompile]
public static class BeamHitInterpolator
{
	internal delegate void FindBestTimeAndMinDistSq_00002E1C_0024PostfixBurstDelegate(in float3 point, in float3 startA, in float3 endA, in float3 startB, in float3 endB, int iterations, out float bestTime, out float minDistSq);

	internal static class FindBestTimeAndMinDistSq_00002E1C_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(FindBestTimeAndMinDistSq_00002E1C_0024PostfixBurstDelegate).TypeHandle);
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public static void Constructor()
		{
			DeferredCompilation = BurstCompiler.CompileILPPMethod2((RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/);
		}

		public static void Initialize()
		{
		}

		static FindBestTimeAndMinDistSq_00002E1C_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in float3 point, in float3 startA, in float3 endA, in float3 startB, in float3 endB, int iterations, out float bestTime, out float minDistSq)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref float3, ref float3, ref float3, ref float3, ref float3, int, ref float, ref float, void>)functionPointer)(ref point, ref startA, ref endA, ref startB, ref endB, iterations, ref bestTime, ref minDistSq);
					return;
				}
			}
			FindBestTimeAndMinDistSq_0024BurstManaged(in point, in startA, in endA, in startB, in endB, iterations, out bestTime, out minDistSq);
		}
	}

	internal delegate float DistanceSqPointSegment_00002E1D_0024PostfixBurstDelegate(in float3 point, in float3 segmentStart, in float3 segmentEnd);

	internal static class DistanceSqPointSegment_00002E1D_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(DistanceSqPointSegment_00002E1D_0024PostfixBurstDelegate).TypeHandle);
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public static void Constructor()
		{
			DeferredCompilation = BurstCompiler.CompileILPPMethod2((RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/);
		}

		public static void Initialize()
		{
		}

		static DistanceSqPointSegment_00002E1D_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static float Invoke(in float3 point, in float3 segmentStart, in float3 segmentEnd)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<ref float3, ref float3, ref float3, float>)functionPointer)(ref point, ref segmentStart, ref segmentEnd);
				}
			}
			return DistanceSqPointSegment_0024BurstManaged(in point, in segmentStart, in segmentEnd);
		}
	}

	internal delegate void ClosestPointOnSegment_00002E1E_0024PostfixBurstDelegate(in float3 point, in float3 segmentStart, in float3 segmentEnd, out float3 closest);

	internal static class ClosestPointOnSegment_00002E1E_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(ClosestPointOnSegment_00002E1E_0024PostfixBurstDelegate).TypeHandle);
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public static void Constructor()
		{
			DeferredCompilation = BurstCompiler.CompileILPPMethod2((RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/);
		}

		public static void Initialize()
		{
		}

		static ClosestPointOnSegment_00002E1E_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in float3 point, in float3 segmentStart, in float3 segmentEnd, out float3 closest)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref float3, ref float3, ref float3, ref float3, void>)functionPointer)(ref point, ref segmentStart, ref segmentEnd, ref closest);
					return;
				}
			}
			ClosestPointOnSegment_0024BurstManaged(in point, in segmentStart, in segmentEnd, out closest);
		}
	}

	internal delegate void CalculateSweptObb_00002E1F_0024PostfixBurstDelegate(in float3 prevOrigin, in float3 prevEnd, in float3 currOrigin, in float3 currEnd, in float radius, out float3 center, out float3 halfExtents, out quaternion orientation);

	internal static class CalculateSweptObb_00002E1F_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(CalculateSweptObb_00002E1F_0024PostfixBurstDelegate).TypeHandle);
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public static void Constructor()
		{
			DeferredCompilation = BurstCompiler.CompileILPPMethod2((RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/);
		}

		public static void Initialize()
		{
		}

		static CalculateSweptObb_00002E1F_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in float3 prevOrigin, in float3 prevEnd, in float3 currOrigin, in float3 currEnd, in float radius, out float3 center, out float3 halfExtents, out quaternion orientation)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref float3, ref float3, ref float3, ref float3, ref float, ref float3, ref float3, ref quaternion, void>)functionPointer)(ref prevOrigin, ref prevEnd, ref currOrigin, ref currEnd, ref radius, ref center, ref halfExtents, ref orientation);
					return;
				}
			}
			CalculateSweptObb_0024BurstManaged(in prevOrigin, in prevEnd, in currOrigin, in currEnd, in radius, out center, out halfExtents, out orientation);
		}
	}

	private const float EpsilonSq = 1E-12f;

	private const float ConvergenceThresholdSq = 1E-06f;

	private const int MaxOverlapResults = 50;

	private const int TimeSearchIterations = 7;

	private const int RefinementIterations = 3;

	private const bool EnableDebugDrawing = false;

	private static readonly Color ObbColor = new Color(0.5f, 0.8f, 1f, 0.5f);

	private static readonly Color RayAColor = Color.red;

	private static readonly Color RayBColor = Color.green;

	public static void HitInterpolated(Ray rayA, float distanceA, Ray rayB, float distanceB, float beamRadius, LayerMask hitMask, List<InterpolatedHit> results, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
	{
		results.Clear();
		float3 prevOrigin = rayA.origin;
		float3 @float = rayA.direction;
		float3 prevEnd = prevOrigin + @float * distanceA;
		float3 currOrigin = rayB.origin;
		float3 float2 = rayB.direction;
		float3 currEnd = currOrigin + float2 * distanceB;
		CalculateSweptObb(in prevOrigin, in prevEnd, in currOrigin, in currEnd, in beamRadius, out var center, out var halfExtents, out var orientation);
		Collider[] colliders = ArrayPool.GetColliders();
		int num = Physics.OverlapBoxNonAlloc(center, halfExtents, colliders, orientation, hitMask, queryTriggerInteraction);
		if (num == 0)
		{
			ArrayPool.ReturnColliders(colliders);
			return;
		}
		for (int i = 0; i < num; i++)
		{
			Collider collider = colliders[i];
			if (!(collider == null) && ValidateSweptHit(collider, prevOrigin, prevEnd, currOrigin, currEnd, beamRadius, out var finalPointOnCollider, out var finalClosestPointOnAxis, out var finalMinDistSq, out var _, out var _))
			{
				results.Add(CreateInterpolatedHit(collider, finalPointOnCollider, finalClosestPointOnAxis, finalMinDistSq));
			}
		}
		ArrayPool.ReturnColliders(colliders);
	}

	private static bool ValidateSweptHit(Collider collider, float3 prevOrigin, float3 prevEnd, float3 currOrigin, float3 currEnd, float radius, out float3 finalPointOnCollider, out float3 finalClosestPointOnAxis, out float finalMinDistSq, out float effectiveRadius, out float optimalTime)
	{
		float num = radius * radius;
		effectiveRadius = radius;
		optimalTime = 0.5f;
		if (collider is SphereCollider sphereCollider)
		{
			float3 point = sphereCollider.transform.TransformPoint(sphereCollider.center);
			float num2 = math.cmax(math.abs(sphereCollider.transform.lossyScale));
			effectiveRadius = radius + sphereCollider.radius * num2;
			float num3 = effectiveRadius * effectiveRadius;
			FindBestTimeAndMinDistSq(in point, in prevOrigin, in prevEnd, in currOrigin, in currEnd, 7, out optimalTime, out finalMinDistSq);
			ClosestPointOnSegment(in point, math.lerp(prevOrigin, currOrigin, optimalTime), math.lerp(prevEnd, currEnd, optimalTime), out finalClosestPointOnAxis);
			finalPointOnCollider = point;
			return finalMinDistSq <= num3;
		}
		if (!(collider is MeshCollider { convex: false }))
		{
			float3 @float = (prevOrigin + prevEnd + currOrigin + currEnd) * 0.25f;
			float3 point2 = collider.ClosestPoint(@float);
			float3 closest = float3.zero;
			finalMinDistSq = float.PositiveInfinity;
			for (int i = 0; i < 3; i++)
			{
				FindBestTimeAndMinDistSq(in point2, in prevOrigin, in prevEnd, in currOrigin, in currEnd, 7, out var bestTime, out var _);
				optimalTime = bestTime;
				ClosestPointOnSegment(in point2, math.lerp(prevOrigin, currOrigin, optimalTime), math.lerp(prevEnd, currEnd, optimalTime), out var closest2);
				closest = closest2;
				finalMinDistSq = math.distancesq(point2, closest);
				float3 float2 = collider.ClosestPoint(closest);
				if (math.distancesq(float2, point2) < 1E-06f)
				{
					point2 = float2;
					ClosestPointOnSegment(in point2, math.lerp(prevOrigin, currOrigin, optimalTime), math.lerp(prevEnd, currEnd, optimalTime), out closest);
					finalMinDistSq = math.distancesq(point2, closest);
					break;
				}
				point2 = float2;
			}
			finalPointOnCollider = point2;
			finalClosestPointOnAxis = closest;
			return finalMinDistSq <= num;
		}
		finalPointOnCollider = float3.zero;
		finalClosestPointOnAxis = float3.zero;
		finalMinDistSq = float.PositiveInfinity;
		return false;
	}

	[BurstCompile]
	private static void FindBestTimeAndMinDistSq(in float3 point, in float3 startA, in float3 endA, in float3 startB, in float3 endB, int iterations, out float bestTime, out float minDistSq)
	{
		FindBestTimeAndMinDistSq_00002E1C_0024BurstDirectCall.Invoke(in point, in startA, in endA, in startB, in endB, iterations, out bestTime, out minDistSq);
	}

	[BurstCompile]
	private static float DistanceSqPointSegment(in float3 point, in float3 segmentStart, in float3 segmentEnd)
	{
		return DistanceSqPointSegment_00002E1D_0024BurstDirectCall.Invoke(in point, in segmentStart, in segmentEnd);
	}

	[BurstCompile]
	private static void ClosestPointOnSegment(in float3 point, in float3 segmentStart, in float3 segmentEnd, out float3 closest)
	{
		ClosestPointOnSegment_00002E1E_0024BurstDirectCall.Invoke(in point, in segmentStart, in segmentEnd, out closest);
	}

	[BurstCompile]
	private static void CalculateSweptObb(in float3 prevOrigin, in float3 prevEnd, in float3 currOrigin, in float3 currEnd, in float radius, out float3 center, out float3 halfExtents, out quaternion orientation)
	{
		CalculateSweptObb_00002E1F_0024BurstDirectCall.Invoke(in prevOrigin, in prevEnd, in currOrigin, in currEnd, in radius, out center, out halfExtents, out orientation);
	}

	private static InterpolatedHit CreateInterpolatedHit(Collider collider, float3 finalPointOnCollider, float3 finalClosestPointOnAxis, float finalMinDistSq)
	{
		float3 @float = collider.ClosestPoint(finalClosestPointOnAxis);
		float3 x = finalClosestPointOnAxis - @float;
		if (math.lengthsq(x) < 1E-12f)
		{
			float3 float2 = collider.bounds.center;
			x = @float - float2;
			if (math.lengthsq(x) < 1E-12f)
			{
				x = collider.transform.up;
			}
		}
		x = math.normalize(x);
		float distance = math.sqrt(finalMinDistSq);
		return new InterpolatedHit
		{
			point = @float,
			normal = x,
			distance = distance,
			collider = collider,
			transform = collider.transform,
			rigidbody = collider.attachedRigidbody
		};
	}

	[Conditional("UNITY_EDITOR")]
	private static void DrawObbDebug(float3 center, float3 halfExtents, quaternion orientation)
	{
	}

	[Conditional("UNITY_EDITOR")]
	private static void DrawRaysDebug(float3 originA, float3 endA, float3 originB, float3 endB)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void FindBestTimeAndMinDistSq_0024BurstManaged(in float3 point, in float3 startA, in float3 endA, in float3 startB, in float3 endB, int iterations, out float bestTime, out float minDistSq)
	{
		float num = 0f;
		float num2 = 1f;
		bestTime = 0.5f;
		minDistSq = DistanceSqPointSegment(in point, math.lerp(startA, startB, bestTime), math.lerp(endA, endB, bestTime));
		float num3 = 1f / 3f;
		for (int i = 0; i < iterations; i++)
		{
			float num4 = num + (num2 - num) * num3;
			float num5 = num2 - (num2 - num) * num3;
			float num6 = DistanceSqPointSegment(in point, math.lerp(startA, startB, num4), math.lerp(endA, endB, num4));
			float num7 = DistanceSqPointSegment(in point, math.lerp(startA, startB, num5), math.lerp(endA, endB, num5));
			if (num6 < num7)
			{
				num2 = num5;
				if (num6 < minDistSq)
				{
					minDistSq = num6;
					bestTime = num4;
				}
			}
			else
			{
				num = num4;
				if (num7 < minDistSq)
				{
					minDistSq = num7;
					bestTime = num5;
				}
			}
		}
		float num8 = (num + num2) * 0.5f;
		float num9 = DistanceSqPointSegment(in point, math.lerp(startA, startB, num8), math.lerp(endA, endB, num8));
		if (num9 < minDistSq)
		{
			minDistSq = num9;
			bestTime = num8;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static float DistanceSqPointSegment_0024BurstManaged(in float3 point, in float3 segmentStart, in float3 segmentEnd)
	{
		float3 @float = segmentEnd - segmentStart;
		float num = math.lengthsq(@float);
		if (num < 1E-12f)
		{
			return math.distancesq(point, segmentStart);
		}
		float valueToClamp = math.dot(point - segmentStart, @float) / num;
		valueToClamp = math.clamp(valueToClamp, 0f, 1f);
		float3 y = segmentStart + valueToClamp * @float;
		return math.distancesq(point, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void ClosestPointOnSegment_0024BurstManaged(in float3 point, in float3 segmentStart, in float3 segmentEnd, out float3 closest)
	{
		float3 @float = segmentEnd - segmentStart;
		float num = math.lengthsq(@float);
		if (num < 1E-12f)
		{
			closest = segmentStart;
			return;
		}
		float valueToClamp = math.dot(point - segmentStart, @float) / num;
		valueToClamp = math.clamp(valueToClamp, 0f, 1f);
		closest = segmentStart + valueToClamp * @float;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void CalculateSweptObb_0024BurstManaged(in float3 prevOrigin, in float3 prevEnd, in float3 currOrigin, in float3 currEnd, in float radius, out float3 center, out float3 halfExtents, out quaternion orientation)
	{
		center = (prevOrigin + prevEnd + currOrigin + currEnd) * 0.25f;
		float3 @float = prevEnd - prevOrigin;
		float3 float2 = currEnd - currOrigin;
		float num = math.length(@float);
		float num2 = math.length(float2);
		@float = ((num > 1E-12f) ? (@float / num) : new float3(0f, 0f, 1f));
		float2 = ((num2 > 1E-12f) ? (float2 / num2) : @float);
		float3 x = @float + float2;
		if (math.lengthsq(x) < 1E-12f)
		{
			x = currOrigin - prevOrigin;
			if (math.lengthsq(x) < 1E-12f)
			{
				x = new float3(0f, 0f, 1f);
			}
		}
		x = math.normalize(x);
		float3 float3 = (prevOrigin + prevEnd) * 0.5f;
		float3 float4 = (currOrigin + currEnd) * 0.5f - float3;
		float3 y = ((math.abs(math.dot(x, new float3(0f, 1f, 0f))) > 0.999f) ? new float3(1f, 0f, 0f) : new float3(0f, 1f, 0f));
		float3 x2;
		if (math.lengthsq(float4) > 1E-12f)
		{
			x2 = float4 - math.dot(float4, x) * x;
			if (math.lengthsq(x2) < 1E-12f)
			{
				x2 = math.cross(x, y);
			}
		}
		else
		{
			x2 = math.cross(x, y);
		}
		x2 = math.normalize(x2);
		float3 y2 = math.normalize(math.cross(x2, x));
		Span<float3> obj = stackalloc float3[4] { prevOrigin, prevEnd, currOrigin, currEnd };
		float num3 = float.PositiveInfinity;
		float num4 = float.NegativeInfinity;
		float num5 = float.PositiveInfinity;
		float num6 = float.NegativeInfinity;
		float num7 = float.PositiveInfinity;
		float num8 = float.NegativeInfinity;
		Span<float3> span = obj;
		for (int i = 0; i < span.Length; i++)
		{
			float3 x3 = span[i] - center;
			float y3 = math.dot(x3, y2);
			num3 = math.min(num3, y3);
			num4 = math.max(num4, y3);
			float y4 = math.dot(x3, x2);
			num5 = math.min(num5, y4);
			num6 = math.max(num6, y4);
			float y5 = math.dot(x3, x);
			num7 = math.min(num7, y5);
			num8 = math.max(num8, y5);
		}
		halfExtents = new float3((num4 - num3) * 0.5f + radius, (num6 - num5) * 0.5f + radius, (num8 - num7) * 0.5f);
		orientation = quaternion.LookRotation(x, x2);
	}
}
