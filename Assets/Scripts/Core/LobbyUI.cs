using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
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

    [Header("Host Code Display")]
    [SerializeField] private GameObject hostCodeGroup;       // a parent object containing the code field + copy button
    [SerializeField] private TMP_InputField hostCodeInput;   // read-only, displays the party code
    [SerializeField] private Button hostCopyButton;

    [SerializeField] private RoleSelectionUI roleSelectionUI;

    private NetworkRunner runner;
    private LobbyPlayerData localLobbyPlayer;   // the lobby object owned by this client
    private Dictionary<PlayerRef, LobbyPlayerData> allPlayers = new();
    private Dictionary<PlayerRef, LobbyPlayerEntry> playerEntries = new();

    private bool isHost = false;
    private bool isTransitioning = false;

    void Start()
    {
        lobbyPanel.SetActive(false);
        startButton.gameObject.SetActive(false);
        readyButton.onClick.AddListener(OnReadyClicked);
        startButton.onClick.AddListener(OnStartGameClicked);

        // Wire copy button if assigned
        if (hostCopyButton != null)
            hostCopyButton.onClick.AddListener(CopyCodeToClipboard);
    }

    
    // Overload that receives party code (called from LobbyManager)
    public async void ShowLobby(string partyCode)
    {
        lobbyPanel.SetActive(true);
        runner = FindObjectOfType<NetworkRunner>();

        if (runner != null)
        {
            isHost = runner.GameMode == GameMode.Host;
            startButton.gameObject.SetActive(isHost);
            
            runner.AddCallbacks(this);
        }

        // Show code UI only for host
        if (hostCodeGroup != null)
            hostCodeGroup.SetActive(isHost);

        if (isHost && hostCodeInput != null)
            hostCodeInput.text = partyCode;

        // For both host and client: immediately scan for any already spawned LobbyPlayers
        RefreshAllPlayers();

        if (isHost)
        {
            await SpawnLocalLobbyPlayerIfNeeded();
        }
        else
        {
            // Client: wait until its own LobbyPlayer appears (spawned by host)
            StartCoroutine(WaitForMyPlayer());
        }
    }

    // In LobbyUI.cs
    public void ShowLobby() => ShowLobby("");   // for clients

    void CopyCodeToClipboard()
    {
        if (hostCodeInput != null)
        {
            GUIUtility.systemCopyBuffer = hostCodeInput.text;
            statusText.text = "Code copied!";
        }
    }

    void OnDestroy()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }
    }
    // ---------- INetworkRunnerCallbacks implementation ----------
    public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Host spawns the lobby object for the new player
        if (isHost)
            await SpawnLobbyPlayer(player);
        else
            // Client refreshes its list because a new LobbyPlayer has been spawned
            RefreshAllPlayers();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log(
            $"[PLAYER LEFT] " +
            $"Local={runner.LocalPlayer} " +
            $"Player={player} " +
            $"Remaining={runner.ActivePlayers.Count()}"
        );

        if (allPlayers.TryGetValue(player, out var data))
        {
            // Fusion objects must be removed by the state authority, never
            // with UnityEngine.Object.Destroy.
            if (data != null && 
                data.Object != null && 
                data.Object.IsValid && 
                data.Object.HasStateAuthority)
            {
                runner.Despawn(data.Object);
            }

            allPlayers.Remove(player);
        }

        // Remove this player's UI entry.
        if (playerEntries.TryGetValue(player, out var entry))
        {
            if (entry != null)
                Destroy(entry.gameObject);

            playerEntries.Remove(player);
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log(
            $"[SCENE START] " +
            $"Local={runner.LocalPlayer} " +
            $"IsHost={runner.IsServer} " +
            $"Scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}"
        );
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log(
            $"[SCENE DONE] " +
            $"Local={runner.LocalPlayer} " +
            $"IsHost={runner.IsServer} " +
            $"Scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}"
        );
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
    
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
    {
            Debug.Log(
            $"[SHUTDOWN] " +
            $"Local={runner.LocalPlayer} " +
            $"Reason={shutdownReason}"
        );
    }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    // -------------------------------------------------------------

    /// <summary>
    /// Finds all LobbyPlayerData objects in the scene and fills the allPlayers dictionary.
    /// </summary>

    private void OnEnable()
    {
        LobbyPlayerData.OnDataChanged += HandlePlayerDataChanged;
    }

    private void OnDisable()
    {
        LobbyPlayerData.OnDataChanged -= HandlePlayerDataChanged;
    }
    
    private void HandlePlayerDataChanged(LobbyPlayerData data)
    {
        //UpdatePlayerEntryFromNetwork(data);

    }

    void RefreshAllPlayers()
    {
        var players = FindObjectsOfType<LobbyPlayerData>();

        foreach (var data in players)
        {
            if (data.Object == null || !data.Object.IsValid) 
                continue;

            PlayerRef playerRef = data.Object.InputAuthority;

            if (!allPlayers.ContainsKey(playerRef))
            {
                allPlayers[playerRef] = data;

                // If this object belongs to us, set local reference
                if (data.Object.HasInputAuthority)
                {
                    localLobbyPlayer = data;
                    // Set the player name (needs to be done only once)
                    SetMyName();
                }
            }

            // Make sure this player's UI entry exists and is current.
            UpdatePlayerEntry(playerRef, data);

        }
    }

    /// <summary>
    /// Host spawns its own LobbyPlayer if it hasn't already.
    /// </summary>
    async Task SpawnLocalLobbyPlayerIfNeeded()
    {
        // If we already have a LobbyPlayer with our input authority, don't spawn again
        if (localLobbyPlayer != null) return;

        await SpawnLobbyPlayer(runner.LocalPlayer);
    }

    /// <summary>
    /// Coroutine for clients: wait until the host spawns our LobbyPlayer, then set name.
    /// </summary>
    IEnumerator WaitForMyPlayer()
    {
        
        while (localLobbyPlayer == null)
        {
            RefreshAllPlayers();
            yield return null;
        }
        SetMyName();
    }

    void SetMyName()
    {
        if (localLobbyPlayer != null && !string.IsNullOrEmpty(PlayerData.LocalPlayerName))
            localLobbyPlayer.RPC_SetName(PlayerData.LocalPlayerName);
    }


    async Task SpawnLobbyPlayer(PlayerRef player)
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

            UpdatePlayerEntry(player, data);
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
        if (isTransitioning) return; // do nothing while transitioning
        if (runner == null) return;

        if (isHost)
        {
            bool allReady = true;
            int count = 0;

            foreach (var kvp in allPlayers)
            {
                if (kvp.Value == null || 
                    kvp.Value.Object == null || 
                    !kvp.Value.Object.IsValid)
                    continue;

                count++;

                if (!kvp.Value.IsReady)
                {
                    allReady = false;
                    break;
                }
            }

            startButton.interactable = (count >= 2 && allReady);
        }
        Debug.Log(
            $"[LOBBY] ActivePlayers={runner.ActivePlayers.Count()} " +
            $"TrackedPlayers={allPlayers.Count}"
        );
    }

    public void UpdatePlayerEntryFromNetwork(LobbyPlayerData data)
    {
        if (data == null || data.Object == null || !data.Object.IsValid)
            return;

        PlayerRef player = data.Object.InputAuthority;

        if (!allPlayers.ContainsKey(player))
            allPlayers[player] = data;

        UpdatePlayerEntry(player, data);
    }

    void UpdatePlayerEntry(PlayerRef player, LobbyPlayerData data)
    {
        if (data == null || data.Object == null || !data.Object.IsValid)
            return;

        if (!playerEntries.TryGetValue(player, out LobbyPlayerEntry entry) || entry == null)
        {
            GameObject entryObject = Instantiate(
                playerEntryPrefab,
                playerListContainer
            );

            entry = entryObject.GetComponent<LobbyPlayerEntry>();

            if (entry == null)
            {
                Debug.LogError(
                    "playerEntryPrefab is missing LobbyPlayerEntry component.",
                    entryObject
                );
                Destroy(entryObject);
                return;
            }

            playerEntries[player] = entry;
        }

        string playerName = "";
        string role = "";
        data.PlayerName.Get(ref playerName);

        if (string.IsNullOrEmpty(playerName))
            playerName = $"Player {player}";

        entry.SetData(playerName,role , data.IsReady);
    }

    public async void OnStartGameClicked()
    {
        if (!isHost || runner == null) return;

        isTransitioning = true;   // stop refreshing
        lobbyPanel.SetActive(false); // hide lobby immediately

        NetworkObject obj = await runner.SpawnAsync(
            Resources.Load<NetworkObject>("RoleSelectionManager"),
            position: Vector3.zero,
            rotation: Quaternion.identity,
            inputAuthority: runner.LocalPlayer
        );
        var manager = obj.GetComponent<RoleSelectionManager>();
        manager.BeginRoleSelection();
    }

}
