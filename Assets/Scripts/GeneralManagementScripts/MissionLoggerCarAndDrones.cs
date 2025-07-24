/*******************************************************************************
* Copyright 2025 INTRIG
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
******************************************************************************/

// Libraries
using System; // Library to use DateTime
using System.IO; // Library to use file operations
using System.Text; // Library to use StringBuilder for efficient string concatenation
using System.Collections.Generic; // Library to use List
using UnityEngine; // Library to use in MonoBehaviour classes

// Class to log mission events for cars and drones
public static class MissionLoggerCarAndDrones
{

    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // Folder path where the logs will be saved
    private static string folderPath = Path.Combine(Application.dataPath, "MissionsLogs");

    // Dictionary to hold file paths for each player
    private static Dictionary<string, string> playerFilePaths = new Dictionary<string, string>();

    // -----------------------------------------------------------------------------------------------------
    // Method for starting a new simulation log for a player

    public static void StartNewSimulationLog(string playerName)
    {

        // Check if the folder exists, if not, create it
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // Create a new file for the player with the current date and time
        string fileName = $"MissionsLog_{playerName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(folderPath, fileName);

        // Add the file path to the dictionary for the player
        playerFilePaths[playerName] = filePath;

        // Write the header to the CSV file
        StringBuilder csvData = new StringBuilder();
        csvData.AppendLine("PlayerName,CurrentTime,Latitude,Longitude,Altitude,Azimuth,BatteryLevel");
        File.WriteAllText(filePath, csvData.ToString());

    }

    // -----------------------------------------------------------------------------------------------------
    // Method for logging a mission event

    public static void LogMissionEvent(
        string playerName, string currentTime, float latitude, float longitude, float altitude,
        float azimuth, float batteryLevel)
    {

        // Try to write the log event to the player's CSV file
        try
        {

            // Check if the player has a file path, if not, start a new log
            if (!playerFilePaths.ContainsKey(playerName))
                StartNewSimulationLog(playerName);

            // Get the file path for the player and append the log event
            string filePath = playerFilePaths[playerName];

            // Create a CSV line with the log event data
            StringBuilder csvData = new StringBuilder();
            csvData.AppendLine($"{playerName},{currentTime},{latitude},{longitude},{altitude},{azimuth},{batteryLevel}");

            // Append the CSV line to the file
            File.AppendAllText(filePath, csvData.ToString());

        }
        catch (Exception e)
        {
            Debug.LogError("Error writing to CSV file: " + e.Message); // Log any errors that occur during file writing
        }

    }

}
