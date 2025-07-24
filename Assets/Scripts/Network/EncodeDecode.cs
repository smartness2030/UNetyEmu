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
using System; // Library to use the Environment class
using UnityEngine; // Library to use the MonoBehaviour class
using Newtonsoft.Json.Linq; // Library to use the JObject and JArray classes
using System.Linq; // Library to use LINQ methods like Concat

// Class to encode and decode JSON messages between Unity and Mininet-WiFi
public class EncodeDecode : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // Class to get the position of the objects
    [Serializable] public class Position
    {
        public float x;
        public float y;
        public float z;

        public Position(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    // Class to store the base station and vehicle positions
    [Serializable] public class BaseStationData
    {
        public Position baseStationPosition;
        public Position[] vehicles;

        public BaseStationData(Position baseStationPosition, Position[] vehicles)
        {
            this.baseStationPosition = baseStationPosition;
            this.vehicles = vehicles;
        }
    }

    // Class to store the coverage radius
    [Serializable] public class CoverageData
    {
        public float coverageRadius;
    }

    // -----------------------------------------------------------------------------------------------------
    // Static class to encode the position and coverage radius as a JSON message

    public static string EncodePositionAndCoverageRadiusMessageOnce(Vector3 baseStationPosition, string label)
    {

        // Create the JSON object
        JObject jsonData = new JObject();
        jsonData["label"] = label;  // Add the label to the message
        JObject baseStationJson = new JObject(); // Add base station
        JObject baseStationPositionJson = new JObject(); // Add base station position

        // Add the base station position
        baseStationPositionJson["x"] = baseStationPosition.x;
        baseStationPositionJson["y"] = baseStationPosition.y;
        baseStationPositionJson["z"] = baseStationPosition.z;

        // Add the position object and ID to the base station JSON
        baseStationJson["position"] = baseStationPositionJson;
        baseStationJson["id"] = "ap1";  // Example ID for the base station

        // Add base station data to the root JSON object
        jsonData["baseStation"] = baseStationJson;

        // Create an array for vehicles
        JArray vehicleArray = new JArray();

        // Get all the vehicles in the scene
        GameObject[] droneObjects = GameObject.FindGameObjectsWithTag("Drone");
        GameObject[] carObjects = GameObject.FindGameObjectsWithTag("Car");

        // Combine the drone and car objects into a single array
        GameObject[] vehicleObjects = droneObjects.Concat(carObjects).ToArray();

        // For each vehicle, add its position and ID to the JSON object
        foreach (GameObject vehicle in vehicleObjects)
        {

            // Get the vehicle's position
            Vector3 vehiclePosition = vehicle.transform.position;

            // Create a JSON object for the vehicle
            JObject vehicleJson = new JObject();
            JObject positionJson = new JObject();

            // Add the vehicle's position and ID
            positionJson["x"] = vehiclePosition.x;
            positionJson["y"] = vehiclePosition.y + 0.1f; // Adjust because of the center of mass of the cars
            positionJson["z"] = vehiclePosition.z;

            // Add the position object and ID to the vehicle JSON
            vehicleJson["position"] = positionJson;
            vehicleJson["id"] = vehicle.name;  // Use the vehicle's name or other unique identifier as the ID

            // Add the vehicle JSON object to the array
            vehicleArray.Add(vehicleJson);

        }

        // Add the vehicle array to the root JSON object
        jsonData["vehicles"] = vehicleArray;

        // Convert the JSON object to a string
        string jsonMessage = jsonData.ToString();

        // Return the JSON message
        return jsonMessage;

    }

    // -----------------------------------------------------------------------------------------------------
    // Static class to encode the base station and vehicle positions as a JSON message

    public static string EncodeBaseStationMessage(Vector3 baseStationPosition, string label)
    {

        // Create the JSON object
        JObject jsonData = new JObject(); // Create the root JSON object
        jsonData["label"] = label;  // Add the label to the message

        // Create a JSON object for the base station
        JObject baseStationJson = new JObject();
        JObject baseStationPositionJson = new JObject();

        // Add the base station position
        baseStationPositionJson["x"] = baseStationPosition.x;
        baseStationPositionJson["y"] = baseStationPosition.y;
        baseStationPositionJson["z"] = baseStationPosition.z;

        // Add the position object and ID to the base station JSON
        baseStationJson["position"] = baseStationPositionJson;
        baseStationJson["id"] = "ap1";  // Example ID for the base station

        // Add base station data to the root JSON object
        jsonData["baseStation"] = baseStationJson;

        // Convert the JSON object to a string
        string jsonMessage = jsonData.ToString();

        // Return the JSON message
        return jsonMessage;

    }

    // -----------------------------------------------------------------------------------------------------
    // Static class to encode the vehicle positions as a JSON message:

    public static string EncodeSingleVehiclePositionWithLabelAndID(GameObject vehicle, string label)
    {

        // Create the JSON object
        JObject jsonData = new JObject();
        jsonData["label"] = label;  // Add the label to the message

        // Add vehicle data
        JObject vehicleJson = new JObject();
        JObject positionJson = new JObject();

        // Get the vehicle's position
        Vector3 vehiclePosition = vehicle.transform.position;
        positionJson["x"] = vehiclePosition.x;
        positionJson["y"] = vehiclePosition.y + 0.1f; // Adjust because of the center of mass of the cars
        positionJson["z"] = vehiclePosition.z;

        // Add the position object and ID to the vehicle JSON
        vehicleJson["position"] = positionJson;
        vehicleJson["id"] = vehicle.name;  // Use the vehicle's name as the IDct
        jsonData["vehicle"] = vehicleJson;

        // Convert the JSON object to a string
        string jsonMessage = jsonData.ToString();

        // Return the JSON message
        return jsonMessage;

    }

}
