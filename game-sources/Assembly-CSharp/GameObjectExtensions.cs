using System;
using Interop;
using Interop.Unity;
using Interop.core;
using PrivateAPIBridge;
using UnityEngine;

public static class GameObjectExtensions
{
	public static T GetOrAddComponent<T>(this UnityEngine.GameObject gameObject) where T : UnityEngine.Component
	{
		if (gameObject.TryGetComponent<T>(out var component))
		{
			return component;
		}
		return gameObject.AddComponent<T>();
	}

	public static UnityEngine.Component GetOrAddComponent(this UnityEngine.GameObject gameObject, System.Type componentType)
	{
		if (gameObject.TryGetComponent(componentType, out var component))
		{
			return component;
		}
		return gameObject.AddComponent(componentType);
	}

	public static T GetOrAddComponent<T>(this UnityEngine.GameObject gameObject, out bool exists) where T : UnityEngine.Component
	{
		if (gameObject.TryGetComponent<T>(out var component))
		{
			exists = true;
			return component;
		}
		exists = false;
		return gameObject.AddComponent<T>();
	}

	public static UnityEngine.Component GetOrAddComponent(this UnityEngine.GameObject gameObject, System.Type componentType, out bool exists)
	{
		if (gameObject.TryGetComponent(componentType, out var component))
		{
			exists = true;
			return component;
		}
		exists = false;
		return gameObject.AddComponent(componentType);
	}

	public static UnityEngine.GameObject CreateChild(this UnityEngine.GameObject parent)
	{
		UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
		gameObject.transform.SetParent(parent.transform, worldPositionStays: false);
		return gameObject;
	}

	public static UnityEngine.GameObject CreateChild(this UnityEngine.GameObject parent, string name)
	{
		UnityEngine.GameObject gameObject = new UnityEngine.GameObject(name);
		gameObject.transform.SetParent(parent.transform, worldPositionStays: false);
		return gameObject;
	}

	public static T CreateChild<T>(this UnityEngine.GameObject parent) where T : UnityEngine.Component
	{
		return parent.CreateChild().GetOrAddComponent<T>();
	}

	public static T CreateChild<T>(this UnityEngine.GameObject parent, string name) where T : UnityEngine.Component
	{
		return parent.CreateChild(name).GetOrAddComponent<T>();
	}

	public static int GetFirstAudioFilterIndex(this UnityEngine.GameObject @this)
	{
		int i = 1;
		for (int componentCount = @this.GetComponentCount(); i < componentCount; i++)
		{
			UnityEngine.Component componentAtIndex = @this.GetComponentAtIndex(i);
			if (componentAtIndex is AudioChorusFilter || componentAtIndex is AudioDistortionFilter || componentAtIndex is AudioEchoFilter || componentAtIndex is AudioHighPassFilter || componentAtIndex is AudioLowPassFilter || componentAtIndex is AudioReverbFilter)
			{
				return i;
			}
		}
		return -1;
	}

	public static int GetLastAudioFilterIndex(this UnityEngine.GameObject @this)
	{
		for (int num = @this.GetComponentCount() - 1; num >= 1; num--)
		{
			UnityEngine.Component componentAtIndex = @this.GetComponentAtIndex(num);
			if (componentAtIndex is AudioChorusFilter || componentAtIndex is AudioDistortionFilter || componentAtIndex is AudioEchoFilter || componentAtIndex is AudioHighPassFilter || componentAtIndex is AudioLowPassFilter || componentAtIndex is AudioReverbFilter)
			{
				return num;
			}
		}
		return -1;
	}

	public unsafe static void SwapComponents(this UnityEngine.GameObject @this, int index1, int index2)
	{
		vector<Interop.GameObject.ComponentPair>* ptr = &((Interop.GameObject*)(void*)@this.GetCachedPtr())->m_Component;
		if (index1 < 1 || index1 >= (int)ptr->m_size)
		{
			throw new ArgumentOutOfRangeException("index1");
		}
		if (index2 < 1 || index2 >= (int)ptr->m_size)
		{
			throw new ArgumentOutOfRangeException("index2");
		}
		ref Interop.GameObject.ComponentPair reference = ref ptr->m_ptr[index2];
		Interop.GameObject.ComponentPair* num = ptr->m_ptr + index1;
		Interop.GameObject.ComponentPair componentPair = ptr->m_ptr[index1];
		Interop.GameObject.ComponentPair componentPair2 = ptr->m_ptr[index2];
		reference = componentPair;
		*num = componentPair2;
		UnityEngine.Object scriptingObject = GetScriptingObject((Interop.Unity.Component*)ptr->m_ptr[index1].component.m_Ptr);
		UnityEngine.Object scriptingObject2 = GetScriptingObject((Interop.Unity.Component*)ptr->m_ptr[index2].component.m_Ptr);
		if ((bool)scriptingObject && scriptingObject is UnityEngine.Behaviour { enabled: not false } behaviour)
		{
			behaviour.enabled = false;
			behaviour.enabled = true;
		}
		if ((bool)scriptingObject2 && scriptingObject2 is UnityEngine.Behaviour { enabled: not false } behaviour2)
		{
			behaviour2.enabled = false;
			behaviour2.enabled = true;
		}
	}

	private unsafe static UnityEngine.Object GetScriptingObject<T>(T* pointer) where T : unmanaged, Interop.Object.Interface
	{
		if (pointer != null)
		{
			return Resources.InstanceIDToObject(CastOperations.UpCasts.static_cast<T, Interop.Object>(pointer)->m_InstanceID);
		}
		return null;
	}
}
