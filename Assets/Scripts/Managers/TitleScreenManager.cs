using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private GameObject titlePanel;    // panel with Play/Options buttons
    [SerializeField] private NameEntryManager nameEntryManager;  // drag in Inspector
    [SerializeField] private GameObject lobbyPanel;   // the panel to show after pressing Play

    void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        // hide panels we don't need yet
        if (nameEntryManager != null)
            nameEntryManager.gameObject.SetActive(false);
        lobbyPanel.SetActive(false);
    }

    void OnPlayClicked()
    {
        titlePanel.SetActive(false);   // hide the main menu buttons
        nameEntryManager.Show();
    }

    // Call this from a Back button inside the Lobby Panel to return to main menu
    public void ShowTitleScreen()
    {
        lobbyPanel.SetActive(false);
        titlePanel.SetActive(true);
    }

}
