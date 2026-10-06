using Interop;
using PrivateAPIBridge;
using UnityEngine;

public static class AudioSourceExtensions
{
	public static void Play(this UnityEngine.AudioSource @this, bool tracked)
	{
		@this.Play();
		if (tracked && MonoSingleton<VirtualAudioManager>.TryGetInstance(out VirtualAudioManager instance))
		{
			instance.AddAudioSource(@this);
		}
	}

	public static void PlayOneShot(this UnityEngine.AudioSource @this, UnityEngine.AudioClip clip, bool tracked)
	{
		@this.PlayOneShot(clip, 1f, tracked);
	}

	public static void PlayOneShot(this UnityEngine.AudioSource @this, UnityEngine.AudioClip clip, float volumeScale, bool tracked)
	{
		@this.PlayOneShot(clip, volumeScale);
		if (tracked && MonoSingleton<VirtualAudioManager>.TryGetInstance(out VirtualAudioManager instance))
		{
			instance.AddAudioSource(@this);
		}
	}

	public static void PlayScheduled(this UnityEngine.AudioSource @this, double time, bool tracked)
	{
		@this.PlayScheduled(time);
		if (tracked && MonoSingleton<VirtualAudioManager>.TryGetInstance(out VirtualAudioManager instance))
		{
			instance.AddAudioSource(@this);
		}
	}

	public static void PlayDelayed(this UnityEngine.AudioSource @this, float delay, bool tracked)
	{
		@this.PlayDelayed(delay);
		if (tracked && MonoSingleton<VirtualAudioManager>.TryGetInstance(out VirtualAudioManager instance))
		{
			instance.AddAudioSource(@this);
		}
	}

	public unsafe static bool IsPaused(this UnityEngine.AudioSource @this)
	{
		return ((Interop.AudioSource*)(void*)@this.GetCachedPtr())->m_pause != 0;
	}

	public static void SetPitch(this UnityEngine.AudioSource @this, float pitch)
	{
		if (@this.TryGetComponent<VirtualAudioFilter>(out var component))
		{
			component.pitch = pitch;
		}
		else
		{
			@this.pitch = pitch;
		}
	}

	public static float GetPitch(this UnityEngine.AudioSource @this)
	{
		if (@this.TryGetComponent<VirtualAudioFilter>(out var component))
		{
			return component.pitch;
		}
		return @this.pitch;
	}

	public static void SetPlayOnAwake(this UnityEngine.AudioSource @this, bool playOnAwake)
	{
		@this.playOnAwake = playOnAwake;
		if (playOnAwake && MonoSingleton<VirtualAudioManager>.TryGetInstance(out VirtualAudioManager instance))
		{
			instance.AddAudioSource(@this);
		}
	}

	public static void SetSpatialBlend(this UnityEngine.AudioSource @this, float spatialBlend)
	{
		if (@this.TryGetComponent<VirtualAudioFilter>(out var component))
		{
			component.spatialBlend = spatialBlend;
		}
		else
		{
			@this.spatialBlend = spatialBlend;
		}
	}

	public static float GetSpatialBlend(this UnityEngine.AudioSource @this)
	{
		if (@this.TryGetComponent<VirtualAudioFilter>(out var component))
		{
			return component.spatialBlend;
		}
		return @this.spatialBlend;
	}
}
