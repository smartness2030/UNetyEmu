// ----------------------------------------------------------------------
// Copyright 2025 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use Application.dataPath for file paths
using System; // To use Serializable attribute
using System.IO; // To use file operations 
using System.Text; // To use StringBuilder for efficient string concatenation
using System.Collections.Generic; // To use List<T>

// ----------------------------------------------------------------------
// Class to save new events to a CSV file
public class SetLogs
{
    // ----------------------------------------------------------------------
    // Public input variables

    // This will store the file path for the current simulation CSV
    public string currentFilePath;
    
    // ----------------------------------------------------------------------
    // Private variables

    // Create the folder path where the CSV files will be saved
    private string folderPath;

    // List of headers for the CSV file, which will be set when creating a new SetLogs instance
    private List<string> headers;

    // ----------------------------------------------------------------------
    // Constructor to initialize the SetLogs instance with the specified headers for the CSV file
    public SetLogs(List<string> headers)
    {
        this.headers = headers;
        folderPath = Path.Combine(Application.dataPath, "ExportedLogs");
    }

    // ----------------------------------------------------------------------
    // Method to start a new simulation log by creating a new CSV file with a unique name based on the current date and time
    public void StartNewSimulationLog(string playerName)
    {
        // Ensure the Logs folder exists
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // Create a new unique file name using the current date and time
        string fileName = "Logs_" + playerName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        currentFilePath = Path.Combine(folderPath, fileName);

        // Create a StringBuilder to store CSV data
        StringBuilder csvData = new StringBuilder();

        // Add the header row to the CSV file if it's the first time creating it
        csvData.AppendLine(string.Join(",", headers));

        // Write the header to the new CSV file
        File.WriteAllText(currentFilePath, csvData.ToString());
    }

    // ----------------------------------------------------------------------
    // Method to log a new event by appending a new row to the existing CSV file with the provided values for each header
    public void LogEvent(List<object> values)
    {
        // Try to write the event data to the CSV file
        try
        {
            // Check if the current simulation file exists
            if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
                return;

            // Convert all values to strings for CSV formatting
            List<string> stringValues = values.ConvertAll(v => v.ToString());

            // Create a StringBuilder to append data for this event
            StringBuilder csvData = new StringBuilder();

            // Append the event data as a new row in CSV format
            csvData.AppendLine(string.Join(",", stringValues));

            // Append the new row to the existing CSV file
            File.AppendAllText(currentFilePath, csvData.ToString());
        }
        catch (Exception e)
        {
            // Log any errors that occur while writing to the file
            Debug.LogError("Error writing to CSV file: " + e.Message);
        }
    }
}
