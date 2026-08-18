using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI timerText;    // or UnityEngine.UI.Text
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("End Screen")]
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private TextMeshProUGUI resultText;   // "Victory" or "Defeat"
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private bool isLeavingSession;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        
        // Restart button listener
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        // Check if end screen panel is active and hide it initially
        if (endScreenPanel != null)
            endScreenPanel.SetActive(false);

        // Main menu button listener
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid)
            return;

        if (GameManager.Instance.gameEnded) return;


        // Update timer
        float timeLeft = GameManager.Instance.Timer;
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);
        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}";

        // Update progress
        if (progressText != null)
            progressText.text = $"Rescued: {GameManager.Instance.RescuedCount}/{GameManager.Instance.totalSurvivors}\nStabilised: {GameManager.Instance.StabilisedCount}/{GameManager.Instance.totalSurvivors}";

    }

    public void ShowEndScreen(bool won, int score)
    {
        endScreenPanel.SetActive(true);
        resultText.text = won ? "Mission Successful!" : "Mission Failed";
        scoreText.text = $"Final Score: {score}";
    }

    public void RestartGame()
    {
        // A new round needs fresh lobby role assignments, so safely end the
        // current Fusion session before returning to the menu.
        ReturnToMainMenu();
    }

    public void ReturnToMainMenu()
    {
        if (!isLeavingSession)
            StartCoroutine(ShutdownAndReturnToMainMenu());
    }

    private IEnumerator ShutdownAndReturnToMainMenu()
    {
        isLeavingSession = true;

        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && runner.IsRunning)
        {
            runner.Shutdown();

            // Shutdown destroys the runner by default. Waiting prevents its
            // network objects from surviving behind the title scene.
            while (runner != null && runner.IsRunning)
                yield return null;
        }

        SceneManager.LoadScene("MainMenu");
    }

}
