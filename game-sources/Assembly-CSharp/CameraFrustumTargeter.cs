using System;
using ULTRAKILL.Portal;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class CameraFrustumTargeter : MonoSingleton<CameraFrustumTargeter>
{
	private const int MaxPotentialTargets = 256;

	public static bool isEnabled;

	[SerializeField]
	private RectTransform crosshair;

	[SerializeField]
	private LayerMask mask;

	private LayerMask occlusionMask;

	[SerializeField]
	private float maximumRange = 1000f;

	public float maxHorAim = 1f;

	private RaycastHit[] occluders;

	private Plane[] frustum;

	private Plane[] portalFrustum;

	private Vector3[] corners;

	private Bounds bounds;

	private Collider[] targets;

	private Collider[] portalTargets;

	private Matrix4x4 currentTargetApparentTransform = Matrix4x4.identity;

	private Camera camera;

	public Collider CurrentTarget { get; private set; }

	public bool IsAutoAimed { get; private set; }

	public Vector3 CurrentTargetAimPosition
	{
		get
		{
			if (CurrentTarget == null)
			{
				return default(Vector3);
			}
			return currentTargetApparentTransform.MultiplyPoint3x4(CurrentTarget.bounds.center);
		}
	}

	public Vector3 GetAimDirectionFrom(Vector3 linearOrigin)
	{
		if (CurrentTarget == null)
		{
			return default(Vector3);
		}
		if (currentTargetApparentTransform.isIdentity)
		{
			return (CurrentTarget.bounds.center - linearOrigin).normalized;
		}
		return (CurrentTargetAimPosition - camera.transform.position).normalized;
	}

	private void Awake()
	{
		frustum = new Plane[6];
		portalFrustum = new Plane[6];
		corners = new Vector3[4];
		targets = new Collider[256];
		portalTargets = new Collider[256];
		camera = GetComponent<Camera>();
		occluders = new RaycastHit[16];
		occlusionMask = LayerMaskDefaults.Get(LMD.Environment);
		occlusionMask = (int)occlusionMask | 1;
	}

	private void Start()
	{
		isEnabled = MonoSingleton<PrefsManager>.Instance.GetBool("autoAim");
		maxHorAim = MonoSingleton<PrefsManager>.Instance.GetFloat("autoAimAmount");
	}

	private void OnEnable()
	{
		PrefsManager.onPrefChanged = (Action<string, object>)Delegate.Combine(PrefsManager.onPrefChanged, new Action<string, object>(OnPrefChanged));
	}

	private void OnDisable()
	{
		PrefsManager.onPrefChanged = (Action<string, object>)Delegate.Remove(PrefsManager.onPrefChanged, new Action<string, object>(OnPrefChanged));
	}

	private void OnPrefChanged(string key, object value)
	{
		if (!(key == "autoAim"))
		{
			if (key == "autoAimAmount" && value is float num)
			{
				maxHorAim = num;
			}
		}
		else if (value is bool flag)
		{
			isEnabled = flag;
		}
	}

	private bool RaycastFromViewportCenterPortalAware(out Collider hitCollider, out float hitDistance, out PortalTraversalV2[] traversals)
	{
		Ray ray = camera.ViewportPointToRay(new Vector2(0.5f, 0.5f));
		if (MonoSingleton<PortalManagerV2>.Instance != null)
		{
			if (PortalPhysicsV2.Raycast(ray.origin, ray.direction, maximumRange, mask.value, out var hitInfo, out traversals, out var _))
			{
				hitCollider = hitInfo.collider;
				hitDistance = hitInfo.distance;
				return hitCollider != null;
			}
			hitCollider = null;
			hitDistance = 0f;
			return false;
		}
		traversals = null;
		if (Physics.Raycast(ray, out var hitInfo2, maximumRange, mask.value))
		{
			hitCollider = hitInfo2.collider;
			hitDistance = hitInfo2.distance;
			return true;
		}
		hitCollider = null;
		hitDistance = 0f;
		return false;
	}

	private bool CenterRayOccluded(float maxDistance)
	{
		Ray ray = camera.ViewportPointToRay(new Vector2(0.5f, 0.5f));
		PhysicsCastResult hitInfo;
		PortalTraversalV2[] portalTraversals;
		Vector3 endPoint;
		if (MonoSingleton<PortalManagerV2>.Instance != null)
		{
			return PortalPhysicsV2.Raycast(ray.origin, ray.direction, maxDistance, occlusionMask.value, out hitInfo, out portalTraversals, out endPoint);
		}
		return Physics.Raycast(ray, maxDistance, occlusionMask);
	}

	private void CalculateFrustumInformation()
	{
		camera.CalculateFrustumCorners(new Rect(0f, 0f, 1f, 1f), maximumRange, Camera.MonoOrStereoscopicEye.Mono, corners);
		bounds = GeometryUtility.CalculateBounds(corners, camera.transform.localToWorldMatrix);
		bounds.size = new Vector3(bounds.size.x, bounds.size.y, maximumRange);
		bounds.center = base.transform.position;
	}

	private bool IsTargetValid(Collider target)
	{
		if (target == null)
		{
			return false;
		}
		if (target.GetComponent<PortalAwareRendererClone>() != null)
		{
			return false;
		}
		if ((target.gameObject.layer != 22 && target.isTrigger) || (target.gameObject.layer == 22 && (!target.TryGetComponent<HookPoint>(out var component) || !component.active)))
		{
			return false;
		}
		if (target.gameObject.layer == 10 && !target.TryGetComponent<Coin>(out var _))
		{
			return false;
		}
		if (target.gameObject.layer == 14 && !target.TryGetComponent<Grenade>(out var _))
		{
			return false;
		}
		return true;
	}

	private Matrix4x4 BuildApparentTransform(Vector3 virtualViewPos, Quaternion virtualViewRot)
	{
		Matrix4x4 matrix4x = Matrix4x4.TRS(virtualViewPos, virtualViewRot, new Vector3(1f, 1f, -1f));
		return camera.cameraToWorldMatrix * matrix4x.inverse;
	}

	private void ProcessThroughPortalCandidates(ref float closestDistance, ref Collider closestTarget, ref Matrix4x4 closestApparentTransform)
	{
		PortalManagerV2 portalManagerV = MonoSingleton<PortalManagerV2>.Instance;
		if (portalManagerV == null || portalManagerV.render == null)
		{
			return;
		}
		NativeList<PortalRenderV2.RenderData> renderDatas = portalManagerV.render.renderDatas;
		if (!renderDatas.IsCreated || renderDatas.Length == 0)
		{
			return;
		}
		Vector3 position = camera.transform.position;
		for (int i = 0; i < renderDatas.Length; i++)
		{
			PortalRenderV2.RenderData renderData = renderDatas[i];
			GeometryUtility.CalculateFrustumPlanes(renderData.cullingMatrix, portalFrustum);
			int num = Physics.OverlapBoxNonAlloc(renderData.viewPos, this.bounds.extents, portalTargets, renderData.viewRot, mask.value);
			Matrix4x4 matrix4x = BuildApparentTransform(renderData.viewPos, renderData.viewRot);
			Vector3 lhs = new Vector3(renderData.clipPlane.x, renderData.clipPlane.y, renderData.clipPlane.z);
			float num2 = renderData.clipPlane.w;
			float magnitude = lhs.magnitude;
			bool flag = magnitude > 0.001f;
			if (flag)
			{
				lhs /= magnitude;
				num2 /= magnitude;
			}
			for (int j = 0; j < num; j++)
			{
				Collider collider = portalTargets[j];
				if (!IsTargetValid(collider))
				{
					continue;
				}
				Bounds bounds = collider.bounds;
				if (!GeometryUtility.TestPlanesAABB(portalFrustum, bounds))
				{
					continue;
				}
				Vector3 center = bounds.center;
				Vector3 vector = center - renderData.viewPos;
				float magnitude2 = vector.magnitude;
				if (magnitude2 < 0.001f)
				{
					continue;
				}
				Vector3 vector2 = vector / magnitude2;
				Vector3 origin = renderData.viewPos;
				float num3 = magnitude2;
				if (flag)
				{
					float num4 = Vector3.Dot(lhs, vector2);
					if (Mathf.Abs(num4) > 0.001f)
					{
						float num5 = (0f - (Vector3.Dot(lhs, renderData.viewPos) + num2)) / num4;
						if (num5 > 0f && num5 < num3)
						{
							float num6 = num5 + 0.001f;
							origin = renderData.viewPos + vector2 * num6;
							num3 = magnitude2 - num6;
						}
					}
				}
				if (num3 >= 0.001f && Physics.Raycast(origin, vector2, num3, occlusionMask.value, QueryTriggerInteraction.Ignore))
				{
					continue;
				}
				Vector3 vector3 = matrix4x.MultiplyPoint3x4(center);
				Vector3 a = camera.WorldToViewportPoint(vector3);
				if (a.x > 0.5f + maxHorAim / 2f || a.x < 0.5f - maxHorAim / 2f || a.y > 0.5f + maxHorAim / 2f || a.y < 0.5f - maxHorAim / 2f || a.z < 0f)
				{
					continue;
				}
				Vector3 vector4 = vector3 - position;
				float magnitude3 = vector4.magnitude;
				if (magnitude3 < 0.001f)
				{
					continue;
				}
				Vector3 direction = vector4 / magnitude3;
				PhysicsCastResult hitInfo;
				PortalTraversalV2[] portalTraversals;
				Vector3 endPoint;
				bool flag2 = PortalPhysicsV2.Raycast(position, direction, magnitude3 + 1f, mask.value | occlusionMask.value, out hitInfo, out portalTraversals, out endPoint, QueryTriggerInteraction.Ignore);
				if (portalTraversals != null && portalTraversals.Length != 0 && !(Vector3.Distance(flag2 ? hitInfo.point : endPoint, center) > collider.bounds.extents.magnitude + 2f))
				{
					float num7 = Vector3.Distance(a, new Vector2(0.5f, 0.5f));
					if (num7 < closestDistance)
					{
						closestDistance = num7;
						closestTarget = collider;
						closestApparentTransform = matrix4x;
					}
				}
			}
		}
	}

	private void Update()
	{
		if (!isEnabled || maxHorAim == 0f)
		{
			CurrentTarget = null;
			IsAutoAimed = false;
			return;
		}
		if (RaycastFromViewportCenterPortalAware(out var hitCollider, out var hitDistance, out var traversals) && !CenterRayOccluded(hitDistance) && (!hitCollider.isTrigger || hitCollider.gameObject.layer == 22))
		{
			CurrentTarget = hitCollider;
			IsAutoAimed = false;
			if (traversals != null && traversals.Length != 0 && MonoSingleton<PortalManagerV2>.Instance != null)
			{
				Matrix4x4 travelMatrix = MonoSingleton<PortalManagerV2>.Instance.Scene.GetTravelMatrix(traversals);
				Transform transform = camera.transform;
				Vector3 virtualViewPos = travelMatrix.MultiplyPoint3x4(transform.position);
				Vector3 forward = travelMatrix.MultiplyVector(transform.forward);
				Vector3 upwards = travelMatrix.MultiplyVector(transform.up);
				Quaternion virtualViewRot = Quaternion.LookRotation(forward, upwards);
				currentTargetApparentTransform = BuildApparentTransform(virtualViewPos, virtualViewRot);
			}
			else
			{
				currentTargetApparentTransform = Matrix4x4.identity;
			}
			return;
		}
		CalculateFrustumInformation();
		int num = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, targets, base.transform.rotation, mask.value);
		float closestDistance = float.PositiveInfinity;
		Collider closestTarget = null;
		Matrix4x4 closestApparentTransform = Matrix4x4.identity;
		for (int i = 0; i < num; i++)
		{
			Vector3 position = base.transform.position;
			Vector3 direction = targets[i].bounds.center - position;
			if (targets[i].GetComponent<PortalAwareRendererClone>() != null || (targets[i].gameObject.layer != 22 && targets[i].isTrigger) || (targets[i].gameObject.layer == 22 && (!targets[i].TryGetComponent<HookPoint>(out var component) || !component.active)) || (targets[i].gameObject.layer == 10 && !targets[i].TryGetComponent<Coin>(out var _)) || (targets[i].gameObject.layer == 14 && !targets[i].TryGetComponent<Grenade>(out var _)))
			{
				continue;
			}
			PortalPhysicsV2.Raycast(position, direction.normalized, direction.magnitude, occlusionMask.value, out var _, out var portalTraversals, out var _, QueryTriggerInteraction.Ignore);
			if (portalTraversals != null && portalTraversals.Length != 0)
			{
				continue;
			}
			int num2 = Physics.RaycastNonAlloc(position, direction, occluders, direction.magnitude, occlusionMask.value, QueryTriggerInteraction.Ignore);
			int num3 = 0;
			while (true)
			{
				if (num3 < num2)
				{
					if (!(occluders[num3].collider == null))
					{
						break;
					}
					num3++;
					continue;
				}
				Vector3 a = camera.WorldToViewportPoint(targets[i].bounds.center);
				float num4 = Vector3.Distance(a, new Vector2(0.5f, 0.5f));
				if (!(a.x > 0.5f + maxHorAim / 2f) && !(a.x < 0.5f - maxHorAim / 2f) && !(a.y > 0.5f + maxHorAim / 2f) && !(a.y < 0.5f - maxHorAim / 2f) && !(a.z < 0f) && num4 < closestDistance)
				{
					closestDistance = num4;
					closestTarget = targets[i];
					closestApparentTransform = Matrix4x4.identity;
				}
				break;
			}
		}
		ProcessThroughPortalCandidates(ref closestDistance, ref closestTarget, ref closestApparentTransform);
		CurrentTarget = closestTarget;
		IsAutoAimed = true;
		currentTargetApparentTransform = closestApparentTransform;
	}

	private void LateUpdate()
	{
		if (CurrentTarget == null || !IsAutoAimed)
		{
			crosshair.anchoredPosition = Vector2.zero;
			return;
		}
		Vector3 position = currentTargetApparentTransform.MultiplyPoint3x4(CurrentTarget.bounds.center);
		Vector2 a = (Vector2)camera.WorldToViewportPoint(position) - new Vector2(0.5f, 0.5f);
		Vector2 referenceResolution = crosshair.GetParentCanvas().GetComponent<CanvasScaler>().referenceResolution;
		crosshair.anchoredPosition = Vector2.Scale(a, referenceResolution);
	}
}
