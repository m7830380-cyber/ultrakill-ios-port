using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
[DisallowMultipleComponent]
public sealed class VirtualAudioFilter : MonoBehaviour
{
	public struct FilterData
	{
		public float gainL;

		public float gainR;

		public float weight;

		public AudioRolloffMode rolloffMode;

		public NativeCurve customRolloffCurve;

		public int consumedCounter;

		internal float maxDistance;

		internal float minDistance;

		internal float spatialBlend;

		internal float closestDistance;

		internal float3 closestPosition;
	}

	internal delegate bool UpdateVelocityBurst_00002247_0024PostfixBurstDelegate(ref float3 lastPosition, ref float _dopplerPitch, float sourceDopplerLevel, in float3 currentPosition, in float3 rbVelocity, in float3 closestPosition, in float3 lastListenerPosition, in float3 listenerVelocity, float pitch, float dopplerFactor, float deltaTime, float spatialBlend, bool hasRigidBody, out float sourcePitch);

	internal static class UpdateVelocityBurst_00002247_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(UpdateVelocityBurst_00002247_0024PostfixBurstDelegate).TypeHandle);
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

		static UpdateVelocityBurst_00002247_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static bool Invoke(ref float3 lastPosition, ref float _dopplerPitch, float sourceDopplerLevel, in float3 currentPosition, in float3 rbVelocity, in float3 closestPosition, in float3 lastListenerPosition, in float3 listenerVelocity, float pitch, float dopplerFactor, float deltaTime, float spatialBlend, bool hasRigidBody, out float sourcePitch)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					return ((delegate* unmanaged[Cdecl]<ref float3, ref float, float, ref float3, ref float3, ref float3, ref float3, ref float3, float, float, float, float, bool, ref float, bool>)functionPointer)(ref lastPosition, ref _dopplerPitch, sourceDopplerLevel, ref currentPosition, ref rbVelocity, ref closestPosition, ref lastListenerPosition, ref listenerVelocity, pitch, dopplerFactor, deltaTime, spatialBlend, hasRigidBody, ref sourcePitch);
				}
			}
			return UpdateVelocityBurst_0024BurstManaged(ref lastPosition, ref _dopplerPitch, sourceDopplerLevel, in currentPosition, in rbVelocity, in closestPosition, in lastListenerPosition, in listenerVelocity, pitch, dopplerFactor, deltaTime, spatialBlend, hasRigidBody, out sourcePitch);
		}
	}

	internal delegate void AddOutputBurst_0000224C_0024PostfixBurstDelegate(ref FilterData filter, in float3 listenerPosition, in float3 listenerRight, in float3 trackedPos, float initialDistance, float spatialBlend);

	internal static class AddOutputBurst_0000224C_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(AddOutputBurst_0000224C_0024PostfixBurstDelegate).TypeHandle);
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

		static AddOutputBurst_0000224C_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(ref FilterData filter, in float3 listenerPosition, in float3 listenerRight, in float3 trackedPos, float initialDistance, float spatialBlend)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref FilterData, ref float3, ref float3, ref float3, float, float, void>)functionPointer)(ref filter, ref listenerPosition, ref listenerRight, ref trackedPos, initialDistance, spatialBlend);
					return;
				}
			}
			AddOutputBurst_0024BurstManaged(ref filter, in listenerPosition, in listenerRight, in trackedPos, initialDistance, spatialBlend);
		}
	}

	internal unsafe delegate void ProcessStereo_0000224F_0024PostfixBurstDelegate(float* data, int sampleCount, float gainL, float gainR);

	internal static class ProcessStereo_0000224F_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(ProcessStereo_0000224F_0024PostfixBurstDelegate).TypeHandle);
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

		static ProcessStereo_0000224F_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(float* data, int sampleCount, float gainL, float gainR)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<float*, int, float, float, void>)functionPointer)(data, sampleCount, gainL, gainR);
					return;
				}
			}
			ProcessStereo_0024BurstManaged(data, sampleCount, gainL, gainR);
		}
	}

	private const float SpeedOfSound = 340f;

	private const float InvSpeedOfSound = 0.0029411765f;

	private AudioSource _source;

	private ulong _gain;

	[SerializeField]
	[HideInInspector]
	private bool _ranAwake;

	[Range(-3f, 3f)]
	[SerializeField]
	private float _pitch = 1f;

	[Range(0f, 1f)]
	[SerializeField]
	private float _spatialBlend;

	private float _dopplerPitch = 1f;

	private Vector3 _lastPosition;

	private int _updateIndex;

	private Rigidbody _rigidBody;

	private bool _hasRigidBody;

	internal NativeCurve customRolloffCurve;

	public FilterData filterData;

	internal int trackedIndex { get; set; } = -1;

	public AudioSource source
	{
		get
		{
			if (this != null && (object)_source == null)
			{
				_source = GetComponent<AudioSource>();
			}
			return _source;
		}
	}

	public float spatialBlend
	{
		get
		{
			if (!base.enabled)
			{
				return source.spatialBlend;
			}
			return _spatialBlend;
		}
		set
		{
			_spatialBlend = Mathf.Clamp01(value);
			if (base.enabled)
			{
				source.spatialBlend = value;
			}
		}
	}

	public float pitch
	{
		get
		{
			if (!base.enabled)
			{
				return source.pitch;
			}
			return _pitch;
		}
		set
		{
			if (float.IsInfinity(value))
			{
				Debug.LogError("Attempt to set pitch to infinite value ignored!", this);
				return;
			}
			if (float.IsNaN(value))
			{
				Debug.LogError("Attempt to set pitch to NaN value ignored!", this);
				return;
			}
			_pitch = value;
			source.pitch = (base.enabled ? (value * _dopplerPitch) : value);
		}
	}

	public void UpdateCachedValues(int updateIndex)
	{
		if (_updateIndex != updateIndex)
		{
			_updateIndex = updateIndex;
			if (_rigidBody == null)
			{
				_hasRigidBody = TryGetComponent<Rigidbody>(out _rigidBody);
			}
		}
	}

	private void Awake()
	{
		_source = GetComponent<AudioSource>();
		if (_ranAwake)
		{
			ResetAudioSource();
			MonoSingleton<VirtualAudioManager>.Instance.AddAudioSource(this);
		}
		_ranAwake = true;
	}

	private void OnEnable()
	{
		if (_source == null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		_pitch = _source.pitch;
		_spatialBlend = _source.spatialBlend;
		_lastPosition = _source.transform.position;
		_source.spatialBlend = 0f;
		_source.bypassEffects = false;
		UpdateFilterData();
	}

	private void OnDisable()
	{
		Volatile.Write(ref _gain, 0uL);
		ResetAudioSource();
	}

	private void ResetAudioSource()
	{
		if (_source != null)
		{
			_source.spatialBlend = _spatialBlend;
			_source.pitch = _pitch;
			UpdateFilterData();
		}
	}

	internal void UpdateVelocity(ref Vector3 lastListenerPosition, ref Vector3 listenerVelocity, float dopplerFactor, float deltaTime)
	{
		Vector3 position = base.transform.position;
		Vector3 vector = (_hasRigidBody ? _rigidBody.velocity : Vector3.zero);
		if (UpdateVelocityBurst(ref Unsafe.As<Vector3, float3>(ref _lastPosition), ref _dopplerPitch, _source.dopplerLevel, in Unsafe.As<Vector3, float3>(ref position), in Unsafe.As<Vector3, float3>(ref vector), in filterData.closestPosition, in Unsafe.As<Vector3, float3>(ref lastListenerPosition), in Unsafe.As<Vector3, float3>(ref listenerVelocity), _pitch, dopplerFactor, deltaTime, _spatialBlend, _hasRigidBody, out var sourcePitch) && base.enabled)
		{
			_source.pitch = sourcePitch;
		}
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	internal static bool UpdateVelocityBurst(ref float3 lastPosition, ref float _dopplerPitch, float sourceDopplerLevel, in float3 currentPosition, in float3 rbVelocity, in float3 closestPosition, in float3 lastListenerPosition, in float3 listenerVelocity, float pitch, float dopplerFactor, float deltaTime, float spatialBlend, bool hasRigidBody, out float sourcePitch)
	{
		return UpdateVelocityBurst_00002247_0024BurstDirectCall.Invoke(ref lastPosition, ref _dopplerPitch, sourceDopplerLevel, in currentPosition, in rbVelocity, in closestPosition, in lastListenerPosition, in listenerVelocity, pitch, dopplerFactor, deltaTime, spatialBlend, hasRigidBody, out sourcePitch);
	}

	private static float3 BurstGetPositionDelta(in float3 currentPosition, in float3 lastPosition, in float deltaTime)
	{
		float num = ((deltaTime > 0f) ? deltaTime : 1f);
		return (lastPosition - currentPosition) / num;
	}

	internal void UpdateFilterData()
	{
		filterData.gainL = 0f;
		filterData.gainR = 0f;
		filterData.weight = 0f;
		filterData.rolloffMode = _source.rolloffMode;
		if (filterData.rolloffMode == AudioRolloffMode.Custom)
		{
			filterData.customRolloffCurve.Update(_source.GetCustomCurve(AudioSourceCurveType.CustomRolloff), 32);
		}
		filterData.consumedCounter = 0;
		filterData.maxDistance = _source.maxDistance;
		filterData.minDistance = _source.minDistance;
		filterData.spatialBlend = _spatialBlend;
		filterData.closestDistance = float.PositiveInfinity;
	}

	internal void AddOutput(Vector3 listenerPosition, Vector3 listenerRight, Vector3 position, float initialDistance)
	{
		AddOutput(listenerPosition, listenerRight, position, initialDistance, filterData.spatialBlend);
	}

	internal void AddOutput(Vector3 listenerPosition, Vector3 listenerRight, Vector3 position, float initialDistance, float spatialBlend)
	{
		if (filterData.spatialBlend == 0f)
		{
			filterData.weight = 1f;
			filterData.gainL = 0.5f;
			filterData.gainR = 0.5f;
		}
		else
		{
			AddOutputBurst(ref filterData, in Unsafe.As<Vector3, float3>(ref listenerPosition), in Unsafe.As<Vector3, float3>(ref listenerRight), in Unsafe.As<Vector3, float3>(ref position), initialDistance, spatialBlend);
		}
	}

	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	private static void AddOutputBurst(ref FilterData filter, in float3 listenerPosition, in float3 listenerRight, in float3 trackedPos, float initialDistance, float spatialBlend)
	{
		AddOutputBurst_0000224C_0024BurstDirectCall.Invoke(ref filter, in listenerPosition, in listenerRight, in trackedPos, initialDistance, spatialBlend);
	}

	internal void EndUpdate()
	{
		if (filterData.weight > 1f)
		{
			filterData.gainL /= filterData.weight;
			filterData.gainR /= filterData.weight;
		}
		float num = Mathf.InverseLerp(0f, 0.01f, Mathf.Abs(_pitch * _dopplerPitch));
		filterData.gainL *= num;
		filterData.gainR *= num;
		Vector2 vector = Vector2.ClampMagnitude(new Vector2(filterData.gainL, filterData.gainR), 1f);
		if (float.IsFinite(vector.x) && float.IsFinite(vector.y))
		{
			Volatile.Write(ref _gain, (uint)BitConverter.SingleToInt32Bits(vector.x) | ((ulong)(uint)BitConverter.SingleToInt32Bits(vector.y) << 32));
		}
	}

	private unsafe void OnAudioFilterRead(float[] data, int channels)
	{
		if (channels == 2 && _spatialBlend != 0f)
		{
			fixed (float* data2 = data)
			{
				ulong num = Volatile.Read(ref _gain);
				float gainL = BitConverter.Int32BitsToSingle((int)num);
				float gainR = BitConverter.Int32BitsToSingle((int)(num >> 32));
				ProcessStereo(data2, data.Length / 2, gainL, gainR);
			}
		}
	}

	[BurstCompile]
	private unsafe static void ProcessStereo(float* data, int sampleCount, float gainL, float gainR)
	{
		ProcessStereo_0000224F_0024BurstDirectCall.Invoke(data, sampleCount, gainL, gainR);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	internal static bool UpdateVelocityBurst_0024BurstManaged(ref float3 lastPosition, ref float _dopplerPitch, float sourceDopplerLevel, in float3 currentPosition, in float3 rbVelocity, in float3 closestPosition, in float3 lastListenerPosition, in float3 listenerVelocity, float pitch, float dopplerFactor, float deltaTime, float spatialBlend, bool hasRigidBody, out float sourcePitch)
	{
		float3 currentPosition2 = (lastPosition = currentPosition);
		if (math.lengthsq(lastPosition - currentPosition) > 10000f)
		{
			sourcePitch = 0f;
			return false;
		}
		float num = 1f;
		float3 obj = (hasRigidBody ? rbVelocity : BurstGetPositionDelta(in currentPosition2, in lastPosition, in deltaTime));
		float3 float5 = closestPosition - lastListenerPosition;
		float3 x = obj - listenerVelocity;
		if (sourceDopplerLevel > 0f && spatialBlend > 0f)
		{
			float num2 = dopplerFactor * sourceDopplerLevel;
			float num3 = math.length(float5);
			float num4 = ((num3 > 0f) ? (math.dot(x, float5) / num3) : 0f);
			num = math.max(1E-06f, (340f - num4 * num2) * 0.0029411765f) * spatialBlend + (1f - spatialBlend);
		}
		if (math.isinf(num) || math.isnan(num) || math.abs(num) <= 0.1f)
		{
			sourcePitch = 0f;
			return false;
		}
		if (math.abs(_dopplerPitch - num) > 2f)
		{
			float num5 = 2f * deltaTime;
			if (math.abs(num - _dopplerPitch) <= num5)
			{
				_dopplerPitch += math.sign(num - _dopplerPitch) * num5;
			}
		}
		else
		{
			_dopplerPitch = num;
		}
		sourcePitch = pitch * _dopplerPitch;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast)]
	internal static void AddOutputBurst_0024BurstManaged(ref FilterData filter, in float3 listenerPosition, in float3 listenerRight, in float3 trackedPos, float initialDistance, float spatialBlend)
	{
		float3 x = trackedPos - listenerPosition;
		float num = math.length(x);
		float num2 = initialDistance + num;
		if (num2 < filter.closestDistance)
		{
			filter.closestDistance = num2;
			filter.closestPosition = trackedPos;
		}
		float3 x2 = math.normalize(x);
		float end = ((filter.rolloffMode == AudioRolloffMode.Linear) ? math.saturate(math.unlerp(filter.maxDistance, filter.minDistance, num2)) : ((filter.rolloffMode != AudioRolloffMode.Logarithmic) ? ((filter.maxDistance > 0f) ? filter.customRolloffCurve.Evaluate(num2 / filter.maxDistance) : 1f) : (filter.minDistance / math.max(num2, 1E-06f))));
		end = math.lerp(1f, end, spatialBlend);
		float num3 = math.dot(x2, listenerRight);
		float num4 = end * math.sqrt(0.5f * (1f - num3));
		float num5 = end * math.sqrt(0.5f * (1f + num3));
		if (math.isfinite(num4 + num5))
		{
			filter.gainL += num4;
			filter.gainR += num5;
			filter.weight += end;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	internal unsafe static void ProcessStereo_0024BurstManaged(float* data, int sampleCount, float gainL, float gainR)
	{
		for (int i = 0; i < sampleCount; i++)
		{
			float num = data[2 * i] + data[2 * i + 1];
			data[2 * i] = num * gainL;
			data[2 * i + 1] = num * gainR;
		}
	}
}
