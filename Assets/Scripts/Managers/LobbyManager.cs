using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private LobbyUI lobbyUI;

    [Header("Panels")]
    [SerializeField] private GameObject sidePanel;
    [SerializeField] private GameObject joinPanel;
    
    [Header("Side Bar Buttons")]
    [SerializeField] private Button createPartyButton;
    [SerializeField] private Button joinPartyButton;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button joinBackButton;

    [Header("Common")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button backToTitleScreenButton;

    [Header("Fusion")]
    [SerializeField] private NetworkRunner runnerPrefab;

    private NetworkRunner runner;
    private string currentPartyCode;

    void Start()
    {
        // Sidebar is always visible; join panel is hidden
        sidePanel.SetActive(true);
        joinPanel.SetActive(false);

        // Create a party immediately
        createPartyButton.onClick.AddListener(StartHost);

        // Show the join panel
        joinPartyButton.onClick.AddListener(() => joinPanel.SetActive(true));

        // Join panel actions
        joinButton.onClick.AddListener(StartClient);
        joinBackButton.onClick.AddListener(() => joinPanel.SetActive(false));

        // Back to title screen
        backToTitleScreenButton.onClick.AddListener(() =>
            FindObjectOfType<TitleScreenManager>()?.ShowTitleScreen());

    }

    async void StartHost()
    {
        if (runner != null) return;

        currentPartyCode = GeneratePartyCode();
        statusText.text = "Creating party...";

        runner = Instantiate(runnerPrefab);
        runner.ProvideInput = true;

        FusionPlayerInputProvider.GetOrCreate(runner);

        var args = new StartGameArgs()
        {
            
            GameMode = GameMode.Host,
            SessionName = currentPartyCode,
            // No Scene specified -> stays in the lobby scene
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        };

        var result = await runner.StartGame(args);
        if (result.Ok)
        {
            statusText.text = "Party created! Waiting for players...";
            // Hide the join panel if it was open, but keep the sidebar
            joinPanel.SetActive(false);
            // Show the merged lobby UI with the generated code
            lobbyUI.ShowLobby(currentPartyCode);
        }
        else
        {
            statusText.text = $"Failed: {result.ShutdownReason}";
        }
    }

    async void StartClient()
    {
        if (runner != null) return;

        string code = codeInputField.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Enter a party code.";
            return;
        }

        statusText.text = "Joining...";

        runner = Instantiate(runnerPrefab);
        runner.ProvideInput = true;

        FusionPlayerInputProvider.GetOrCreate(runner);

        var args = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = code,
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        };

        var result = await runner.StartGame(args);
        if (result.Ok)
        {
            statusText.text = "Joined!";
            joinPanel.SetActive(false);
            lobbyUI.ShowLobby();   // No party code needed for clients
        }
        else
        {
            statusText.text = $"Failed: {result.ShutdownReason}";
        }
    }

    string GeneratePartyCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        System.Random rng = new System.Random();
        char[] code = new char[5];
        for (int i = 0; i < 5; i++)
            code[i] = chars[rng.Next(chars.Length)];
        return new string(code);
    }

}
