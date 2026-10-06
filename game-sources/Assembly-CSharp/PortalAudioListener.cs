using System.Runtime.CompilerServices;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Geometry;
using ULTRAKILL.Portal.Native;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PortalIdentifier))]
public sealed class PortalAudioListener : VirtualAudioListener
{
	private PortalManagerV2 _portalManager;

	private PortalIdentifier _identifier;

	private float _width;

	private float _height;

	private float3 _center;

	private float3 _right;

	private float3 _up;

	private float3 _forward;

	private PortalTransform _transform;

	private void Awake()
	{
		_portalManager = MonoSingleton<PortalManagerV2>.Instance;
		_identifier = GetComponent<PortalIdentifier>();
	}

	protected override void UpdateCachedValuesCore()
	{
		if (!_identifier)
		{
			_identifier = GetComponent<PortalIdentifier>();
		}
		NativePortalScene nativeScene = _portalManager.Scene.nativeScene;
		NativePortal nativePortal = nativeScene.LookupPortal(_identifier.Handle);
		if (nativePortal.valid)
		{
			float4x4 toWorld = nativePortal.transform.toWorld;
			_width = nativePortal.dimensions.x;
			_height = nativePortal.dimensions.y;
			_center = toWorld.c3.xyz;
			_forward = toWorld.c2.xyz;
			_up = toWorld.c1.xyz;
			_right = toWorld.c0.xyz;
		}
	}

	public override Vector3 GetInputPosition(Vector3 position)
	{
		PlaneShapeExtensions.GetClosestPoint(_width, _height, in _center, in _right, in _up, in _forward, in Unsafe.As<Vector3, float3>(ref position), out var closest);
		return Unsafe.As<float3, Vector3>(ref closest);
	}
}
