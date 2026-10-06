using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ULTRAKILL.Portal.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace ULTRAKILL.Portal;

[BurstCompile]
public class PortalRenderV2 : MonoBehaviour
{
	public struct OnscreenPortalData
	{
		public ushort handleIndex;

		public int depth;

		public int parentOnscreenIndex;

		public int depthSortedIndex;

		public float nearClip;
	}

	private struct Vertex
	{
		public float3 position;

		public half2 uv;

		public Vertex(float3 position, half2 uv)
		{
			this.position = position;
			this.uv = uv;
		}
	}

	public enum PortalLightType
	{
		Player,
		Enemy,
		Environment
	}

	public struct RenderData
	{
		public Vector3 viewPos;

		public Quaternion viewRot;

		public Matrix4x4 enterViewMatrix;

		public Matrix4x4 cullingMatrix;

		public Matrix4x4 parentViewMatrix;

		public ushort parentIndex;

		public ushort handleIndex;

		public ushort onscreenIndex;

		internal bool inMirroredSpace;

		internal bool canSeeItself;

		internal bool outMirroredSpace;

		internal int cullingMask;

		internal int depth;

		internal int maxDepth;

		internal bool useFog;

		internal Color fogColor;

		internal float fogStart;

		internal float fogEnd;

		internal float4 clipPlane;
	}

	private struct PrepassData
	{
		public int handleIndex;

		public ushort parentHandleIndex;

		public float4x4 viewMatrix;

		public ushort onscreenIndex;

		public bool inMirroredSpace;

		public float4 clipPlane;

		public PrepassData(int handleIndex, ushort parentHandleIndex, Matrix4x4 viewMatrix, ushort onscreenIndex, float4 clipPlane, bool inMirroredSpace)
		{
			this.handleIndex = handleIndex;
			this.parentHandleIndex = parentHandleIndex;
			this.viewMatrix = viewMatrix;
			this.onscreenIndex = onscreenIndex;
			this.inMirroredSpace = inMirroredSpace;
			this.clipPlane = clipPlane;
		}
	}

	private struct PortalView
	{
		public CameraData camData;

		public int depth;

		public int maxDepth;

		public ushort parentHandleIndex;

		public int parentOnscreenIndex;

		public int ignoreIndex;

		public bool inMirroredSpace;

		public float4x4 parentViewMatrix;

		public float4 clipPlane;

		public float4 parentClipPlane;

		public FogData lastFogData;
	}

	public struct DepthComparer : IComparer<int>
	{
		[ReadOnly]
		public NativeArray<OnscreenPortalData> Data;

		public int Compare(int a, int b)
		{
			return Data[a].depth.CompareTo(Data[b].depth);
		}
	}

	internal unsafe delegate void RebuildMesh_00002A09_0024PostfixBurstDelegate(NativePortal* portalsSpan, in int spanLength, SubMeshDescriptor* subMeshes, ref Mesh.MeshData data);

	internal static class RebuildMesh_00002A09_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(RebuildMesh_00002A09_0024PostfixBurstDelegate).TypeHandle);
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

		static RebuildMesh_00002A09_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(NativePortal* portalsSpan, in int spanLength, SubMeshDescriptor* subMeshes, ref Mesh.MeshData data)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<NativePortal*, ref int, SubMeshDescriptor*, ref Mesh.MeshData, void>)functionPointer)(portalsSpan, ref spanLength, subMeshes, ref data);
					return;
				}
			}
			RebuildMesh_0024BurstManaged(portalsSpan, in spanLength, subMeshes, ref data);
		}
	}

	internal unsafe delegate void BurstRenderData_00002A0A_0024PostfixBurstDelegate(in NativePortalScene nativeScene, int mainCullingMask, in FogData defaultFogData, float maxDistance, float farClipPlane, int screenArea, float minPortalPixelArea, bool doOcclusionPass, in float4x4 defaultProjGPU, in float4x4 defaultProj, in float4x4 mirrorMatrix, ref NativeList<PortalView> portalViewStack, ref NativeList<PrepassData> prepass, ref NativeList<OnscreenPortalData> onscreenPortalData, ref NativeList<float4x4> cullingFrusta, ref NativeHashMap<int, ushort> textureStoreMap, float3* clipBufferA, float3* clipBufferB, float4* frustumPlanes, ref NativeList<RenderData> portalRenders);

	internal static class BurstRenderData_00002A0A_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(BurstRenderData_00002A0A_0024PostfixBurstDelegate).TypeHandle);
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

		static BurstRenderData_00002A0A_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(in NativePortalScene nativeScene, int mainCullingMask, in FogData defaultFogData, float maxDistance, float farClipPlane, int screenArea, float minPortalPixelArea, bool doOcclusionPass, in float4x4 defaultProjGPU, in float4x4 defaultProj, in float4x4 mirrorMatrix, ref NativeList<PortalView> portalViewStack, ref NativeList<PrepassData> prepass, ref NativeList<OnscreenPortalData> onscreenPortalData, ref NativeList<float4x4> cullingFrusta, ref NativeHashMap<int, ushort> textureStoreMap, float3* clipBufferA, float3* clipBufferB, float4* frustumPlanes, ref NativeList<RenderData> portalRenders)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref NativePortalScene, int, ref FogData, float, float, int, float, bool, ref float4x4, ref float4x4, ref float4x4, ref NativeList<PortalView>, ref NativeList<PrepassData>, ref NativeList<OnscreenPortalData>, ref NativeList<float4x4>, ref NativeHashMap<int, ushort>, float3*, float3*, float4*, ref NativeList<RenderData>, void>)functionPointer)(ref nativeScene, mainCullingMask, ref defaultFogData, maxDistance, farClipPlane, screenArea, minPortalPixelArea, doOcclusionPass, ref defaultProjGPU, ref defaultProj, ref mirrorMatrix, ref portalViewStack, ref prepass, ref onscreenPortalData, ref cullingFrusta, ref textureStoreMap, clipBufferA, clipBufferB, frustumPlanes, ref portalRenders);
					return;
				}
			}
			BurstRenderData_0024BurstManaged(in nativeScene, mainCullingMask, in defaultFogData, maxDistance, farClipPlane, screenArea, minPortalPixelArea, doOcclusionPass, in defaultProjGPU, in defaultProj, in mirrorMatrix, ref portalViewStack, ref prepass, ref onscreenPortalData, ref cullingFrusta, ref textureStoreMap, clipBufferA, clipBufferB, frustumPlanes, ref portalRenders);
		}
	}

	internal delegate void SortPortals_00002A0B_0024PostfixBurstDelegate(ref NativeArray<OnscreenPortalData> data, ref NativeArray<int> indices);

	internal static class SortPortals_00002A0B_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(SortPortals_00002A0B_0024PostfixBurstDelegate).TypeHandle);
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

		static SortPortals_00002A0B_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(ref NativeArray<OnscreenPortalData> data, ref NativeArray<int> indices)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref NativeArray<OnscreenPortalData>, ref NativeArray<int>, void>)functionPointer)(ref data, ref indices);
					return;
				}
			}
			SortPortals_0024BurstManaged(ref data, ref indices);
		}
	}

	internal unsafe delegate void ExtractFrustumPlanes_00002A0D_0024PostfixBurstDelegate(float4x4* matrix, float4* planes);

	internal static class ExtractFrustumPlanes_00002A0D_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(ExtractFrustumPlanes_00002A0D_0024PostfixBurstDelegate).TypeHandle);
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

		static ExtractFrustumPlanes_00002A0D_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(float4x4* matrix, float4* planes)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<float4x4*, float4*, void>)functionPointer)(matrix, planes);
					return;
				}
			}
			ExtractFrustumPlanes_0024BurstManaged(matrix, planes);
		}
	}

	internal unsafe delegate ushort GetOnscreenPortalsBurst_00002A0E_0024PostfixBurstDelegate(in NativePortalScene scene, float3* enterPos, in float4x4 enterViewM, float4x4* enterCullM, float maxDistance, float farClipPlane, int ignoreIndex, int depth, int parentOnscreenIndex, float4* frustumPlanes, float3* clipBufferA, float3* clipBufferB, int screenArea, float minPortalPixelArea, in float4x4 defaultProjectionMatrix, ref NativeList<float4x4> cullingFrusta, ref NativeList<OnscreenPortalData> onscreenPortalData);

	internal static class GetOnscreenPortalsBurst_00002A0E_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(GetOnscreenPortalsBurst_00002A0E_0024PostfixBurstDelegate).TypeHandle);
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

		static GetOnscreenPortalsBurst_00002A0E_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static ushort Invoke(in NativePortalScene scene, float3* enterPos, in float4x4 enterViewM, float4x4* enterCullM, float maxDistance, float farClipPlane, int ignoreIndex, int depth, int parentOnscreenIndex, float4* frustumPlanes, float3* clipBufferA, float3* clipBufferB, int screenArea, float minPortalPixelArea, in float4x4 defaultProjectionMatrix, ref NativeList<float4x4> cullingFrusta, ref NativeList<OnscreenPortalData> onscreenPortalData)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<ref NativePortalScene, float3*, ref float4x4, float4x4*, float, float, int, int, int, float4*, float3*, float3*, int, float, ref float4x4, ref NativeList<float4x4>, ref NativeList<OnscreenPortalData>, ushort>)functionPointer)(ref scene, enterPos, ref enterViewM, enterCullM, maxDistance, farClipPlane, ignoreIndex, depth, parentOnscreenIndex, frustumPlanes, clipBufferA, clipBufferB, screenArea, minPortalPixelArea, ref defaultProjectionMatrix, ref cullingFrusta, ref onscreenPortalData);
				}
			}
			return GetOnscreenPortalsBurst_0024BurstManaged(in scene, enterPos, in enterViewM, enterCullM, maxDistance, farClipPlane, ignoreIndex, depth, parentOnscreenIndex, frustumPlanes, clipBufferA, clipBufferB, screenArea, minPortalPixelArea, in defaultProjectionMatrix, ref cullingFrusta, ref onscreenPortalData);
		}
	}

	internal unsafe delegate bool CalculateCullingData_00002A10_0024PostfixBurstDelegate(float3* clippedVerts, int count, in float4x4 enterViewM, in float4x4 proj, int screenArea, float areaThreshold, out float4 rectBounds, out float nearClip);

	internal static class CalculateCullingData_00002A10_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(CalculateCullingData_00002A10_0024PostfixBurstDelegate).TypeHandle);
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

		static CalculateCullingData_00002A10_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static bool Invoke(float3* clippedVerts, int count, in float4x4 enterViewM, in float4x4 proj, int screenArea, float areaThreshold, out float4 rectBounds, out float nearClip)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<float3*, int, ref float4x4, ref float4x4, int, float, ref float4, ref float, bool>)functionPointer)(clippedVerts, count, ref enterViewM, ref proj, screenArea, areaThreshold, ref rectBounds, ref nearClip);
				}
			}
			return CalculateCullingData_0024BurstManaged(clippedVerts, count, in enterViewM, in proj, screenArea, areaThreshold, out rectBounds, out nearClip);
		}
	}

	internal delegate void UpdateOcclusionBurst_00002A18_0024PostfixBurstDelegate([NoAlias] ref NativeList<OnscreenPortalData> data, ulong bitset, [NoAlias] ref NativeArray<bool> visibility);

	internal static class UpdateOcclusionBurst_00002A18_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(UpdateOcclusionBurst_00002A18_0024PostfixBurstDelegate).TypeHandle);
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

		static UpdateOcclusionBurst_00002A18_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke([NoAlias] ref NativeList<OnscreenPortalData> data, ulong bitset, [NoAlias] ref NativeArray<bool> visibility)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref NativeList<OnscreenPortalData>, ulong, ref NativeArray<bool>, void>)functionPointer)(ref data, bitset, ref visibility);
					return;
				}
			}
			UpdateOcclusionBurst_0024BurstManaged(ref data, bitset, ref visibility);
		}
	}

	private const int EVENT_BEGIN_READBACK = 1001;

	private const int EVENT_FETCH_RESULTS = 1002;

	[Space(10f)]
	public bool doOcclusionPass;

	public Material portalCompositeMaterial;

	public Material portalMaterial;

	public Material portalBitset64DownsampleMat;

	public Material fakeRecursionCopy;

	public PortalScene scene;

	public float obliqueCutoff = 0.5f;

	public float minPortalPixelArea = 2f;

	private UnityEngine.Plane[] frustumPlanes = new UnityEngine.Plane[6];

	private float4[] frustumPlanesF4 = new float4[6];

	[HideInInspector]
	public Mesh totalPortalMesh;

	private Camera mainCam;

	private Camera portalCam;

	private NativeList<PrepassData> prepassPortals;

	public NativeList<RenderData> renderDatas;

	private CommandBuffer cb;

	private RenderTexture portalCompositeColor;

	private RenderTexture portalCompositeOutlineData;

	private RenderTargetIdentifier[] portalCompositeIdentifiers;

	private CommandBuffer recursionPortalDraw;

	private CommandBuffer bloodOilCB;

	private RenderTexture portalCompositeDepth;

	private RenderTargetIdentifier portalCompositeDepthIdentifier;

	[HideInInspector]
	public bool needsInit = true;

	private int lastWidth;

	private int lastHeight;

	private int screenArea;

	private PostProcessV2_Handler pph;

	[SerializeField]
	private Mesh occluderMesh;

	private RenderTexture portalOcclusionData;

	private RenderTargetIdentifier portalOcclusionDataIdentifier;

	private RenderTexture[] portalCompositeOcclusionData;

	private RenderTargetIdentifier[] portalCompositeOcclusionDataIdentifiers;

	private int bitsetMaxMip;

	public Texture2D testOcclusionMap;

	private float[] texArray;

	private AsyncGPUReadbackRequest occlusionReadback;

	private NativeList<OnscreenPortalData> onscreenPortalData;

	private NativeList<int> indexCache;

	private NativeList<float4x4> cullingFrusta;

	private float3[] clipBufferA = new float3[8];

	private float3[] clipBufferB = new float3[8];

	private int portalDepthID = Shader.PropertyToID("_PortalDepth");

	private int portalOcclusionDataID = Shader.PropertyToID("_PortalOcclusionData");

	private int portalCompositeColorID = Shader.PropertyToID("_PortalCompositeColor");

	private int portalCompositeOutlineDatahID = Shader.PropertyToID("_PortalCompositeOutlineData");

	private int portalDrawBitID = Shader.PropertyToID("_PortalDrawBit");

	private int portalVPMatrixID = Shader.PropertyToID("_PortalVPMatrix");

	private int portalCompositeOcclusionDataID = Shader.PropertyToID("_PortalCompositeOcclusion");

	private int InMirroredSpaceID = Shader.PropertyToID("_InMirroredSpace");

	private int portalClipPlaneID = Shader.PropertyToID("_PortalClipPlane");

	private int skipOcclusionDiscardID = Shader.PropertyToID("_SkipOcclusionDiscard");

	private int portalCamForwardID = Shader.PropertyToID("_PortalCamForward");

	private List<Vertex> portalMeshVertices = new List<Vertex>();

	private List<ushort> portalMeshIndices = new List<ushort>();

	private Matrix4x4 defaultProjectionMatrix;

	private Matrix4x4 defaultProjectionMatrixMirrored;

	private Matrix4x4 defaultProjectionMatrixGPU;

	private FogData defaultFogData;

	public static readonly int[] quadTriangleIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	private static readonly ManualResetEventSlim waitHandle = new ManualResetEventSlim();

	private float maxDistance = float.MaxValue;

	private ulong occlusionBitset;

	private static float4x4 mirrorMatrix = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));

	private CommandBuffer mainPortalDraw;

	private List<(Light light, PortalLightType type)> tempPortalLights = new List<(Light, PortalLightType)>(16);

	private List<(Light light, PortalLightType type, Matrix4x4 travelMatrix, Vector4 portalPlane)> portalLights = new List<(Light, PortalLightType, Matrix4x4, Vector4)>(16);

	private Vector4[] globalPortalLightPositions;

	private Vector4[] globalPortalLightColors;

	private Vector4[] globalPortalLightAttens;

	private Vector4[] globalPortalLightDirs;

	private Vector4[] globalPortalLightPlanes;

	private int portalLightCount;

	private Vector3[] reusableVerts;

	private NativeArray<bool> portalVisibility;

	private NativeHashMap<int, ushort> textureStoreMap;

	private static NativeList<PortalView> portalViewStack;

	private VertexAttributeDescriptor[] vertexDescriptor;

	private static Matrix4x4 identityMatrix = Matrix4x4.identity;

	[DllImport("SameFrameReadback")]
	private static extern void BeginReadback_NonBlocking(IntPtr sourceTexture);

	[DllImport("SameFrameReadback")]
	private unsafe static extern void GetData_Blocking(long* result);

	[DllImport("SameFrameReadback")]
	private static extern IntPtr GetRenderEventFunc();

	[DllImport("SameFrameReadback")]
	private static extern IntPtr GetRenderEventAndDataFunc();

	private void Start()
	{
		InitializeMesh();
		pph = MonoSingleton<PostProcessV2_Handler>.Instance;
		_ = pph.mainTex;
		globalPortalLightPositions = new Vector4[16];
		globalPortalLightAttens = new Vector4[16];
		globalPortalLightColors = new Vector4[16];
		globalPortalLightDirs = new Vector4[16];
		globalPortalLightPlanes = new Vector4[16];
		prepassPortals = new NativeList<PrepassData>(128, Allocator.Persistent);
		onscreenPortalData = new NativeList<OnscreenPortalData>(128, Allocator.Persistent);
		indexCache = new NativeList<int>(128, Allocator.Persistent);
		portalVisibility = new NativeArray<bool>(256, Allocator.Persistent);
		cullingFrusta = new NativeList<float4x4>(64, Allocator.Persistent);
		textureStoreMap = new NativeHashMap<int, ushort>(64, Allocator.Persistent);
		renderDatas = new NativeList<RenderData>(256, Allocator.Persistent);
		portalViewStack = new NativeList<PortalView>(256, Allocator.Persistent);
		vertexDescriptor = new VertexAttributeDescriptor[2]
		{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2)
		};
		Shader.EnableKeyword("PORTAL_LIGHTS");
	}

	private void InitializeMesh()
	{
		totalPortalMesh = new Mesh();
		totalPortalMesh.MarkDynamic();
	}

	private void OnDisable()
	{
		Shader.DisableKeyword("PORTAL_LIGHTS");
	}

	private void OnDestroy()
	{
		onscreenPortalData.Dispose();
		portalVisibility.Dispose();
		cullingFrusta.Dispose();
		textureStoreMap.Dispose();
		renderDatas.Dispose();
		prepassPortals.Dispose();
		portalViewStack.Dispose();
		indexCache.Dispose();
		Shader.DisableKeyword("PORTAL_LIGHTS");
	}

	public unsafe void Setup(PortalScene portalScene, Camera mainCamera, Camera portalCamera)
	{
		scene = portalScene;
		mainCam = mainCamera;
		portalCam = portalCamera;
		renderDatas.Clear();
		prepassPortals.Clear();
		onscreenPortalData.Clear();
		cullingFrusta.Clear();
		if (scene.nativeScene.renderPortals.Length == 0)
		{
			return;
		}
		portalCam.CopyFrom(mainCam);
		portalCam.useOcclusionCulling = false;
		mainCam.transform.GetPositionAndRotation(out var position, out var rotation);
		portalCam.transform.SetPositionAndRotation(position, rotation);
		maxDistance = (RenderSettings.fog ? RenderSettings.fogEndDistance : 99999f);
		Initialize();
		Span<NativePortal> span = scene.nativeScene.renderPortals.AsArray().AsSpan();
		int length = span.Length;
		int vertexCount = length * 4;
		int indexCount = length * 6;
		Mesh.MeshDataArray data = Mesh.AllocateWritableMeshData(1);
		Mesh.MeshData data2 = data[0];
		data2.SetVertexBufferParams(vertexCount, vertexDescriptor);
		data2.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
		fixed (SubMeshDescriptor* subMeshes = new SubMeshDescriptor[length + 1])
		{
			fixed (NativePortal* portalsSpan = span)
			{
				RebuildMesh(portalsSpan, span.Length, subMeshes, ref data2);
			}
		}
		Mesh.ApplyAndDisposeWritableMeshData(data, totalPortalMesh);
		defaultProjectionMatrix = mainCam.projectionMatrix;
		defaultProjectionMatrixMirrored = math.mul(mirrorMatrix, defaultProjectionMatrix);
		defaultProjectionMatrixGPU = GL.GetGPUProjectionMatrix(defaultProjectionMatrix, renderIntoTexture: true);
		defaultFogData = new FogData
		{
			useFog = (RenderSettings.fog ? 1 : 0),
			fogColor = RenderSettings.fogColor,
			fogStart = RenderSettings.fogStartDistance,
			fogEnd = RenderSettings.fogEndDistance
		};
		CameraData camData = CameraData.FromCamera(mainCam);
		textureStoreMap.Clear();
		portalViewStack.Clear();
		portalViewStack.Add(new PortalView
		{
			camData = camData,
			depth = 0,
			maxDepth = 4,
			parentHandleIndex = ushort.MaxValue,
			parentOnscreenIndex = -1,
			ignoreIndex = -1,
			inMirroredSpace = false,
			parentViewMatrix = float4x4.identity,
			clipPlane = new float4(-camData.Forward, 0f - math.dot(-camData.Forward, camData.Position)),
			parentClipPlane = float4.zero,
			lastFogData = defaultFogData
		});
		fixed (float3* ptr = clipBufferA)
		{
			fixed (float3* ptr2 = clipBufferB)
			{
				fixed (float4* ptr3 = frustumPlanesF4)
				{
					BurstRenderData(in scene.nativeScene, mainCam.cullingMask, in defaultFogData, maxDistance, mainCam.farClipPlane, screenArea, minPortalPixelArea, doOcclusionPass, (float4x4)defaultProjectionMatrixGPU, (float4x4)defaultProjectionMatrix, in mirrorMatrix, ref portalViewStack, ref prepassPortals, ref onscreenPortalData, ref cullingFrusta, ref textureStoreMap, ptr, ptr2, ptr3, ref renderDatas);
				}
			}
		}
		foreach (KVPair<int, ushort> item in textureStoreMap)
		{
			int key = item.Key;
			ushort value = item.Value;
			PortalHandle handle = scene.nativeScene.renderPortals[key].handle;
			scene.GetPortalObject(handle).SetNeedToStoreTexture(value, handle.side);
		}
		indexCache.Clear();
		indexCache.ResizeUninitialized(onscreenPortalData.Length);
		NativeArray<OnscreenPortalData> data3 = onscreenPortalData.AsArray();
		NativeArray<int> indices = indexCache.AsArray();
		SortPortals(ref data3, ref indices);
		DepthPrepass();
	}

	private unsafe void MOCTestDrawScene()
	{
		MaskedOcclusionCulling maskedOcclusionCulling = new MaskedOcclusionCulling();
		uint num = 1920u;
		uint num2 = 1080u;
		maskedOcclusionCulling.SetResolution(num, num2);
		maskedOcclusionCulling.SetNearClipPlane(1f);
		maskedOcclusionCulling.ClearBuffer();
		Matrix4x4 matrix4x = GL.GetGPUProjectionMatrix(mainCam.projectionMatrix, renderIntoTexture: true) * mainCam.worldToCameraMatrix;
		Vector3[] vertices = occluderMesh.vertices;
		Vector4[] array = new Vector4[vertices.Length];
		for (int i = 0; i < vertices.Length; i++)
		{
			array[i] = matrix4x * new Vector4(vertices[i].x, vertices[i].y, vertices[i].z, 1f);
		}
		int[] triangles = occluderMesh.triangles;
		uint[] array2 = new uint[triangles.Length];
		for (int j = 0; j < triangles.Length; j++)
		{
			array2[j] = (uint)triangles[j];
		}
		if (testOcclusionMap == null)
		{
			testOcclusionMap = new Texture2D((int)num, (int)num2, TextureFormat.RFloat, mipChain: false, linear: true);
			texArray = new float[num * num2];
		}
		fixed (Vector4* inVtx = array)
		{
			fixed (uint* inTris = array2)
			{
				maskedOcclusionCulling.RenderTriangles((float*)inVtx, inTris, triangles.Length / 3, null, MaskedOcclusionCulling.BackfaceWinding.Clockwise, MaskedOcclusionCulling.ClipPlanes.None);
			}
		}
	}

	internal void AddPortalAwareLight(Light thisLight, PortalLightType lightType)
	{
		int count = tempPortalLights.Count;
		for (int i = 0; i < count; i++)
		{
			if (tempPortalLights[i].light == thisLight)
			{
				return;
			}
		}
		tempPortalLights.Add((thisLight, lightType));
	}

	private void UpdatePortalLights()
	{
		if (!MonoSingleton<PlayerMovementParenting>.TryGetInstance(out PlayerMovementParenting instance))
		{
			return;
		}
		GameObject gameObject = instance.gameObject;
		tempPortalLights.Sort(((Light light, PortalLightType type) a, (Light light, PortalLightType type) b) => b.type.CompareTo(a.type));
		for (int num = tempPortalLights.Count - 1; num >= 0; num--)
		{
			Light item = tempPortalLights[num].light;
			if (!item || !item.isActiveAndEnabled)
			{
				tempPortalLights.RemoveAt(num);
			}
		}
		Light[] componentsInChildren = gameObject.GetComponentsInChildren<Light>(includeInactive: false);
		foreach (Light light in componentsInChildren)
		{
			if (light.enabled && !tempPortalLights.Contains((light, PortalLightType.Player)))
			{
				tempPortalLights.Add((light, PortalLightType.Player));
			}
		}
		Vector3 position = gameObject.transform.position;
		NativePortalScene nativeScene = MonoSingleton<PortalManagerV2>.Instance.Scene.nativeScene;
		ReadOnlySpan<NativePortal> readOnlySpan = nativeScene.renderPortals.AsArray().AsReadOnlySpan();
		for (int num3 = tempPortalLights.Count - 1; num3 >= 0; num3--)
		{
			(Light, PortalLightType) tuple = tempPortalLights[num3];
			Light item2 = tuple.Item1;
			Vector3 source = ((tuple.Item2 == PortalLightType.Player) ? position : item2.transform.position);
			float3 point = Unsafe.As<Vector3, float3>(ref source);
			Vector3 position2 = item2.transform.position;
			float range = item2.range;
			for (int num4 = 0; num4 < readOnlySpan.Length; num4++)
			{
				NativePortal nativePortal = readOnlySpan[num4];
				PortalHandle handle = nativePortal.handle;
				Unity.Mathematics.Geometry.Plane plane = nativePortal.plane;
				float num5 = plane.SignedDistanceToPoint(point);
				if (num5 > 0f || MathF.Abs(num5) > range)
				{
					continue;
				}
				Vector3 vector = nativePortal.transform.toLocalManaged.MultiplyPoint3x4(position2);
				float num6 = nativePortal.dimensions.x * 0.5f;
				float num7 = nativePortal.dimensions.y * 0.5f;
				Vector2 vector2 = new Vector2(vector.x, vector.y);
				Vector2 vector3 = new Vector2(System.Math.Clamp(vector.x, 0f - num6, num6), System.Math.Clamp(vector.y, 0f - num7, num7));
				float sqrMagnitude = (vector2 - vector3).sqrMagnitude;
				float num8 = range * range - num5 * num5;
				if (!(sqrMagnitude > num8))
				{
					int num9 = ((handle.side == PortalSide.Enter) ? 1 : (-1));
					if (nativePortal.renderData.mirror)
					{
						num9 = 0;
					}
					Unity.Mathematics.Geometry.Plane plane2 = readOnlySpan[num4 + num9].plane;
					portalLights.Add((item2, tuple.Item2, nativePortal.travelMatrixManaged, plane2.NormalAndDistance));
				}
			}
		}
		while (portalLights.Count > globalPortalLightPositions.Length)
		{
			portalLights.RemoveAt(portalLights.Count - 1);
		}
		portalLightCount = Mathf.Min(portalLights.Count, globalPortalLightPositions.Length);
		Shader.SetGlobalFloat("_PortalLightCount", portalLightCount);
		for (int num10 = 0; num10 < portalLightCount; num10++)
		{
			(Light, PortalLightType, Matrix4x4, Vector4) tuple2 = portalLights[num10];
			Light item3 = portalLights[num10].light;
			Transform transform = item3.transform;
			globalPortalLightPositions[num10] = tuple2.Item3.MultiplyPoint3x4(transform.position);
			globalPortalLightDirs[num10] = tuple2.Item3.MultiplyVector(-transform.forward);
			globalPortalLightColors[num10] = item3.color * item3.intensity;
			bool num11 = item3.type == LightType.Spot;
			float num12 = item3.spotAngle * (MathF.PI / 180f);
			float x = (num11 ? Mathf.Cos(num12 / 2f) : (-1f));
			float y = (num11 ? (1f / (Mathf.Cos(num12 / 4f) - Mathf.Cos(num12 / 2f))) : 1f);
			float range2 = item3.range;
			globalPortalLightAttens[num10] = new Vector4(x, y, range2, 0f);
			globalPortalLightPlanes[num10] = tuple2.Item4;
		}
		Shader.SetGlobalVectorArray("_PortalLightPositions", globalPortalLightPositions);
		Shader.SetGlobalVectorArray("_PortalLightDirs", globalPortalLightDirs);
		Shader.SetGlobalVectorArray("_PortalLightColors", globalPortalLightColors);
		Shader.SetGlobalVectorArray("_PortalLightAttens", globalPortalLightAttens);
		Shader.SetGlobalVectorArray("_PortalLightPlanes", globalPortalLightPlanes);
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	private unsafe static void RebuildMesh(NativePortal* portalsSpan, in int spanLength, SubMeshDescriptor* subMeshes, ref Mesh.MeshData data)
	{
		RebuildMesh_00002A09_0024BurstDirectCall.Invoke(portalsSpan, in spanLength, subMeshes, ref data);
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	private unsafe static void BurstRenderData(in NativePortalScene nativeScene, int mainCullingMask, in FogData defaultFogData, float maxDistance, float farClipPlane, int screenArea, float minPortalPixelArea, bool doOcclusionPass, in float4x4 defaultProjGPU, in float4x4 defaultProj, in float4x4 mirrorMatrix, ref NativeList<PortalView> portalViewStack, ref NativeList<PrepassData> prepass, ref NativeList<OnscreenPortalData> onscreenPortalData, ref NativeList<float4x4> cullingFrusta, ref NativeHashMap<int, ushort> textureStoreMap, float3* clipBufferA, float3* clipBufferB, float4* frustumPlanes, ref NativeList<RenderData> portalRenders)
	{
		BurstRenderData_00002A0A_0024BurstDirectCall.Invoke(in nativeScene, mainCullingMask, in defaultFogData, maxDistance, farClipPlane, screenArea, minPortalPixelArea, doOcclusionPass, in defaultProjGPU, in defaultProj, in mirrorMatrix, ref portalViewStack, ref prepass, ref onscreenPortalData, ref cullingFrusta, ref textureStoreMap, clipBufferA, clipBufferB, frustumPlanes, ref portalRenders);
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	public static void SortPortals(ref NativeArray<OnscreenPortalData> data, ref NativeArray<int> indices)
	{
		SortPortals_00002A0B_0024BurstDirectCall.Invoke(ref data, ref indices);
	}

	public static void DebugFrustum(Matrix4x4 viewM, Matrix4x4 projM, Color color)
	{
		Matrix4x4 inverse = (projM * viewM).inverse;
		Vector3[] array = new Vector3[8]
		{
			new Vector3(-1f, -1f, -1f),
			new Vector3(1f, -1f, -1f),
			new Vector3(1f, 1f, -1f),
			new Vector3(-1f, 1f, -1f),
			new Vector3(-1f, -1f, 1f),
			new Vector3(1f, -1f, 1f),
			new Vector3(1f, 1f, 1f),
			new Vector3(-1f, 1f, 1f)
		};
		Vector3[] array2 = new Vector3[8];
		for (int i = 0; i < 8; i++)
		{
			Vector4 vector = new Vector4(array[i].x, array[i].y, array[i].z, 1f);
			vector = inverse * vector;
			array2[i] = new Vector3(vector.x / vector.w, vector.y / vector.w, vector.z / vector.w);
		}
		float duration = 0.01f;
		Debug.DrawLine(array2[0], array2[1], color, duration);
		Debug.DrawLine(array2[1], array2[2], color, duration);
		Debug.DrawLine(array2[2], array2[3], color, duration);
		Debug.DrawLine(array2[3], array2[0], color, duration);
		Debug.DrawLine(array2[4], array2[5], color, duration);
		Debug.DrawLine(array2[5], array2[6], color, duration);
		Debug.DrawLine(array2[6], array2[7], color, duration);
		Debug.DrawLine(array2[7], array2[4], color, duration);
		Debug.DrawLine(array2[0], array2[4], color, duration);
		Debug.DrawLine(array2[1], array2[5], color, duration);
		Debug.DrawLine(array2[2], array2[6], color, duration);
		Debug.DrawLine(array2[3], array2[7], color, duration);
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	public unsafe static void ExtractFrustumPlanes(float4x4* matrix, float4* planes)
	{
		ExtractFrustumPlanes_00002A0D_0024BurstDirectCall.Invoke(matrix, planes);
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	private unsafe static ushort GetOnscreenPortalsBurst(in NativePortalScene scene, float3* enterPos, in float4x4 enterViewM, float4x4* enterCullM, float maxDistance, float farClipPlane, int ignoreIndex, int depth, int parentOnscreenIndex, float4* frustumPlanes, float3* clipBufferA, float3* clipBufferB, int screenArea, float minPortalPixelArea, in float4x4 defaultProjectionMatrix, ref NativeList<float4x4> cullingFrusta, ref NativeList<OnscreenPortalData> onscreenPortalData)
	{
		return GetOnscreenPortalsBurst_00002A0E_0024BurstDirectCall.Invoke(in scene, enterPos, in enterViewM, enterCullM, maxDistance, farClipPlane, ignoreIndex, depth, parentOnscreenIndex, frustumPlanes, clipBufferA, clipBufferB, screenArea, minPortalPixelArea, in defaultProjectionMatrix, ref cullingFrusta, ref onscreenPortalData);
	}

	private static float4x4 Frustum(float l, float r, float b, float t, float n, float f)
	{
		float num = 1f / (r - l);
		float num2 = 1f / (t - b);
		float num3 = 1f / (n - f);
		return new float4x4(new float4(2f * n * num, 0f, 0f, 0f), new float4(0f, 2f * n * num2, 0f, 0f), new float4((r + l) * num, (t + b) * num2, (f + n) * num3, -1f), new float4(0f, 0f, 2f * f * n * num3, 0f));
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	public unsafe static bool CalculateCullingData(float3* clippedVerts, int count, in float4x4 enterViewM, in float4x4 proj, int screenArea, float areaThreshold, out float4 rectBounds, out float nearClip)
	{
		return CalculateCullingData_00002A10_0024BurstDirectCall.Invoke(clippedVerts, count, in enterViewM, in proj, screenArea, areaThreshold, out rectBounds, out nearClip);
	}

	private void DebugRect(float minX, float maxX, float minY, float maxY, float nearClip, Matrix4x4 enterViewMatrix)
	{
		Vector3 point = new Vector3(minX, maxY, nearClip);
		Vector3 point2 = new Vector3(maxX, maxY, nearClip);
		Vector3 point3 = new Vector3(minX, minY, nearClip);
		Vector3 point4 = new Vector3(maxX, minY, nearClip);
		Matrix4x4 inverse = enterViewMatrix.inverse;
		point = inverse.MultiplyPoint3x4(point);
		point2 = inverse.MultiplyPoint3x4(point2);
		point3 = inverse.MultiplyPoint3x4(point3);
		point4 = inverse.MultiplyPoint3x4(point4);
		Debug.DrawLine(point, point2, Color.red, 0.01f);
		Debug.DrawLine(point2, point4, Color.red, 0.01f);
		Debug.DrawLine(point4, point3, Color.red, 0.01f);
		Debug.DrawLine(point3, point, Color.red, 0.01f);
	}

	private void DestroyTextures()
	{
		PostProcessV2_Handler.TryDestroyTexture(portalOcclusionData, destroyImmediate: true);
		PostProcessV2_Handler.TryDestroyTexture(portalCompositeColor, destroyImmediate: true);
		PostProcessV2_Handler.TryDestroyTexture(portalCompositeDepth, destroyImmediate: true);
		PostProcessV2_Handler.TryDestroyTexture(portalCompositeOutlineData, destroyImmediate: true);
		if (portalCompositeOcclusionData != null)
		{
			for (int i = 0; i < portalCompositeOcclusionData.Length; i++)
			{
				PostProcessV2_Handler.TryDestroyTexture(portalCompositeOcclusionData[i], destroyImmediate: true);
			}
		}
	}

	public void Initialize(bool forceInit = false)
	{
		if (portalCam == null)
		{
			return;
		}
		RenderTexture mainTex = pph.mainTex;
		if (mainTex == null)
		{
			return;
		}
		int width = mainTex.width;
		int height = mainTex.height;
		if (!forceInit)
		{
			if (width == lastWidth && height == lastHeight)
			{
				return;
			}
			DestroyTextures();
			lastWidth = width;
			lastHeight = height;
			screenArea = width * height;
		}
		lastWidth = width;
		lastHeight = height;
		screenArea = width * height;
		if (portalOcclusionData == null)
		{
			portalOcclusionData = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear)
			{
				name = "Portal Occlusion Data",
				antiAliasing = 1,
				filterMode = FilterMode.Point
			};
			portalOcclusionDataIdentifier = new RenderTargetIdentifier(portalOcclusionData);
		}
		if (portalCompositeIdentifiers == null || portalCompositeIdentifiers.Length != 2)
		{
			portalCompositeIdentifiers = new RenderTargetIdentifier[2];
		}
		if (portalCompositeColor == null)
		{
			portalCompositeColor = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
			{
				name = "Portal Composite Color",
				antiAliasing = 1,
				filterMode = FilterMode.Point
			};
			portalCompositeIdentifiers[0] = new RenderTargetIdentifier(portalCompositeColor);
			portalCompositeOutlineData = new RenderTexture(width, height, 0, RenderTextureFormat.RG16)
			{
				name = "Portal Composite Outline Data",
				antiAliasing = 1,
				filterMode = FilterMode.Point
			};
			portalCompositeIdentifiers[1] = new RenderTargetIdentifier(portalCompositeOutlineData);
		}
		if (portalCompositeDepth == null)
		{
			portalCompositeDepth = new RenderTexture(width, height, 32, RenderTextureFormat.Depth)
			{
				name = "Portal Composite Depth",
				antiAliasing = 1,
				filterMode = FilterMode.Point
			};
			portalCompositeDepthIdentifier = new RenderTargetIdentifier(portalCompositeDepth);
		}
		bitsetMaxMip = Mathf.FloorToInt(Mathf.Log(Mathf.Max(width, height), 2f));
		int num = Mathf.Max(1, bitsetMaxMip + 1);
		portalCompositeOcclusionData = new RenderTexture[num];
		portalCompositeOcclusionDataIdentifiers = new RenderTargetIdentifier[portalCompositeOcclusionData.Length];
		int num2 = width;
		int num3 = height;
		for (int i = 0; i < num; i++)
		{
			RenderTexture renderTexture = new RenderTexture(num2, num3, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear)
			{
				name = $"Portal Composite Occlusion Data Mip {i}",
				antiAliasing = 1,
				filterMode = FilterMode.Point
			};
			renderTexture.Create();
			portalCompositeOcclusionData[i] = renderTexture;
			portalCompositeOcclusionDataIdentifiers[i] = new RenderTargetIdentifier(renderTexture);
			num2 /= 2;
			num3 /= 2;
			num2 = ((num2 <= 0) ? 1 : num2);
			num3 = ((num3 <= 0) ? 1 : num3);
		}
		PostProcessV2_Handler instance = MonoSingleton<PostProcessV2_Handler>.Instance;
		instance.buffers[0] = instance.mainTex.colorBuffer;
		instance.buffers[1] = instance.reusableBufferA.colorBuffer;
		instance.buffers[2] = instance.viewNormal.colorBuffer;
		portalCam.SetTargetBuffers(instance.buffers, instance.depthBuffer.depthBuffer);
		if (Application.isPlaying)
		{
			if (bloodOilCB == null)
			{
				bloodOilCB = new CommandBuffer();
				bloodOilCB.name = "Portal Blood and Oil";
			}
			bloodOilCB.Clear();
			bloodOilCB.SetGlobalTexture("_WorldNormal", pph.viewNormal.colorBuffer);
			bloodOilCB.SetGlobalTexture("_BloodstainTex", pph.reusableBufferB.colorBuffer);
			bloodOilCB.SetGlobalTexture("_OilStainTex", pph.reusableBufferB.colorBuffer);
			Graphics.ExecuteCommandBuffer(bloodOilCB);
		}
		if (mainPortalDraw == null)
		{
			mainPortalDraw = new CommandBuffer();
			mainPortalDraw.name = "Portal Draw";
			mainPortalDraw.DrawMesh(totalPortalMesh, Matrix4x4.identity, portalMaterial, 0, 0);
		}
		mainCam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, mainPortalDraw);
		mainCam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, mainPortalDraw);
		if (cb == null)
		{
			cb = new CommandBuffer();
			cb.name = "Portal";
		}
	}

	private Matrix4x4 MakeProjectionMatrixFlippedZ(Matrix4x4 proj)
	{
		proj[2, 0] *= -1f;
		proj[2, 1] *= -1f;
		proj[2, 2] *= -1f;
		proj[2, 3] *= -1f;
		return proj;
	}

	private unsafe void DepthPrepass()
	{
		RenderBuffer colorBuffer;
		RenderBuffer depthBuffer;
		if (Application.isPlaying)
		{
			colorBuffer = pph.mainTex.colorBuffer;
			depthBuffer = pph.depthBuffer.depthBuffer;
		}
		else
		{
			if (mainCam == null || mainCam.activeTexture == null)
			{
				return;
			}
			colorBuffer = mainCam.activeTexture.colorBuffer;
			depthBuffer = mainCam.activeTexture.depthBuffer;
		}
		colorBuffer.GetNativeRenderBufferPtr();
		depthBuffer.GetNativeRenderBufferPtr();
		if (cb == null)
		{
			cb = new CommandBuffer();
			cb.name = "Portal Depth Prepass";
		}
		else
		{
			cb.Clear();
		}
		Color fogColor = RenderSettings.fogColor;
		cb.SetRenderTarget(portalCompositeIdentifiers[0], portalCompositeDepthIdentifier);
		cb.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);
		PostProcessV2_Handler instance = MonoSingleton<PostProcessV2_Handler>.Instance;
		cb.SetRenderTarget(instance.mainTex, instance.depthBuffer.depthBuffer);
		cb.ClearRenderTarget(clearDepth: true, clearColor: true, fogColor);
		cb.SetRenderTarget(portalOcclusionDataIdentifier);
		cb.ClearRenderTarget(clearDepth: false, clearColor: true, Color.clear);
		RenderTargetIdentifier renderTargetIdentifier = portalCompositeOcclusionDataIdentifiers[0];
		cb.SetRenderTarget(renderTargetIdentifier);
		cb.ClearRenderTarget(clearDepth: false, clearColor: true, Color.clear);
		cb.SetGlobalTexture(portalDepthID, instance.depthBuffer.depthBuffer);
		cb.SetGlobalTexture(portalOcclusionDataID, portalOcclusionDataIdentifier);
		cb.SetGlobalTexture(portalCompositeColorID, portalCompositeIdentifiers[0]);
		cb.SetGlobalTexture(portalCompositeOutlineDatahID, portalCompositeIdentifiers[1]);
		cb.SetGlobalTexture(portalCompositeOcclusionDataID, renderTargetIdentifier);
		int length = prepassPortals.Length;
		PrepassData* unsafeReadOnlyPtr = prepassPortals.GetUnsafeReadOnlyPtr();
		OnscreenPortalData* unsafeReadOnlyPtr2 = onscreenPortalData.GetUnsafeReadOnlyPtr();
		for (int i = 0; i < length; i++)
		{
			ref PrepassData reference = ref unsafeReadOnlyPtr[i];
			ref Matrix4x4 reference2 = ref Unsafe.As<float4x4, Matrix4x4>(ref reference.viewMatrix);
			ref Vector4 reference3 = ref Unsafe.As<float4, Vector4>(ref reference.clipPlane);
			cb.SetGlobalVector(portalClipPlaneID, reference3);
			if (reference.handleIndex == -3)
			{
				if (reference.parentHandleIndex == ushort.MaxValue)
				{
					cb.SetRenderTarget(renderTargetIdentifier, portalCompositeDepthIdentifier);
				}
				else
				{
					cb.SetRenderTarget(portalOcclusionDataIdentifier, instance.depthBuffer.depthBuffer);
				}
			}
			if (reference.handleIndex > -1)
			{
				if (reference.inMirroredSpace)
				{
					cb.SetInvertCulling(invertCulling: true);
					cb.SetViewProjectionMatrices(reference2, defaultProjectionMatrixMirrored);
				}
				else
				{
					cb.SetViewProjectionMatrices(reference2, defaultProjectionMatrix);
				}
				int depthSortedIndex = unsafeReadOnlyPtr2[(int)reference.onscreenIndex].depthSortedIndex;
				Vector4 value = ((depthSortedIndex >= 64) ? Vector4.zero : IndexToBitMask(depthSortedIndex));
				cb.SetGlobalVector(portalDrawBitID, value);
				cb.DrawMesh(totalPortalMesh, identityMatrix, portalCompositeMaterial, reference.handleIndex + 1, 0);
				cb.SetInvertCulling(invertCulling: false);
			}
			if (reference.handleIndex == -2)
			{
				if (reference.inMirroredSpace)
				{
					cb.SetInvertCulling(invertCulling: true);
					cb.SetViewProjectionMatrices(reference2, defaultProjectionMatrixMirrored);
				}
				else
				{
					cb.SetViewProjectionMatrices(reference2, defaultProjectionMatrix);
				}
				cb.DrawMesh(occluderMesh, identityMatrix, portalCompositeMaterial, 0, 1);
				cb.SetInvertCulling(invertCulling: false);
			}
			if (reference.handleIndex == -1 && reference.parentHandleIndex != ushort.MaxValue)
			{
				cb.SetRenderTarget(renderTargetIdentifier, portalCompositeDepthIdentifier);
				cb.ClearRenderTarget(RTClearFlags.Stencil, Color.clear, 0f, 0u);
				if (scene.nativeScene.renderPortals[reference.parentHandleIndex].renderData.mirror ^ reference.inMirroredSpace)
				{
					cb.SetInvertCulling(invertCulling: true);
					cb.SetViewProjectionMatrices(reference2, defaultProjectionMatrixMirrored);
				}
				else
				{
					cb.SetViewProjectionMatrices(reference2, defaultProjectionMatrix);
				}
				cb.SetGlobalFloat(InMirroredSpaceID, reference.inMirroredSpace ? 1f : 0f);
				int submeshIndex = reference.parentHandleIndex + 1;
				cb.DrawMesh(totalPortalMesh, identityMatrix, portalCompositeMaterial, submeshIndex, 2);
				cb.DrawMesh(totalPortalMesh, identityMatrix, portalCompositeMaterial, submeshIndex, 3);
				cb.SetInvertCulling(invertCulling: false);
				cb.SetRenderTarget(portalOcclusionDataIdentifier, instance.depthBuffer.depthBuffer);
				cb.ClearRenderTarget(clearDepth: true, clearColor: false, Color.clear);
			}
		}
		if (doOcclusionPass)
		{
			for (int j = 0; j < bitsetMaxMip; j++)
			{
				cb.SetGlobalTexture(portalOcclusionDataID, portalCompositeOcclusionDataIdentifiers[j]);
				cb.SetRenderTarget(portalCompositeOcclusionDataIdentifiers[j + 1]);
				cb.DrawProcedural(identityMatrix, portalBitset64DownsampleMat, 0, MeshTopology.Triangles, 3, 1);
			}
		}
		Graphics.ExecuteCommandBuffer(cb);
		if (doOcclusionPass)
		{
			occlusionReadback = AsyncGPUReadback.Request(portalCompositeOcclusionData[bitsetMaxMip], 0, OnOcclusionReadbackCompleted);
		}
		GL.Flush();
	}

	private static void OnOcclusionReadbackCompleted(AsyncGPUReadbackRequest request)
	{
		MonoSingleton<PortalManagerV2>.Instance.render.occlusionBitset = request.GetData<ulong>()[0];
	}

	private static Vector4 IndexToBitMask(int index)
	{
		int num = index % 16;
		int num2 = 1 << num;
		Vector4 zero = Vector4.zero;
		if (index < 16)
		{
			zero.x = num2;
		}
		else if (index < 32)
		{
			zero.y = num2;
		}
		else if (index < 48)
		{
			zero.z = num2;
		}
		else
		{
			zero.w = num2;
		}
		return zero / 65535f;
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	public static void UpdateOcclusionBurst([NoAlias] ref NativeList<OnscreenPortalData> data, ulong bitset, [NoAlias] ref NativeArray<bool> visibility)
	{
		UpdateOcclusionBurst_00002A18_0024BurstDirectCall.Invoke(ref data, bitset, ref visibility);
	}

	public unsafe void Render(Camera cam)
	{
		mainCam = cam;
		UpdatePortalLights();
		RenderBuffer colorBuffer;
		RenderBuffer depthBuffer;
		if (Application.isPlaying)
		{
			colorBuffer = pph.mainTex.colorBuffer;
			depthBuffer = pph.depthBuffer.depthBuffer;
		}
		else
		{
			if (mainCam == null || mainCam.activeTexture == null)
			{
				return;
			}
			colorBuffer = mainCam.targetTexture.colorBuffer;
			depthBuffer = mainCam.targetTexture.depthBuffer;
		}
		colorBuffer.GetNativeRenderBufferPtr();
		depthBuffer.GetNativeRenderBufferPtr();
		int length = renderDatas.Length;
		Matrix4x4 identity = Matrix4x4.identity;
		BloodsplatterManager instance = MonoSingleton<BloodsplatterManager>.Instance;
		StainVoxelManager instance2 = MonoSingleton<StainVoxelManager>.Instance;
		PostProcessV2_Handler instance3 = MonoSingleton<PostProcessV2_Handler>.Instance;
		_ = RenderSettings.fog;
		_ = RenderSettings.fogColor;
		_ = RenderSettings.fogStartDistance;
		_ = RenderSettings.fogEndDistance;
		if (doOcclusionPass)
		{
			if (!occlusionReadback.done)
			{
				occlusionReadback.WaitForCompletion();
			}
			UpdateOcclusionBurst(ref this.onscreenPortalData, MonoSingleton<PortalManagerV2>.Instance.render.occlusionBitset, ref portalVisibility);
		}
		if (scene == null || !scene.nativeScene.valid || !scene.nativeScene.renderPortals.IsCreated || scene.nativeScene.renderPortals.Length == 0)
		{
			return;
		}
		RenderData* unsafeReadOnlyPtr = renderDatas.GetUnsafeReadOnlyPtr();
		NativePortal* unsafeReadOnlyPtr2 = scene.nativeScene.renderPortals.GetUnsafeReadOnlyPtr();
		int length2 = scene.nativeScene.renderPortals.Length;
		portalCam.projectionMatrix = defaultProjectionMatrix;
		if (recursionPortalDraw == null)
		{
			recursionPortalDraw = new CommandBuffer();
			recursionPortalDraw.name = "Recursion Portal Draw";
		}
		for (int num = length - 1; num >= 0; num--)
		{
			RenderData renderData = unsafeReadOnlyPtr[num];
			if (!doOcclusionPass || portalVisibility[renderData.onscreenIndex])
			{
				Vector4 value = Unsafe.As<float4, Vector4>(ref renderData.clipPlane);
				Shader.SetGlobalVector(portalClipPlaneID, value);
				OnscreenPortalData onscreenPortalData = this.onscreenPortalData[renderData.onscreenIndex];
				portalCam.transform.SetPositionAndRotation(renderData.viewPos, renderData.viewRot);
				portalCam.nearClipPlane = onscreenPortalData.nearClip;
				portalCam.cullingMatrix = renderData.cullingMatrix;
				portalCam.SetTargetBuffers(instance3.buffers, instance3.depthBuffer.depthBuffer);
				int handleIndex = renderData.handleIndex;
				NativePortal nativePortal = unsafeReadOnlyPtr2[(int)renderData.handleIndex];
				Portal portalObject = scene.GetPortalObject(nativePortal.handle);
				portalCam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, recursionPortalDraw);
				recursionPortalDraw.Clear();
				recursionPortalDraw.SetGlobalFloat(InMirroredSpaceID, renderData.outMirroredSpace ? 1f : 0f);
				recursionPortalDraw.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 0f, 0u);
				if (nativePortal.renderData.mirror)
				{
					recursionPortalDraw.SetViewProjectionMatrices(renderData.enterViewMatrix, defaultProjectionMatrixMirrored);
				}
				else
				{
					recursionPortalDraw.SetViewProjectionMatrices(renderData.enterViewMatrix, defaultProjectionMatrix);
				}
				int depthSortedIndex = onscreenPortalData.depthSortedIndex;
				recursionPortalDraw.SetGlobalFloat(skipOcclusionDiscardID, (depthSortedIndex >= 64) ? 1f : 0f);
				recursionPortalDraw.SetGlobalVector(portalDrawBitID, IndexToBitMask(depthSortedIndex));
				recursionPortalDraw.DrawMesh(totalPortalMesh, identity, portalCompositeMaterial, handleIndex + 1, 4);
				int num2 = ((nativePortal.handle.side == PortalSide.Enter) ? 1 : (-1));
				if (nativePortal.renderData.mirror)
				{
					num2 = 0;
				}
				Unity.Mathematics.Geometry.Plane plane = unsafeReadOnlyPtr2[renderData.handleIndex + num2].plane;
				recursionPortalDraw.SetGlobalVector(portalCamForwardID, portalCam.transform.forward);
				recursionPortalDraw.SetViewProjectionMatrices(portalCam.worldToCameraMatrix, defaultProjectionMatrix);
				for (int i = 0; i < length2; i++)
				{
					if (!renderData.canSeeItself && i == handleIndex)
					{
						continue;
					}
					NativePortal nativePortal2 = unsafeReadOnlyPtr2[i];
					if ((nativePortal2.renderData.mirror && nativePortal2.handle.side == PortalSide.Exit) || (nativePortal2.renderData.renderSettings == PortalSideFlags.Exit && nativePortal2.handle.side == PortalSide.Enter) || (nativePortal2.renderData.renderSettings == PortalSideFlags.Enter && nativePortal2.handle.side == PortalSide.Exit) || nativePortal2.renderData.renderSettings == PortalSideFlags.None)
					{
						continue;
					}
					float3 center = nativePortal2.transform.center;
					if (Vector3.Dot(plane.Normal, center) + plane.Distance > 0f)
					{
						continue;
					}
					Unity.Mathematics.Geometry.Plane plane2 = nativePortal2.plane;
					float num3 = MathF.Abs(Vector3.Dot(plane.Normal, plane2.Normal));
					float num4 = Mathf.Abs(MathF.Abs(plane2.Distance) - MathF.Abs(plane.Distance));
					if (!(num3 >= -0.99f) || !(num4 < 0.01f))
					{
						if (renderData.depth >= renderData.maxDepth && nativePortal2.renderData.supportsInfiniteRecursion)
						{
							Portal portal = Resources.InstanceIDToObject(nativePortal2.handle.instanceId) as Portal;
							recursionPortalDraw.SetGlobalTexture("_FallbackTex", (nativePortal2.handle.side == PortalSide.Enter) ? portal.fakeEnterTex : portal.fakeExitTex);
							recursionPortalDraw.SetGlobalMatrix("_FallbackViewMatrix", portal.fakeVPMatrix);
							recursionPortalDraw.DrawMesh(totalPortalMesh, identity, portalMaterial, i + 1, 1);
						}
						else
						{
							recursionPortalDraw.DrawMesh(totalPortalMesh, identity, portalMaterial, i + 1, 0);
						}
					}
				}
				portalCam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, recursionPortalDraw);
				portalCam.RemoveCommandBuffers(CameraEvent.BeforeForwardAlpha);
				bloodOilCB.Clear();
				Matrix4x4 inverseVP_NonOblique = MonoSingleton<PostProcessV2_Handler>.Instance.GetInverseVP_NonOblique(portalCam);
				bloodOilCB.SetGlobalMatrix("_InverseVP", inverseVP_NonOblique);
				bloodOilCB.SetGlobalVector("_ProjectionParams_Oblique", PostProcessV2_Handler.GetProjectionParams(portalCam));
				bloodOilCB.SetGlobalMatrix("_InvProjection_Oblique", portalCam.projectionMatrix.inverse);
				bloodOilCB.SetGlobalMatrix("_InverseView", portalCam.cameraToWorldMatrix);
				bloodOilCB.SetGlobalTexture("_DepthBuffer", instance3.depthBuffer);
				bloodOilCB.SetGlobalInteger("_FixOblique", 1);
				bloodOilCB.SetRenderTarget(pph.reusableBufferB.colorBuffer);
				bloodOilCB.ClearRenderTarget(clearDepth: false, clearColor: true, Color.clear);
				bloodOilCB.SetViewProjectionMatrices(portalCam.worldToCameraMatrix, portalCam.projectionMatrix);
				if (instance.usedComputeShadersAtStart)
				{
					bloodOilCB.DrawMeshInstancedIndirect(instance.optimizedBloodMesh, 0, instance.stainMat, 0, instance.argsBuffer, 0, null);
				}
				else
				{
					bloodOilCB.DrawMesh(instance.totalStainMesh, identity, instance.stainMat);
				}
				bloodOilCB.SetRenderTarget(instance3.mainTex.colorBuffer);
				if (nativePortal.renderData.mirror)
				{
					bloodOilCB.SetViewProjectionMatrices(renderData.enterViewMatrix, defaultProjectionMatrixMirrored);
				}
				else
				{
					bloodOilCB.SetViewProjectionMatrices(renderData.enterViewMatrix, mainCam.projectionMatrix);
				}
				bloodOilCB.DrawMesh(totalPortalMesh, identity, instance.bloodCompositeMaterial, handleIndex + 1, 0);
				bloodOilCB.SetRenderTarget(pph.reusableBufferB.colorBuffer);
				bloodOilCB.ClearRenderTarget(clearDepth: false, clearColor: true, new Color(1f, 1f, 1f, 0f));
				bloodOilCB.SetViewProjectionMatrices(portalCam.worldToCameraMatrix, portalCam.projectionMatrix);
				if (instance.usedComputeShadersAtStart)
				{
					bloodOilCB.DrawMeshInstancedIndirect(instance2.gasStainMesh, 0, instance2.gasStainMat, 0, instance2.argsBuffer, 0, null);
				}
				bloodOilCB.SetRenderTarget(instance3.mainTex.colorBuffer);
				if (nativePortal.renderData.mirror)
				{
					bloodOilCB.SetViewProjectionMatrices(renderData.enterViewMatrix, defaultProjectionMatrixMirrored);
				}
				else
				{
					bloodOilCB.SetViewProjectionMatrices(renderData.enterViewMatrix, mainCam.projectionMatrix);
				}
				bloodOilCB.DrawMesh(totalPortalMesh, identity, instance2.gasolineCompositeMaterial, handleIndex + 1, 0);
				portalCam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, bloodOilCB);
				portalCam.cullingMask = renderData.cullingMask;
				Material skybox = RenderSettings.skybox;
				Material material = ((nativePortal.handle.side == PortalSide.Enter) ? portalObject.overrideSkyboxEnter : portalObject.overrideSkyboxExit);
				if (material != null)
				{
					RenderSettings.skybox = material;
				}
				RenderSettings.fog = renderData.useFog;
				RenderSettings.fogColor = renderData.fogColor;
				RenderSettings.fogStartDistance = renderData.fogStart;
				RenderSettings.fogEndDistance = renderData.fogEnd;
				instance3.RenderSpaceSky(portalCam);
				if (portalObject.updateLimboSkybox)
				{
					instance3.RenderLimboSkyboxes(portalCam);
				}
				portalCam.Render();
				RenderSettings.skybox = skybox;
				RenderSettings.fog = defaultFogData.useFog == 1;
				RenderSettings.fogColor = defaultFogData.fogColor;
				RenderSettings.fogStartDistance = defaultFogData.fogStart;
				RenderSettings.fogEndDistance = defaultFogData.fogEnd;
				cb.Clear();
				cb.name = "Portal Composite";
				cb.SetGlobalTexture("_PortalColor", instance3.mainTex.colorBuffer);
				cb.SetGlobalTexture("_PortalOutlineData", instance3.reusableBufferA);
				cb.SetRenderTarget(portalCompositeIdentifiers, portalCompositeDepthIdentifier);
				cb.SetGlobalFloat("_PortalDepthLerp", 0f);
				if (renderData.parentIndex != ushort.MaxValue)
				{
					cb.ClearRenderTarget(RTClearFlags.Stencil, Color.clear, 0f, 0u);
					if (renderData.inMirroredSpace)
					{
						cb.SetInvertCulling(invertCulling: true);
						cb.SetViewProjectionMatrices(renderData.enterViewMatrix, defaultProjectionMatrixMirrored);
					}
					else
					{
						cb.SetViewProjectionMatrices(renderData.enterViewMatrix, defaultProjectionMatrix);
					}
					cb.DrawMesh(totalPortalMesh, identity, portalCompositeMaterial, handleIndex + 1, 2);
					cb.SetInvertCulling(invertCulling: false);
					if (unsafeReadOnlyPtr2[(int)renderData.parentIndex].renderData.mirror ^ renderData.inMirroredSpace)
					{
						cb.SetInvertCulling(invertCulling: true);
						cb.SetViewProjectionMatrices(renderData.parentViewMatrix, defaultProjectionMatrixMirrored);
					}
					else
					{
						cb.SetViewProjectionMatrices(renderData.parentViewMatrix, defaultProjectionMatrix);
					}
					cb.SetGlobalFloat(InMirroredSpaceID, renderData.outMirroredSpace ? 1f : 0f);
					cb.DrawMesh(totalPortalMesh, identity, portalCompositeMaterial, renderData.parentIndex + 1, 5);
					cb.SetInvertCulling(invertCulling: false);
					cb.SetGlobalFloat(InMirroredSpaceID, 0f);
				}
				else
				{
					cb.SetGlobalFloat(InMirroredSpaceID, renderData.outMirroredSpace ? 1f : 0f);
					cb.SetViewProjectionMatrices(renderData.enterViewMatrix, defaultProjectionMatrix);
					cb.DrawMesh(totalPortalMesh, identity, portalCompositeMaterial, handleIndex + 1, 6);
				}
				Graphics.ExecuteCommandBuffer(cb);
				Shader.SetGlobalFloat(InMirroredSpaceID, 0f);
				portalObject.TryStoreTexture(renderData.onscreenIndex, ref defaultProjectionMatrix, ref renderData.enterViewMatrix, portalCompositeColor, nativePortal.handle.side);
			}
		}
		tempPortalLights.Clear();
		portalLights.Clear();
	}

	internal void SetPortalOcclusion(bool enabled)
	{
		doOcclusionPass = enabled;
	}

	internal void GetOcclusionResults()
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	internal unsafe static void RebuildMesh_0024BurstManaged(NativePortal* portalsSpan, in int spanLength, SubMeshDescriptor* subMeshes, ref Mesh.MeshData data)
	{
		NativeArray<Vertex> vertexData = data.GetVertexData<Vertex>();
		NativeArray<ushort> indexData = data.GetIndexData<ushort>();
		for (int i = 0; i < spanLength; i++)
		{
			ref NativePortal reference = ref portalsSpan[i];
			int num = i * 4;
			int num2 = i * 6;
			bool flag = false;
			if (reference.renderData.renderSettings == PortalSideFlags.Exit && reference.handle.side == PortalSide.Enter)
			{
				flag = true;
			}
			if (reference.renderData.renderSettings == PortalSideFlags.Enter && reference.handle.side == PortalSide.Exit)
			{
				flag = true;
			}
			if (reference.renderData.renderSettings == PortalSideFlags.None)
			{
				flag = true;
			}
			for (int j = 0; j < 4; j++)
			{
				float num3 = ((j == 1 || j == 2) ? 1f : 0f);
				float num4 = ((j == 2 || j == 3) ? 1f : 0f);
				vertexData[num + j] = new Vertex(flag ? float3.zero : reference.vertices[j], new half2((half)num3, (half)num4));
			}
			indexData[num2] = (ushort)num;
			indexData[num2 + 1] = (ushort)(num + 1);
			indexData[num2 + 2] = (ushort)(num + 2);
			indexData[num2 + 3] = (ushort)num;
			indexData[num2 + 4] = (ushort)(num + 2);
			indexData[num2 + 5] = (ushort)(num + 3);
			subMeshes[i + 1] = new SubMeshDescriptor(num2, 6);
		}
		*subMeshes = new SubMeshDescriptor(0, spanLength * 6);
		int num5 = (data.subMeshCount = spanLength + 1);
		for (int k = 0; k < num5; k++)
		{
			data.SetSubMesh(k, subMeshes[k]);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	internal unsafe static void BurstRenderData_0024BurstManaged(in NativePortalScene nativeScene, int mainCullingMask, in FogData defaultFogData, float maxDistance, float farClipPlane, int screenArea, float minPortalPixelArea, bool doOcclusionPass, in float4x4 defaultProjGPU, in float4x4 defaultProj, in float4x4 mirrorMatrix, ref NativeList<PortalView> portalViewStack, ref NativeList<PrepassData> prepass, ref NativeList<OnscreenPortalData> onscreenPortalData, ref NativeList<float4x4> cullingFrusta, ref NativeHashMap<int, ushort> textureStoreMap, float3* clipBufferA, float3* clipBufferB, float4* frustumPlanes, ref NativeList<RenderData> portalRenders)
	{
		while (portalViewStack.Length > 0)
		{
			PortalView portalView = portalViewStack[0];
			portalViewStack.RemoveAt(0);
			if (portalView.depth > portalView.maxDepth)
			{
				continue;
			}
			CameraData camData = portalView.camData;
			float4x4 enterViewM = camData.WorldToCamera;
			float4x4 cullingMatrix = camData.CullingMatrix;
			float3 position = camData.Position;
			float3 forward = camData.Forward;
			ushort num = (ushort)onscreenPortalData.Length;
			ushort onscreenPortalsBurst = GetOnscreenPortalsBurst(in nativeScene, &position, in enterViewM, &cullingMatrix, maxDistance, farClipPlane, portalView.ignoreIndex, portalView.depth, portalView.parentOnscreenIndex, frustumPlanes, clipBufferA, clipBufferB, screenArea, minPortalPixelArea, in defaultProj, ref cullingFrusta, ref onscreenPortalData);
			if (onscreenPortalsBurst == 0)
			{
				continue;
			}
			prepass.Add(new PrepassData(-3, portalView.parentHandleIndex, enterViewM, ushort.MaxValue, portalView.clipPlane, portalView.inMirroredSpace));
			for (int i = 0; i < onscreenPortalsBurst; i++)
			{
				ushort num2 = (ushort)(num + i);
				prepass.Add(new PrepassData(onscreenPortalData[num2].handleIndex, portalView.parentHandleIndex, enterViewM, num2, portalView.clipPlane, portalView.inMirroredSpace));
			}
			if (doOcclusionPass)
			{
				prepass.Add(new PrepassData(-2, portalView.parentHandleIndex, enterViewM, ushort.MaxValue, portalView.clipPlane, portalView.inMirroredSpace));
			}
			if (portalView.parentHandleIndex != ushort.MaxValue)
			{
				prepass.Add(new PrepassData(-1, portalView.parentHandleIndex, portalView.parentViewMatrix, ushort.MaxValue, portalView.parentClipPlane, portalView.inMirroredSpace));
			}
			portalViewStack.InsertRangeWithBeginEnd(0, onscreenPortalsBurst);
			math.transpose(camData.CameraToWorld);
			float3 up = camData.Up;
			for (int j = 0; j < onscreenPortalsBurst; j++)
			{
				ushort num3 = (ushort)(num + j);
				ushort handleIndex = onscreenPortalData[num3].handleIndex;
				NativePortal nativePortal = nativeScene.renderPortals[handleIndex];
				PortalHandle handle = nativePortal.handle;
				Unity.Mathematics.Geometry.Plane plane = nativePortal.plane;
				if (!nativePortal.renderData.mirror)
				{
					int index = ((handle.side == PortalSide.Enter) ? (handleIndex + 1) : (handleIndex - 1));
					plane = nativeScene.renderPortals[index].plane;
				}
				ushort key = handleIndex;
				textureStoreMap[key] = num3;
				int num4 = 0;
				num4 = ((!nativePortal.renderData.canSeePortalLayer) ? (mainCullingMask & -1073741825) : (mainCullingMask | 0x40000000));
				float4 normalAndDistance = nativePortal.plane.NormalAndDistance;
				float3 @float;
				float3 forward2;
				float3 up2;
				if (nativePortal.renderData.mirror)
				{
					float num5 = 0f - math.dot(normalAndDistance.xyz, position) - normalAndDistance.w;
					@float = position + 2f * num5 * normalAndDistance.xyz;
					forward2 = math.reflect(forward, normalAndDistance.xyz);
					up2 = math.reflect(up, normalAndDistance.xyz);
				}
				else
				{
					float4x4 travelMatrix = nativePortal.travelMatrix;
					@float = math.transform(travelMatrix, position);
					forward2 = math.rotate(travelMatrix, forward);
					up2 = math.rotate(travelMatrix, up);
				}
				quaternion quaternion = quaternion.LookRotation(forward2, up2);
				CameraData camData2 = CameraData.FromValues(@float, quaternion, num4);
				Matrix4x4 matrix4x;
				if (nativePortal.renderData.mirror)
				{
					matrix4x = math.mul(math.mul(cullingFrusta[num3], mirrorMatrix), (Matrix4x4)camData2.WorldToCamera);
					camData2.CullingMatrix = matrix4x;
				}
				else
				{
					matrix4x = math.mul(cullingFrusta[num3], (Matrix4x4)camData2.WorldToCamera);
					camData2.CullingMatrix = matrix4x;
				}
				bool flag = nativePortal.renderData.mirror ^ portalView.inMirroredSpace;
				int maxRecursions = nativePortal.renderData.maxRecursions;
				FogData lastFogData = (nativePortal.renderData.overrideFog ? nativePortal.renderData.fogData : portalView.lastFogData);
				portalRenders.Add(new RenderData
				{
					viewPos = @float,
					viewRot = quaternion,
					enterViewMatrix = enterViewM,
					cullingMatrix = matrix4x,
					parentIndex = portalView.parentHandleIndex,
					parentViewMatrix = portalView.parentViewMatrix,
					handleIndex = handleIndex,
					onscreenIndex = num3,
					inMirroredSpace = portalView.inMirroredSpace,
					outMirroredSpace = flag,
					canSeeItself = nativePortal.renderData.canSeeItself,
					cullingMask = num4,
					depth = portalView.depth,
					maxDepth = maxRecursions,
					useFog = (lastFogData.useFog == 1),
					fogColor = lastFogData.fogColor,
					fogStart = lastFogData.fogStart,
					fogEnd = lastFogData.fogEnd,
					clipPlane = plane
				});
				int ignoreIndex = (nativePortal.renderData.mirror ? handleIndex : ((handle.side == PortalSide.Enter) ? (handleIndex + 1) : (handleIndex - 1)));
				portalViewStack[j] = new PortalView
				{
					camData = camData2,
					depth = portalView.depth + 1,
					maxDepth = nativePortal.renderData.maxRecursions,
					parentHandleIndex = handleIndex,
					parentOnscreenIndex = num3,
					ignoreIndex = ignoreIndex,
					inMirroredSpace = flag,
					parentViewMatrix = enterViewM,
					lastFogData = lastFogData,
					clipPlane = plane,
					parentClipPlane = portalView.clipPlane
				};
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	internal static void SortPortals_0024BurstManaged(ref NativeArray<OnscreenPortalData> data, ref NativeArray<int> indices)
	{
		int length = data.Length;
		for (int i = 0; i < length; i++)
		{
			indices[i] = i;
		}
		DepthComparer comp = new DepthComparer
		{
			Data = data
		};
		indices.Sort(comp);
		for (int j = 0; j < length; j++)
		{
			int index = indices[j];
			OnscreenPortalData value = data[index];
			value.depthSortedIndex = j;
			data[index] = value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	internal unsafe static void ExtractFrustumPlanes_0024BurstManaged(float4x4* matrix, float4* planes)
	{
		float4x4 float4x = *matrix;
		float4 @float = new float4(float4x.c0.x, float4x.c1.x, float4x.c2.x, float4x.c3.x);
		float4 float2 = new float4(float4x.c0.y, float4x.c1.y, float4x.c2.y, float4x.c3.y);
		float4 float3 = new float4(float4x.c0.w, float4x.c1.w, float4x.c2.w, float4x.c3.w);
		*planes = math.normalize(float3 + @float);
		planes[1] = math.normalize(float3 - @float);
		planes[2] = math.normalize(float3 + float2);
		planes[3] = math.normalize(float3 - float2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	internal unsafe static ushort GetOnscreenPortalsBurst_0024BurstManaged(in NativePortalScene scene, float3* enterPos, in float4x4 enterViewM, float4x4* enterCullM, float maxDistance, float farClipPlane, int ignoreIndex, int depth, int parentOnscreenIndex, float4* frustumPlanes, float3* clipBufferA, float3* clipBufferB, int screenArea, float minPortalPixelArea, in float4x4 defaultProjectionMatrix, ref NativeList<float4x4> cullingFrusta, ref NativeList<OnscreenPortalData> onscreenPortalData)
	{
		ExtractFrustumPlanes(enterCullM, frustumPlanes);
		float3 @float = *enterPos;
		float4 float2 = float4.zero;
		if (ignoreIndex != -1)
		{
			float2 = (frustumPlanes[4] = -scene.renderPortals[ignoreIndex].plane.NormalAndDistance);
		}
		else
		{
			float3 float3 = new float3(0f - enterViewM.c0.z, 0f - enterViewM.c1.z, 0f - enterViewM.c2.z);
			frustumPlanes[4] = new float4(float3, 0f - math.dot(float3, @float));
		}
		ushort num = 0;
		int length = scene.renderPortals.Length;
		for (int i = 0; i < length; i++)
		{
			if (ignoreIndex == i)
			{
				continue;
			}
			NativePortal nativePortal = scene.renderPortals[i];
			if ((nativePortal.renderData.renderSettings == PortalSideFlags.Exit && nativePortal.handle.side == PortalSide.Enter) || (nativePortal.renderData.renderSettings == PortalSideFlags.Enter && nativePortal.handle.side == PortalSide.Exit) || nativePortal.renderData.renderSettings == PortalSideFlags.None || (depth > 0 && !nativePortal.renderData.appearsInRecursions))
			{
				continue;
			}
			float4 normalAndDistance = nativePortal.plane.NormalAndDistance;
			if (math.dot(normalAndDistance.xyz, @float) + normalAndDistance.w > 0f || math.distance(@float, nativePortal.transform.center) > maxDistance)
			{
				continue;
			}
			if (ignoreIndex != -1)
			{
				bool flag = false;
				for (int j = 0; j < 4; j++)
				{
					if (math.dot(float2.xyz, nativePortal.vertices[j]) + float2.w > 0f)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
			}
			for (int k = 0; k < 4; k++)
			{
				clipBufferA[k] = nativePortal.vertices[k];
			}
			int finalCount;
			bool flag2 = FrustumClipper.ClipQuadToCameraFrustum(frustumPlanes, clipBufferA, clipBufferB, out finalCount);
			if (finalCount >= 3 && CalculateCullingData(flag2 ? clipBufferB : clipBufferA, finalCount, in enterViewM, in defaultProjectionMatrix, screenArea, minPortalPixelArea, out var rectBounds, out var nearClip))
			{
				cullingFrusta.Add(Frustum(rectBounds.x, rectBounds.y, rectBounds.z, rectBounds.w, nearClip, farClipPlane));
				OnscreenPortalData value = new OnscreenPortalData
				{
					handleIndex = (ushort)i,
					depth = depth,
					parentOnscreenIndex = parentOnscreenIndex,
					depthSortedIndex = i,
					nearClip = nearClip
				};
				onscreenPortalData.Add(in value);
				num++;
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	internal unsafe static bool CalculateCullingData_0024BurstManaged(float3* clippedVerts, int count, in float4x4 enterViewM, in float4x4 proj, int screenArea, float areaThreshold, out float4 rectBounds, out float nearClip)
	{
		rectBounds = default(float4);
		nearClip = float.NegativeInfinity;
		for (int i = 0; i < count; i++)
		{
			float3 @float = math.transform(enterViewM, clippedVerts[i]);
			nearClip = math.max(@float.z, nearClip);
			clippedVerts[i] = @float;
		}
		float x = proj.c0.x;
		float y = proj.c1.y;
		float num = 0f;
		for (int j = 0; j < count; j++)
		{
			float3 float2 = clippedVerts[j];
			float3 float3 = clippedVerts[(j + 1) % count];
			float num2 = math.rcp(0f - float2.z);
			float num3 = math.rcp(0f - float3.z);
			float num4 = x * float2.x * num2;
			float num5 = y * float2.y * num2;
			float num6 = x * float3.x * num3;
			float num7 = y * float3.y * num3;
			num += num4 * num7 - num6 * num5;
		}
		if (math.abs(num) * 0.5f * (float)screenArea * 0.25f < areaThreshold)
		{
			return false;
		}
		float x2 = float.PositiveInfinity;
		float num8 = float.NegativeInfinity;
		float num9 = float.PositiveInfinity;
		float num10 = float.NegativeInfinity;
		for (int k = 0; k < count; k++)
		{
			float3 float4 = clippedVerts[k];
			float num11 = nearClip * math.rcp(float4.z);
			float4 *= num11;
			x2 = math.min(x2, float4.x);
			num8 = math.max(num8, float4.x);
			num9 = math.min(num9, float4.y);
			num10 = math.max(num10, float4.y);
		}
		rectBounds = new float4(x2, num8, num9, num10);
		nearClip = 0f - nearClip;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	internal unsafe static void UpdateOcclusionBurst_0024BurstManaged([NoAlias] ref NativeList<OnscreenPortalData> data, ulong bitset, [NoAlias] ref NativeArray<bool> visibility)
	{
		int length = data.Length;
		OnscreenPortalData* unsafeReadOnlyPtr = data.GetUnsafeReadOnlyPtr();
		bool* unsafePtr = (bool*)visibility.GetUnsafePtr();
		for (int i = 0; i < length; i++)
		{
			int depthSortedIndex = unsafeReadOnlyPtr[i].depthSortedIndex;
			if (depthSortedIndex < 64)
			{
				unsafePtr[i] = (bitset & (ulong)(1L << depthSortedIndex)) != 0;
				continue;
			}
			int parentOnscreenIndex = unsafeReadOnlyPtr[i].parentOnscreenIndex;
			bool flag = false;
			for (int j = 0; j < 6; j++)
			{
				if (unsafeReadOnlyPtr[parentOnscreenIndex].depthSortedIndex < 64)
				{
					unsafePtr[i] = (bitset & (ulong)(1L << unsafeReadOnlyPtr[parentOnscreenIndex].depthSortedIndex)) != 0;
					flag = true;
					break;
				}
				parentOnscreenIndex = unsafeReadOnlyPtr[parentOnscreenIndex].parentOnscreenIndex;
			}
			if (!flag)
			{
				unsafePtr[i] = true;
			}
		}
	}
}
