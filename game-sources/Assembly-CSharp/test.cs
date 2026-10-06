using UnityEngine;

public class test : MonoBehaviour
{
	private float FixedUpdatesPerFrame;

	private void FixedUpdate()
	{
		FixedUpdatesPerFrame++;
	}

	private void Update()
	{
		Debug.Log("FixedUpdatesPerFrame: " + FixedUpdatesPerFrame);
		FixedUpdatesPerFrame = 0f;
	}
}
