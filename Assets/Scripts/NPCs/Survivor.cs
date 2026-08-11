using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TagColor { None, Green, Yellow, Red }
public class Survivor : MonoBehaviour
{
    [Header("Survivor Properties")]
    [Header("Stability")]
    [SerializeField] public float maxStability = 100f;
    public float currentStability { get; private set; }
    public float initialStability { get; private set; } //for scoring purposes

    [Header("Tags and Status")]
    public TagColor tagColor = TagColor.None;
    public bool isRescued = false;
    public bool isStable = false;

    [Header("Carry")]
    public Transform carryPoint; // where the survivor snaps to when being carried
    public GameObject carrier = null; // reference to the player carrying the survivor

    // Called by the Rescuer when they press Action near this survivor
    // Returns true if an action was performed
    public bool InteractRescuer()
    {
        if (isRescued || isStable) return false;

        switch (tagColor)
        {
            case TagColor.Green:
                // Talk – survivor walks to safe zone themselves (placeholder)
                Debug.Log($"{name} (Green) thanks you and heads to safety!");
                // They are immediately considered rescued (since they leave on their own).
                isRescued = true;
                GameManager.Instance.OnSurvivorRescued(this);
                return true;

            case TagColor.Yellow:
                if (carrier == null)
                {
                    // Pickup – this is handled by InteractionHandler calling PickupSurvivor
                    // So from here we don't do anything; the handler will check.
                    return false;
                }
                else
                {
                    // Drop is handled separately – not through this method.
                    return false;
                }

            case TagColor.Red:
                // Cannot do anything
                Debug.Log($"{name} (Red) is beyond help...");
                return false;

            default:
                return false;

        }
    }

    // Heal a single chunk of stability (Medic press). Only works on Yellow survivors.
    public void HealChunk(float amount)
    {
        if (tagColor != TagColor.Yellow || isStable)
        {
            Debug.Log($"{name} cannot be healed (tag={tagColor}, stabilised={isStable})");
            return;
        }

        float oldStability = currentStability;
        currentStability = Mathf.Min(currentStability + amount, maxStability);
        Debug.Log($"{name} healed: {oldStability:F1} -> {currentStability:F1}");

        if (currentStability >= maxStability)
        {
            currentStability = maxStability;
            isStable = true;
            GameManager.Instance.OnSurvivorStabilised(this);
            Debug.Log($"{name} fully stabilised!");
        }
    }


    // TO BE REWORKED - Call this ever frame while the Medic is healing the survivor
    /* public void Heal(float amount)
    {
        if (isStable) return; // No need to heal if already stable
        currentStability = Mathf.Min(currentStability + amount, maxStability);
        if (currentStability >= maxStability)
        {
            currentStability = maxStability;
            isStable = true;
            //GameManager.Instance.OnSurvivorStabilized(this); // Notify the GameManager that this survivor is stabilized
        }
    } */

    // Returns the correct tag based on the initial injury severity
    public TagColor GetCorrectTag()
    {
        if (initialStability >= 75f) return TagColor.Green; // mildly injured
        else if (initialStability >= 50f) return TagColor.Yellow; // moderately injured
        else return TagColor.Red; // critically injured
    }

}
