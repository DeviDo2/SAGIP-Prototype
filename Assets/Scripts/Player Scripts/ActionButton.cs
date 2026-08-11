using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActionButton : MonoBehaviour, IPointerDownHandler
{
    
    public void OnPointerDown(PointerEventData eventData)
    {
        // Get the active player’s InteractionHandler
        InteractionHandler handler = GetActiveInteractionHandler();
        if (handler != null)
            handler.PerformAction();

    }

    private InteractionHandler GetActiveInteractionHandler()
    {
        // Access the GameManager to get the active player's InteractionHandler
        if (GameManager.Instance == null) return null;
        int idx = (int)GameManager.Instance.currentRole;
        if (idx >= 0 && idx < GameManager.Instance.interactionHandlers.Length)
            return GameManager.Instance.interactionHandlers[idx];
        return null;
    }

}
