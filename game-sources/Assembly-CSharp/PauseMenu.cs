using TMPro;
using UnityEngine;
using UnityEngine.UI;
using plog;

public class PauseMenu : MonoBehaviour
{
	private static readonly plog.Logger Log = new plog.Logger("PauseMenu");

	[SerializeField]
	private Button checkpointButton;

	[SerializeField]
	private TMP_Text checkpointText;

	private bool nonStandardCheckpointButton;

	private void OnEnable()
	{
		MapInfoBase instance = MapInfoBase.Instance;
		if (instance == null)
		{
			checkpointButton.interactable = false;
			Log.Warning("MapInfoBase.Instance is null");
		}
		else if (instance.replaceCheckpointButtonWithSkip)
		{
			if (!nonStandardCheckpointButton)
			{
				if (StockMapInfo.Instance != null && !SceneHelper.IsPlayingCustom)
				{
					checkpointText.text = "SKIP";
					checkpointButton.interactable = true;
					checkpointButton.onClick.RemoveAllListeners();
					checkpointButton.onClick.AddListener(OnCheckpointButton);
				}
				else
				{
					checkpointText.text = "NOT IMPLEMENTED";
					checkpointButton.interactable = false;
					Log.Warning("StockMapInfo is null or SceneHelper.IsPlayingCustom is true");
				}
				nonStandardCheckpointButton = true;
			}
		}
		else
		{
			bool interactable = MonoSingleton<StatsManager>.Instance.currentCheckPoint != null;
			checkpointButton.interactable = interactable;
		}
	}

	public void OnCheckpointButton()
	{
		StockMapInfo instance = StockMapInfo.Instance;
		if (!(instance == null))
		{
			string nextSceneName = instance.nextSceneName;
			if (!string.IsNullOrEmpty(nextSceneName))
			{
				MonoSingleton<OptionsMenuToManager>.Instance.ChangeLevel(nextSceneName);
			}
		}
	}
}
