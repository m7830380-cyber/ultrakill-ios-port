using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CopyColor : MonoBehaviour
{
	private Image img;

	public Image target;

	public TMP_Text textTarget;

	private void Start()
	{
		img = GetComponent<Image>();
	}

	private void Update()
	{
		if ((bool)img)
		{
			if ((bool)target)
			{
				img.color = target.color;
			}
			else if ((bool)textTarget)
			{
				img.color = textTarget.color;
			}
		}
	}
}
