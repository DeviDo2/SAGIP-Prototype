using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class LobbyPlayerData : NetworkBehaviour
{
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }

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
