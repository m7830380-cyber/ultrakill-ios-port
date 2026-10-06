using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class VirtualAudioListener : MonoBehaviour
{
	[SerializeField]
	private AudioListenerMode m_ListenerMode;

	[SerializeField]
	private Vector3 m_LastPosition;

	[SerializeField]
	private Vector3 m_Velocity;

	[SerializeField]
	private AudioVelocityUpdateMode m_VelocityUpdateMode;

	private int m_UpdateIndex;

	private readonly List<VirtualAudioOutput> m_Outputs = new List<VirtualAudioOutput>();

	internal int trackedIndex { get; set; } = -1;

	public IReadOnlyCollection<VirtualAudioOutput> outputs => m_Outputs;

	public AudioListenerMode listenerMode
	{
		get
		{
			return m_ListenerMode;
		}
		set
		{
			m_ListenerMode = value;
		}
	}

	public AudioVelocityUpdateMode velocityUpdateMode
	{
		get
		{
			return m_VelocityUpdateMode;
		}
		set
		{
			m_VelocityUpdateMode = value;
		}
	}

	public Vector3 lastPosition => m_LastPosition;

	public Vector3 velocity => m_Velocity;

	public void AddOutput(VirtualAudioOutput output)
	{
		if (!m_Outputs.Contains(output))
		{
			m_Outputs.Add(output);
		}
	}

	public void UpdateCachedValues(int updateIndex)
	{
		if (m_UpdateIndex != updateIndex)
		{
			m_UpdateIndex = updateIndex;
			UpdateCachedValuesCore();
		}
	}

	protected virtual void UpdateCachedValuesCore()
	{
	}

	public bool RemoveOutput(VirtualAudioOutput output)
	{
		return m_Outputs.Remove(output);
	}

	public virtual Vector3 GetInputPosition(Vector3 position)
	{
		return position;
	}

	private void Update()
	{
		if (m_VelocityUpdateMode == AudioVelocityUpdateMode.Auto)
		{
			m_VelocityUpdateMode = VirtualAudioManager.GetAutomaticUpdateMode(base.gameObject);
		}
		if (m_VelocityUpdateMode == AudioVelocityUpdateMode.Dynamic)
		{
			DoUpdate();
		}
	}

	private void FixedUpdate()
	{
		if (m_VelocityUpdateMode == AudioVelocityUpdateMode.Auto)
		{
			m_VelocityUpdateMode = VirtualAudioManager.GetAutomaticUpdateMode(base.gameObject);
		}
		if (m_VelocityUpdateMode == AudioVelocityUpdateMode.Fixed)
		{
			DoUpdate();
		}
	}

	private void DoUpdate()
	{
		Vector3 position = base.transform.position;
		float num = ((Time.deltaTime > 0f) ? Time.deltaTime : 1f);
		m_Velocity = (m_LastPosition - position) / num;
		m_LastPosition = position;
	}

	private void OnEnable()
	{
		m_LastPosition = base.transform.position;
		m_Velocity = Vector3.zero;
		MonoSingleton<VirtualAudioManager>.Instance.AddAudioListener(this);
	}
}
