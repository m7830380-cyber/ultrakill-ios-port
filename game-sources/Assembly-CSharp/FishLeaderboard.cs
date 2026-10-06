using System.Text;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;

public class FishLeaderboard : MonoBehaviour
{
	[SerializeField]
	private TMP_Text globalText;

	[SerializeField]
	private TMP_Text friendsText;

	private void OnEnable()
	{
		Fetch();
	}

	private async void Fetch()
	{
		LeaderboardEntry[] array = await MonoSingleton<LeaderboardController>.Instance.GetFishScores(LeaderboardType.Global);
		StringBuilder strBlrd = new StringBuilder();
		if (array != null)
		{
			strBlrd.AppendLine("<b>GLOBAL</b>");
			int num = 1;
			LeaderboardEntry[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				LeaderboardEntry leaderboardEntry = array2[i];
				Friend user = leaderboardEntry.User;
				string arg = user.Name;
				user = leaderboardEntry.User;
				if (user.IsMe)
				{
					strBlrd.Append("<color=orange>");
				}
				strBlrd.Append("<noparse>");
				string text = $"[{num}] {leaderboardEntry.Score} - {arg}";
				if (text.Length > 25)
				{
					text = text.Substring(0, 25);
				}
				strBlrd.AppendLine(text);
				strBlrd.Append("</noparse>");
				user = leaderboardEntry.User;
				if (user.IsMe)
				{
					strBlrd.Append("</color>");
				}
				num++;
			}
		}
		else
		{
			strBlrd.Append("Error fetching leaderboard data.");
		}
		globalText.text = strBlrd.ToString();
		LeaderboardEntry[] array3 = await MonoSingleton<LeaderboardController>.Instance.GetFishScores(LeaderboardType.Friends);
		strBlrd.Clear();
		if (array3 != null)
		{
			strBlrd.AppendLine("<b>FRIENDS</b>");
			LeaderboardEntry[] array2 = array3;
			for (int i = 0; i < array2.Length; i++)
			{
				LeaderboardEntry leaderboardEntry2 = array2[i];
				Friend user = leaderboardEntry2.User;
				string arg2 = user.Name;
				user = leaderboardEntry2.User;
				if (user.IsMe)
				{
					strBlrd.Append("<color=orange>");
				}
				strBlrd.Append("<noparse>");
				string text2 = $"[{leaderboardEntry2.GlobalRank}] {leaderboardEntry2.Score} - {arg2}";
				if (text2.Length > 25)
				{
					text2 = text2.Substring(0, 25);
				}
				strBlrd.AppendLine(text2);
				strBlrd.Append("</noparse>");
				user = leaderboardEntry2.User;
				if (user.IsMe)
				{
					strBlrd.Append("</color>");
				}
			}
		}
		else
		{
			strBlrd.Append("Error fetching leaderboard data.");
		}
		friendsText.text = strBlrd.ToString();
	}
}
