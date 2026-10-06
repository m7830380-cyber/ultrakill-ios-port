using System.Collections;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LevelEndLeaderboard : MonoBehaviour
{
	[SerializeField]
	private GameObject template;

	[SerializeField]
	private TMP_Text templateUsername;

	[SerializeField]
	private TMP_Text templateTime;

	[SerializeField]
	private TMP_Text templateDifficulty;

	[SerializeField]
	private GameObject[] templateHighlight;

	[Space]
	[SerializeField]
	private Transform container;

	[SerializeField]
	private TMP_Text leaderboardType;

	[SerializeField]
	private TMP_Text switchTypeInput;

	[Space]
	[SerializeField]
	private ScrollRect scrollRect;

	[SerializeField]
	private float controllerScrollSpeed = 250f;

	[Space]
	[SerializeField]
	private GameObject loadingPanel;

	private bool displayPRank;

	private int ownEntryIndex = -1;

	private InputControlScheme keyboardControlScheme;

	private const string LeftBracket = "<color=white>[";

	private const string RightBracket = "]</color>";

	private void Start()
	{
		keyboardControlScheme = MonoSingleton<InputManager>.Instance.InputSource.Actions.KeyboardMouseScheme;
	}

	private void OnEnable()
	{
		if (!SceneHelper.CurrentScene.StartsWith("Level ") || !LeaderboardController.ShowLevelEndLeaderboards || MonoSingleton<StatsManager>.Instance.firstPlayThrough)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		Debug.Log("Fetching level leaderboards for " + SceneHelper.CurrentScene);
		displayPRank = MonoSingleton<StatsManager>.Instance.rankScore == 12;
		StartCoroutine(Fetch(SceneHelper.CurrentScene));
	}

	private IEnumerator Fetch(string levelName)
	{
		if (string.IsNullOrEmpty(levelName))
		{
			yield break;
		}
		ResetEntries();
		loadingPanel.SetActive(value: true);
		scrollRect.gameObject.SetActive(value: false);
		leaderboardType.text = (displayPRank ? "P RANK" : "ANY RANK");
		Task<LeaderboardEntry[]> entryTask = MonoSingleton<LeaderboardController>.Instance.GetLevelScores(levelName, displayPRank);
		while (!entryTask.IsCompleted)
		{
			yield return null;
		}
		if (entryTask.Result == null)
		{
			yield break;
		}
		LeaderboardEntry[] result = entryTask.Result;
		int entryCount = 0;
		LeaderboardEntry[] array = result;
		for (int i = 0; i < array.Length; i++)
		{
			LeaderboardEntry leaderboardEntry = array[i];
			TMP_Text tMP_Text = templateUsername;
			Friend user = leaderboardEntry.User;
			tMP_Text.text = user.Name;
			int score = leaderboardEntry.Score;
			int num = score / 60000;
			float num2 = (float)(score - num * 60000) / 1000f;
			templateTime.text = $"{num}:{num2:00.000}";
			int? num3 = null;
			if (leaderboardEntry.Details.Length != 0)
			{
				num3 = leaderboardEntry.Details[0];
			}
			if (LeaderboardProperties.Difficulties.Length <= num3)
			{
				Debug.LogWarning($"Difficulty {num3} is out of range for {levelName}");
				continue;
			}
			templateDifficulty.text = ((!num3.HasValue) ? "UNKNOWN" : LeaderboardProperties.Difficulties[num3.Value].ToUpper());
			GameObject[] array2 = templateHighlight;
			if (array2 != null && array2.Length > 0)
			{
				array2 = templateHighlight;
				foreach (GameObject gameObject in array2)
				{
					if (!(gameObject == null))
					{
						user = leaderboardEntry.User;
						gameObject.SetActive(user.IsMe);
					}
				}
			}
			user = leaderboardEntry.User;
			if (user.IsMe)
			{
				ownEntryIndex = entryCount;
			}
			GameObject obj = Object.Instantiate(template, container);
			obj.SetActive(value: true);
			SteamController.FetchAvatar(obj.GetComponentInChildren<RawImage>(), leaderboardEntry.User);
			entryCount++;
		}
		loadingPanel.SetActive(value: false);
		scrollRect.gameObject.SetActive(value: true);
		if ((bool)scrollRect)
		{
			yield return null;
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)container);
			LeaderboardProperties.ScrollToEntry(scrollRect, ownEntryIndex, entryCount);
		}
	}

	private void ResetEntries()
	{
		ownEntryIndex = -1;
		foreach (Transform item in container)
		{
			if (!(item == template.transform))
			{
				Object.Destroy(item.gameObject);
			}
		}
	}

	private void Update()
	{
		InputBinding? inputBinding = null;
		if (MonoSingleton<InputManager>.Instance.LastButtonDevice is Gamepad)
		{
			if (MonoSingleton<InputManager>.Instance.InputSource.NextWeapon.Action.bindings.Count > 0)
			{
				foreach (InputBinding binding in MonoSingleton<InputManager>.Instance.InputSource.NextWeapon.Action.bindings)
				{
					if (!binding.groups.Contains(keyboardControlScheme.bindingGroup))
					{
						inputBinding = binding;
					}
				}
			}
		}
		else
		{
			if (MonoSingleton<InputManager>.Instance.InputSource.PreviousVariation.Action.bindings.Count > 0)
			{
				foreach (InputBinding binding2 in MonoSingleton<InputManager>.Instance.InputSource.PreviousVariation.Action.bindings)
				{
					if (binding2.groups.Contains(keyboardControlScheme.bindingGroup))
					{
						inputBinding = binding2;
					}
				}
			}
			if (!inputBinding.HasValue && MonoSingleton<InputManager>.Instance.InputSource.LastWeapon.Action.bindings.Count > 0)
			{
				foreach (InputBinding binding3 in MonoSingleton<InputManager>.Instance.InputSource.LastWeapon.Action.bindings)
				{
					if (binding3.groups.Contains(keyboardControlScheme.bindingGroup))
					{
						inputBinding = binding3;
					}
				}
			}
		}
		if (!inputBinding.HasValue)
		{
			switchTypeInput.text = "<color=white>[<color=orange>NO BINDING</color>]</color>";
			return;
		}
		switchTypeInput.text = "<color=white>[<color=orange>" + inputBinding?.ToDisplayString().ToUpper() + "</color>]</color>";
		if (MonoSingleton<InputManager>.Instance.InputSource.NextWeapon.WasPerformedThisFrame || MonoSingleton<InputManager>.Instance.InputSource.LastWeapon.WasPerformedThisFrame || MonoSingleton<InputManager>.Instance.InputSource.PreviousVariation.WasPerformedThisFrame)
		{
			displayPRank = !displayPRank;
			StopAllCoroutines();
			StartCoroutine(Fetch(SceneHelper.CurrentScene));
		}
		if (!scrollRect || !scrollRect.content)
		{
			return;
		}
		float num = Mouse.current.scroll.y.ReadValue();
		if (num != 0f)
		{
			scrollRect.verticalNormalizedPosition += num / scrollRect.content.sizeDelta.y;
		}
		if (MonoSingleton<InputManager>.Instance.LastButtonDevice is Gamepad && Gamepad.current != null)
		{
			Vector2 vector = Gamepad.current.rightStick.ReadValue();
			if (vector.y != 0f)
			{
				scrollRect.verticalNormalizedPosition += 0.01f * Time.unscaledDeltaTime * controllerScrollSpeed * Mathf.Sign(vector.y);
			}
		}
	}
}
