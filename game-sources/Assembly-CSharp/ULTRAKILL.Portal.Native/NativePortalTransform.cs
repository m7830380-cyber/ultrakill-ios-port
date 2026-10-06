using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace ULTRAKILL.Portal.Native;

public struct NativePortalTransform
{
	public float4x4 toWorld;

	public float4x4 toLocal;

	public readonly Matrix4x4 toWorldManaged => Unsafe.As<float4x4, Matrix4x4>(ref Unsafe.AsRef(in toWorld));

	public readonly Matrix4x4 toLocalManaged => Unsafe.As<float4x4, Matrix4x4>(ref Unsafe.AsRef(in toLocal));

	public readonly float3 right => toWorld.c0.xyz;

	public readonly float3 left => -toWorld.c0.xyz;

	public readonly float3 up => toWorld.c1.xyz;

	public readonly float3 down => -toWorld.c1.xyz;

	public readonly float3 forward => toWorld.c2.xyz;

	public readonly float3 back => -toWorld.c2.xyz;

	public readonly float3 center => toWorld.c3.xyz;

	public readonly Vector3 rightManaged => Unsafe.As<float3, Vector3>(ref Unsafe.AsRef<float3>(toWorld.c0.xyz));

	public readonly Vector3 leftManaged => Unsafe.As<float3, Vector3>(ref Unsafe.AsRef<float3>(-toWorld.c0.xyz));

	public readonly Vector3 upManaged => Unsafe.As<float3, Vector3>(ref Unsafe.AsRef<float3>(toWorld.c1.xyz));

	public readonly Vector3 downManaged => Unsafe.As<float3, Vector3>(ref Unsafe.AsRef<float3>(-toWorld.c1.xyz));

	public readonly Vector3 forwardManaged => Unsafe.As<float3, Vector3>(ref Unsafe.AsRef<float3>(toWorld.c2.xyz));

	public readonly Vector3 backManaged => Unsafe.As<float3, Vector3>(ref Unsafe.AsRef<float3>(-toWorld.c2.xyz));

	public readonly Vector3 centerManaged => Unsafe.As<float3, Vector3>(ref Unsafe.AsRef<float3>(toWorld.c3.xyz));

	[BurstCompile]
	public Vector3 WorldToLocal(in float3 world)
	{
		return math.transform(toLocal, world);
	}

	[BurstCompile]
	public Vector3 LocalToWorld(in float3 local)
	{
		return math.transform(toWorld, local);
	}
}
