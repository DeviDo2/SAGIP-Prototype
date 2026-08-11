using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 2.5f; // Range within which the player can interact
    [SerializeField] private LayerMask survivorLayer; // Layer for survivors
    [SerializeField] private float healRate = 25f; // Stability healed per second by Medic

    [Header("Tag Popup for Scout")]
    [SerializeField] private TagSelectionPopup tagPopup; // Reference to the popup panel

    [Header("Rescuer Carry and Drop")]
    [SerializeField] private Transform rescuerCarryPoint;   // drag the CarryPoint child here
    [SerializeField] private Transform rescuerDropPoint;   // drag the DropPoint here

    private bool isActive = false; // Set by GameManager to determine if this player can interact
    private Survivor currentTarget = null; // The survivor currently in range for interaction

    private Rigidbody rb; // Used for Rescuer carry offset

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// Called by GameManager to set whether this player can interact
    
    public void SetActive(bool state)
    {
        isActive = state;
    }

    private void Update()
    {
        if (!isActive) return;

        // Reset current target each frame
        currentTarget = null;

        // Check for survivors in range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactRange, survivorLayer);
        float closestDistance = Mathf.Infinity;
        foreach (Collider col in hitColliders)
        {
            Survivor survivor = col.GetComponent<Survivor>();
            if (survivor == null) continue; // Skip collidersif no Survivor component found

            float distance = Vector3.Distance(transform.position, col.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = survivor;
            }
        }
    }

    /// Call this every frame while the ACTION button is held down. The behavior depends on the current player role.
    public void PerformAction()
    {
        if (!isActive || currentTarget == null) return;

        switch (GameManager.Instance.currentRole)
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

    // Handle the Scout's interaction: show the tag selection popup
    private void HandleScout()
    {
        if (currentTarget.isRescued) return; // No need to tag rescued survivors

        Debug.Log($"tagPopup = {tagPopup}, IsVisible = {tagPopup?.IsVisible}, currentTarget = {currentTarget}");

        // Show the tag selection popup
        if (tagPopup != null && !tagPopup.IsVisible)
        {
            tagPopup.Show(this, currentTarget.transform.position);
        }
    }

    public void OnTagColorSelected(TagColor selectedColor)
    {
        if (currentTarget != null && !currentTarget.isRescued)
        {
            currentTarget.tagColor = selectedColor;

            // Visual feedback for tagging can be added here (e.g., change survivor's color, play sound, etc.)
            Debug.Log($"{currentTarget.name} tagged {selectedColor}");
        }
        if (tagPopup != null) tagPopup.Hide();
    }

    // Handle the Rescuer's interaction: pick up or drop a survivor
    private void HandleRescuer()
    {
        if (currentTarget.isRescued || currentTarget.isStable) return; // Already rescued, no need to pick up again

        // If already carrying THIS survivor, drop them
        if (currentTarget.carrier == gameObject)
        {
            TryDrop();
            return;
        }

        // If carrying someone else, ignore (you must drop them first)
        if (IsCarryingAnyone())
            return;

        // Try to interact with the survivor based on tag
        bool used = currentTarget.InteractRescuer();
        if (!used && currentTarget.tagColor == TagColor.Yellow && currentTarget.carrier == null)
        {
            // Only allow pickup for Yellow survivors
            PickupSurvivor(currentTarget);
        }

    }

    // Separate logic for dropping a survivor when the action button is released
    public void TryDrop()
    {
        if (!isActive || GameManager.Instance.currentRole != GameManager.PlayerRole.Rescuer) return;

        // Find the survivor being carried by this Rescuer
        Survivor carriedSurvivor = GetCarriedSurvivor();
        if (carriedSurvivor != null)
        {
            DropSurvivor(carriedSurvivor);
        }
    }

    private bool IsCarryingAnyone()
    {
        return GetCarriedSurvivor() != null;
    }

    private Survivor GetCarriedSurvivor()
    {
        // Check all survivors in the scene to see if any are being carried by this Rescuer
        Survivor[] all = FindObjectsOfType<Survivor>();
        foreach (Survivor survivor in all)
        {
            if (survivor.carrier == gameObject)
            {
                return survivor;
            }
        }
        return null;
    }

    private void PickupSurvivor(Survivor survivor)
    {
        survivor.carrier = gameObject;

        // Disable collider to prevent bumping into things
        /*
        Collider survivorCol = survivor.GetComponent<Collider>();
        if (survivorCol != null)
            survivorCol.enabled = false;
        */

        survivor.transform.SetParent(rescuerCarryPoint);   // parent to the carry point, not the root
        survivor.transform.localPosition = Vector3.zero;    // snap exactly to the carry point
        survivor.transform.localRotation = Quaternion.identity;

        // Make kinematic so it follows the carrier without physics forces
        Rigidbody survivorRb = survivor.GetComponent<Rigidbody>();
        if (survivorRb != null)
        {
            survivorRb.isKinematic = true;
        }

        
        Debug.Log($"Picked up {survivor.name}");
    }
    
    private void DropSurvivor(Survivor survivor)
    {
        survivor.carrier = null;

        // Save the desired world position (drop point)
        Vector3 dropPosition = rescuerDropPoint.position;

        // Detach from Rescuer
        survivor.transform.SetParent(null);

        // Move to drop point
        survivor.transform.position = dropPosition;
        survivor.transform.rotation = rescuerDropPoint.rotation;

        // Re-enable collider
        /*
        Collider survivorCol = survivor.GetComponent<Collider>();
        if (survivorCol != null)
            survivorCol.enabled = true;
        */

        // Allow physics(gravity) to take over again
        Rigidbody survivorRb = survivor.GetComponent<Rigidbody>();
        if (survivorRb != null)
        {
            survivorRb.isKinematic = false;

            // Use AddForce with VelocityChange mode to forcibly reset inherited motion to zero.
            survivorRb.AddForce(-survivorRb.velocity, ForceMode.VelocityChange);
            survivorRb.AddTorque(-survivorRb.angularVelocity, ForceMode.VelocityChange);

        }

        // Check if the survivor is within a safe zone to mark as rescued
        if (IsInsideSafeZone(survivor.transform.position))
        {
            survivor.isRescued = true;
            GameManager.Instance.OnSurvivorRescued(survivor);

            // Make the survivor kinematic again so they can't be pushed
            if (survivorRb != null)
                survivorRb.isKinematic = true;

            Debug.Log($"{survivor.name} has been rescued!");
        }
    }

    // Handle the Medic's interaction: heal the survivor over time
    private void HandleMedic()
    {
        Debug.Log($"Medic trying to heal {currentTarget?.name}, rescued={currentTarget?.isRescued}, stable={currentTarget?.isStable}");

        if (!currentTarget.isRescued || currentTarget.isStable) return;
        currentTarget.HealChunk(healRate); // healRate now serves as the chunk amount (e.g., 25)
    }

    // Helpers
    private bool IsInsideSafeZone(Vector3 position)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, 0.5f, LayerMask.GetMask("SafeZone")); // Small radius to check for safe zone
        return hitColliders.Length > 0;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the interaction range in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

}
