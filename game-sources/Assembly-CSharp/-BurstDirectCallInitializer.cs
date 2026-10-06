using BeamHitInterpolation;
using ULTRAKILL.Enemy;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Geometry;
using ULTRAKILL.Portal.Native;
using UnityEngine;

internal static class _0024BurstDirectCallInitializer
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void Initialize()
	{
		BloodsplatterManager.CreateBloodstain_000002BE_0024BurstDirectCall.Initialize();
		ColliderUtility.BurstClosestPoint_Int16_000004A9_0024BurstDirectCall.Initialize();
		ColliderUtility.BurstClosestPoint_Int32_000004AA_0024BurstDirectCall.Initialize();
		CameraData.Create_000017CB_0024BurstDirectCall.Initialize();
		CameraData.CalculateObliqueMatrix_000017CC_0024BurstDirectCall.Initialize();
		FrustumClipper.ClipQuadToCameraFrustum_000017D0_0024BurstDirectCall.Initialize();
		VirtualAudioFilter.UpdateVelocityBurst_00002247_0024BurstDirectCall.Initialize();
		VirtualAudioFilter.AddOutputBurst_0000224C_0024BurstDirectCall.Initialize();
		VirtualAudioFilter.ProcessStereo_0000224F_0024BurstDirectCall.Initialize();
		VirtualAudioManager.LoopOverListeners_0000226E_0024BurstDirectCall.Initialize();
		Portal.UpdatePortalBurst_00002980_0024BurstDirectCall.Initialize();
		PortalRenderV2.RebuildMesh_00002A09_0024BurstDirectCall.Initialize();
		PortalRenderV2.BurstRenderData_00002A0A_0024BurstDirectCall.Initialize();
		PortalRenderV2.SortPortals_00002A0B_0024BurstDirectCall.Initialize();
		PortalRenderV2.ExtractFrustumPlanes_00002A0D_0024BurstDirectCall.Initialize();
		PortalRenderV2.GetOnscreenPortalsBurst_00002A0E_0024BurstDirectCall.Initialize();
		PortalRenderV2.CalculateCullingData_00002A10_0024BurstDirectCall.Initialize();
		PortalRenderV2.UpdateOcclusionBurst_00002A18_0024BurstDirectCall.Initialize();
		PortalScene.CalculateMatrices_00002A31_0024BurstDirectCall.Initialize();
		PortalScene.Internal_FindCrossedPortals_00002A36_0024BurstDirectCall.Initialize();
		PortalScene.Internal_FindPortalsBetween_00002A39_0024BurstDirectCall.Initialize();
		PortalScene.Internal_TraversePortalSequence_00002A3A_0024BurstDirectCall.Initialize();
		NativePortalExtensions.CalculateData_00002B22_0024BurstDirectCall.Initialize();
		NativePortalExtensions.Raycast_00002B23_0024BurstDirectCall.Initialize();
		PlaneShapeExtensions.GetClosestPoint_00002B41_0024BurstDirectCall.Initialize();
		TargetTracker.CountPermutations_00002B9C_0024BurstDirectCall.Initialize();
		BeamHitInterpolator.FindBestTimeAndMinDistSq_00002E1C_0024BurstDirectCall.Initialize();
		BeamHitInterpolator.DistanceSqPointSegment_00002E1D_0024BurstDirectCall.Initialize();
		BeamHitInterpolator.ClosestPointOnSegment_00002E1E_0024BurstDirectCall.Initialize();
		BeamHitInterpolator.CalculateSweptObb_00002E1F_0024BurstDirectCall.Initialize();
	}
}
