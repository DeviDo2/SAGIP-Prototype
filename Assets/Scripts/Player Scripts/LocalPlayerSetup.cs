using Fusion;
using UnityEngine;

public class LocalPlayerSetup : NetworkBehaviour
{
    public static LocalPlayerSetup Instance { get; private set; }

    public InteractionHandler InteractionHandler { get; private set; }

    public override void Spawned()
    {
        Debug.Log(
            $"[LOCAL SETUP SPAWNED] " +
            $"Object={Object.Id} " +
            $"InputAuthority={Object.InputAuthority} " +
            $"LocalPlayer={Runner.LocalPlayer} " +
            $"HasInputAuthority={Object.HasInputAuthority}"
        );

        if (!Object.HasInputAuthority)
            return;

        Instance = this;

        InteractionHandler = GetComponent<InteractionHandler>();

        GetComponent<PlayerMovement>()?.SetActive(true);
        InteractionHandler?.SetActive(true);

        // Attach the local camera to this player's character.
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetTarget(transform);
        }
        else
        {
            Debug.LogWarning("CameraController.Instance is null.");
        }
    }

}
