using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace ULTRAKILL.Portal.Geometry;

[BurstCompile]
public static class PlaneShapeExtensions
{
	internal delegate void GetClosestPoint_00002B41_0024PostfixBurstDelegate(float width, float height, in float3 center, in float3 right, in float3 up, in float3 forward, in float3 point, out float3 closest);

	internal static class GetClosestPoint_00002B41_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(GetClosestPoint_00002B41_0024PostfixBurstDelegate).TypeHandle);
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

		static GetClosestPoint_00002B41_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(float width, float height, in float3 center, in float3 right, in float3 up, in float3 forward, in float3 point, out float3 closest)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<float, float, ref float3, ref float3, ref float3, ref float3, ref float3, ref float3, void>)functionPointer)(width, height, ref center, ref right, ref up, ref forward, ref point, ref closest);
					return;
				}
			}
			GetClosestPoint_0024BurstManaged(width, height, in center, in right, in up, in forward, in point, out closest);
		}
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	public static void GetClosestPoint(float width, float height, in float3 center, in float3 right, in float3 up, in float3 forward, in float3 point, out float3 closest)
	{
		GetClosestPoint_00002B41_0024BurstDirectCall.Invoke(width, height, in center, in right, in up, in forward, in point, out closest);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	internal static void GetClosestPoint_0024BurstManaged(float width, float height, in float3 center, in float3 right, in float3 up, in float3 forward, in float3 point, out float3 closest)
	{
		float3 x = point - center;
		float valueToClamp = math.dot(x, right);
		float valueToClamp2 = math.dot(x, up);
		float num = width * 0.5f;
		float num2 = height * 0.5f;
		float num3 = math.clamp(valueToClamp, 0f - num, num);
		float num4 = math.clamp(valueToClamp2, 0f - num2, num2);
		closest = center + right * num3 + up * num4;
	}
}
