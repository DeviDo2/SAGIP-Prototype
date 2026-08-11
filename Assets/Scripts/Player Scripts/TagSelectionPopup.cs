using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TagSelectionPopup : MonoBehaviour
{
    [SerializeField] private Button greenButton;
    [SerializeField] private Button yellowButton;
    [SerializeField] private Button redButton;

    private InteractionHandler scoutHandler;   // The Scout that invoked it

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        // Hide at start
        gameObject.SetActive(false);
        IsVisible = false;

        greenButton.onClick.AddListener(() => SelectTag(TagColor.Green));
        yellowButton.onClick.AddListener(() => SelectTag(TagColor.Yellow));
        redButton.onClick.AddListener(() => SelectTag(TagColor.Red));
    }

    public void Show(InteractionHandler handler, Vector3 worldPosition)
    {
        scoutHandler = handler;

        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition); 
        transform.position = screenPos;
        gameObject.SetActive(true);
        IsVisible = true;

    }

    public void Hide()
    {
        gameObject.SetActive(false);
        IsVisible = false;
        scoutHandler = null;
    }

    private void SelectTag(TagColor selectedColor)
    {
        if (scoutHandler != null)
        {
            scoutHandler.OnTagColorSelected(selectedColor);
        }
        Hide();
    }

}
