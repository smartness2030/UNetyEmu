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
using System; // Library to use serializable classes
using UnityEngine; // Unity Engine library to use in MonoBehaviour classes
using Newtonsoft.Json.Linq; // Library to use JSON objects
using System.Collections.Generic; // Library to use in List and Dictionary classes
using System.Linq; // Library to use in OrderBy method

// Class to generate a random mission JSON using the DronePadCustomer objects in the scene
public class MissionGenerator : MonoBehaviour
{
    
    // -----------------------------------------------------------------------------------------------------
    // Class to represent location details:

    [Serializable] public class Location
    {
        
        // Variables of this class
        public float latitude; // Unity z-axis
        public float longitude; // Unity x-axis
        public float altitude; // Unity y-axis
        public float azimuth; // Orientation angle

        // Constructor of this class
        public Location(float latitude, float longitude, float altitude, float azimuth)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.altitude = altitude;
            this.azimuth = azimuth;
        }

    }
    
    // -----------------------------------------------------------------------------------------------------
    // Class to store mission details:

    [Serializable] public class NewMission
    {
        
        // Variables of this class
        public string missionId;
        public string arrivalDateTime;
        public Location deliveryLocation;
        public float packageWeight;
        public int priority;

        // Constructor of this class
        public NewMission(string missionId, string arrivalDateTime, Location deliveryLocation, float packageWeight, int priority)
        {
            this.missionId = missionId;
            this.arrivalDateTime = arrivalDateTime;
            this.deliveryLocation = deliveryLocation;
            this.packageWeight = packageWeight;
            this.priority = priority;
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to generate a mission JSON:

    public static string GenerateMission(List<String> DronePadCustomersList, bool randomPickupAndDelivery)
    {
        
        // Generate a missionId (UUID format)
        string missionId = Guid.NewGuid().ToString();

        // Generate the arrival date and time
        DateTime now = DateTime.Now;
        string arrivalDateTime = now.ToString("yyyy-MM-dd_HH-mm-ss");

        // Initialize the delivery location variables
        Location deliveryLocation = new Location(0f, 0f, 0f, 0f);

        // Find all DronePadCustomer objects in the scene
        GameObject[] dronePadsCustomer = GameObject.FindGameObjectsWithTag("DronePadCustomer");

        // Sort the drone pads by name
        dronePadsCustomer = dronePadsCustomer.OrderBy(obj => obj.name).ToArray();

        // Initialize the index list
        List<int> indexList = new List<int>();

        //  Initialize the index variable
        int index;

        // Initialize the game object variable to store the selected DronePadCustomer object
        GameObject selectedDronePadCustomer;

        // Initialize the countIndex variable
        int countIndex = 0;

        // Generate a DronePadCustomer object
        do
        {
            
            // Get the DronePadCustomer object at an index
            if(randomPickupAndDelivery) index = UnityEngine.Random.Range(0, dronePadsCustomer.Length);
            else index = countIndex;

            // Get the DronePadCustomer object
            selectedDronePadCustomer = dronePadsCustomer[index]; 

            Debug.Log(missionId + selectedDronePadCustomer.name);

            // Check if the index is already in the list
            if (indexList.Contains(index)){
                countIndex++;
                continue;
            } 

            // Add the index to the list
            indexList.Add(index);

        } while (DronePadCustomersList.Contains(selectedDronePadCustomer.name) && indexList.Count < dronePadsCustomer.Length);

        // Add the DronePadCustomer object to the list
        DronePadCustomersList.Add(selectedDronePadCustomer.name);

        // Check if there are DronePadCustomer objects in the scene
        if (dronePadsCustomer.Length == 0) 
        {
            Debug.LogError("No DronePadCustomer objects found in the scene. It is not possible to generate a mission.");
        }
        else
        {
            
            // Set the delivery location to the DronePadCustomer object
            deliveryLocation = new Location(
                selectedDronePadCustomer.transform.position.z,
                selectedDronePadCustomer.transform.position.x,
                selectedDronePadCustomer.transform.position.y,
                selectedDronePadCustomer.transform.eulerAngles.y
            );

        }
        
        // Generate random package weight (just for testing purposes)
        float packageWeight = UnityEngine.Random.Range(0.1f, 5.0f);

        // Generate a random priority (just for testing purposes)
        int priority = UnityEngine.Random.Range(0, 3);

        // Create a new mission object with the generated details
        NewMission newMission = new NewMission(missionId, arrivalDateTime, deliveryLocation, packageWeight, priority);

        // Return the EncodeMessageMissionGenerator method to generate the JSON string
        return EncodeMessageMissionGenerator(newMission);

    }

    // -----------------------------------------------------------------------------------------------------
    // Function to encode the mission details into a JSON string:

    public static string EncodeMessageMissionGenerator(NewMission newMission)
    {
        return JObject.FromObject(newMission).ToString();
    }

    // -----------------------------------------------------------------------------------------------------
    // Function to decode the mission details from a JSON string:
    
    public static NewMission DecodeMessageMissionGenerator(string jsonMessage)
    {
        
        // Check if the JSON message is empty or null
        if (string.IsNullOrEmpty(jsonMessage))
        {
            Debug.LogError("Mission is empty or null.");
            return null;
        }

        // Try to parse the JSON message
        try
        {
            return JObject.Parse(jsonMessage).ToObject<NewMission>();
        }
        catch (Exception e)
        {
            Debug.LogError("Error decoding JSON message: " + e.Message); // Log the error message if an exception occurs
            return null;
        }
        
    }
    
}
