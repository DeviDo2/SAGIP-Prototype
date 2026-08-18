using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoleSelectionPlayerEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI confirmationText;

    public void SetData(
        string playerName,
        string role,
        bool confirmed)
    {
        playerNameText.text = playerName;
        roleText.text = role;
        confirmationText.text = confirmed ? "READY" : "-";
    }

}
