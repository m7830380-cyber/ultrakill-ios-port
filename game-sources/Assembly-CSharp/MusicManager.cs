using UnityEngine;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
[DefaultExecutionOrder(600)]
public class MusicManager : MonoSingleton<MusicManager>
{
	public bool off;

	public bool dontMatch;

	public bool useBossTheme;

	public AudioSource battleTheme;

	public AudioSource cleanTheme;

	public AudioSource bossTheme;

	public AudioSource targetTheme;

	private AudioSource[] allThemes;

	public float volume = 1f;

	public float requestedThemes;

	private bool arenaMode;

	private float defaultVolume;

	public float fadeSpeed;

	private float fadeOutSpeed;

	public bool forcedOff;

	private bool filtering;

	private bool falseStartToken;

	private void OnEnable()
	{
		if (fadeSpeed == 0f)
		{
			fadeSpeed = 1f;
		}
		allThemes = GetComponentsInChildren<AudioSource>();
		defaultVolume = volume;
		if (!off)
		{
			AudioSource[] array = allThemes;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play(tracked: true);
			}
			cleanTheme.volume = volume;
			targetTheme = cleanTheme;
		}
		else
		{
			targetTheme = GetComponent<AudioSource>();
		}
		if ((bool)MonoSingleton<AudioMixerController>.Instance.musicSound)
		{
			MonoSingleton<AudioMixerController>.Instance.musicSound.FindSnapshot("Unpaused").TransitionTo(0f);
		}
	}

	private void Update()
	{
		if (!off && targetTheme.volume != volume)
		{
			AudioSource[] array = allThemes;
			foreach (AudioSource audioSource in array)
			{
				if (audioSource == targetTheme)
				{
					if (audioSource.volume > volume)
					{
						audioSource.volume = volume;
					}
					if (Time.timeScale == 0f)
					{
						audioSource.volume = volume;
					}
					else
					{
						audioSource.volume = Mathf.MoveTowards(audioSource.volume, volume, fadeSpeed * Time.deltaTime);
					}
				}
				else if (Time.timeScale == 0f)
				{
					audioSource.volume = 0f;
				}
				else
				{
					audioSource.volume = Mathf.MoveTowards(audioSource.volume, 0f, fadeSpeed * Time.deltaTime);
				}
			}
			if (targetTheme.volume == volume)
			{
				array = allThemes;
				foreach (AudioSource audioSource2 in array)
				{
					if (audioSource2 != targetTheme)
					{
						audioSource2.volume = 0f;
					}
				}
			}
		}
		if (filtering)
		{
			MonoSingleton<AudioMixerController>.Instance.musicSound.GetFloat("highPassVolume", out var value);
			value = Mathf.MoveTowards(value, 0f, 1200f * Time.unscaledDeltaTime);
			MonoSingleton<AudioMixerController>.Instance.musicSound.SetFloat("highPassVolume", value);
			if (value == 0f)
			{
				filtering = false;
			}
		}
		if (volume == 0f || off)
		{
			AudioSource[] array = allThemes;
			foreach (AudioSource obj in array)
			{
				obj.volume = Mathf.MoveTowards(obj.volume, 0f, Time.deltaTime / 5f * fadeSpeed * fadeOutSpeed);
			}
		}
	}

	public void ForceStartBattleMusic()
	{
		forcedOff = false;
		ArenaMusicStart();
	}

	public void ForceStartMusic()
	{
		forcedOff = false;
		StartMusic();
	}

	public void StartMusic()
	{
		if (forcedOff || !off)
		{
			return;
		}
		AudioSource[] array = allThemes;
		foreach (AudioSource audioSource in array)
		{
			if (audioSource.clip != null)
			{
				audioSource.Play(tracked: true);
				if (off && audioSource.time != 0f)
				{
					audioSource.time = 0f;
				}
			}
		}
		off = false;
		fadeOutSpeed = 1f;
		if (!arenaMode && requestedThemes <= 0f)
		{
			cleanTheme.volume = volume;
			targetTheme = cleanTheme;
			battleTheme.volume = 0f;
			bossTheme.volume = 0f;
		}
		else
		{
			battleTheme.volume = volume;
			targetTheme = battleTheme;
			cleanTheme.volume = 0f;
			bossTheme.volume = 0f;
		}
	}

	public void PlayBattleMusic()
	{
		if (!dontMatch && targetTheme != battleTheme && cleanTheme.clip != null)
		{
			battleTheme.time = cleanTheme.time;
		}
		if (targetTheme != bossTheme)
		{
			targetTheme = battleTheme;
		}
		if (falseStartToken)
		{
			falseStartToken = false;
		}
		else
		{
			requestedThemes++;
		}
	}

	public void PlayCleanMusic()
	{
		requestedThemes--;
		if (requestedThemes <= 0f && !arenaMode)
		{
			requestedThemes = 0f;
			if (!dontMatch && targetTheme != cleanTheme && battleTheme.clip != null)
			{
				cleanTheme.time = battleTheme.time;
			}
			if (battleTheme.volume == volume)
			{
				cleanTheme.time = battleTheme.time;
			}
			targetTheme = cleanTheme;
		}
	}

	public void PlayBossMusic()
	{
		if (targetTheme != bossTheme)
		{
			bossTheme.time = cleanTheme.time;
		}
		targetTheme = bossTheme;
	}

	public void ArenaMusicStart(bool goIntoArenaMode = true)
	{
		if (forcedOff)
		{
			return;
		}
		if (off)
		{
			AudioSource[] array = allThemes;
			foreach (AudioSource audioSource in array)
			{
				if (audioSource.clip != null)
				{
					audioSource.Play(tracked: true);
					if (off && audioSource.time != 0f)
					{
						audioSource.time = 0f;
					}
				}
			}
			off = false;
			fadeOutSpeed = 1f;
			battleTheme.volume = volume;
			targetTheme = battleTheme;
		}
		if (!battleTheme.isPlaying)
		{
			AudioSource[] array = allThemes;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play(tracked: true);
			}
			battleTheme.volume = volume;
		}
		if (targetTheme != bossTheme)
		{
			targetTheme = battleTheme;
		}
		if (goIntoArenaMode)
		{
			arenaMode = true;
		}
		else if (requestedThemes <= 0f)
		{
			falseStartToken = true;
			requestedThemes++;
		}
	}

	public void ArenaMusicEnd()
	{
		requestedThemes = 0f;
		targetTheme = cleanTheme;
		arenaMode = false;
	}

	public void ForceStopMusic()
	{
		forcedOff = true;
		StopMusic();
	}

	public void StopMusic()
	{
		off = true;
		if (allThemes != null && allThemes.Length != 0)
		{
			AudioSource[] array = allThemes;
			foreach (AudioSource obj in array)
			{
				obj.volume = 0f;
				obj.Stop();
			}
		}
	}

	public void FadeOut(float newFadeoutSpeed)
	{
		off = true;
		fadeOutSpeed = newFadeoutSpeed;
	}

	public void FilterMusic()
	{
		MonoSingleton<AudioMixerController>.Instance.musicSound.SetFloat("highPassVolume", -80f);
		CancelInvoke("RemoveHighPass");
		MonoSingleton<AudioMixerController>.Instance.musicSound.FindSnapshot("Paused").TransitionTo(0f);
		filtering = true;
	}

	public void UnfilterMusic()
	{
		filtering = false;
		MonoSingleton<AudioMixerController>.Instance.musicSound.FindSnapshot("Unpaused").TransitionTo(0.5f);
		Invoke("RemoveHighPass", 0.5f);
	}

	private void RemoveHighPass()
	{
		MonoSingleton<AudioMixerController>.Instance.musicSound.SetFloat("highPassVolume", -80f);
	}

	public bool IsInBattle()
	{
		if (!arenaMode)
		{
			return requestedThemes > 0f;
		}
		return true;
	}

	public void ChangeFadeSpeed(float speed)
	{
		fadeSpeed = speed;
	}
}
