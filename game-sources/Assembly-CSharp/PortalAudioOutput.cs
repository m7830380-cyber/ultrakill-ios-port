using System.Runtime.CompilerServices;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Native;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PortalIdentifier))]
public sealed class PortalAudioOutput : VirtualAudioOutput
{
	private PortalManagerV2 _portalManager;

	private PortalIdentifier _identifier;

	private Matrix4x4 _travelMatrix;

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
			float4x4 source = nativePortal.travelMatrix;
			_travelMatrix = Unsafe.As<float4x4, Matrix4x4>(ref source);
		}
	}

	public override Vector3 GetOutputPosition(AudioListener mainListener, VirtualAudioListener listener, Vector3 position)
	{
		return _travelMatrix.MultiplyPoint3x4(position);
	}
}
