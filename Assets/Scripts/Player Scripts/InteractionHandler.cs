using Fusion;
using UnityEngine;

public class InteractionHandler : NetworkBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 0.5f; // Range within which the player can interact
    [SerializeField] private LayerMask survivorLayer; // Layer for survivors
    [SerializeField] private float healRate = 25f; // Stability healed per second by Medic


    [field:Header("Rescuer Carry and Drop")]
    [field: SerializeField] public Transform rescuerCarryPoint { get; private set; }
    [field: SerializeField] public Transform rescuerDropPoint { get; private set; }

    // The host assigns this when spawning the player. It must be networked so
    // the owner and every remote peer use the same role.
    [Networked] public GameManager.PlayerRole PlayerRole { get; private set; }

    public void SetRole(GameManager.PlayerRole role)
    {
        if (Object != null && Object.HasStateAuthority)
            PlayerRole = role;
    }

    private bool isActive = false;
    private Survivor currentTarget;

    private void Awake() { }

    public void SetActive(bool state)
    {
        isActive = state;
    }

    private void Update()
    {
        if (!isActive || !Object.HasInputAuthority) return;

        currentTarget = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, survivorLayer);
        float closest = Mathf.Infinity;
        foreach (var col in hits)
        {
            Survivor survivor = col.GetComponent<Survivor>();
            if (survivor == null) continue;
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closest)
            {
                closest = dist;
                currentTarget = survivor;
            }
        }
    }

    // Called by the Action button (once).
    public void PerformAction()
    {
        if (!isActive || !Object.HasInputAuthority || currentTarget == null) return;

        switch (PlayerRole)
        {
            case GameManager.PlayerRole.Scout:
                HandleScout();
                break;
            case GameManager.PlayerRole.Rescuer:
                HandleRescuer();
                break;
            case GameManager.PlayerRole.Medic:
                HandleMedic();
                break;
        }
    }

    // ---------- SCOUT ----------
    private void HandleScout()
    {
        if (currentTarget == null || currentTarget.isRescued) return;

        if (TagSelectionPopup.Instance != null && !TagSelectionPopup.Instance.IsVisible)
        {
            TagSelectionPopup.Instance.Show(this, currentTarget.transform.position);
        }
    }

    // Called by the tag popup when a color is chosen.
    public void OnTagColorSelected(TagColor selectedColor)
    {
        if (currentTarget != null && !currentTarget.isRescued && Object.HasInputAuthority)
        {
            RPC_RequestTag(currentTarget.Object.Id, selectedColor);
            TagSelectionPopup.Instance?.Hide();
        }
    }

    // ---------- RESCUER ----------
    private void HandleRescuer()
    {
        if (currentTarget.isRescued || currentTarget.isStable) return;

        // If already carrying this survivor -> drop
        if (currentTarget.carrier == Object.Id)
        {
            RPC_RequestRescuerAction(currentTarget.Object.Id);
            return;
        }

        // If carrying someone else, can't pick up another.
        if (IsCarryingAnyone()) return;

        // The state authority decides whether to talk, pick up, or drop.
        RPC_RequestRescuerAction(currentTarget.Object.Id);
    }

    public void TryDrop()
    {
        if (!isActive || !Object.HasInputAuthority || PlayerRole != GameManager.PlayerRole.Rescuer) return;
        Survivor carried = GetCarriedSurvivor();
        if (carried != null)
            RPC_RequestRescuerAction(carried.Object.Id);
    }

    private bool IsCarryingAnyone() => GetCarriedSurvivor() != null;

    private Survivor GetCarriedSurvivor()
    {
        foreach (var s in FindObjectsOfType<Survivor>())
        {
            if (s.carrier == Object.Id)
                return s;
        }
        return null;
    }
    private bool IsInsideSafeZone(Vector3 position) =>
        Physics.OverlapSphere(position, 0.5f, LayerMask.GetMask("SafeZone")).Length > 0;

    // ---------- MEDIC ----------
    private void HandleMedic()
    {
        if (!currentTarget.isRescued || currentTarget.isStable || !Object.HasInputAuthority) return;
        RPC_RequestHeal(currentTarget.Object.Id);
    }

    // All player requests start on the player-owned NetworkObject. This is
    // essential: scene Survivors have no InputAuthority, so clients cannot
    // safely send RPCs from them.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestTag(NetworkId survivorId, TagColor selectedColor)
    {
        if (PlayerRole != GameManager.PlayerRole.Scout || !TryGetValidSurvivor(survivorId, out Survivor survivor))
            return;

        survivor.SetTagFromAuthority(selectedColor);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestRescuerAction(NetworkId survivorId)
    {
        if (PlayerRole != GameManager.PlayerRole.Rescuer || !TryGetValidSurvivor(survivorId, out Survivor survivor))
            return;

        if (survivor.carrier == Object.Id)
        {
            survivor.DropFromAuthority(Object.InputAuthority, IsInsideSafeZone(survivor.transform.position));
            return;
        }

        if (!IsCarryingAnyone())
            survivor.RescuerInteractFromAuthority(Object.InputAuthority);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestHeal(NetworkId survivorId)
    {
        if (PlayerRole == GameManager.PlayerRole.Medic && TryGetValidSurvivor(survivorId, out Survivor survivor))
            survivor.HealFromAuthority(healRate);
    }

    private bool TryGetValidSurvivor(NetworkId survivorId, out Survivor survivor)
    {
        survivor = null;

        if (!Runner.TryFindObject(survivorId, out NetworkObject survivorObject))
            return false;

        survivor = survivorObject.GetComponent<Survivor>();
        if (survivor == null)
            return false;

        return (survivor.transform.position - transform.position).sqrMagnitude <= interactRange * interactRange;
    }

    // ---------- Gizmo ----------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

}
