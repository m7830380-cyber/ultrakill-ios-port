using System;
using System.Runtime.CompilerServices;
using ULTRAKILL.Cheats;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[BurstCompile]
public static class ColliderUtility
{
	internal delegate void BurstClosestPoint_Int16_000004A9_0024PostfixBurstDelegate(ref Mesh.MeshData data, in float3 testPosition, in float3 localUp, bool ignoreVerticalTriangles, out float3 closestPoint);

	internal static class BurstClosestPoint_Int16_000004A9_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(BurstClosestPoint_Int16_000004A9_0024PostfixBurstDelegate).TypeHandle);
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

		static BurstClosestPoint_Int16_000004A9_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(ref Mesh.MeshData data, in float3 testPosition, in float3 localUp, bool ignoreVerticalTriangles, out float3 closestPoint)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref Mesh.MeshData, ref float3, ref float3, bool, ref float3, void>)functionPointer)(ref data, ref testPosition, ref localUp, ignoreVerticalTriangles, ref closestPoint);
					return;
				}
			}
			BurstClosestPoint_Int16_0024BurstManaged(ref data, in testPosition, in localUp, ignoreVerticalTriangles, out closestPoint);
		}
	}

	internal delegate void BurstClosestPoint_Int32_000004AA_0024PostfixBurstDelegate(ref Mesh.MeshData data, in float3 testPosition, in float3 localUp, bool ignoreVerticalTriangles, out float3 closestPoint);

	internal static class BurstClosestPoint_Int32_000004AA_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(BurstClosestPoint_Int32_000004AA_0024PostfixBurstDelegate).TypeHandle);
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

		static BurstClosestPoint_Int32_000004AA_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(ref Mesh.MeshData data, in float3 testPosition, in float3 localUp, bool ignoreVerticalTriangles, out float3 closestPoint)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref Mesh.MeshData, ref float3, ref float3, bool, ref float3, void>)functionPointer)(ref data, ref testPosition, ref localUp, ignoreVerticalTriangles, ref closestPoint);
					return;
				}
			}
			BurstClosestPoint_Int32_0024BurstManaged(ref data, in testPosition, in localUp, ignoreVerticalTriangles, out closestPoint);
		}
	}

	private static bool InTriangleBurst(float3 a, float3 b, float3 c, float3 p)
	{
		float3 @float = b - a;
		float3 float2 = c - a;
		float3 y = p - a;
		float num = math.dot(@float, @float);
		float num2 = math.dot(@float, float2);
		float num3 = math.dot(@float, y);
		float num4 = math.dot(float2, float2);
		float num5 = math.dot(float2, y);
		float num6 = 1f / (num * num4 - num2 * num2);
		float num7 = (num4 * num3 - num2 * num5) * num6;
		float num8 = (num * num5 - num2 * num3) * num6;
		if (num7 >= 0f && num8 >= 0f)
		{
			return num7 + num8 < 1f;
		}
		return false;
	}

	public static Vector3 FindClosestPoint(Collider collider, Vector3 position)
	{
		return FindClosestPoint(collider, position, ignoreVerticalTriangles: false);
	}

	public static Vector3 FindClosestPoint(Collider collider, Vector3 position, bool ignoreVerticalTriangles)
	{
		if (NonConvexJumpDebug.Active)
		{
			NonConvexJumpDebug.Reset();
		}
		if (collider is MeshCollider { convex: false } meshCollider)
		{
			Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(meshCollider.sharedMesh);
			Transform transform = meshCollider.transform;
			position = transform.InverseTransformPoint(position);
			Vector3 source = transform.InverseTransformDirection(Vector3.up);
			Mesh.MeshData data = meshDataArray[0];
			float3 closestPoint = float3.zero;
			if (data.indexFormat == IndexFormat.UInt16)
			{
				BurstClosestPoint_Int16(ref data, in Unsafe.As<Vector3, float3>(ref position), in Unsafe.As<Vector3, float3>(ref source), ignoreVerticalTriangles, out closestPoint);
			}
			else
			{
				BurstClosestPoint_Int32(ref data, in Unsafe.As<Vector3, float3>(ref position), in Unsafe.As<Vector3, float3>(ref source), ignoreVerticalTriangles, out closestPoint);
			}
			meshDataArray.Dispose();
			return transform.TransformPoint(Unsafe.As<float3, Vector3>(ref closestPoint));
		}
		return collider.ClosestPoint(position);
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	private static void BurstClosestPoint_Int16(ref Mesh.MeshData data, in float3 testPosition, in float3 localUp, bool ignoreVerticalTriangles, out float3 closestPoint)
	{
		BurstClosestPoint_Int16_000004A9_0024BurstDirectCall.Invoke(ref data, in testPosition, in localUp, ignoreVerticalTriangles, out closestPoint);
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	private static void BurstClosestPoint_Int32(ref Mesh.MeshData data, in float3 testPosition, in float3 localUp, bool ignoreVerticalTriangles, out float3 closestPoint)
	{
		BurstClosestPoint_Int32_000004AA_0024BurstDirectCall.Invoke(ref data, in testPosition, in localUp, ignoreVerticalTriangles, out closestPoint);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	internal static void BurstClosestPoint_Int16_0024BurstManaged(ref Mesh.MeshData data, in float3 testPosition, in float3 localUp, bool ignoreVerticalTriangles, out float3 closestPoint)
	{
		closestPoint = float3.zero;
		float num = float.PositiveInfinity;
		NativeSlice<float3> vertexAttributeSlice = data.GetVertexAttributeSlice<float3>(VertexAttribute.Position);
		NativeArray<ushort> indexData = data.GetIndexData<ushort>();
		int i = 0;
		for (int num2 = indexData.Length / 3; i < num2; i++)
		{
			float3 @float = vertexAttributeSlice[indexData[3 * i]];
			float3 float2 = vertexAttributeSlice[indexData[3 * i + 1]];
			float3 float3 = vertexAttributeSlice[indexData[3 * i + 2]];
			float3 float4 = math.normalize(math.cross(float2 - @float, float3 - @float));
			float num3 = 0f - math.dot(float4, @float);
			if (ignoreVerticalTriangles && math.abs(math.dot(float4, localUp)) >= 0.9f)
			{
				continue;
			}
			float num4 = math.dot(float4, testPosition) + num3;
			float num5 = math.abs(num4);
			if (!(num5 >= num))
			{
				float3 float5 = testPosition - float4 * num4;
				if (InTriangleBurst(@float, float2, float3, float5))
				{
					num = num5;
					closestPoint = float5;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	internal static void BurstClosestPoint_Int32_0024BurstManaged(ref Mesh.MeshData data, in float3 testPosition, in float3 localUp, bool ignoreVerticalTriangles, out float3 closestPoint)
	{
		closestPoint = float3.zero;
		float num = float.PositiveInfinity;
		NativeSlice<float3> vertexAttributeSlice = data.GetVertexAttributeSlice<float3>(VertexAttribute.Position);
		NativeArray<int> indexData = data.GetIndexData<int>();
		int i = 0;
		for (int num2 = indexData.Length / 3; i < num2; i++)
		{
			float3 @float = vertexAttributeSlice[indexData[3 * i]];
			float3 float2 = vertexAttributeSlice[indexData[3 * i + 1]];
			float3 float3 = vertexAttributeSlice[indexData[3 * i + 2]];
			float3 float4 = math.normalize(math.cross(float2 - @float, float3 - @float));
			float num3 = 0f - math.dot(float4, @float);
			if (ignoreVerticalTriangles && math.abs(math.dot(float4, localUp)) >= 0.9f)
			{
				continue;
			}
			float num4 = math.dot(float4, testPosition) + num3;
			float num5 = math.abs(num4);
			if (!(num5 >= num))
			{
				float3 float5 = testPosition - float4 * num4;
				if (InTriangleBurst(@float, float2, float3, float5))
				{
					num = num5;
					closestPoint = float5;
				}
			}
		}
	}
}
