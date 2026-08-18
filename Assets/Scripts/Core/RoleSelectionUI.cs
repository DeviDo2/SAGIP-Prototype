using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Linq;

public class RoleSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject roleSelectionPanel;
    [SerializeField] private Button scoutButton;
    [SerializeField] private Button rescuerButton;
    [SerializeField] private Button medicButton;
    [SerializeField] private Image scoutButtonImage;
    [SerializeField] private Image rescuerButtonImage;
    [SerializeField] private Image medicButtonImage;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Transform playerStatusContainer;
    [SerializeField] private GameObject playerEntryPrefab; // shows player name, role, confirmed icon

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.cyan;
    [SerializeField] private Color takenColor = Color.gray;

    private NetworkRunner runner;
    private RoleSelectionManager roleManager;
    private GameManager.PlayerRole? pendingRole = null;    // local selection before confirm

    private Dictionary<PlayerRef, RoleSelectionPlayerEntry> playerEntries = new();

    public static RoleSelectionUI Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        roleSelectionPanel.SetActive(false);
        confirmButton.interactable = false;

        scoutButton.onClick.AddListener(() => OnRoleClicked(GameManager.PlayerRole.Scout));
        rescuerButton.onClick.AddListener(() => OnRoleClicked(GameManager.PlayerRole.Rescuer));
        medicButton.onClick.AddListener(() => OnRoleClicked(GameManager.PlayerRole.Medic));
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }
    
    private void OnEnable()
    {
        RoleSelectionManager.OnPlayerRoleDataChanged += HandlePlayerRoleDataChanged;
    }

    private void OnDisable()
    {
        RoleSelectionManager.OnPlayerRoleDataChanged -= HandlePlayerRoleDataChanged;
    }
    

    public void Show(RoleSelectionManager manager)
    {
        roleManager = manager;
        runner = FindObjectOfType<NetworkRunner>();

        roleSelectionPanel.SetActive(true);
        
        CreateAllPlayerEntries();
        RefreshRoleButtons();
        RefreshConfirmButton();
        
    }

    private void HandlePlayerRoleDataChanged(PlayerRef player)
    {
        UpdatePlayerEntry(player);

        RefreshRoleButtons();
        RefreshConfirmButton();
    }

    void OnRoleClicked(GameManager.PlayerRole role)
    {
        if (roleManager == null)
            return;

        if (IsRoleTaken(role))
            return;

        roleManager.RPC_RequestRole(
            runner.LocalPlayer,
            role
        );

        pendingRole = role;

        RefreshRoleButtons();
        RefreshConfirmButton();
    }

    void OnConfirmClicked()
    {
        if (pendingRole == null || roleManager == null)
            return;

        roleManager.RPC_ConfirmRole(
            runner.LocalPlayer
        );

        pendingRole = null;

        RefreshRoleButtons();
        RefreshConfirmButton();
    }

    bool IsRoleTaken(GameManager.PlayerRole role)
    {
        if (roleManager == null) return false;
        var choices = roleManager.GetRoleChoices();
        var confirms = roleManager.GetConfirmations();
        foreach (var kv in choices)
        {
            if (kv.Key == runner.LocalPlayer) continue;
            if (confirms.TryGet(kv.Key, out var confirmed) && confirmed && (GameManager.PlayerRole)kv.Value == role)
                return true;
        }
        return false;
    }

    private void RefreshRoleButtons()
    {
        if (roleManager == null)
            return;

        var choices = roleManager.GetRoleChoices();
        var confirmations = roleManager.GetConfirmations();

        UpdateButton(
            scoutButton,
            scoutButtonImage,
            GameManager.PlayerRole.Scout,
            choices,
            confirmations
        );

        UpdateButton(
            rescuerButton,
            rescuerButtonImage,
            GameManager.PlayerRole.Rescuer,
            choices,
            confirmations
        );

        UpdateButton(
            medicButton,
            medicButtonImage,
            GameManager.PlayerRole.Medic,
            choices,
            confirmations
        );
    }

    private void RefreshConfirmButton()
    {
        if (roleManager == null)
        {
            confirmButton.interactable = false;
            return;
        }

        confirmButton.interactable =
            pendingRole.HasValue &&
            !IsRoleTaken(pendingRole.Value);
    }

    void UpdateButton(Button btn, Image img, GameManager.PlayerRole role,
                      NetworkDictionary<PlayerRef, int> choices, NetworkDictionary<PlayerRef, NetworkBool> confirms)
    {
        bool taken = IsRoleTaken(role);
        bool selected = pendingRole.HasValue && pendingRole.Value == role;

        btn.interactable = !taken; // cant click if taken
        if (taken)
            img.color = takenColor;
        else if (selected)
            img.color = selectedColor;
        else
            img.color = normalColor;
    }

    private void CreateAllPlayerEntries()
    {
        if (runner == null)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            UpdatePlayerEntry(player);
        }
    }

    private void UpdatePlayerEntry(PlayerRef player)
    {
        if (roleManager == null || runner == null)
            return;

        RoleSelectionPlayerEntry entry;

        if (!playerEntries.TryGetValue(player, out entry) || entry == null)
        {
            GameObject entryObject = Instantiate(
                playerEntryPrefab,
                playerStatusContainer
            );

            entry = entryObject.GetComponent<RoleSelectionPlayerEntry>();

            if (entry == null)
            {
                Debug.LogError(
                    "Role Selection playerEntryPrefab is missing RoleSelectionPlayerEntry.",
                    entryObject
                );

                Destroy(entryObject);
                return;
            }

            playerEntries[player] = entry;
        }

        var choices = roleManager.GetRoleChoices();
        var confirmations = roleManager.GetConfirmations();

        string playerName = GetPlayerName(player);

        string roleText = "-";

        if (choices.TryGet(player, out var roleInt))
        {
            roleText = ((GameManager.PlayerRole)roleInt).ToString();
        }

        bool confirmed = false;

        if (confirmations.TryGet(player, out var confirmation))
        {
            confirmed = confirmation;
        }

        entry.SetData(
            playerName,
            roleText,
            confirmed
        );
    }

    private string GetPlayerName(PlayerRef player)
    {
        // Find the LobbyPlayerData object whose input authority matches this player
        var lobbyPlayers = FindObjectsOfType<LobbyPlayerData>();
        foreach (var lp in lobbyPlayers)
        {
            if (lp.Object == null) continue;
            if (lp.Object.InputAuthority == player)
            {
                string name = "";
                lp.PlayerName.Get(ref name);
                if (!string.IsNullOrEmpty(name)) return name;
            }
        }
        return $"Player {player.PlayerId}"; // fallback
    }

    public void Hide()
    {
        roleSelectionPanel.SetActive(false);
    }

}
