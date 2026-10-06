using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class SeaBodies : MonoBehaviour
{
	[BurstCompile]
	public struct VibrateSeaBodiesJob : IJobParallelFor
	{
		public float intensity;

		public float deltaTime;

		public float3 cameraPosition;

		public NativeArray<float3> originalPos;

		public NativeArray<float3> originalScale;

		public NativeArray<float3> targetPos;

		public NativeArray<float> speeds;

		public NativeArray<float3> currentPos;

		public NativeArray<Matrix4x4> instanceMatrices;

		public NativeArray<Unity.Mathematics.Random> randomArray;

		public void Execute(int i)
		{
			float3 @float = currentPos[i];
			float3 float2 = targetPos[i];
			float num = speeds[i];
			float3 float3 = originalPos[i];
			Unity.Mathematics.Random value = randomArray[i];
			math.distance(@float, float2);
			if (math.distance(@float, float2) < 0.001f)
			{
				@float = float2;
				float3 float4 = value.NextFloat3Direction() * intensity;
				float2 = float3 + float4;
				targetPos[i] = float2;
			}
			else
			{
				@float = Vector3.MoveTowards(@float, float2, num * deltaTime);
			}
			currentPos[i] = @float;
			float3 float5 = new float3(cameraPosition.x, @float.y, cameraPosition.z) - @float;
			quaternion quaternion = quaternion.identity;
			if (math.lengthsq(float5) > 0.0001f)
			{
				quaternion = quaternion.LookRotationSafe(float5, math.up());
			}
			randomArray[i] = value;
			instanceMatrices[i] = Matrix4x4.TRS(@float, quaternion, originalScale[i]);
		}
	}

	public float intensity = 1f;

	public float speedMin = 4f;

	public float speedMax = 5f;

	public Texture2D textureAtlas;

	private NativeArray<float3> originalPositions;

	private NativeArray<float3> originalScales;

	private NativeArray<float3> targetPositions;

	private NativeArray<float> speeds;

	private NativeArray<float3> currentPositions;

	private NativeArray<Unity.Mathematics.Random> randomStates;

	private NativeArray<Matrix4x4> instanceMatricesNative;

	private JobHandle jobHandle;

	public Mesh seaBodyMesh;

	public int atlasCount = 2;

	public Material seaBodyMaterial;

	private int[] instanceAtlasOffset;

	private Vector4[] instanceColors;

	private int instanceCount;

	private MaterialPropertyBlock mpb;

	private float[] atlasOffsetBuffer = new float[1023];

	private Vector4[] colorsBuffer = new Vector4[1023];

	private Matrix4x4[] subMatrices = new Matrix4x4[1023];

	private void Start()
	{
		List<Transform> leafChildrenOfAllChunks = GetLeafChildrenOfAllChunks(base.transform);
		seaBodyMaterial.SetFloat("_AtlasCount", atlasCount);
		instanceCount = leafChildrenOfAllChunks.Count;
		originalPositions = new NativeArray<float3>(instanceCount, Allocator.Persistent);
		originalScales = new NativeArray<float3>(instanceCount, Allocator.Persistent);
		targetPositions = new NativeArray<float3>(instanceCount, Allocator.Persistent);
		speeds = new NativeArray<float>(instanceCount, Allocator.Persistent);
		randomStates = new NativeArray<Unity.Mathematics.Random>(instanceCount, Allocator.Persistent);
		currentPositions = new NativeArray<float3>(instanceCount, Allocator.Persistent);
		instanceMatricesNative = new NativeArray<Matrix4x4>(instanceCount, Allocator.Persistent);
		instanceAtlasOffset = new int[instanceCount];
		instanceColors = new Vector4[instanceCount];
		uint num = 12345u;
		for (int i = 0; i < instanceCount; i++)
		{
			SpriteRenderer component = leafChildrenOfAllChunks[i].GetComponent<SpriteRenderer>();
			instanceColors[i] = component.color;
			instanceAtlasOffset[i] = ((!component.sprite.name.Contains("1")) ? 1 : 0);
			float3 @float = leafChildrenOfAllChunks[i].position;
			originalPositions[i] = @float;
			targetPositions[i] = @float;
			speeds[i] = UnityEngine.Random.Range(speedMin, speedMax);
			quaternion quaternion = leafChildrenOfAllChunks[i].rotation;
			currentPositions[i] = @float;
			float3 float2 = leafChildrenOfAllChunks[i].lossyScale * new float3(1f, 2f, 1f);
			originalScales[i] = float2;
			instanceMatricesNative[i] = Matrix4x4.TRS(@float, quaternion, float2);
			randomStates[i] = new Unity.Mathematics.Random((uint)((int)(num + i * 31) | 1));
			component.enabled = false;
		}
	}

	private static List<Transform> FindAllChildrenContainingName(Transform parent, string substring)
	{
		List<Transform> list = new List<Transform>();
		foreach (Transform item in parent)
		{
			if (item.name.Contains(substring))
			{
				list.Add(item);
			}
			list.AddRange(FindAllChildrenContainingName(item, substring));
		}
		return list;
	}

	public static List<Transform> GetAllLeafChildren(Transform parent)
	{
		List<Transform> list = new List<Transform>();
		foreach (Transform item in parent)
		{
			if (item.childCount == 0)
			{
				list.Add(item);
			}
			else
			{
				list.AddRange(GetAllLeafChildren(item));
			}
		}
		return list;
	}

	public static List<Transform> GetLeafChildrenOfAllChunks(Transform root)
	{
		List<Transform> list = FindAllChildrenContainingName(root, "Chunk");
		List<Transform> list2 = new List<Transform>();
		foreach (Transform item in list)
		{
			list2.AddRange(GetAllLeafChildren(item));
		}
		return list2;
	}

	private void Update()
	{
		CameraController instance = MonoSingleton<CameraController>.Instance;
		if ((bool)instance)
		{
			VibrateSeaBodiesJob jobData = new VibrateSeaBodiesJob
			{
				intensity = intensity,
				deltaTime = Time.deltaTime,
				cameraPosition = instance.cam.transform.position,
				originalPos = originalPositions,
				originalScale = originalScales,
				targetPos = targetPositions,
				speeds = speeds,
				currentPos = currentPositions,
				instanceMatrices = instanceMatricesNative,
				randomArray = randomStates
			};
			jobHandle = IJobParallelForExtensions.Schedule(jobData, instanceCount, 64);
		}
	}

	private void LateUpdate()
	{
		jobHandle.Complete();
		if (mpb == null)
		{
			mpb = new MaterialPropertyBlock();
		}
		int num = 0;
		int num2 = instanceCount;
		while (num2 > 0)
		{
			int num3 = Mathf.Min(1023, num2);
			for (int i = 0; i < num3; i++)
			{
				int num4 = num + i;
				atlasOffsetBuffer[i] = instanceAtlasOffset[num4];
				colorsBuffer[i] = instanceColors[num4];
				subMatrices[i] = instanceMatricesNative[num4];
			}
			mpb.Clear();
			mpb.SetFloatArray("_AtlasOffsetArray", atlasOffsetBuffer);
			mpb.SetVectorArray("_PerInstanceColor", colorsBuffer);
			Graphics.DrawMeshInstanced(seaBodyMesh, 0, seaBodyMaterial, subMatrices, num3, mpb, ShadowCastingMode.Off, receiveShadows: false);
			num += num3;
			num2 -= num3;
		}
	}

	private void OnDestroy()
	{
		originalPositions.Dispose();
		originalScales.Dispose();
		targetPositions.Dispose();
		speeds.Dispose();
		currentPositions.Dispose();
		randomStates.Dispose();
		instanceMatricesNative.Dispose();
	}
}
