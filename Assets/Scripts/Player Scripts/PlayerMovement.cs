using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The input payload sent by each client to the state authority.
/// </summary>
public struct PlayerNetworkInput : INetworkInput
{
    public Vector2 Move;
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference moveActionRef;

    public bool IsActive { get; private set; }

    private Rigidbody rb;
    private InputAction moveAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void Spawned()
    {
        Debug.Log(
        $"[PlayerMovement.Spawned] " +
        $"Object={Object.Id} " +
        $"InputAuthority={Object.InputAuthority} " +
        $"LocalPlayer={Runner.LocalPlayer} " +
        $"HasInputAuthority={Object.HasInputAuthority} " +
        $"Name={name}"
        );

        // Only the client that owns this player provides input for it.
        if (!Object.HasInputAuthority)
            return;

        IsActive = true;
        moveAction = moveActionRef != null ? moveActionRef.action : null;

        if (moveAction == null)
        {
            Debug.LogError($"{name} has no Move InputAction assigned.", this);
            return;
        }

        moveAction.Enable();
        FusionPlayerInputProvider.GetOrCreate(Runner).SetMoveAction(moveAction);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (moveAction != null)
            moveAction.Disable();
    }

    public void SetActive(bool state)
    {
        IsActive = state;
    }

    public override void FixedUpdateNetwork()
    {
        // In Host mode, remote clients do not own simulation state. The host
        // reads their submitted input and is the only peer that moves bodies.

        if (Object.HasInputAuthority)
        {
            //Debug.Log($"[{Object.InputAuthority}] PlayerMovement is processing local input");
        }

        if (!Object.HasStateAuthority)
            return;

        Vector2 move = Vector2.zero;
        if (Runner.TryGetInputForPlayer(Object.InputAuthority, out PlayerNetworkInput input))
            move = input.Move;

        Vector3 direction = new Vector3(move.x, 0f, move.y);
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        rb.velocity = new Vector3(direction.x * moveSpeed, rb.velocity.y, direction.z * moveSpeed);
    }

}

/// <summary>
/// A single persistent input callback on the NetworkRunner. It avoids using a
/// short-lived player prefab as the runner input source during scene changes.
/// </summary>
public class FusionPlayerInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;
    private InputAction moveAction;
    private bool callbacksRegistered;

    public static FusionPlayerInputProvider GetOrCreate(NetworkRunner runner)
    {
        FusionPlayerInputProvider provider = runner.GetComponent<FusionPlayerInputProvider>();
        if (provider == null)
            provider = runner.gameObject.AddComponent<FusionPlayerInputProvider>();

        provider.Initialize(runner);
        return provider;
    }

    public void SetMoveAction(InputAction action)
    {
        moveAction = action;

        Debug.Log(
        $"[InputProvider.SetMoveAction] " +
        $"Runner={runner?.LocalPlayer} " +
        $"Action={(action != null ? action.name : "NULL")} " +
        $"Enabled={(action != null && action.enabled)}"
        );

        if (moveAction != null && !moveAction.enabled)
            moveAction.Enable();
    }

    private void Initialize(NetworkRunner networkRunner)
    {
        if (callbacksRegistered)
            return;

        runner = networkRunner;

        Debug.Log(
        $"[InputProvider.Initialize] " +
        $"LocalPlayer={runner.LocalPlayer} " +
        $"Runner={runner.name}"
        );

        runner.AddCallbacks(this);
        callbacksRegistered = true;
    }

    private void OnDestroy()
    {
        if (callbacksRegistered && runner != null)
            runner.RemoveCallbacks(this);
    }

    public void OnInput(NetworkRunner networkRunner, NetworkInput input)
    {

        Vector2 move = moveAction != null 
            ? moveAction.ReadValue<Vector2>() 
            : Vector2.zero;
        /*
        Debug.Log(
        $"INPUT | Local={networkRunner.LocalPlayer} | " +
        $"Move={move} | Tick={networkRunner.Tick} | " +
        $"Action={(moveAction != null)}"
        );
        */
        input.Set(new PlayerNetworkInput
        {
            Move = move
        });
    }

    public void OnPlayerJoined(NetworkRunner networkRunner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner networkRunner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner networkRunner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner networkRunner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner networkRunner) { }
    public void OnDisconnectedFromServer(NetworkRunner networkRunner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner networkRunner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner networkRunner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner networkRunner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner networkRunner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner networkRunner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner networkRunner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataReceived(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner networkRunner) { }
    public void OnSceneLoadStart(NetworkRunner networkRunner) { }
    public void OnObjectExitAOI(NetworkRunner networkRunner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner networkRunner, NetworkObject obj, PlayerRef player) { }
}
