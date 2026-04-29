using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    public static bool IsShowing { get; private set; }

    Rect _lastPanelSafeArea;

    [SerializeField] Text finalScoreText;
    [SerializeField] Button shareButton;
    [SerializeField] Button nextLevelButton;

    void Awake()
    {
        if (finalScoreText == null)
        {
            var t = UiFactory.Text("FinalScore", transform, "Final score: 0", 34, TextAnchor.MiddleCenter, Color.white);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = UiFactory.SV(760f, 96f);
            rt.anchoredPosition = UiFactory.SV(0f, 52f);
            finalScoreText = t;
        }

        if (finalScoreText != null)
        {
            finalScoreText.color = new Color(0.98f, 0.99f, 1f, 1f);
            UiFactory.ApplyReadableOutline(finalScoreText, 2.55f);
        }

        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);

        // Scene "Play Button" — same rounded slate + shadow as UiFactory menus / Share score
        var playTf = transform.Find("Play Button");
        if (playTf != null)
        {
            var playImg = playTf.GetComponent<Image>();
            var playBt = playTf.GetComponent<Button>();
            if (playImg != null && playBt != null)
                UiFactory.ApplyHudCompactButton(playImg, playBt);

            var playTx = playTf.GetComponentInChildren<Text>(true);
            if (playTx != null)
            {
                playTx.font = UiFactory.DefaultFont;
                playTx.text = "Play Again";
                playTx.color = new Color(0.93f, 0.97f, 1f, 1f);
                playTx.fontSize = Mathf.Clamp(UiFactory.Font(48), 28, 96);
                playTx.fontStyle = FontStyle.Bold;
                playTx.raycastTarget = false;
            }
        }

        if (shareButton == null)
        {
            var btn = UiFactory.IconButton("ShareBtn", transform, new Vector2(0f, -292f), new Vector2(80f, 80f), UiFactory.ShareIconSprite(), ShareScore);
            shareButton = btn;
            btn.transform.SetAsFirstSibling();
        }
        else
            shareButton.onClick.AddListener(ShareScore);

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            UiFactory.ApplySafeAreaFullBleed(GetComponent<RectTransform>(), canvas);
        _lastPanelSafeArea = Screen.safeArea;
    }

    void LateUpdate()
    {
        if (!isActiveAndEnabled)
            return;

        Rect sa = Screen.safeArea;
        if (SafeAreaChanged(sa, _lastPanelSafeArea))
        {
            _lastPanelSafeArea = sa;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                UiFactory.ApplySafeAreaFullBleed(GetComponent<RectTransform>(), canvas);
        }
    }

    static bool SafeAreaChanged(Rect a, Rect b)
    {
        const float eps = 0.5f;
        return Mathf.Abs(a.x - b.x) > eps || Mathf.Abs(a.y - b.y) > eps ||
               Mathf.Abs(a.width - b.width) > eps || Mathf.Abs(a.height - b.height) > eps;
    }

    void Start()
    {
        gameObject.SetActive(false);
        Bomb.OnGameOver += Bomb_OnGameOver;
        IsShowing = false;
    }

    void Bomb_OnGameOver(object sender, System.EventArgs e)
    {
        IsShowing = true;
        LeaderboardStore.SubmitScore(GameSession.Score);

        if (finalScoreText != null)
            finalScoreText.text = "Final score: " + GameSession.Score + "\nBest: " + LeaderboardStore.HighScore;

        GameAudio.PlayGameOverSting();

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        IsShowing = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShareScore()
    {
        string msg = $"Fruit Ninja — Score: {GameSession.Score} (Best: {LeaderboardStore.HighScore})";
        GUIUtility.systemCopyBuffer = msg;
    }

    public void NextLevel()
    {
        GameSession.Level++;
        PlayAgain();
    }

    /// <summary>Hides game-over overlay when returning to main menu without reloading.</summary>
    public static void HidePanel()
    {
        IsShowing = false;
        var go = FindObjectOfType<GameOver>(true);
        if (go != null)
            go.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnDisable()
    {
        Bomb.OnGameOver -= Bomb_OnGameOver;
    }
}
