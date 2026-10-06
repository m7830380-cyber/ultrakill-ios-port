using System.Collections.Generic;
using UnityEngine;

public class ResumeSoundsOnEnable : MonoBehaviour
{
	private sealed class TrackedInfo
	{
		private readonly AudioSource source;

		private AudioClip clip;

		private float time;

		private bool wasActive;

		public TrackedInfo(AudioSource source)
		{
			this.source = source;
			Store();
		}

		public void Update(bool trackTime = false)
		{
			if (source == null)
			{
				return;
			}
			bool activeInHierarchy = source.gameObject.activeInHierarchy;
			if (activeInHierarchy != wasActive && activeInHierarchy && source.clip == clip && !source.isPlaying)
			{
				if (trackTime)
				{
					source.time = time;
				}
				source.Play(tracked: true);
			}
			Store();
		}

		private void Store()
		{
			if (!(source == null))
			{
				if (source.gameObject.activeInHierarchy && source.isPlaying)
				{
					clip = source.clip;
					time = source.time;
				}
				wasActive = source.gameObject.activeInHierarchy;
			}
		}
	}

	private readonly Dictionary<AudioSource, TrackedInfo> trackedInfos = new Dictionary<AudioSource, TrackedInfo>();

	[SerializeField]
	private AudioSource[] audioSources;

	[SerializeField]
	private bool trackTime;

	private void Update()
	{
		for (int i = 0; i < audioSources.Length; i++)
		{
			AudioSource audioSource = audioSources[i];
			if (!trackedInfos.TryGetValue(audioSource, out var value))
			{
				TrackedInfo trackedInfo = (trackedInfos[audioSource] = new TrackedInfo(audioSource));
				value = trackedInfo;
			}
			value.Update(trackTime);
		}
	}
}
