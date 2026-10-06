using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public static class FrustumClipper
{
	internal unsafe delegate bool ClipQuadToCameraFrustum_000017D0_0024PostfixBurstDelegate(float4* frustumPlanes, [NoAlias] float3* polyIn, [NoAlias] float3* polyOut, out int finalCount);

	internal static class ClipQuadToCameraFrustum_000017D0_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(ClipQuadToCameraFrustum_000017D0_0024PostfixBurstDelegate).TypeHandle);
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

		static ClipQuadToCameraFrustum_000017D0_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static bool Invoke(float4* frustumPlanes, [NoAlias] float3* polyIn, [NoAlias] float3* polyOut, out int finalCount)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<float4*, float3*, float3*, ref int, bool>)functionPointer)(frustumPlanes, polyIn, polyOut, ref finalCount);
				}
			}
			return ClipQuadToCameraFrustum_0024BurstManaged(frustumPlanes, polyIn, polyOut, out finalCount);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsInsidePlane(in float3 point, in float3 planeNormal, float planeDistance)
	{
		return math.dot(planeNormal, point) + planeDistance >= 0f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LinePlaneIntersection(in float3 p1, in float3 p2, in float3 planeNormal, float planeDistance, out float3 outVec)
	{
		float3 @float = p2 - p1;
		float3 y = planeNormal;
		float num = math.dot(@float, y);
		float num2 = (0f - math.dot(p1, y) - planeDistance) / num;
		outVec = p1 + @float * num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int ClipPolygonWithPlane(in float3 planeNormal, float planeDistance, [NoAlias] float3* polyIn, [NoAlias] float3* polyOut, int polyInCount)
	{
		int num = 0;
		for (int i = 0; i < polyInCount; i++)
		{
			float3 point = polyIn[i];
			float3 point2 = polyIn[(i + 1) % polyInCount];
			bool num2 = IsInsidePlane(in point, in planeNormal, planeDistance);
			bool flag = IsInsidePlane(in point2, in planeNormal, planeDistance);
			if (num2)
			{
				polyOut[num] = point;
				num++;
			}
			if (num2 != flag)
			{
				LinePlaneIntersection(in point, in point2, in planeNormal, planeDistance, out var outVec);
				polyOut[num] = outVec;
				num++;
			}
		}
		return num;
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast)]
	public unsafe static bool ClipQuadToCameraFrustum(float4* frustumPlanes, [NoAlias] float3* polyIn, [NoAlias] float3* polyOut, out int finalCount)
	{
		return ClipQuadToCameraFrustum_000017D0_0024BurstDirectCall.Invoke(frustumPlanes, polyIn, polyOut, out finalCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast)]
	internal unsafe static bool ClipQuadToCameraFrustum_0024BurstManaged(float4* frustumPlanes, [NoAlias] float3* polyIn, [NoAlias] float3* polyOut, out int finalCount)
	{
		float3* ptr = polyIn;
		finalCount = 0;
		int polyInCount = 4;
		for (int i = 0; i < 5; i++)
		{
			float4 @float = frustumPlanes[i];
			finalCount = ClipPolygonWithPlane(@float.xyz, @float.w, polyIn, polyOut, polyInCount);
			if (finalCount == 0)
			{
				break;
			}
			polyInCount = finalCount;
			float3* intPtr = polyIn;
			polyIn = polyOut;
			polyOut = intPtr;
		}
		return ptr == polyOut;
	}
}
