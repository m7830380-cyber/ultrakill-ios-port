using UnityEngine;
using UnityEngine.Audio;

public static class OneShotAudioExtension
{
	public static AudioSource PlayClipAtPoint(this AudioClip clip, AudioMixerGroup mixGroup, Vector3 position, int priority = 128, float spatialBlend = 0f, float volume = 1f, float pitch = 1f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, float minimumDistance = 1f, float maximumDistance = 100f)
	{
		GameObject gameObject = new GameObject("TempAudio");
		gameObject.transform.position = position;
		AudioSource audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.clip = clip;
		audioSource.outputAudioMixerGroup = mixGroup;
		audioSource.priority = priority;
		audioSource.volume = volume;
		audioSource.SetPitch(pitch);
		audioSource.SetSpatialBlend(spatialBlend);
		audioSource.rolloffMode = rolloffMode;
		audioSource.minDistance = minimumDistance;
		audioSource.maxDistance = maximumDistance;
		audioSource.Play(tracked: true);
		Object.Destroy(gameObject, clip.length / pitch);
		return audioSource;
	}
}
