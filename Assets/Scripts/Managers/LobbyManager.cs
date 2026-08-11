using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.Unicode;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private LobbyUI lobbyUI;

    [Header("Panels")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;

    [Header("Choice Buttons")]
    [SerializeField] private Button createPartyButton;
    [SerializeField] private Button joinPartyButton;

    [Header("Host Panel")]
    [SerializeField] private Button generateCodeButton;
    [SerializeField] private TextMeshProUGUI partyCodeText;
    [SerializeField] private Button copyButton;
    [SerializeField] private Button hostBackButton;   // back to choice

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button joinBackButton;   // back to choice

    [Header("Common")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button backToTitleScreenButton;

    [Header("Fusion")]
    [SerializeField] private NetworkRunner runnerPrefab;

    private NetworkRunner runner;
    private string currentPartyCode;

    void Start()
    {
        // Show only choice panel initially
        ShowPanel(choicePanel);

        // Choice buttons
        createPartyButton.onClick.AddListener(() => ShowPanel(hostPanel));
        joinPartyButton.onClick.AddListener(() => ShowPanel(joinPanel));

        // Host panel actions
        generateCodeButton.onClick.AddListener(StartHost);
        copyButton.onClick.AddListener(CopyCodeToClipboard);
        hostBackButton.onClick.AddListener(() => ShowPanel(choicePanel));

        // Join panel actions
        joinButton.onClick.AddListener(StartClient);
        joinBackButton.onClick.AddListener(() => ShowPanel(choicePanel));

        // Back to title screen button
        backToTitleScreenButton.onClick.AddListener(() => FindObjectOfType<TitleScreenManager>()?.ShowTitleScreen());

    }

    void ShowPanel(GameObject panelToShow)
    {
        choicePanel.SetActive(panelToShow == choicePanel);
        hostPanel.SetActive(panelToShow == hostPanel);
        joinPanel.SetActive(panelToShow == joinPanel);
    }

    async void StartHost()
    {
        if (runner != null) return;

        currentPartyCode = GeneratePartyCode();
        partyCodeText.text = currentPartyCode;
        statusText.text = "Creating party...";

        runner = Instantiate(runnerPrefab);
        var args = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = currentPartyCode,
            // No Scene specified -> stays in the current scene
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()

        };

        var result = await runner.StartGame(args);
        if (result.Ok)
        {
            statusText.text = "Party created! Waiting for players...";
            // Show the lobby UI (player list, ready button, etc.)
            lobbyUI.ShowLobby();
        }
        else
            statusText.text = $"Failed: {result.ShutdownReason}";
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
            lobbyUI.ShowLobby();   // Show the lobby UI for client
        }
        else
            statusText.text = $"Failed: {result.ShutdownReason}";
    }

    void CopyCodeToClipboard()
    {
        GUIUtility.systemCopyBuffer = currentPartyCode;
        statusText.text = "Code copied to clipboard!";
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
