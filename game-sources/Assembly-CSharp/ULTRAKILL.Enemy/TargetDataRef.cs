using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ULTRAKILL.Portal;
using Unity.Mathematics;
using UnityEngine;

namespace ULTRAKILL.Enemy;

public readonly ref struct TargetDataRef
{
	private readonly Span<TargetDataArrays> _arr;

	private readonly Span<float4x4> _matrix;

	private readonly Span<PortalHandleSequence> _portalHandleSequence;

	public readonly PortalScene scene;

	public readonly ITarget target;

	public int Index => _arr.Length;

	public ref Matrix4x4 portalMatrix => ref Unsafe.As<float4x4, Matrix4x4>(ref MemoryMarshal.GetReference(_matrix));

	public ref PortalHandleSequence portals => ref MemoryMarshal.GetReference(_portalHandleSequence);

	public bool isSequenceCulled => _portalHandleSequence.Length == 0;

	public bool isAcrossPortals => portals.Count > 0;

	public ref Vector3 position => ref Unsafe.As<float3, Vector3>(ref NativeArrayExtensions.ItemRefUnsafe(in MemoryMarshal.GetReference(_arr).positions, _arr.Length));

	public ref Vector3 headPosition => ref Unsafe.As<float3, Vector3>(ref NativeArrayExtensions.ItemRefUnsafe(in MemoryMarshal.GetReference(_arr).headPositions, _arr.Length));

	public ref Vector3 velocity => ref Unsafe.As<float3, Vector3>(ref NativeArrayExtensions.ItemRefUnsafe(in MemoryMarshal.GetReference(_arr).velocities, _arr.Length));

	public TargetDataRef(PortalScene scene, ITarget target, Span<float4x4> matrix, Span<PortalHandleSequence> sequence, Span<TargetDataArrays> arr)
	{
		_arr = arr;
		_matrix = matrix;
		_portalHandleSequence = sequence;
		this.scene = scene;
		this.target = target;
	}

	public TargetHandle CreateHandle()
	{
		return new TargetHandle(target, portals);
	}
}
