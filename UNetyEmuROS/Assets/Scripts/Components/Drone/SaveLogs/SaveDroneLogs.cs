// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute
using System.Collections.Generic; // To use List<T>

// ----------------------------------------------------------------------
// Class to save logs of the drone's state and energy consumption during the simulation
// Requires a DroneEnergyConsumption component to read the battery level
[RequireComponent(typeof(DroneEnergyConsumption))]
public class SaveDroneLogs : MonoBehaviour
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
        "Altitude",
        "Azimuth",
        "BatteryLevel"
    };

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Name of the folder where the logs will be saved")]
    public string outputFolderName;

    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the DroneEnergyConsumption component
    private DroneEnergyConsumption droneEnergyConsumption;

    // Reference to the SetLogs class to handle the logging of events
    private SetLogs logger;
    
    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get reference to the DroneEnergyConsumption component on the same GameObject
        droneEnergyConsumption = GetComponent<DroneEnergyConsumption>();

        // Initialize the SetLogs class with the list of variable names to save in the logs
        logger = new SetLogs(variablesToSave);

        // Start a new simulation log for this drone, which will create a new file with a unique name based on the current date and time
        logger.StartNewSimulationLog(gameObject.name);
        
        // Show the name of the output folder in the Inspector
        outputFolderName = logger.currentFilePath;

        // Invoke the LogStorage method every N seconds to log the current state of the drone
        InvokeRepeating(nameof(LogStorage), 0f, 1f);
    }

    // ----------------------------------------------------------------------
    // Method to log the current state of the drone and its energy consumption
    void LogStorage()
    {
        // Get the name of the game object
        string playerName = gameObject.name;
        
        // Get the current time in a readable format (e.g., "hh:mm:ss")
        float elapsedSeconds = Time.time;
        TimeSpan timeSpan = TimeSpan.FromSeconds(elapsedSeconds);
        string currentTime = timeSpan.ToString(@"hh\:mm\:ss");

        // Get the current latitude, longitude, altitude and azimuth of the drone
        float latitude = transform.position.x;
        float longitude = transform.position.z;
        float altitude = transform.position.y;
        
        // Get the current azimuth (yaw) of the drone in degrees, which is the rotation around the vertical axis
        float azimuth = transform.localEulerAngles.y;

        // Get the current battery level of the drone as a percentage
        float batteryLevel = droneEnergyConsumption.batteryRemainingPercent;

        // Create a list of variable values to log, which should match the order of the variable names defined in the variablesToSave list
        List<object> variableValues = new List<object>
        {
            playerName,
            currentTime,
            latitude,
            longitude,
            altitude,
            azimuth,
            batteryLevel
        };

        // Log the event with the variable values, which will save them to the current log file in a structured format
        logger.LogEvent(variableValues);
    }
}
