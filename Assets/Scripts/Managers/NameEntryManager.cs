using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NameEntryManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private GameObject nameEntryPanel;
    [SerializeField] private GameObject lobbyPanel;   // the panel that appears after name entry

    void Start()
    {
        nameEntryPanel.SetActive(false);   // ensure hidden on start
        // (MainMenuManager will activate it)
    }
    
    public void Show()
    {
        nameEntryPanel.SetActive(true);
        nameInput.text = PlayerData.LocalPlayerName; // pre-fill if already set
    }

    public void OnConfirmClicked()
    {
        string input = nameInput.text.Trim();
        if (string.IsNullOrEmpty(input))
            return; // maybe show a warning

        PlayerData.LocalPlayerName = input;
        nameEntryPanel.SetActive(false);

        // Show the lobby choice panel (host/join)
        lobbyPanel.SetActive(true);
    }

}
