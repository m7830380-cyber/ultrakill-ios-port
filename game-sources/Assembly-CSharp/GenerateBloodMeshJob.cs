using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GenerateBloodMeshJob : IJobParallelFor
{
	public struct VertexData
	{
		public float3 position;

		public half4 normal_Offset;

		public half2 uv;

		public float3 center;
	}

	[ReadOnly]
	public NativeArray<BloodsplatterManager.InstanceProperties> props;

	public Mesh.MeshData meshData;

	public bool isUInt16;

	public void Execute(int index)
	{
		NativeArray<VertexData> vertexData = meshData.GetVertexData<VertexData>();
		float3 pos = props[index].pos;
		float3 norm = props[index].norm;
		math.orthonormal_basis(norm, out var basis, out var _);
		float4x4 a = float4x4.TRS(pos, math.mul(quaternion.LookRotation(norm, basis), quaternion.RotateZ(index % 359)), new float3(1.28f, 1.28f, 1f));
		float4 float5 = math.mul(a, new float4(-1f, 1f, 0f, 1f));
		float4 float6 = math.mul(a, new float4(1f, 1f, 0f, 1f));
		float4 float7 = math.mul(a, new float4(1f, -1f, 0f, 1f));
		float4 float8 = math.mul(a, new float4(-1f, -1f, 0f, 1f));
		int num = index * 4;
		int num2 = num + 1;
		int num3 = num + 2;
		int num4 = num + 3;
		half4 normal_Offset = (half4)new float4(norm, index);
		half half5 = new half(0f);
		half half6 = new half(1f);
		vertexData[num] = new VertexData
		{
			position = float5.xyz,
			normal_Offset = normal_Offset,
			uv = new half2(half5, half5),
			center = pos
		};
		vertexData[num2] = new VertexData
		{
			position = float6.xyz,
			normal_Offset = normal_Offset,
			uv = new half2(half6, half5),
			center = pos
		};
		vertexData[num3] = new VertexData
		{
			position = float7.xyz,
			normal_Offset = normal_Offset,
			uv = new half2(half6, half6),
			center = pos
		};
		vertexData[num4] = new VertexData
		{
			position = float8.xyz,
			normal_Offset = normal_Offset,
			uv = new half2(half5, half6),
			center = pos
		};
		int num5 = index * 6;
		int index2 = num5 + 1;
		int index3 = num5 + 2;
		int index4 = num5 + 3;
		int index5 = num5 + 4;
		int index6 = num5 + 5;
		if (isUInt16)
		{
			NativeArray<ushort> indexData = meshData.GetIndexData<ushort>();
			indexData[num5] = Convert.ToUInt16(num);
			indexData[index2] = Convert.ToUInt16(num2);
			indexData[index3] = Convert.ToUInt16(num3);
			indexData[index4] = Convert.ToUInt16(num);
			indexData[index5] = Convert.ToUInt16(num3);
			indexData[index6] = Convert.ToUInt16(num4);
		}
		else
		{
			NativeArray<uint> indexData2 = meshData.GetIndexData<uint>();
			indexData2[num5] = Convert.ToUInt32(num);
			indexData2[index2] = Convert.ToUInt32(num2);
			indexData2[index3] = Convert.ToUInt32(num3);
			indexData2[index4] = Convert.ToUInt32(num);
			indexData2[index5] = Convert.ToUInt32(num3);
			indexData2[index6] = Convert.ToUInt32(num4);
		}
	}
}
