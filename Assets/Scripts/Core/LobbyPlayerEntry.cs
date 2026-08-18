using UnityEngine;
using TMPro;

public class LobbyPlayerEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI readyText;

    public void SetData(string playerName, string role, bool isReady)
    {
        nameText.text = playerName;
        roleText.text = role;
        readyText.text = isReady ? "Ready" : "Not Ready";
    }

}
