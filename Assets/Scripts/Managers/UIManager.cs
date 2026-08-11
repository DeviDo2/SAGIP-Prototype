using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // for restart
using TMPro; // if using TextMeshPro, else use regular Text

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
        if (GameManager.Instance == null || GameManager.Instance.gameEnded) return;

        // Update timer
        float timeLeft = GameManager.Instance.timer;
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);
        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}";

        // Update progress
        if (progressText != null)
            progressText.text = $"Rescued: {GameManager.Instance.rescuedCount}/{GameManager.Instance.totalSurvivors}\nStabilised: {GameManager.Instance.stabilisedCount}/{GameManager.Instance.totalSurvivors}";

    }

    public void ShowEndScreen(bool won, int score)
    {
        endScreenPanel.SetActive(true);
        resultText.text = won ? "Mission Successful!" : "Mission Failed";
        scoreText.text = $"Final Score: {score}";
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

}
