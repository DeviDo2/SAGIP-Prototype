using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;

public class ActionButton : MonoBehaviour, IPointerDownHandler
{

    public void OnPointerDown(PointerEventData eventData)
    {
        LocalPlayerSetup localPlayer = LocalPlayerSetup.Instance;

        if (localPlayer != null && localPlayer.InteractionHandler != null)
            localPlayer.InteractionHandler.PerformAction();
    }

}
