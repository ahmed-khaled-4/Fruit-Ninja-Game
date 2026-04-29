using System;
using UnityEngine;

/// <summary>
/// Shared runtime session: score, lives, level, objective text (HUD / game over).
/// </summary>
public static class GameSession
{
    public static int Score { get; private set; }
    public static int Lives { get; private set; } = 3;
    public static int Level { get; set; } = 1;
    public static int Coins => Score;
    public static string ObjectiveShort { get; set; } = "Slice fruits · Avoid bombs";
    public static int LanguageIndex { get; set; }
    public static int ControlScheme { get; set; }

    public static event Action<int> ScoreChanged;
    public static event Action<int> LivesChanged;
    public static event Action<bool> PlayingStateChanged;

    public static bool IsPlaying { get; private set; }

    public static void SetPlaying(bool playing)
    {
        if (IsPlaying == playing) return;
        IsPlaying = playing;
        PlayingStateChanged?.Invoke(playing);
    }

    public static void ResetRun()
    {
        Score = 0;
        Lives = 3;
        ScoreChanged?.Invoke(Score);
        LivesChanged?.Invoke(Lives);
    }

    public static void AddScore(int amount = 1)
    {
        Score += amount;
        ScoreChanged?.Invoke(Score);
    }

    public static void LoseLife(int amount = 1)
    {
        Lives = Mathf.Max(0, Lives - amount);
        LivesChanged?.Invoke(Lives);
    }
}
