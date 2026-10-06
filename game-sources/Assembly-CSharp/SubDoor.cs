using UnityEngine;

public class SubDoor : MonoBehaviour
{
	public SubDoorType type;

	public Vector3 openPos;

	public Vector3 origPos;

	public Vector3 targetPos;

	public float speed = 1f;

	public bool playerSpeedMultiplier;

	[HideInInspector]
	public bool valuesSet;

	[HideInInspector]
	public bool isOpen;

	[HideInInspector]
	public AudioSource aud;

	private float origPitch;

	public Door dr;

	[HideInInspector]
	public Animator anim;

	public AudioClip[] sounds;

	public AudioClip openSound;

	public AudioClip stopSound;

	public UltrakillEvent[] animationEvents;

	private void Awake()
	{
		SetValues();
	}

	private void Update()
	{
		if (type == SubDoorType.Animation)
		{
			if (!anim)
			{
				return;
			}
			float normalizedTime = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
			if (normalizedTime > 1f)
			{
				anim.Play(0, -1, 1f);
				anim.SetFloat("Speed", 0f);
				if ((bool)aud)
				{
					PlayStopSound();
				}
			}
			else if (normalizedTime < 0f)
			{
				anim.Play(0, -1, 0f);
				anim.SetFloat("Speed", 0f);
				if ((bool)aud)
				{
					PlayStopSound();
				}
			}
		}
		else
		{
			if (!(base.transform.localPosition != targetPos))
			{
				return;
			}
			base.transform.localPosition = Vector3.MoveTowards(base.transform.localPosition, targetPos, Time.deltaTime * (playerSpeedMultiplier ? Mathf.Max(speed, speed * (MonoSingleton<NewMovement>.Instance.rb.velocity.magnitude / 15f)) : speed));
			if (!(base.transform.localPosition == targetPos))
			{
				return;
			}
			if (targetPos == origPos)
			{
				dr?.BigDoorClosed();
			}
			else if ((bool)dr)
			{
				dr.onFullyOpened?.Invoke();
				dr.isFullyOpened = true;
			}
			if ((bool)aud)
			{
				if ((bool)stopSound)
				{
					aud.clip = stopSound;
					aud.SetPitch(origPitch + Random.Range(-0.1f, 0.1f));
					aud.Play(tracked: true);
				}
				else
				{
					aud.Stop();
				}
			}
		}
	}

	public void Open()
	{
		SetValues();
		isOpen = true;
		if ((bool)dr)
		{
			dr.isFullyClosed = false;
		}
		if (type == SubDoorType.Animation)
		{
			if ((bool)aud && anim.GetFloat("Speed") != speed)
			{
				if ((bool)openSound)
				{
					aud.clip = openSound;
				}
				aud.SetPitch(origPitch + Random.Range(-0.1f, 0.1f));
				aud.Play(tracked: true);
			}
			anim.SetFloat("Speed", playerSpeedMultiplier ? Mathf.Max(speed, speed * (MonoSingleton<NewMovement>.Instance.rb.velocity.magnitude / 15f)) : speed);
			return;
		}
		targetPos = origPos + openPos;
		if ((bool)aud && base.transform.localPosition != targetPos)
		{
			if ((bool)openSound)
			{
				aud.clip = openSound;
			}
			aud.SetPitch(origPitch + Random.Range(-0.1f, 0.1f));
			aud.Play(tracked: true);
		}
	}

	public void Close()
	{
		SetValues();
		isOpen = false;
		if ((bool)dr)
		{
			dr.isFullyOpened = false;
		}
		if (type == SubDoorType.Animation)
		{
			if ((bool)aud && anim.GetFloat("Speed") != 0f - speed)
			{
				if ((bool)openSound)
				{
					aud.clip = openSound;
				}
				aud.SetPitch(origPitch + Random.Range(-0.1f, 0.1f));
				aud.Play(tracked: true);
			}
			anim.SetFloat("Speed", 0f - (playerSpeedMultiplier ? Mathf.Max(speed, speed * (MonoSingleton<NewMovement>.Instance.rb.velocity.magnitude / 15f)) : speed));
			return;
		}
		targetPos = origPos;
		if ((bool)aud && base.transform.localPosition != targetPos)
		{
			if ((bool)openSound)
			{
				aud.clip = openSound;
			}
			aud.SetPitch(origPitch + Random.Range(-0.1f, 0.1f));
			aud.Play(tracked: true);
		}
	}

	public void SetValues()
	{
		if (!valuesSet)
		{
			valuesSet = true;
			origPos = base.transform.localPosition;
			targetPos = origPos;
			aud = GetComponent<AudioSource>();
			if ((bool)aud)
			{
				origPitch = aud.GetPitch();
			}
			if (type == SubDoorType.Animation)
			{
				anim = GetComponent<Animator>();
			}
		}
	}

	public void AnimationEvent(int i)
	{
		animationEvents[i].Invoke();
	}

	public void PlaySound(int targetSound)
	{
		if (!(aud.clip == sounds[targetSound]) || !aud.isPlaying)
		{
			aud.clip = sounds[targetSound];
			aud.loop = true;
			aud.Play(tracked: true);
		}
	}

	public void PlayStopSound()
	{
		if ((bool)aud)
		{
			if ((bool)stopSound)
			{
				aud.loop = false;
				aud.clip = stopSound;
				aud.Play(tracked: true);
			}
			else if (aud.isPlaying)
			{
				aud.Stop();
			}
		}
	}
}
