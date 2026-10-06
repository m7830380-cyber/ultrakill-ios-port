using System;
using Interop;
using Interop.core;
using ULTRAKILL.Portal.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

namespace ULTRAKILL.Portal;

[BurstCompile]
public struct ParticleResponseJob : IJobParticleSystem
{
	public float4x4 toLocal;

	public float4x4 toWorld;

	[ReadOnly]
	public NativePortalScene scene;

	[NativeDisableContainerSafetyRestriction]
	[ReadOnly]
	public NativeSlice<IntersectionAndIndex> intersections;

	[NativeDisableContainerSafetyRestriction]
	[ReadOnly]
	public NativeSlice<RaycastHit> hits;

	[NativeDisableUnsafePtrRestriction]
	public unsafe ParticleTrails* trails;

	public unsafe void Execute(ParticleSystemJobData jobData)
	{
		ParticleSystemNativeArray4 customData = jobData.customData1;
		int count = jobData.count;
		for (int i = 0; i < count; i++)
		{
			NativePortalIntersection intersection = intersections[i].intersection;
			RaycastHit raycastHit = hits[i];
			bool flag = intersection.handle.IsValid();
			bool flag2 = raycastHit.colliderInstanceID != 0;
			if (!flag && !flag2)
			{
				continue;
			}
			if (flag && (!flag2 || intersection.distance < raycastHit.distance))
			{
				PortalHandle handle = intersection.handle;
				NativePortal nativePortal = scene.LookupPortal(in handle);
				if (nativePortal.valid)
				{
					float4x4 travelMatrix = nativePortal.travelMatrix;
					customData[i] = math.float4(math.transform(travelMatrix, intersection.point), 1f);
					ParticleSystemNativeArray3 positions = jobData.positions;
					ParticleSystemNativeArray3 velocities = jobData.velocities;
					Vector3 vector = positions[i];
					Vector3 vector2 = velocities[i];
					float3 b = math.transform(toWorld, vector);
					b = math.transform(travelMatrix, b);
					float3 b2 = math.rotate(toWorld, vector2);
					b2 = math.rotate(travelMatrix, b2);
					positions[i] = math.transform(toLocal, b);
					velocities[i] = math.rotate(toLocal, b2);
					vector<Vector4> positions2 = trails->m_Positions;
					int num = (int)trails->m_MaxPositionsPerTrail;
					Span<float4> span = new Span<float4>(positions2.data(), (int)positions2.size()).Slice(num * i, num);
					for (int j = 0; j < span.Length; j++)
					{
						ref float4 reference = ref span[j];
						float3 b3 = math.transform(toWorld, reference.xyz);
						b3 = math.transform(travelMatrix, b3);
						reference = new float4(math.transform(toLocal, b3), reference.w);
					}
				}
			}
			else
			{
				NativeArray<float> aliveTimePercent = jobData.aliveTimePercent;
				aliveTimePercent[i] = 100f;
			}
		}
	}
}
