using System;
using System.Runtime.CompilerServices;
using Gravity;
using ULTRAKILL.Portal.Geometry;
using ULTRAKILL.Portal.Native;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace ULTRAKILL.Portal;

[BurstCompile]
public class Portal : MonoBehaviour
{
	public struct PortalBurstData
	{
		public unsafe Vector3* entryPos;

		public unsafe Vector3* entryFwd;

		public unsafe Vector3* exitPos;

		public unsafe Vector3* exitFwd;

		public unsafe Quaternion* entryRot;

		public unsafe Quaternion* exitRot;

		public float hW;

		public float hH;

		public bool infiniteRecursion;

		public unsafe float4x4* portalScale;

		public unsafe float4x4* portalScaleInv;

		public unsafe float4x4* outEntryToWorld;

		public unsafe float4x4* outEntryToLocal;

		public unsafe float4x4* outExitToWorld;

		public unsafe float4x4* outExitToLocal;

		public unsafe float4x4* outTravel;

		public unsafe float4x4* outTravelRev;

		public unsafe float4* outEntryPlane;

		public unsafe float4* outExitPlane;

		public unsafe float3* enterVerts;

		public unsafe float3* exitVerts;

		public unsafe float4* enterVertsVec4;

		public unsafe float4* exitVertsVec4;

		public unsafe float4x4* entryBase;

		public unsafe float4x4* exitBase;
	}

	internal unsafe delegate void UpdatePortalBurst_00002980_0024PostfixBurstDelegate([NoAlias] PortalBurstData* d);

	internal static class UpdatePortalBurst_00002980_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(UpdatePortalBurst_00002980_0024PostfixBurstDelegate).TypeHandle);
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

		static UpdatePortalBurst_00002980_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke([NoAlias] PortalBurstData* d)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<PortalBurstData*, void>)functionPointer)(d);
					return;
				}
			}
			UpdatePortalBurst_0024BurstManaged(d);
		}
	}

	public static readonly float4x4 PORTAL_SCALE = Matrix4x4.Scale(new Vector3(-1f, 1f, -1f));

	public static readonly float4x4 PORTAL_SCALE_INV = Matrix4x4.Scale(new Vector3(-1f, 1f, -1f)).inverse;

	[Header("Shape")]
	[SerializeReference]
	public IPortalShape shape;

	[Header("Linked Portals")]
	public Transform entry;

	public bool usePerceivedGravityOnEnter;

	public bool forceOrthogonalGravityOnEnter;

	public GravityVolume enterGravityVolume;

	[Space]
	public Transform exit;

	public bool usePerceivedGravityOnExit;

	public bool forceOrthogonalGravityOnExit;

	public GravityVolume exitGravityVolume;

	public float disableRange;

	[Header("Events")]
	public UnityEventPortalTravel onEntryTravel;

	public UnityEventPortalTravel onExitTravel;

	[Header("Settings")]
	public PortalTravellerFlags entryTravelFlags = PortalTravellerFlags.Player | PortalTravellerFlags.PlayerProjectile | PortalTravellerFlags.Enemy | PortalTravellerFlags.EnemyProjectile | PortalTravellerFlags.Other;

	public PortalTravellerFlags exitTravelFlags = PortalTravellerFlags.Player | PortalTravellerFlags.PlayerProjectile | PortalTravellerFlags.Enemy | PortalTravellerFlags.EnemyProjectile | PortalTravellerFlags.Other;

	public bool passThroughNonTraversals = true;

	public const float DEFAULT_OFFSET = 1.5f;

	public bool overrideLinkOffset;

	public float enterOffset = 1.5f;

	public float exitOffset = 1.5f;

	public float additionalSampleThreshold;

	public bool isMultiPanel;

	[Space]
	public PortalClippingMethod clippingMethod;

	public PortalSideFlags renderSettings = PortalSideFlags.Enter | PortalSideFlags.Exit;

	public bool mirror;

	public bool appearsInRecursions = true;

	public bool canSeeItself = true;

	public bool canSeePortalLayer = true;

	public bool allowCameraTraversals;

	public bool canHearAudio = true;

	public bool consumeAudio;

	public bool supportInfiniteRecursion;

	public bool updateLimboSkybox;

	[Space]
	public float minimumEntrySideSpeed;

	public float minimumExitSideSpeed;

	public int maxRecursions = 3;

	[Space(10f)]
	public Material overrideSkyboxEnter;

	public Material overrideSkyboxExit;

	[Space(10f)]
	public bool enableOverrideFog;

	public bool useFogEnter = true;

	public Color overrideFogColorEnter = Color.black;

	public float overrideFogStartEnter;

	public float overrideFogEndEnter = 300f;

	public bool useFogExit = true;

	public Color overrideFogColorExit = Color.black;

	public float overrideFogStartExit;

	public float overrideFogEndExit = 300f;

	[HideInInspector]
	public RenderTexture fakeEnterTex;

	[HideInInspector]
	public RenderTexture fakeExitTex;

	public Matrix4x4 fakeVPMatrix;

	private bool storeEnterThisFrame;

	private bool storeExitThisFrame;

	private int enterOnscreenIndex = -1;

	private int exitOnscreenIndex = -1;

	public NativePortalTransform entryTransform => GetTransform(PortalSide.Enter);

	public NativePortalTransform exitTransform => GetTransform(PortalSide.Exit);

	public UnityEventPortalTravel onTravel(PortalSide side)
	{
		if (side != PortalSide.Enter)
		{
			return onExitTravel;
		}
		return onEntryTravel;
	}

	public PortalTravellerFlags GetTravelFlags(PortalSide side)
	{
		if (side != PortalSide.Enter)
		{
			return exitTravelFlags;
		}
		return entryTravelFlags;
	}

	public float LinkOffset(PortalSide side)
	{
		if (!overrideLinkOffset)
		{
			return 1.5f;
		}
		if (side != PortalSide.Enter)
		{
			return exitOffset;
		}
		return enterOffset;
	}

	private void Start()
	{
		MonoSingleton<PortalManagerV2>.Instance.AddPortal(this);
		if (supportInfiniteRecursion)
		{
			PlaneShape planeShape = (PlaneShape)(object)shape;
			float num = planeShape.width / planeShape.height;
			int width = ((num > 0f) ? 256 : Mathf.RoundToInt(256f * num));
			int height = ((num > 0f) ? Mathf.RoundToInt(256f / num) : 256);
			fakeEnterTex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
			fakeExitTex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
		}
	}

	public void SetNeedToStoreTexture(int currentOnscreenIndex, PortalSide side)
	{
		if (supportInfiniteRecursion)
		{
			if (side == PortalSide.Enter)
			{
				storeEnterThisFrame = true;
				enterOnscreenIndex = currentOnscreenIndex;
			}
			else
			{
				storeExitThisFrame = true;
				exitOnscreenIndex = currentOnscreenIndex;
			}
		}
	}

	internal void TryStoreTexture(int onscreenHandleIndex, ref Matrix4x4 enterProjectionMatrix, ref Matrix4x4 enterViewMatrix, RenderTexture portalCompositeColor, PortalSide side)
	{
		if (!supportInfiniteRecursion)
		{
			return;
		}
		bool flag = side == PortalSide.Enter;
		int num = (flag ? enterOnscreenIndex : exitOnscreenIndex);
		bool flag2 = (flag ? storeEnterThisFrame : storeExitThisFrame);
		if ((onscreenHandleIndex == num) & flag2)
		{
			if (flag)
			{
				storeEnterThisFrame = false;
			}
			else
			{
				storeExitThisFrame = false;
			}
			RenderTexture dest = (flag ? fakeEnterTex : fakeExitTex);
			Matrix4x4 value = enterProjectionMatrix * enterViewMatrix;
			Material fakeRecursionCopy = MonoSingleton<PortalManagerV2>.Instance.render.fakeRecursionCopy;
			NativePortalScene nativeScene = MonoSingleton<PortalManagerV2>.Instance.Scene.nativeScene;
			NativePortal nativePortal = nativeScene.LookupPortal(new PortalHandle(GetInstanceID(), side));
			if (nativePortal.valid)
			{
				PortalVertices vertices = nativePortal.vertices;
				Vector4[] values = new Vector4[4]
				{
					new Vector4(vertices.v0.x, vertices.v0.y, vertices.v0.z, 1f),
					new Vector4(vertices.v1.x, vertices.v1.y, vertices.v1.z, 1f),
					new Vector4(vertices.v2.x, vertices.v2.y, vertices.v2.z, 1f),
					new Vector4(vertices.v3.x, vertices.v3.y, vertices.v3.z, 1f)
				};
				fakeRecursionCopy.SetVectorArray("_PortalCorners", values);
				fakeRecursionCopy.SetMatrix("_PortalMatrix", value);
				Graphics.Blit(portalCompositeColor, dest, fakeRecursionCopy);
			}
		}
	}

	private void OnEnable()
	{
		if (TryGetComponent<VirtualAudioListener>(out var component))
		{
			component.enabled = true;
		}
		if (TryGetComponent<VirtualAudioOutput>(out var component2))
		{
			component2.enabled = true;
		}
	}

	private void OnDisable()
	{
		if (TryGetComponent<VirtualAudioListener>(out var component))
		{
			component.enabled = false;
		}
		if (TryGetComponent<VirtualAudioOutput>(out var component2))
		{
			component2.enabled = false;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!(entry == null))
		{
			_ = exit == null;
		}
	}

	private void OnDrawGizmos()
	{
		if (!(entry == null) && !(exit == null))
		{
			PortalTransform portalTransform = new PortalTransform();
			Matrix4x4 entryToWorld = Matrix4x4.TRS(entry.position, entry.rotation, Vector3.one);
			Matrix4x4 entryToLocal = Matrix4x4.Inverse(entryToWorld);
			portalTransform.UpdateTransform(ref entryToLocal, ref entryToWorld);
			PortalTransform portalTransform2 = new PortalTransform();
			Matrix4x4 entryToWorld2 = Matrix4x4.TRS(exit.position, exit.rotation, Vector3.one);
			Matrix4x4 entryToLocal2 = Matrix4x4.Inverse(entryToWorld2);
			portalTransform2.UpdateTransform(ref entryToLocal2, ref entryToWorld2);
			shape.DrawDebug(portalTransform, 0f, Color.grey);
			shape.DrawDebug(portalTransform2, 0f, Color.grey);
		}
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	private unsafe static void UpdatePortalBurst([NoAlias] PortalBurstData* d)
	{
		UpdatePortalBurst_00002980_0024BurstDirectCall.Invoke(d);
	}

	public void SetUpdatedSkyFog(PortalSide side)
	{
		SetUpdatedSkyFog(side == PortalSide.Enter);
	}

	public void SetEnterSkyFog()
	{
		SetUpdatedSkyFog(isEnter: true);
	}

	public void SetExitSkyFog()
	{
		SetUpdatedSkyFog(isEnter: false);
	}

	private void SetUpdatedSkyFog(bool isEnter)
	{
		if (enableOverrideFog)
		{
			RenderSettings.fog = (isEnter ? useFogEnter : useFogExit);
			RenderSettings.fogColor = (isEnter ? overrideFogColorEnter : overrideFogColorExit);
			RenderSettings.fogStartDistance = (isEnter ? overrideFogStartEnter : overrideFogStartExit);
			RenderSettings.fogEndDistance = (isEnter ? overrideFogEndEnter : overrideFogEndExit);
		}
		Material material = (isEnter ? overrideSkyboxEnter : overrideSkyboxExit);
		if (material != null)
		{
			RenderSettings.skybox = material;
		}
	}

	public PlaneShape GetShape()
	{
		return (PlaneShape)(object)shape;
	}

	[Obsolete("Use NativePortal directly instead where possible")]
	public Matrix4x4 GetTravelMatrix(PortalSide side)
	{
		NativePortal nativePortal = MonoSingleton<PortalManagerV2>.Instance.Scene.nativeScene.LookupPortal(new PortalHandle(GetInstanceID(), side));
		return nativePortal.travelMatrixManaged;
	}

	[Obsolete("Use NativePortal directly instead where possible")]
	public NativePortalTransform GetTransform(PortalSide side)
	{
		return MonoSingleton<PortalManagerV2>.Instance.Scene.nativeScene.LookupPortal(new PortalHandle(GetInstanceID(), side)).transform;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
	internal unsafe static void UpdatePortalBurst_0024BurstManaged([NoAlias] PortalBurstData* d)
	{
		float3 entryPos = *(float3*)d->entryPos;
		quaternion entryRot = *(quaternion*)d->entryRot;
		float3 exitPos = *(float3*)d->exitPos;
		quaternion exitRot = *(quaternion*)d->exitRot;
		float3 entryFwd = *(float3*)d->entryFwd;
		float3 exitFwd = *(float3*)d->exitFwd;
		float4x4 portalScale = *d->portalScale;
		float4x4 portalScaleInv = *d->portalScaleInv;
		float4x4 float4x5 = float4x4.TRS(entryPos, entryRot, new float3(1));
		float4x4 float4x6 = math.fastinverse(float4x5);
		float4x4 float4x7 = float4x4.TRS(exitPos, exitRot, new float3(1));
		float4x4 float4x8 = math.fastinverse(float4x7);
		*d->outTravel = math.mul(math.mul(float4x7, portalScale), float4x6);
		*d->outTravelRev = math.mul(math.mul(float4x5, portalScaleInv), float4x8);
		*d->entryBase = float4x5;
		d->entryBase[1] = float4x6;
		float3* ptr = (float3*)(d->entryBase + 2);
		*ptr = float4x5.c3.xyz;
		ptr[1] = float4x5.c2.xyz;
		ptr[2] = float4x5.c1.xyz;
		ptr[3] = float4x5.c0.xyz;
		ptr[4] = -ptr[3];
		ptr[5] = -ptr[2];
		ptr[6] = -ptr[1];
		*d->exitBase = float4x7;
		d->exitBase[1] = float4x8;
		float3* ptr2 = (float3*)(d->exitBase + 2);
		*ptr2 = float4x7.c3.xyz;
		ptr2[1] = float4x7.c2.xyz;
		ptr2[2] = float4x7.c1.xyz;
		ptr2[3] = float4x7.c0.xyz;
		ptr2[4] = -ptr2[3];
		ptr2[5] = -ptr2[2];
		ptr2[6] = -ptr2[1];
		*d->outEntryPlane = new float4(entryFwd, 0f - math.dot(entryFwd, entryPos));
		*d->outExitPlane = new float4(exitFwd, 0f - math.dot(exitFwd, exitPos));
		float4 float5 = new float4(0f - d->hW, d->hW, d->hW, 0f - d->hW);
		float4 float6 = new float4(d->hH, d->hH, 0f - d->hH, 0f - d->hH);
		float4 float7 = float4x5.c0.x * float5 + float4x5.c1.x * float6 + float4x5.c3.x;
		float4 float8 = float4x5.c0.y * float5 + float4x5.c1.y * float6 + float4x5.c3.y;
		float4 float9 = float4x5.c0.z * float5 + float4x5.c1.z * float6 + float4x5.c3.z;
		float4 float10 = float4x7.c0.x * float5 + float4x7.c1.x * float6 + float4x7.c3.x;
		float4 float11 = float4x7.c0.y * float5 + float4x7.c1.y * float6 + float4x7.c3.y;
		float4 float12 = float4x7.c0.z * float5 + float4x7.c1.z * float6 + float4x7.c3.z;
		bool infiniteRecursion = d->infiniteRecursion;
		for (int i = 0; i < 4; i++)
		{
			d->enterVerts[i] = new float3(float7[i], float8[i], float9[i]);
			d->exitVerts[i] = new float3(float10[i], float11[i], float12[i]);
			if (infiniteRecursion)
			{
				d->enterVertsVec4[i] = new float4(float7[i], float8[i], float9[i], 1f);
				d->exitVertsVec4[i] = new float4(float10[i], float11[i], float12[i], 1f);
			}
		}
	}
}
