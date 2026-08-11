using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject lobbyPanel;               // panel containing all lobby UI
    [SerializeField] private Transform playerListContainer;       // parent with VerticalLayoutGroup
    [SerializeField] private GameObject playerEntryPrefab;        // a simple prefab (UI) to show one player
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private NetworkRunner runner;
    private LobbyPlayerData localLobbyPlayer;   // the lobby object owned by this client
    private Dictionary<PlayerRef, LobbyPlayerData> allPlayers = new();
    private bool isHost = false;

    void Start()
    {
        lobbyPanel.SetActive(false);
        startButton.gameObject.SetActive(false);
        readyButton.onClick.AddListener(OnReadyClicked);
    }

    public void ShowLobby()
    {
        lobbyPanel.SetActive(true);
        runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            isHost = runner.GameMode == GameMode.Host;
            startButton.gameObject.SetActive(isHost);
            // Hook into player join/leave events
            runner.AddCallbacks(this);
        }

        // If host, spawn the local lobby player immediately
        if (isHost)
            SpawnLobbyPlayer(runner.LocalPlayer);
        else
            StartCoroutine(WaitForMyPlayer());
    }

    // For clients: wait until the host spawns our LobbyPlayer, then set name
    IEnumerator WaitForMyPlayer()
    {
        while (localLobbyPlayer == null)
        {
            // Try to find our player object (the one with input authority = local player)
            foreach (var kvp in allPlayers)
            {
                if (kvp.Value.Object.HasInputAuthority)
                {
                    localLobbyPlayer = kvp.Value;
                    break;
                }
            }
            yield return null;
        }
        SetMyName();
    }

    // After spawning (or finding) our own player, set the name
    void SetMyName()
    {
        if (localLobbyPlayer != null && !string.IsNullOrEmpty(PlayerData.LocalPlayerName))
            localLobbyPlayer.RPC_SetName(PlayerData.LocalPlayerName);
    }

    void OnDestroy()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }
    }
    // ---------- INetworkRunnerCallbacks implementation ----------
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (isHost)
        {
            SpawnLobbyPlayer(player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (allPlayers.TryGetValue(player, out var data))
        {
            Destroy(data.gameObject);
            allPlayers.Remove(player);
            RefreshPlayerList();
        }
    }

    // These are required by the interface but we don't need them yet.
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    // -------------------------------------------------------------

    async void SpawnLobbyPlayer(PlayerRef player)
    {
        // Load the prefab from Resources folder (must be named exactly "LobbyPlayer")
        NetworkObject obj = await runner.SpawnAsync(
            Resources.Load<NetworkObject>("LobbyPlayer"),
            position: Vector3.zero,
            rotation: Quaternion.identity,
            inputAuthority: player,
            onBeforeSpawned: (runner, no) =>
            {
                no.name = $"LobbyPlayer_{player}";
            }
        );

        var data = obj.GetComponent<LobbyPlayerData>();
        if (data != null)
        {
            allPlayers[player] = data;
            if (player == runner.LocalPlayer)
            {
                localLobbyPlayer = data;
                SetMyName();
            }
            RefreshPlayerList();
        }
    }

    void OnReadyClicked()
    {
        if (localLobbyPlayer != null)
        {
            localLobbyPlayer.RPC_ToggleReady();
        }
    }

    void Update()
    {
        if (runner == null) return;

        if (isHost)
        {
            bool allReady = true;
            int count = 0;
            foreach (var kvp in allPlayers)
            {
                count++;
                if (!kvp.Value.IsReady)
                {
                    allReady = false;
                    break;
                }
            }
            startButton.interactable = (count >= 2 && allReady);
        }

        RefreshPlayerList();
    }

    void RefreshPlayerList()
    {
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        foreach (var kvp in allPlayers)
        {
            var entry = Instantiate(playerEntryPrefab, playerListContainer);
            var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();

            string playerName = "";
            kvp.Value.PlayerName.Get(ref playerName);
            texts[0].text = string.IsNullOrEmpty(playerName) ? $"Player {kvp.Key}" : playerName;
            texts[1].text = kvp.Value.IsReady ? "Ready" : "Not Ready";
        }
    }

    public void OnStartGameClicked()
    {
        if (isHost && runner != null)
        {
            runner.LoadScene(SceneRef.FromIndex(1)); // Gameplay scene index
        }
    }

}
