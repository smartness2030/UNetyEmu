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
using System.Collections.Generic; // Library to use generic collections
using UnityEngine; // Library to use in MonoBehaviour classes

// Class to manage the logistic center with vehicles
public class LogisticCenterWithVehicles : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // List to store the names of the vehicles that will be used in the simulation
    public List<string> vehicleNames = new List<string>{
        "DRO001A",
        "DRO002B",
        "CAR003C"
    };

    // List to store the names of the hubs for the vehicles
    public List<string> hubNames = new List<string>{
        "prefabGroupA_1",
        "prefabGroupB_1",
        "prefabGroupC_1"
    };

    // List to store the names of the package pads
    public List<string> packagePadNames = new List<string>{
        "prefabPadPackage_1",
        "prefabPadPackage_2",
        "prefabPadPackage_1"
    };

    // List to store the names of the prefab packages
    public List<string> prefabPackageNames = new List<string>{
        "prefabPackage1",
        "prefabPackage2",
        "null"
    };

    // List to store the names of the customers
    public List<string> customerNames = new List<string>{
        "prefabCustomer_1",
        "prefabCustomer_2",
        "prefabCustomer_1"
    };

    // List to store the names of the pad trucks
    public List<string> padTruckNames = new List<string>{
        "prefabPadTruck_003",
        "null",
        "null"
    };

    // List to store the tags of the objects that will be used in the simulation
    public List<string> tagObjects = new List<string>{
        "VehiclePadStart",
        "DronePadPackage",
        "DronePadCustomer",
        "DronePadTruck"
    };

    // Get the BaseStationMininetWifi script to get the flag from MininetWifi
    public BaseStationMininetWifi baseStationMininetWifi;

    // Variable to store the base station game object name
    public string baseStationGameObjectName = "BaseStation";

    // Flag to indicate if a message has been received
    public bool flagConnectionEstablished = false; 

    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // Game object to store the packages
    private GameObject packages;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        // Get the BaseStationMininetWifi script
        baseStationMininetWifi = GameObject.Find(baseStationGameObjectName).GetComponent<BaseStationMininetWifi>();

        // Initialize the game object to store the packages
        packages = GameObject.Find("Packages");

    }

    // -----------------------------------------------------------------------------------------------------
    // FixedUpdate is called at a fixed interval:

    void FixedUpdate()
    {

        // Check if the flag from MininetWifi is true
        if (baseStationMininetWifi.flagMininetWifiReady && (!flagConnectionEstablished))
        {

            // Create the information for the vehicles
            createInfoVehicles();

            // Set the flag to true to indicate that the connection has been established
            flagConnectionEstablished = true; 

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to create the information of the vehicles:

    void createInfoVehicles()
    {

        // Count the number of elements in the lists
        int count = Mathf.Min(hubNames.Count, packagePadNames.Count, customerNames.Count, padTruckNames.Count);

        // Loop through each vehicle and set its information
        for (int i = 0; i < count; i++)
        {

            // Find the game objects in the scene by their names and tags
            GameObject vehicle = FindInSceneByName(vehicleNames[i], null);
            GameObject hub = FindInSceneByName(hubNames[i], tagObjects[0]);
            GameObject packagePad = FindInSceneByName(packagePadNames[i], tagObjects[1]);
            GameObject customer = FindInSceneByName(customerNames[i], tagObjects[2]);
            GameObject padTruck = (padTruckNames[i] != "null") ? FindInSceneByName(padTruckNames[i], tagObjects[3]) : null;

            // Get the hub position and orientation
            Vector3 hubPosition = hub.transform.position;
            float hubRotation = hub.transform.eulerAngles.y;
            string dataHub = "hub:" + hubPosition.x + ";" + hubPosition.y + ";" + hubPosition.z + ";" + hubRotation;

            // Get the package pad position and orientation
            Vector3 packagePadPosition = packagePad.transform.position;
            float packagePadRotation = packagePad.transform.eulerAngles.y;
            string dataPackage = "package:" + packagePadPosition.x + ";" + packagePadPosition.y + ";" + packagePadPosition.z + ";" + packagePadRotation;

            // Get the customer position and orientation
            Vector3 customerPosition = customer.transform.position;
            float customerRotation = customer.transform.eulerAngles.y;
            string dataCustomer = "customer:" + customerPosition.x + ";" + customerPosition.y + ";" + customerPosition.z + ";" + customerRotation;

            // Get the car position and orientation
            Vector3 carPosition = (padTruck != null) ? padTruck.transform.position : Vector3.zero;
            float carRotation = (padTruck != null) ? padTruck.transform.eulerAngles.y : 0f;
            string dataCar = (padTruck != null) ? "padTruck:" + carPosition.x + ";" + carPosition.y + ";" + carPosition.z + ";" + carRotation : "padTruck:null";

            // Set the vehicle communication data
            vehicle.GetComponent<VehicleCommunication>().setMessageData = new List<string> { dataHub, dataPackage, dataCustomer, dataCar };

            // If the prefab package name is not "null", instantiate the package
            if (prefabPackageNames[i] != "null")
            {

                // Load the package prefab from Resources
                GameObject prefabPackage = Resources.Load<GameObject>("Prefabs/" + prefabPackageNames[i]);

                // Set the packagePad position and orientation
                Vector3 positionPackage = new Vector3(
                    packagePad.transform.position.x,
                    packagePad.transform.position.y,
                    packagePad.transform.position.z
                );

                // Set the packagePad rotation
                Quaternion rotationPackage = Quaternion.Euler(
                    packagePad.transform.eulerAngles.x,
                    packagePad.transform.eulerAngles.y,
                    packagePad.transform.eulerAngles.z
                );

                // Instantiate the package game object
                GameObject instantiatedPackage = Instantiate(prefabPackage, positionPackage, rotationPackage);

                // Set the package game object properties
                instantiatedPackage.transform.SetParent(packages.transform);
                instantiatedPackage.transform.name = "prefabPackage_" + vehicle.name;
                instantiatedPackage.transform.tag = "Package";

            }

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to find a game object in the scene by its name and tag:

    GameObject FindInSceneByName(string name, string tag = null)
    {

        // Initialize an array to store the candidates
        GameObject[] candidates;

        // If a tag is provided, find objects with that tag; otherwise, find all objects
        if (!string.IsNullOrEmpty(tag))
        {
            candidates = GameObject.FindGameObjectsWithTag(tag);
        }
        else
        {
            candidates = Resources.FindObjectsOfTypeAll<GameObject>();
        }

        // Loop through the candidates and return the first one that matches the name and is loaded in the scene
        foreach (GameObject obj in candidates)
        {
            if (obj.name == name && obj.scene.isLoaded)
            {
                return obj;
            }
        }

        return null;
        
    }

}
