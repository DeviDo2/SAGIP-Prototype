using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class RoleSelectionManager : NetworkBehaviour
{
    public static event Action<PlayerRef> OnPlayerRoleDataChanged;

    [Networked, Capacity(3)]
    private NetworkDictionary<PlayerRef, int> RoleChoices => default; // int = (int)GameManager.PlayerRole


    [Networked, Capacity(3)]
    private NetworkDictionary<PlayerRef, NetworkBool> Confirmations => default;

    [Networked] private NetworkBool isLoadingGameplay { get; set; }
    [Networked] private TickTimer gameplayLoadTimer { get; set; }


    public bool AllConfirmed
    {
        get
        {
            if (Confirmations.Count != Runner.ActivePlayers.Count())
            {
                Debug.Log(
                    $"[ROLE COMPLETE] " +
                    $"RoleChoices={RoleChoices.Count} " +
                    $"Confirmations={Confirmations.Count} " +
                    $"ActivePlayers={Runner.ActivePlayers.Count()}"
                );
                return false;
            }
            
            foreach (var kv in Confirmations)
            {
                if (!kv.Value)
                    return false;
            }
            return true;
        }
    }


    /// <summary>
    /// Called by a client to request a role. Only succeeds if the role is available.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRole(PlayerRef player, GameManager.PlayerRole role)
    {
        if (!Object.HasStateAuthority) return;

        if (IsRoleTaken(role, excludePlayer: player))
        {
            Debug.LogWarning($"{player} tried to take {role} but it's already taken.");
            return;
        }

        RoleChoices.Set(player, (int)role);
        if (Confirmations.ContainsKey(player))
            Confirmations.Set(player, false);

        OnPlayerRoleDataChanged?.Invoke(player);
    }


    /// <summary>
    /// Called by a client to confirm their current selection.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ConfirmRole(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (!RoleChoices.ContainsKey(player))
        {
            Debug.LogWarning(
                $"{player} tried to confirm without choosing a role."
            );
            return;
        }

        Confirmations.Set(player, true);

        OnPlayerRoleDataChanged?.Invoke(player);

        if (AllConfirmed && !isLoadingGameplay)
        {
            Dictionary<PlayerRef, GameManager.PlayerRole> finalRoles = new();
            foreach (var kv in RoleChoices)
                finalRoles[kv.Key] = (GameManager.PlayerRole)kv.Value;

            SessionData.FinalRoles = finalRoles;
            isLoadingGameplay = true;
            // Keep the selection object alive long enough for every client to
            // receive its spawn before the scene transition despawns it.
            gameplayLoadTimer = TickTimer.CreateFromTicks(Runner, 60);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !isLoadingGameplay || !gameplayLoadTimer.Expired(Runner))
            return;

        isLoadingGameplay = false;
        Runner.LoadScene(SceneRef.FromIndex(1));
    }

    bool IsRoleTaken(GameManager.PlayerRole role, PlayerRef excludePlayer)
    {
        foreach (var kv in RoleChoices)
        {
            if (kv.Key == excludePlayer) continue;
            if (Confirmations.TryGet(kv.Key, out var confirmed) && confirmed && (GameManager.PlayerRole)kv.Value == role)
                return true;
        }
        return false;
    }

    // Helper to get current selections for UI
    public NetworkDictionary<PlayerRef, int> GetRoleChoices() => RoleChoices;
    public NetworkDictionary<PlayerRef, NetworkBool> GetConfirmations() => Confirmations;

    public void BeginRoleSelection()
    {
        if (Object.HasStateAuthority)
            RPC_StartRoleSelection();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_StartRoleSelection()
    {
        RoleSelectionUI.Instance?.Show(this);
    }

}
