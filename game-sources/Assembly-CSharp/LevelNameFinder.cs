using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelNameFinder : MonoBehaviour
{
	public string textBeforeName;

	public bool breakLine;

	private int thisLevelNumber;

	public int otherLevelNumber;

	private Text txt;

	private TMP_Text txt2;

	public bool lookForPreviousMission;

	public bool lookForLatestMission;

	private void OnEnable()
	{
		if (lookForPreviousMission || lookForLatestMission)
		{
			bool flag = false;
			if (lookForPreviousMission)
			{
				PreviousMissionSaver instance = MonoSingleton<PreviousMissionSaver>.Instance;
				if (instance != null)
				{
					flag = true;
					otherLevelNumber = instance.previousMission;
				}
			}
			if (!flag && lookForLatestMission)
			{
				otherLevelNumber = GameProgressSaver.GetProgress(MonoSingleton<PrefsManager>.Instance.GetInt("difficulty"));
			}
		}
		string text = "";
		if (otherLevelNumber != 0)
		{
			text = textBeforeName + (breakLine ? "\n" : "") + GetMissionName.GetMission(otherLevelNumber);
		}
		else
		{
			if (thisLevelNumber == 0)
			{
				thisLevelNumber = (MonoSingleton<StatsManager>.Instance ? MonoSingleton<StatsManager>.Instance.levelNumber : (-1));
			}
			text = textBeforeName + (breakLine ? "\n" : "") + GetMissionName.GetMission(thisLevelNumber);
		}
		if (!txt2)
		{
			txt2 = GetComponent<TMP_Text>();
		}
		if ((bool)txt2)
		{
			txt2.text = text;
			return;
		}
		if (!txt)
		{
			txt = GetComponent<Text>();
		}
		if ((bool)txt)
		{
			txt.text = text;
		}
	}
}
