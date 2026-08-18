using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks; // added for Task.Yield()
using UnityEngine;
using System.Linq;
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public enum PlayerRole { Scout, Rescuer, Medic }

    [Header("Player Prefabs")]
    [SerializeField] private NetworkObject scoutPrefab;
    [SerializeField] private NetworkObject rescuerPrefab;
    [SerializeField] private NetworkObject medicPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;   // assign 3 empty GameObjects in the scene

    /*
    // Internal array to hold the player GameObjects for easy access
    private PlayerMovement[] playerMovements;
    [HideInInspector] public InteractionHandler[] interactionHandlers;
    */

    [field: Header("Mission Settings")]
    [field: SerializeField] public int totalSurvivors { get; private set; } = 3; // set in Inspector
    [field: SerializeField] public float levelTime = 300f;           // 5 minutes

    // Networked state: only the host writes to these, but all clients can read them.
    [Networked] private int rescuedCount { get; set; }
    [Networked] private int stabilisedCount { get; set; }
    [Networked] private int correctTagCount { get; set; }
    [Networked] private int incorrectTagCount { get; set; }
    [Networked] private float timer { get; set; }
    [Networked] public NetworkBool gameEnded { get; set; }

    public int RescuedCount => rescuedCount;
    public int StabilisedCount => stabilisedCount;
    public int CorrectTagCount => correctTagCount;
    public int IncorrectTagCount => incorrectTagCount;
    public float Timer => timer;

    public override void Spawned()
    {
        Debug.Log(
            $"[GAME MANAGER SPAWNED] " +
            $"Local={Runner.LocalPlayer} " +
            $"Authority={Object.HasStateAuthority} " +
            $"ActivePlayers={Runner.ActivePlayers.Count()} " +
            $"Roles={SessionData.FinalRoles?.Count ?? -1}"
        );

        foreach (var player in Runner.ActivePlayers)
        {
            Debug.Log($"[GAME MANAGER PLAYER] {player}");
        }

        Instance = this;

        if (!Object.HasStateAuthority)
            return;

        timer = levelTime;

        StartCoroutine(SpawnPlayersWhenReady());
    }

    private IEnumerator SpawnPlayersWhenReady()
    {
        yield return null;
        yield return null;
        yield return null;

        if (!Object.HasStateAuthority)
            yield break;

        Debug.Log(
            $"[GAME MANAGER BEFORE SPAWN] " +
            $"ActivePlayers={Runner.ActivePlayers.Count()} " +
            $"Roles={SessionData.FinalRoles?.Count ?? -1}"
        );

        foreach (var player in Runner.ActivePlayers)
        {
            Debug.Log($"[GAME MANAGER BEFORE SPAWN PLAYER] {player}");
        }

        SpawnPlayers();
    }

    private async Task SpawnPlayers()
    {
        if (!Object.HasStateAuthority)
            return;

        var roles = SessionData.FinalRoles;

        if (roles == null || roles.Count == 0)
        {
            Debug.LogError(
                $"[GAME MANAGER] No role data. " +
                $"Roles={roles?.Count ?? 0}"
            );
            return;
        }

        if (roles.Count != Runner.ActivePlayers.Count())
        {
            Debug.LogWarning(
                $"[GAME MANAGER] Role/player count mismatch. " +
                $"Roles={roles.Count}, " +
                $"Players={Runner.ActivePlayers.Count()}"
            );
        }

        int index = 0;

        foreach (var kvp in roles)
        {
            NetworkObject prefab = GetPrefab(kvp.Value);

            if (prefab == null)
            {
                Debug.LogError($"No prefab for role {kvp.Value}");
                return;
            }

            Transform spawn = spawnPoints[index];

            var playerObj = await Runner.SpawnAsync(
                prefab,
                spawn.position,
                spawn.rotation,
                kvp.Key
            );

            if (playerObj == null)
            {
                Debug.LogError($"Failed to spawn {kvp.Key}");
                return;
            }

            Runner.SetPlayerObject(kvp.Key, playerObj);

            index++;
        }

        Debug.Log("[GAME MANAGER] ALL PLAYERS SPAWNED");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (gameEnded) return;

        timer -= Runner.DeltaTime;

        if (timer <= 0f)
        {
            EndGame(false);
        }
        else if (AllSurvivorsHandled())
        {
            EndGame(true);
        }
    }

    bool AllSurvivorsHandled()
    {
        Survivor[] all = FindObjectsOfType<Survivor>();
        foreach (Survivor survivor in all)
        {
            switch (survivor.tagColor)
            {
                case TagColor.None:
                    return false;

                case TagColor.Green:
                    // Green survivors self-rescue and are never stabilized.
                    if (!survivor.isRescued)
                        return false;
                    break;

                case TagColor.Yellow:
                    if (!survivor.isRescued || !survivor.isStable)
                        return false;
                    break;

                case TagColor.Red:
                    // Red survivors are intentionally not rescuable.
                    break;
            }
        }
        return true;
    }

    // Called by survivors (via RPCs) when their state changes
    public void OnSurvivorRescued(Survivor survivor)
    {
        if (!Object.HasStateAuthority) return;
        rescuedCount++;

        // Score bonus/penalty based on correct tagging
        if (survivor.GetCorrectTag() == survivor.tagColor)
            correctTagCount++;
        else
            incorrectTagCount++;
    }

    public void OnSurvivorStabilised(Survivor s)
    {
        if (!Object.HasStateAuthority) return;
        stabilisedCount++;
    }

    void EndGame(bool won)
    {
        gameEnded = true;
        int score = (rescuedCount * 100) + (stabilisedCount * 50)
                    + (int)(timer * 10) + (correctTagCount * 30) - (incorrectTagCount * 20);

        // Tell all clients to show the end screen
        RPC_ShowEndScreen(won, score);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ShowEndScreen(bool won, int score)
    {
        UIManager.Instance.ShowEndScreen(won, score);
    }

    private NetworkObject GetPrefab(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Scout:
                return scoutPrefab;
            case PlayerRole.Rescuer:
                return rescuerPrefab;
            case PlayerRole.Medic:
                return medicPrefab;
            default:
                return null;
        }
    }
}
