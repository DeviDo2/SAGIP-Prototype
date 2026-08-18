using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NameEntryManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private GameObject nameEntryPanel;
    [SerializeField] private GameObject lobbyPanel;   // the panel that appears after name entry

    
    public void Show()
    {
        nameEntryPanel.SetActive(true);
        nameInput.text = PlayerData.LocalPlayerName; // pre-fill if already set
    }

    public void OnConfirmClicked()
    {
        Debug.Log("Confirm button pressed");

        string input = nameInput.text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            Debug.LogWarning("Name input is empty");
            return;
        }

        PlayerData.LocalPlayerName = input;
        nameEntryPanel.SetActive(false);

        // Show the lobby choice panel
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            Debug.Log("Lobby panel activated");
        }
        else
        {
            Debug.LogError("lobbyPanel is not assigned in the Inspector!");
        }
    }

}
