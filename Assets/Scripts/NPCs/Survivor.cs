using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public enum TagColor { None, Green, Yellow, Red }
public class Survivor : NetworkBehaviour
{
    [Header("Stability")]
    [SerializeField] private float maxStability = 100f;

    [Networked] public float currentStability { get; set; }
    [Networked] public float initialStability { get; set; }

    [Networked] public TagColor tagColor { get; set; }
    [Networked] public NetworkBool isRescued { get; set; }
    [Networked] public NetworkBool isStable { get; set; }
    [Networked] public NetworkId carrier { get; set; }        // NetworkId of the Rescuer holding this survivor

    public Transform carryPoint;   // visual snap point (local use)

    private SpriteRenderer spriteRenderer;   // for colour changing

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            initialStability = Random.Range(20f, 100f);
            currentStability = initialStability;
        }
        // All clients now have the correct initialStability, so update color.
        UpdateSpriteColor(initialStability);
    }


    private void UpdateSpriteColor(float stability)
    {
        if (spriteRenderer != null)
        {
            float t = Mathf.Clamp01(stability / maxStability);
            spriteRenderer.color = Color.Lerp(Color.red, Color.green, t);
        }
    }

    // ---------- RPCs (called by clients, executed on host) ----------
    public void SetTagFromAuthority(TagColor newTag)
    {
        if (!Object.HasStateAuthority) return;
        if (isRescued) return;
        tagColor = newTag;
        Debug.Log($"{name} tagged {newTag}");
    }
    public void RescuerInteractFromAuthority(PlayerRef rescuer)
    {
        if (!Object.HasStateAuthority || isRescued || isStable) return;

        switch (tagColor)
        {
            case TagColor.Green:
                // Talk -> survivor self-rescues
                Debug.Log($"{name} (Green) thanks you and heads to safety!");
                isRescued = true;
                GameManager.Instance.OnSurvivorRescued(this);
                break;

            case TagColor.Yellow:
                // Only pick up if not already carried
                if (carrier == NetworkId.None)
                {
                    // Find the Rescuer NetworkObject using PlayerRef.
                    if (Runner.TryGetPlayerObject(rescuer, out var playerObj))
                    {
                        carrier = playerObj.Id;
                        // Parent the survivor to the carrier carry point for visuals.
                        // This is handled locally, but we can set a trigger.
                        // We'll let the local InteractionHandler handle parenting via a callback.
                        // For simplicity, we'll use a simple networked event:
                        RPC_OnPickedUp(playerObj.Id);
                    }
                }
                break;

            case TagColor.Red:
                Debug.Log($"{name} (Red) is beyond help...");
                break;
        }
    }
    public void DropFromAuthority(PlayerRef dropper, NetworkBool isInsideSafeZone)
    {
        if (!Object.HasStateAuthority || carrier == NetworkId.None) return;

        // Only the current carrier can drop
        if (Runner.TryGetPlayerObject(dropper, out var playerObj) && carrier == playerObj.Id)
        {
            carrier = NetworkId.None;
            if (isInsideSafeZone)
            {
                isRescued = true;
                GameManager.Instance.OnSurvivorRescued(this);
                Debug.Log($"{name} has been rescued!");
            }
            // Notify clients to detach parent and restore physics
            RPC_OnDropped(isInsideSafeZone);
        }
    }
    public void HealFromAuthority(float amount)
    {
        if (!Object.HasStateAuthority || tagColor != TagColor.Yellow || isStable) return;

        currentStability = Mathf.Min(currentStability + amount, maxStability);
        Debug.Log($"{name} healed: {currentStability}");

        if (currentStability >= maxStability)
        {
            currentStability = maxStability;
            isStable = true;
            GameManager.Instance.OnSurvivorStabilised(this);
        }
    }
    // Local callbacks to update visuals/parenting on all clients
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnPickedUp(NetworkId carrierId)
    {
        // Find the carrier object and parent the survivor to its carry point
        if (Runner.TryFindObject(carrierId, out var carrierObj))
        {
            InteractionHandler handler = carrierObj.GetComponent<InteractionHandler>();
            if (handler != null && handler.rescuerCarryPoint != null)
            {
                transform.SetParent(handler.rescuerCarryPoint);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb) rb.isKinematic = true;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDropped(NetworkBool isInsideSafeZone)
    {
        transform.SetParent(null);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            if (isInsideSafeZone)
                rb.isKinematic = true; // immovable after rescue
        }
    }

    // ---------- Scoring helper ----------
    public TagColor GetCorrectTag()
    {
        if (initialStability >= 75f) return TagColor.Green;
        if (initialStability >= 50f) return TagColor.Yellow;
        return TagColor.Red;
    }

}
