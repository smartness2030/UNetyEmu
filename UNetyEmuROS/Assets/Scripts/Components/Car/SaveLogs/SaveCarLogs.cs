// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute
using System.Collections.Generic; // To use List<T>

// ----------------------------------------------------------------------
// Class to save logs of the car's state and energy consumption during the simulation
public class SaveCarLogs : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("List of variable names to save in the logs, which should match the order of the variable values passed to the LogEvent method")]
    public List<string> variablesToSave = new List<string>
    {
        "PlayerName",
        "CurrentTime",
        "Latitude",
        "Longitude",
        "Altitude"
    };

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Name of the folder where the logs will be saved")]
    public string outputFolderName;

    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the SetLogs class to handle the logging of events
    private SetLogs logger;
    
    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Initialize the SetLogs class with the list of variable names to save in the logs
        logger = new SetLogs(variablesToSave);

        // Start a new simulation log for this car, which will create a new file with a unique name based on the current date and time
        logger.StartNewSimulationLog(gameObject.name);
        
        // Show the name of the output folder in the Inspector
        outputFolderName = logger.currentFilePath;

        // Invoke the LogStorage method every N seconds to log the current state of the car
        InvokeRepeating(nameof(LogStorage), 0f, 1f);
    }

    // ----------------------------------------------------------------------
    // Method to log the current state of the car and its energy consumption
    void LogStorage()
    {
        // Get the name of the game object
        string playerName = gameObject.name;
        
        // Get the current time in a readable format (e.g., "hh:mm:ss")
        float elapsedSeconds = Time.time;
        TimeSpan timeSpan = TimeSpan.FromSeconds(elapsedSeconds);
        string currentTime = timeSpan.ToString(@"hh\:mm\:ss");

        // Get the current latitude, longitude, altitude and azimuth of the car
        float latitude = transform.position.x;
        float longitude = transform.position.z;
        float altitude = transform.position.y;

        // Create a list of variable values to log, which should match the order of the variable names defined in the variablesToSave list
        List<object> variableValues = new List<object>
        {
            playerName,
            currentTime,
            latitude,
            longitude,
            altitude
        };

        // Log the event with the variable values, which will save them to the current log file in a structured format
        logger.LogEvent(variableValues);
    }
}
