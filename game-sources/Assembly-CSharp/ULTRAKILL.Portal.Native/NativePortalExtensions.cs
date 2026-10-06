using System;
using System.Runtime.CompilerServices;
using ULTRAKILL.Portal.Geometry;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace ULTRAKILL.Portal.Native;

[BurstCompile]
public static class NativePortalExtensions
{
	internal delegate void CalculateData_00002B22_0024PostfixBurstDelegate(ref NativePortal enter, ref NativePortal exit, in PortalHandle enterHandle, in PortalHandle exitHandle, in PlaneShape plane, in float3 enterPos, in quaternion enterRot, in float3 exitPos, in quaternion exitRot);

	internal static class CalculateData_00002B22_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(CalculateData_00002B22_0024PostfixBurstDelegate).TypeHandle);
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

		static CalculateData_00002B22_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(ref NativePortal enter, ref NativePortal exit, in PortalHandle enterHandle, in PortalHandle exitHandle, in PlaneShape plane, in float3 enterPos, in quaternion enterRot, in float3 exitPos, in quaternion exitRot)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref NativePortal, ref NativePortal, ref PortalHandle, ref PortalHandle, ref PlaneShape, ref float3, ref quaternion, ref float3, ref quaternion, void>)functionPointer)(ref enter, ref exit, ref enterHandle, ref exitHandle, ref plane, ref enterPos, ref enterRot, ref exitPos, ref exitRot);
					return;
				}
			}
			CalculateData_0024BurstManaged(ref enter, ref exit, in enterHandle, in exitHandle, in plane, in enterPos, in enterRot, in exitPos, in exitRot);
		}
	}

	internal delegate bool Raycast_00002B23_0024PostfixBurstDelegate(in NativePortal portal, in PortalRay ray, out NativePortalIntersection intersection, bool allowBackfaces = false);

	internal static class Raycast_00002B23_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(Raycast_00002B23_0024PostfixBurstDelegate).TypeHandle);
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

		static Raycast_00002B23_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static bool Invoke(in NativePortal portal, in PortalRay ray, out NativePortalIntersection intersection, bool allowBackfaces = false)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<ref NativePortal, ref PortalRay, ref NativePortalIntersection, bool, bool>)functionPointer)(ref portal, ref ray, ref intersection, allowBackfaces);
				}
			}
			return portal.Raycast_0024BurstManaged(in ray, out intersection, allowBackfaces);
		}
	}

	[BurstCompile]
	public static void CalculateData(ref NativePortal enter, ref NativePortal exit, in PortalHandle enterHandle, in PortalHandle exitHandle, in PlaneShape plane, in float3 enterPos, in quaternion enterRot, in float3 exitPos, in quaternion exitRot)
	{
		CalculateData_00002B22_0024BurstDirectCall.Invoke(ref enter, ref exit, in enterHandle, in exitHandle, in plane, in enterPos, in enterRot, in exitPos, in exitRot);
	}

	[BurstCompile]
	public static bool Raycast(this in NativePortal portal, in PortalRay ray, out NativePortalIntersection intersection, bool allowBackfaces = false)
	{
		return Raycast_00002B23_0024BurstDirectCall.Invoke(in portal, in ray, out intersection, allowBackfaces);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void CalculateData_0024BurstManaged(ref NativePortal enter, ref NativePortal exit, in PortalHandle enterHandle, in PortalHandle exitHandle, in PlaneShape plane, in float3 enterPos, in quaternion enterRot, in float3 exitPos, in quaternion exitRot)
	{
		float2 @float = new float2(plane.width, plane.height);
		float2 dim = @float * 0.5f;
		float4x4 float4x = float4x4.TRS(enterPos, enterRot, 1f);
		float4x4 float4x2 = math.fastinverse(float4x);
		enter.handle = enterHandle;
		enter.valid = true;
		enter.dimensions = @float;
		enter.transform = new NativePortalTransform
		{
			toWorld = float4x,
			toLocal = float4x2
		};
		enter.vertices = new PortalVertices(float4x, dim);
		enter.plane = new Plane(float4x.c2.xyz, float4x.c3.xyz);
		float4x4 float4x3 = float4x4.TRS(exitPos, exitRot, 1f);
		float4x4 float4x4 = math.fastinverse(float4x3);
		exit.handle = exitHandle;
		exit.valid = true;
		exit.dimensions = @float;
		exit.transform = new NativePortalTransform
		{
			toWorld = float4x3,
			toLocal = float4x4
		};
		exit.vertices = new PortalVertices(float4x3, dim);
		exit.plane = new Plane(float4x3.c2.xyz, float4x3.c3.xyz);
		float4x4 a = math.mul(float4x3, NativePortal.ScaleMatrix);
		float4x4 a2 = math.mul(float4x, NativePortal.ScaleMatrix);
		enter.travelMatrix = math.mul(a, float4x2);
		exit.travelMatrix = math.mul(a2, float4x4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static bool Raycast_0024BurstManaged(this in NativePortal portal, in PortalRay ray, out NativePortalIntersection intersection, bool allowBackfaces = false)
	{
		intersection = default(NativePortalIntersection);
		intersection.handle = portal.handle;
		return PortalMath.Raycast(in ray, in portal.transform.toWorld, in portal.dimensions, out intersection.point, out intersection.distance, allowBackfaces);
	}
}
