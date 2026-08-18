using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyPlayerData : NetworkBehaviour
{
    public static event Action<LobbyPlayerData> OnDataChanged;

    [Networked, OnChangedRender(nameof(OnLobbyDataChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnLobbyDataChanged))]
    public NetworkBool IsReady { get; set; }

    private void OnLobbyDataChanged()
    {
        OnDataChanged?.Invoke(this);
    }

    /// <summary>
    /// Called by the owning client to set their name.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetName(string newName)
    {
        PlayerName = newName;
    }

    /// <summary>
    /// Called by the owning client to toggle ready state.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ToggleReady()
    {
        IsReady = !IsReady;
    }

}
