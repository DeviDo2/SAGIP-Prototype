using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoleSelectionPanel : MonoBehaviour
{
    public void OnScoutSelected()
    {
        SessionData.SelectedRole = GameManager.PlayerRole.Scout;
        SceneManager.LoadScene("Gameplay");
    }

    public void OnRescuerSelected()
    {
        SessionData.SelectedRole = GameManager.PlayerRole.Rescuer;
        SceneManager.LoadScene("Gameplay");
    }

    public void OnMedicSelected()
    {
        SessionData.SelectedRole = GameManager.PlayerRole.Medic;
        SceneManager.LoadScene("Gameplay");
    }

}
