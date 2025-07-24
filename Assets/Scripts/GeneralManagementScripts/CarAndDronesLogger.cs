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
using UnityEngine; // Library to use in MonoBehaviour classes
using System; // Library to use DateTime for timestamps

// Class to log car and drone events
public class CarAndDronesLogger : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // Player name, used to identify the player in the logs
    public string playerName;

    // Components for drone and car dynamics
    DroneDynamics droneDynamics;
    CarDynamics carDynamics;

    // Battery level of the player, used to log battery status
    public float playerBatteryLevel;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        // Initialize the player name based on the game object name
        playerName = gameObject.name;

        // Start the mission logger for the player
        MissionLoggerCarAndDrones.StartNewSimulationLog(playerName);

        // Call LogEvent every 1 second
        InvokeRepeating(nameof(LogEvent), 1f, 1f); 

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to log the event with the current time, position, and battery level:

    void LogEvent()
    {

        // Get the current time and position of the player
        string currentTime = DateTime.Now.ToString("HH:mm:ss.fff");
        Vector3 pos = transform.position;

        // Get the latitude, longitude, altitude, azimuth, and battery level
        float latitude = pos.x;
        float longitude = pos.z;
        float altitude = pos.y;
        float azimuth = transform.eulerAngles.y;
        float batteryLevel;

        // Check if the player is a drone or a car and get the battery level accordingly
        if (playerName.Contains("DRO"))
        {
            if (droneDynamics == null)
                droneDynamics = GetComponent<DroneDynamics>();

            batteryLevel = droneDynamics.batteryLevel;
        }
        else if (playerName.Contains("CAR"))
        {
            if (carDynamics == null)
                carDynamics = GetComponent<CarDynamics>();

            batteryLevel = carDynamics.batteryLevel;
        }
        else
        {
            batteryLevel = 100f;
        }

        // Log the mission event with the collected data
        MissionLoggerCarAndDrones.LogMissionEvent(
            playerName, currentTime, latitude, longitude, altitude,
            azimuth, batteryLevel
        );

    }

}
