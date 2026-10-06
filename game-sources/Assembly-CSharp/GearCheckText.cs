using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearCheckText : MonoBehaviour
{
	public string gearName;

	private Text target;

	private TMP_Text target2;

	private string originalName;

	private void OnEnable()
	{
		if (!target && !target2)
		{
			target = GetComponent<Text>();
			if ((bool)target)
			{
				originalName = target.text;
			}
			else
			{
				target2 = GetComponent<TMP_Text>();
				if ((bool)target2)
				{
					originalName = target2.text;
				}
			}
		}
		if (GameProgressSaver.CheckGear(gearName) == 0)
		{
			if ((bool)target)
			{
				target.text = "???";
			}
			else
			{
				target2.text = "???";
			}
		}
		else if ((bool)target)
		{
			target.text = originalName;
		}
		else
		{
			target2.text = originalName;
		}
	}
}
