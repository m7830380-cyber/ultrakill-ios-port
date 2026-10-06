using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ULTRAKILL.Portal.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace ULTRAKILL.Portal;

[BurstCompile]
public class PortalScene : IDisposable
{
	public struct PortalRaySegment
	{
		public PortalHandle handle;

		public float3 start;

		public float3 end;

		public float3 direction;
	}

	internal delegate void CalculateMatrices_00002A31_0024PostfixBurstDelegate(int depth, in NativeArray<NativePortal> singleMatrices, ref NativeArray<float4x4> matrices);

	internal static class CalculateMatrices_00002A31_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(CalculateMatrices_00002A31_0024PostfixBurstDelegate).TypeHandle);
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

		static CalculateMatrices_00002A31_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(int depth, in NativeArray<NativePortal> singleMatrices, ref NativeArray<float4x4> matrices)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<int, ref NativeArray<NativePortal>, ref NativeArray<float4x4>, void>)functionPointer)(depth, ref singleMatrices, ref matrices);
					return;
				}
			}
			CalculateMatrices_0024BurstManaged(depth, in singleMatrices, ref matrices);
		}
	}

	internal delegate void Internal_FindCrossedPortals_00002A36_0024PostfixBurstDelegate(in NativePortalScene lastScene, in NativePortalScene currentScene, in float3 a, in float3 b, ref NativeList<NativePortalIntersection> intersections);

	internal static class Internal_FindCrossedPortals_00002A36_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(Internal_FindCrossedPortals_00002A36_0024PostfixBurstDelegate).TypeHandle);
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

		static Internal_FindCrossedPortals_00002A36_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in NativePortalScene lastScene, in NativePortalScene currentScene, in float3 a, in float3 b, ref NativeList<NativePortalIntersection> intersections)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref NativePortalScene, ref NativePortalScene, ref float3, ref float3, ref NativeList<NativePortalIntersection>, void>)functionPointer)(ref lastScene, ref currentScene, ref a, ref b, ref intersections);
					return;
				}
			}
			Internal_FindCrossedPortals_0024BurstManaged(in lastScene, in currentScene, in a, in b, ref intersections);
		}
	}

	internal delegate void Internal_FindPortalsBetween_00002A39_0024PostfixBurstDelegate(in NativePortalScene currentScene, in float3 start, in float3 end, ref NativeList<NativePortalIntersection> intersections, bool allowBackfaces = false);

	internal static class Internal_FindPortalsBetween_00002A39_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(Internal_FindPortalsBetween_00002A39_0024PostfixBurstDelegate).TypeHandle);
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

		static Internal_FindPortalsBetween_00002A39_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in NativePortalScene currentScene, in float3 start, in float3 end, ref NativeList<NativePortalIntersection> intersections, bool allowBackfaces = false)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref NativePortalScene, ref float3, ref float3, ref NativeList<NativePortalIntersection>, bool, void>)functionPointer)(ref currentScene, ref start, ref end, ref intersections, allowBackfaces);
					return;
				}
			}
			Internal_FindPortalsBetween_0024BurstManaged(in currentScene, in start, in end, ref intersections, allowBackfaces);
		}
	}

	internal delegate void Internal_TraversePortalSequence_00002A3A_0024PostfixBurstDelegate(in NativePortalScene currentScene, in float3 start, in float3 end, in float3 realEnd, ref NativeList<PortalRaySegment> segments, in NativeList<PortalHandle> handles, out bool result);

	internal static class Internal_TraversePortalSequence_00002A3A_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(Internal_TraversePortalSequence_00002A3A_0024PostfixBurstDelegate).TypeHandle);
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

		static Internal_TraversePortalSequence_00002A3A_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in NativePortalScene currentScene, in float3 start, in float3 end, in float3 realEnd, ref NativeList<PortalRaySegment> segments, in NativeList<PortalHandle> handles, out bool result)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref NativePortalScene, ref float3, ref float3, ref float3, ref NativeList<PortalRaySegment>, ref NativeList<PortalHandle>, ref bool, void>)functionPointer)(ref currentScene, ref start, ref end, ref realEnd, ref segments, ref handles, ref result);
					return;
				}
			}
			Internal_TraversePortalSequence_0024BurstManaged(in currentScene, in start, in end, in realEnd, ref segments, in handles, out result);
		}
	}

	public const int visionPortalDepth = 2;

	public List<PortalIdentifier> identifiers = new List<PortalIdentifier>();

	public Dictionary<PortalHandle, PortalIdentifier> portalIdentifiersLookup = new Dictionary<PortalHandle, PortalIdentifier>();

	public List<PortalHandleSequence> portalSequences = new List<PortalHandleSequence>();

	public List<bool> culledSequences = new List<bool>();

	public NativeArray<float4x4> sequenceMatrices;

	private NativeList<PortalHandle> sequenceHandleCache;

	private NativeList<PortalRaySegment> raySegmentCache;

	public NativeList<bool> visionPossible;

	public NativePortalScene lastScene;

	public NativePortalScene nativeScene;

	private PortalVisionJob visionJob;

	private NativeList<NativePortalIntersection> intersections;

	[BurstCompile]
	public static void CalculateMatrices(int depth, in NativeArray<NativePortal> singleMatrices, ref NativeArray<float4x4> matrices)
	{
		CalculateMatrices_00002A31_0024BurstDirectCall.Invoke(depth, in singleMatrices, ref matrices);
	}

	private void UpdatePortalSequences(int depth, int lastNumAddedSquences, NativeList<NativePortal> portals)
	{
		Span<bool> span = visionPossible.AsArray().AsSpan();
		Span<NativePortal> span2 = portals.AsArray().AsSpan();
		int length = span2.Length;
		int num;
		switch (depth)
		{
		case 0:
			portalSequences.Clear();
			culledSequences.Clear();
			portalSequences.Add(default(PortalHandleSequence));
			culledSequences.Add(item: false);
			num = 1;
			break;
		case 1:
		{
			for (int k = 0; k < length; k++)
			{
				PortalHandle handle2 = span2[k].handle;
				portalSequences.Add(new PortalHandleSequence(handle2));
				culledSequences.Add(item: false);
			}
			num = 1;
			break;
		}
		default:
		{
			num = portalSequences.Count;
			Span<PortalHandleSequence> span3 = CollectionsMarshal.AsSpan(portalSequences);
			for (int i = lastNumAddedSquences; i < num; i++)
			{
				PortalHandleSequence portalHandleSequence = span3[i];
				PortalHandle portalHandle = portalHandleSequence[portalHandleSequence.Count - 1];
				int num2 = (i - lastNumAddedSquences) % length;
				int num3 = ((portalHandle.side == PortalSide.Enter) ? (num2 + 1) : (num2 - 1));
				PortalHandleSequence item = portalHandleSequence.Append(PortalHandle.None);
				for (int j = 0; j < length; j++)
				{
					PortalHandle handle = span2[j].handle;
					bool flag = span[num3 * length + j];
					culledSequences.Add(num3 == j || flag);
					item[item.Count - 1] = handle;
					portalSequences.Add(item);
				}
			}
			break;
		}
		}
		if (depth < 2)
		{
			UpdatePortalSequences(depth + 1, num, portals);
		}
	}

	public PortalScene()
	{
		sequenceHandleCache = new NativeList<PortalHandle>(0, Allocator.Persistent);
		raySegmentCache = new NativeList<PortalRaySegment>(0, Allocator.Persistent);
		visionPossible = new NativeList<bool>(0, Allocator.Persistent);
	}

	public bool IsTraversable(Portal portal, PortalSide side, bool asEnemy = false)
	{
		PortalTravellerFlags travelFlags = portal.GetTravelFlags(side);
		return travelFlags.HasFlag(asEnemy ? PortalTravellerFlags.Enemy : PortalTravellerFlags.Player);
	}

	public float GetMinimumPassThroughSpeed(Portal portal, PortalSide side)
	{
		if (!portal)
		{
			return 0f;
		}
		if (side != PortalSide.Enter)
		{
			return portal.minimumExitSideSpeed;
		}
		return portal.minimumEntrySideSpeed;
	}

	[BurstCompile]
	private static void Internal_FindCrossedPortals(in NativePortalScene lastScene, in NativePortalScene currentScene, in float3 a, in float3 b, ref NativeList<NativePortalIntersection> intersections)
	{
		Internal_FindCrossedPortals_00002A36_0024BurstDirectCall.Invoke(in lastScene, in currentScene, in a, in b, ref intersections);
	}

	public bool FindCrossedPortals(Vector3 lastPos, Vector3 pos, out List<(PortalHandle, Vector3, float)> hitPortals)
	{
		hitPortals = new List<(PortalHandle, Vector3, float)>();
		intersections.Clear();
		Internal_FindCrossedPortals(in lastScene, in nativeScene, (float3)lastPos, (float3)pos, ref intersections);
		foreach (NativePortalIntersection intersection in intersections)
		{
			hitPortals.Add((intersection.handle, intersection.point, intersection.distance));
		}
		hitPortals.Sort(((PortalHandle, Vector3, float) a, (PortalHandle, Vector3, float) b) => a.Item3.CompareTo(b.Item3));
		return hitPortals.Count > 0;
	}

	public bool FindCrossedPortal(Vector3 lastPos, Vector3 pos, out PortalHandle handle, out Vector3 intersection)
	{
		handle = PortalHandle.None;
		intersection = Vector3.zero;
		if (!FindCrossedPortals(lastPos, pos, out var hitPortals))
		{
			return false;
		}
		(PortalHandle, Vector3, float) tuple = hitPortals[0];
		PortalHandle item = tuple.Item1;
		Vector3 item2 = tuple.Item2;
		handle = item;
		intersection = item2;
		return true;
	}

	[BurstCompile]
	private static void Internal_FindPortalsBetween(in NativePortalScene currentScene, in float3 start, in float3 end, ref NativeList<NativePortalIntersection> intersections, bool allowBackfaces = false)
	{
		Internal_FindPortalsBetween_00002A39_0024BurstDirectCall.Invoke(in currentScene, in start, in end, ref intersections, allowBackfaces);
	}

	[BurstCompile]
	private static void Internal_TraversePortalSequence(in NativePortalScene currentScene, in float3 start, in float3 end, in float3 realEnd, ref NativeList<PortalRaySegment> segments, in NativeList<PortalHandle> handles, out bool result)
	{
		Internal_TraversePortalSequence_00002A3A_0024BurstDirectCall.Invoke(in currentScene, in start, in end, in realEnd, ref segments, in handles, out result);
	}

	public unsafe bool TraversePortalSequence(Vector3 start, Vector3 end, Vector3 realEnd, PortalHandleSequence sequence, out NativeList<PortalRaySegment> outSegments)
	{
		Span<PortalHandle> span = sequence.AsSpan();
		int length = span.Length;
		raySegmentCache.GetUnsafeList()->m_length = 0;
		UnsafeList<PortalHandle>* unsafeList = sequenceHandleCache.GetUnsafeList();
		if (unsafeList->m_capacity < length)
		{
			unsafeList->Resize(length * 2);
		}
		unsafeList->m_length = length;
		PortalHandle* ptr = unsafeList->Ptr;
		for (int i = 0; i < length; i++)
		{
			PortalHandle portalHandle = span[i];
			ptr[i] = portalHandle;
		}
		Internal_TraversePortalSequence(in nativeScene, in Unsafe.As<Vector3, float3>(ref start), in Unsafe.As<Vector3, float3>(ref end), in Unsafe.As<Vector3, float3>(ref realEnd), ref raySegmentCache, in sequenceHandleCache, out var result);
		outSegments = raySegmentCache;
		return result;
	}

	public bool FindPortalsBetween(Vector3 start, Vector3 end, ref List<(PortalHandle, Vector3, float)> hitPortals, bool allowBackfaces = false)
	{
		hitPortals.Clear();
		intersections.Clear();
		Internal_FindPortalsBetween(in nativeScene, in Unsafe.As<Vector3, float3>(ref start), in Unsafe.As<Vector3, float3>(ref end), ref intersections, allowBackfaces);
		foreach (NativePortalIntersection intersection in intersections)
		{
			hitPortals.Add((intersection.handle, intersection.point, intersection.distance));
		}
		hitPortals.Sort(((PortalHandle, Vector3, float) a, (PortalHandle, Vector3, float) b) => a.Item3.CompareTo(b.Item3));
		return hitPortals.Count > 0;
	}

	public bool FindPortalBetween(Vector3 start, Vector3 end, out PortalHandle hitPortal, out Vector3 intersection, out float distance, bool allowBackfaces = false)
	{
		if (nativeScene.portals.Length == 0)
		{
			hitPortal = PortalHandle.None;
			intersection = default(Vector3);
			distance = 0f;
			return false;
		}
		intersections.Clear();
		Internal_FindPortalsBetween(in nativeScene, in Unsafe.As<Vector3, float3>(ref start), in Unsafe.As<Vector3, float3>(ref end), ref intersections);
		if (intersections.IsEmpty)
		{
			hitPortal = PortalHandle.None;
			intersection = default(Vector3);
			distance = 0f;
			return false;
		}
		intersections.Sort();
		NativePortalIntersection nativePortalIntersection = intersections[0];
		hitPortal = nativePortalIntersection.handle;
		intersection = nativePortalIntersection.point;
		distance = nativePortalIntersection.distance;
		return true;
	}

	public Matrix4x4 GetTravelMatrix(PortalHandle handle)
	{
		NativePortal nativePortal = nativeScene.LookupPortal(in handle);
		return nativePortal.travelMatrixManaged;
	}

	public Matrix4x4 GetTravelMatrix(in PortalHandleSequence travelHandles)
	{
		Matrix4x4 matrix4x = Matrix4x4.identity;
		int count = travelHandles.Count;
		for (int i = 0; i < count; i++)
		{
			PortalHandle handle = travelHandles[i];
			matrix4x = GetTravelMatrix(handle) * matrix4x;
		}
		return matrix4x;
	}

	public Matrix4x4 GetTravelMatrix(PortalTraversalV2[] travelHandles)
	{
		Matrix4x4 matrix4x = Matrix4x4.identity;
		int num = travelHandles.Length;
		for (int i = 0; i < num; i++)
		{
			PortalHandle portalHandle = travelHandles[i].portalHandle;
			matrix4x = GetTravelMatrix(portalHandle) * matrix4x;
		}
		return matrix4x;
	}

	public Portal GetPortalObject(PortalHandle handle)
	{
		return Resources.InstanceIDToObject(handle.instanceId) as Portal;
	}

	public string GetPortalName(PortalHandle handle)
	{
		Portal portalObject = GetPortalObject(handle);
		if (handle.side != PortalSide.Enter)
		{
			return portalObject.exit.name;
		}
		return portalObject.entry.name;
	}

	public void Sync(List<Portal> portals)
	{
		if (lastScene.valid)
		{
			lastScene.Dispose();
		}
		lastScene = nativeScene;
		identifiers.Clear();
		portalIdentifiersLookup.Clear();
		nativeScene = NativePortalScene.Create(portals, identifiers, portalIdentifiersLookup);
		int length = nativeScene.portals.Length;
		visionPossible.Resize(length * length, NativeArrayOptions.UninitializedMemory);
		visionPossible.AsArray().AsSpan().Fill(value: true);
		if (!intersections.IsCreated)
		{
			intersections = new NativeList<NativePortalIntersection>(Allocator.Persistent);
		}
		else
		{
			intersections.Clear();
		}
		portalSequences.Clear();
		culledSequences.Clear();
		UpdatePortalSequences(0, 0, nativeScene.portals);
		if (sequenceMatrices.IsCreated)
		{
			sequenceMatrices.Dispose();
		}
		sequenceMatrices = new NativeArray<float4x4>(portalSequences.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		CalculateMatrices(2, nativeScene.portals.AsArray(), ref sequenceMatrices);
	}

	public void Dispose()
	{
		if (lastScene.valid)
		{
			lastScene.Dispose();
		}
		if (nativeScene.valid)
		{
			nativeScene.Dispose();
		}
		sequenceHandleCache.Dispose();
		raySegmentCache.Dispose();
		sequenceMatrices.Dispose();
		visionPossible.Dispose();
		intersections.Dispose();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void CalculateMatrices_0024BurstManaged(int depth, in NativeArray<NativePortal> singleMatrices, ref NativeArray<float4x4> matrices)
	{
		int length = singleMatrices.Length;
		int num = 0;
		int num2 = length;
		int num3 = length;
		for (int i = 0; i <= depth; i++)
		{
			switch (i)
			{
			case 0:
				matrices[0] = float4x4.identity;
				continue;
			case 1:
			{
				for (int j = 0; j < length; j++)
				{
					matrices[j + 1] = singleMatrices[j].travelMatrix;
				}
				num = 1;
				num2 = length + 1;
				num3 = length;
				continue;
			}
			}
			for (int k = 0; k < num3; k++)
			{
				float4x4 b = matrices[num + k];
				for (int l = 0; l < length; l++)
				{
					int index = num2 + k * length + l;
					matrices[index] = math.mul(singleMatrices[l].travelMatrix, b);
				}
			}
			num3 *= length;
			num = num2;
			num2 += num3;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void Internal_FindCrossedPortals_0024BurstManaged(in NativePortalScene lastScene, in NativePortalScene currentScene, in float3 a, in float3 b, ref NativeList<NativePortalIntersection> intersections)
	{
		float3 @float = b - a;
		int length = currentScene.portals.Length;
		for (int i = 0; i < length; i++)
		{
			NativePortal nativePortal = currentScene.portals[i];
			NativePortal nativePortal2 = nativePortal;
			if (lastScene.valid)
			{
				ref NativePortal reference = ref lastScene.LookupPortal(in nativePortal.handle);
				if (reference.valid)
				{
					nativePortal2 = reference;
				}
			}
			float4x4 toWorld = nativePortal.transform.toWorld;
			float4x4 toWorld2 = nativePortal2.transform.toWorld;
			float3 xyz = toWorld.c3.xyz;
			float3 xyz2 = toWorld2.c3.xyz;
			float3 float2 = math.normalizesafe(toWorld.c2.xyz);
			float3 float3 = math.normalizesafe(toWorld2.c2.xyz);
			quaternion quaternion = math.quaternion(toWorld2);
			quaternion quaternion2 = math.quaternion(toWorld);
			float3 x = math.normalizesafe(math.mul(quaternion2, math.inverse(quaternion)).value.xyz);
			float3 float4 = xyz - xyz2;
			float3 x2 = @float - float4;
			float num = math.dot(a - xyz2, float3);
			float num2 = math.dot(b - xyz, float2);
			bool flag = num <= 0.001f && num2 > -0.001f && (num * num2 < 0f || (num <= 0f && num2 > 0f));
			if (math.abs(num) > 5f || !flag || num * num2 > 0f)
			{
				continue;
			}
			float num3 = 0.5f;
			for (int j = 0; j < 20; j++)
			{
				float3 float5 = math.lerp(xyz2, xyz, num3);
				float3 float6 = math.lerp(a, b, num3);
				float3 y = math.normalizesafe(math.lerp(float3, float2, num3));
				float3 x3 = float6 - float5;
				float num4 = math.dot(x3, y);
				float num5 = math.dot(x2, y);
				float3 y2 = math.cross(x, y);
				float num6 = math.dot(x3, y2);
				float num7 = num5 + num6;
				if (math.abs(num7) < 0.0001f)
				{
					break;
				}
				num3 = math.clamp(num3 - num4 / num7, 0f, 1f);
				if (math.abs(num4) < 0.001f)
				{
					break;
				}
			}
			float3 translation = math.lerp(xyz2, xyz, num3);
			float3 float7 = math.lerp(a, b, num3);
			quaternion rotation = math.slerp(quaternion, quaternion2, num3);
			float3 float8 = math.transform(math.fastinverse(new float4x4(rotation, translation)), float7);
			if (!(math.abs(float8.x) > nativePortal.dimensions.x / 2f) && !(math.abs(float8.y) > nativePortal.dimensions.y / 2f))
			{
				NativePortalIntersection value = new NativePortalIntersection
				{
					handle = nativePortal.handle,
					distance = math.length(b - a) * num3,
					point = float7
				};
				intersections.Add(in value);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void Internal_FindPortalsBetween_0024BurstManaged(in NativePortalScene currentScene, in float3 start, in float3 end, ref NativeList<NativePortalIntersection> intersections, bool allowBackfaces = false)
	{
		PortalRay ray = new PortalRay(start, end);
		foreach (NativePortal portal in currentScene.portals)
		{
			if (portal.Raycast(in ray, out var intersection, allowBackfaces))
			{
				intersections.Add(in intersection);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal static void Internal_TraversePortalSequence_0024BurstManaged(in NativePortalScene currentScene, in float3 start, in float3 end, in float3 realEnd, ref NativeList<PortalRaySegment> segments, in NativeList<PortalHandle> handles, out bool result)
	{
		result = true;
		PortalRay ray = new PortalRay(start, end);
		foreach (PortalHandle handle2 in handles)
		{
			PortalHandle handle = handle2;
			NativePortal portal = currentScene.LookupPortal(in handle);
			if (portal.valid)
			{
				if (!portal.Raycast(in ray, out var intersection))
				{
					result = false;
					return;
				}
				float num = intersection.distance * intersection.distance;
				if (num > ray.distanceSq)
				{
					result = false;
					return;
				}
				segments.Add(new PortalRaySegment
				{
					start = ray.start,
					end = intersection.point,
					direction = ray.direction,
					handle = portal.handle
				});
				float4x4 travelMatrix = portal.travelMatrix;
				float3 start2 = math.transform(travelMatrix, intersection.point);
				float3 direction = math.rotate(travelMatrix, ray.direction);
				ray.start = start2;
				ray.direction = direction;
				ray.distanceSq -= num;
			}
		}
		segments.Add(new PortalRaySegment
		{
			start = ray.start,
			end = realEnd,
			direction = ray.direction,
			handle = PortalHandle.None
		});
	}
}
