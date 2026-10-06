using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace ULTRAKILL.Portal.Native;

[BurstCompile]
public struct PortalVertices : IEnumerable<float3>, IEnumerable
{
	public float3 v0;

	public float3 v1;

	public float3 v2;

	public float3 v3;

	public int Length => 4;

	public float3 this[int index]
	{
		get
		{
			if ((uint)index > 3u)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return Unsafe.Add(ref v0, index);
		}
	}

	public IEnumerator<float3> GetEnumerator()
	{
		yield return v0;
		yield return v1;
		yield return v2;
		yield return v3;
	}

	public PortalVertices(float4x4 trans, float2 dim)
	{
		float3 xyz = trans.c3.xyz;
		float3 xyz2 = trans.c0.xyz;
		float3 xyz3 = trans.c1.xyz;
		float3 float5 = -xyz2;
		float3 float6 = -xyz3;
		v0 = xyz + (float5 * dim.x + xyz3 * dim.y);
		v1 = xyz + (xyz2 * dim.x + xyz3 * dim.y);
		v2 = xyz + (xyz2 * dim.x + float6 * dim.y);
		v3 = xyz + (float5 * dim.x + float6 * dim.y);
	}

	public bool IsBehindPlane(float3 origin, float3 normal)
	{
		float3 x = v0 - origin;
		float3 x2 = v1 - origin;
		float3 x3 = v2 - origin;
		float3 x4 = v3 - origin;
		if (math.dot(x, normal) > 0f)
		{
			return false;
		}
		if (math.dot(x2, normal) > 0f)
		{
			return false;
		}
		if (math.dot(x3, normal) > 0f)
		{
			return false;
		}
		if (math.dot(x4, normal) > 0f)
		{
			return false;
		}
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
