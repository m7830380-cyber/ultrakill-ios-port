using UnityEngine;
using plog;

public static class WaveUtils
{
	private static readonly plog.Logger Log = new plog.Logger("WaveUtils");

	public static bool IsWaveSelectable(int waveToCheck, int highestWave)
	{
		return highestWave >= waveToCheck * 2;
	}

	public static bool IsValidStartingWave(int wave)
	{
		Log.Info("Checking wave validity: " + wave);
		if (wave < 0)
		{
			return false;
		}
		return wave % 5 == 0;
	}

	public static int GetSafeStartingWave(int requestedWave)
	{
		if (IsValidStartingWave(requestedWave))
		{
			return requestedWave;
		}
		Log.Warning("Invalid starting wave format");
		return 0;
	}

	public static int? GetHighestWaveForDifficulty(int difficulty)
	{
		CyberRankData bestCyber = GameProgressSaver.GetBestCyber();
		if (bestCyber != null && bestCyber.preciseWavesByDifficulty.Length > difficulty)
		{
			return Mathf.FloorToInt(bestCyber.preciseWavesByDifficulty[difficulty]);
		}
		return null;
	}
}
