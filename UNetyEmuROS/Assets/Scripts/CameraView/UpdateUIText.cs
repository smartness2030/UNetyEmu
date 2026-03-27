// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine;
using UnityEngine.UI;
using System;

// ----------------------------------------------------------------------
// Class to update the player name and elapsed time in the UI elements
// Requires a FollowCamera component to get the player name
[RequireComponent(typeof(FollowCamera))]
public class UpdateUIText : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("UI Text element to display the elapsed time")]
    public Text timeText;

    [Tooltip("UI Text element to display the player name")]
    public Text playerText;    

    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the FollowCamera component to get the player name
    private FollowCamera followCamera;

    // Variable to store the player name obtained from the FollowCamera component
    private string playerName;

    // Variable to store the elapsed time as a string in the format "hh:mm:ss"
    private string timeString;

    // Variable to keep track of the elapsed time in seconds
    private float elapsedTime;

    // Variable to store the elapsed time as a TimeSpan object for formatting
    private TimeSpan timeSpan;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Initialize the elapsed time to 0 seconds
        elapsedTime = 0f;

        // Initialize the timeString to "00:00:00" to display at the start
        timeString = "00:00:00";

        // Get the FollowCamera component attached to the same GameObject to access the player name
        followCamera = GetComponent<FollowCamera>();

        // Initialize the playerName to an empty string at the start
        playerName = "";
    }

    // ----------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // Increment the elapsed time by the time that has passed since the last frame
        elapsedTime += Time.deltaTime;

        // Convert the elapsed time in seconds to a TimeSpan object for easier formatting
        timeSpan = TimeSpan.FromSeconds(elapsedTime);

        // Format the TimeSpan object into a string in the format "hh:mm:ss" to display in the UI
        timeString = timeSpan.ToString(@"hh\:mm\:ss");

        // If the timeText is not null, update the time string in the UI element to show the elapsed time
        if (timeText != null)
        {
            timeText.text = timeString;
        }

        // If the followCamera is not null, get the player name from the FollowCamera component to display in the UI
        if (followCamera != null)
        {
            playerName = followCamera.playerName;
        }

        // If the playerText and currentStatusText are not null, update the player name and current status in the UI elements
        if ((playerText != null) && (playerName != null))
        {
            playerText.text = playerName;
        }
    }
}
