using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// Routes fruit scoring into <see cref="GameSession"/>; score is shown on the runtime HUD, not this legacy text.
/// </summary>
public class PlayerScore : MonoBehaviour
{
    [SerializeField] Text scoreText;

    void Start()
    {
        Fruit.OnScoredChanged += Instance_OnScoredChanged;
        GameSession.ScoreChanged += OnSessionScore;

        if (scoreText != null)
        {
            scoreText.enabled = false;
            scoreText.text = GameSession.Score.ToString();
        }
    }

    private void Instance_OnScoredChanged(object sender, System.EventArgs e)
    {
        GameSession.AddScore(1);
    }

    private void OnSessionScore(int value)
    {
        if (scoreText != null)
            scoreText.text = value.ToString();
    }

    private void OnDisable()
    {
        Fruit.OnScoredChanged -= Instance_OnScoredChanged;
        GameSession.ScoreChanged -= OnSessionScore;
    }
}
