using UnityEngine;

/// <summary>
/// Persists high score for leaderboard UI (PlayerPrefs).
/// </summary>
public static class LeaderboardStore
{
    const string Key = "FruitNinja_HighScore";

    public static int HighScore
    {
        get => PlayerPrefs.GetInt(Key, 0);
        private set => PlayerPrefs.SetInt(Key, value);
    }

    public static bool SubmitScore(int score)
    {
        if (score <= HighScore) return false;
        HighScore = score;
        PlayerPrefs.Save();
        return true;
    }
}
