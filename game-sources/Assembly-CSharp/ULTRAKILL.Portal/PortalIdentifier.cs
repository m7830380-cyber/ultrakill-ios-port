using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace ULTRAKILL.Portal;

public class PortalIdentifier : MonoBehaviour
{
	private NavMeshLinkData[] datas = Array.Empty<NavMeshLinkData>();

	private NavMeshLinkInstance[] instances = Array.Empty<NavMeshLinkInstance>();

	private Vector3[] positions = Array.Empty<Vector3>();

	private readonly List<int> validLinkIdx = new List<int>();

	[NonSerialized]
	public bool isTraversable;

	[NonSerialized]
	public bool lastIsTraversable;

	[NonSerialized]
	private PortalHandle handle;

	[NonSerialized]
	private Vector3 lastPos;

	[NonSerialized]
	private Quaternion lastRot;

	[NonSerialized]
	private bool dirty = true;

	private readonly List<NavMeshLinkInstance>[] linksToRemove = new List<NavMeshLinkInstance>[2]
	{
		new List<NavMeshLinkInstance>(),
		new List<NavMeshLinkInstance>()
	};

	private bool isRemoveLinkFromOther;

	private Quaternion updateRot;

	private Vector3 updatePos;

	private float lastUpdateTimer;

	public bool IsValidLinkExist => validLinkIdx.Count > 0;

	public PortalHandle Handle
	{
		get
		{
			return handle;
		}
		set
		{
			handle = value;
		}
	}

	public IEnumerable<PortalNavMeshLink> GetValidLinks()
	{
		return validLinkIdx.Select((int validIndex) => new PortalNavMeshLink(in instances[validIndex], in datas[validIndex], in positions[validIndex]));
	}

	public bool GetClosestLink(Vector3 startPos, out PortalNavMeshLink portalLink)
	{
		if (validLinkIdx.Count < 1)
		{
			portalLink = null;
			return false;
		}
		int num = validLinkIdx[0];
		float num2 = Vector3.Distance(startPos, datas[num].startPosition);
		for (int i = 1; i < validLinkIdx.Count; i++)
		{
			int num3 = validLinkIdx[i];
			NavMeshLinkData navMeshLinkData = datas[num3];
			float num4 = Vector3.Distance(startPos, navMeshLinkData.startPosition);
			if (num4 < num2)
			{
				num2 = num4;
				num = num3;
			}
		}
		portalLink = new PortalNavMeshLink(in instances[num], in datas[num], in positions[num]);
		return true;
	}

	public bool TryGetOther(out PortalIdentifier other)
	{
		other = null;
		PortalManagerV2 instance = MonoSingleton<PortalManagerV2>.Instance;
		if (instance == null || instance.Scene == null || !handle.IsValid())
		{
			return false;
		}
		PortalHandle key = handle.Reverse();
		return MonoSingleton<PortalManagerV2>.Instance.Scene.portalIdentifiersLookup.TryGetValue(key, out other);
	}

	public void ResetDirtiness()
	{
		dirty = false;
	}

	public void SetLinks(NavMeshLinkData[] datas, NavMeshLinkInstance[] instances, Vector3[] positions)
	{
		validLinkIdx.Clear();
		this.datas = datas;
		this.instances = instances;
		this.positions = positions;
		for (int i = 0; i < instances.Length; i++)
		{
			if (instances[i].valid)
			{
				validLinkIdx.Add(i);
			}
		}
	}

	public void RemoveQueuedLinks()
	{
		List<NavMeshLinkInstance> list = (isRemoveLinkFromOther ? linksToRemove[1] : linksToRemove[0]);
		for (int i = 0; i < list.Count; i++)
		{
			list[i].Remove();
		}
		list.Clear();
		isRemoveLinkFromOther = !isRemoveLinkFromOther;
	}

	public void QueueLinksForRemoval()
	{
		if (instances != null)
		{
			(isRemoveLinkFromOther ? linksToRemove[0] : linksToRemove[1]).AddRange(instances);
			validLinkIdx.Clear();
			dirty = true;
		}
	}

	public bool UpdateDirtiness()
	{
		base.transform.GetPositionAndRotation(out var position, out var rotation);
		if (base.transform.hasChanged || position != lastPos || rotation != lastRot || isTraversable != lastIsTraversable)
		{
			dirty = true;
			if (TryGetOther(out var other))
			{
				other.dirty = true;
			}
			base.transform.hasChanged = false;
		}
		lastPos = position;
		lastRot = rotation;
		lastIsTraversable = isTraversable;
		return dirty;
	}

	public bool CheckUpdateDiff()
	{
		lastUpdateTimer += Time.deltaTime;
		if (lastUpdateTimer > 0.1f || Vector3.Distance(updatePos, base.transform.position) > 0.5f || Quaternion.Angle(updateRot, base.transform.rotation) > 0.5f)
		{
			lastUpdateTimer = 0f;
			updateRot = base.transform.rotation;
			updatePos = base.transform.position;
			return true;
		}
		return false;
	}

	private void OnDisable()
	{
		RemoveLinks();
		RemoveCurrentLinks();
		dirty = true;
		ResetData();
	}

	private void RemoveCurrentLinks()
	{
		for (int i = 0; i < instances.Length; i++)
		{
			instances[i].Remove();
		}
	}

	public void RemoveLinks()
	{
		for (int i = 0; i < linksToRemove.Length; i++)
		{
			for (int j = 0; j < linksToRemove[i].Count; j++)
			{
				linksToRemove[i][j].Remove();
			}
		}
	}

	private void ResetData()
	{
		datas = Array.Empty<NavMeshLinkData>();
		instances = Array.Empty<NavMeshLinkInstance>();
		positions = Array.Empty<Vector3>();
		validLinkIdx.Clear();
	}
}
