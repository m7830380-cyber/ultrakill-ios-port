using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct CameraData
{
	internal delegate void Create_000017CB_0024PostfixBurstDelegate(in float3 pos, in quaternion rot, int cullingMask, out CameraData data);

	internal static class Create_000017CB_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(Create_000017CB_0024PostfixBurstDelegate).TypeHandle);
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

		static Create_000017CB_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in float3 pos, in quaternion rot, int cullingMask, out CameraData data)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref float3, ref quaternion, int, ref CameraData, void>)functionPointer)(ref pos, ref rot, cullingMask, ref data);
					return;
				}
			}
			Create_0024BurstManaged(in pos, in rot, cullingMask, out data);
		}
	}

	internal delegate void CalculateObliqueMatrix_000017CC_0024PostfixBurstDelegate(in float4x4 projection, in float4 clipPlane, out float4x4 obliqueMatrix);

	internal static class CalculateObliqueMatrix_000017CC_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(CalculateObliqueMatrix_000017CC_0024PostfixBurstDelegate).TypeHandle);
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

		static CalculateObliqueMatrix_000017CC_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in float4x4 projection, in float4 clipPlane, out float4x4 obliqueMatrix)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref float4x4, ref float4, ref float4x4, void>)functionPointer)(ref projection, ref clipPlane, ref obliqueMatrix);
					return;
				}
			}
			CalculateObliqueMatrix_0024BurstManaged(in projection, in clipPlane, out obliqueMatrix);
		}
	}

	public float4x4 WorldToCamera;

	public float4x4 CameraToWorld;

	public float4x4 CullingMatrix;

	public float3 Position;

	public float3 Forward;

	public float3 Up;

	public int cullingMask;

	public static CameraData FromCamera(Camera cam)
	{
		Vector3 source = cam.transform.position;
		Quaternion source2 = cam.transform.rotation;
		Create(in Unsafe.As<Vector3, float3>(ref source), in Unsafe.As<Quaternion, quaternion>(ref source2), cam.cullingMask, out var data);
		Matrix4x4 source3 = cam.cullingMatrix;
		data.CullingMatrix = Unsafe.As<Matrix4x4, float4x4>(ref source3);
		return data;
	}

	public static CameraData FromValues(float3 pos, quaternion rot, int cullingMask)
	{
		Create(in pos, in rot, cullingMask, out var data);
		return data;
	}

	[BurstCompile]
	public static void Create(in float3 pos, in quaternion rot, int cullingMask, out CameraData data)
	{
		Create_000017CB_0024BurstDirectCall.Invoke(in pos, in rot, cullingMask, out data);
	}

	[BurstCompile]
	public static void CalculateObliqueMatrix(in float4x4 projection, in float4 clipPlane, out float4x4 obliqueMatrix)
	{
		CalculateObliqueMatrix_000017CC_0024BurstDirectCall.Invoke(in projection, in clipPlane, out obliqueMatrix);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void Create_0024BurstManaged(in float3 pos, in quaternion rot, int cullingMask, out CameraData data)
	{
		data.Position = pos;
		data.CameraToWorld = float4x4.TRS(pos, rot, new float3(1f, 1f, -1f));
		data.WorldToCamera = math.inverse(data.CameraToWorld);
		data.Forward = math.rotate(rot, new float3(0f, 0f, 1f));
		data.Up = math.rotate(rot, new float3(0f, 1f, 0f));
		data.cullingMask = cullingMask;
		data.CullingMatrix = default;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void CalculateObliqueMatrix_0024BurstManaged(in float4x4 projection, in float4 clipPlane, out float4x4 obliqueMatrix)
	{
		float4 y = new float4((math.sign(clipPlane.x) + projection.c2.x) / projection.c0.x, (math.sign(clipPlane.y) + projection.c2.y) / projection.c1.y, -1f, (1f + projection.c2.z) / projection.c3.z);
		float4 float5 = clipPlane * (2f / math.dot(clipPlane, y));
		obliqueMatrix = projection;
		obliqueMatrix.c0.z = float5.x;
		obliqueMatrix.c1.z = float5.y;
		obliqueMatrix.c2.z = float5.z + 1f;
		obliqueMatrix.c3.z = float5.w;
	}
}
