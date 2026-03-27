// ----------------------------------------------------------------------
// Copyright 2025 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes

// ----------------------------------------------------------------------
// Class to control the Info Panel in the UI by clicking the Info Button
public class InfoPanelController : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Private input variables

    [Header("• PRIVATE INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Reference to the Info Panel GameObject to toggle its active state")]
    [SerializeField] private GameObject infoPanel;

    // -----------------------------------------------------------------------------------------------------
    // Method to toggle the Info Panel by clicking the Info Button
    public void TogglePanel()
    {
        // Toggle the active state of the Info Panel GameObject
        infoPanel.SetActive(!infoPanel.activeSelf);
    }
}
