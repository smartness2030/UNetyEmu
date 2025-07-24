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
using System; // Library for general types and functions
using UnityEngine; // Library to use in MonoBehaviour classes
using System.Collections.Generic; // Library for using generic collections like List and Dictionary

// Class to manage communication between the car and the base station
public class CarCommunication : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // Dictionary to hold message handlers for the base station
    public static Dictionary<string, Action<string>> baseStationMessageHandlers = new Dictionary<string, Action<string>>();

    // Name of the base station and the interval for sending messages
    public string baseStationName = "BaseStation";
    public float messageIntervalTime = 5f;

    // Public variables to hold car's WiFi signal strength and received mission list
    public float carWifiSignal;
    public List<string> missionReceivedGeneralList = new List<string>();

    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // Private variables to hold the car's message, name, and flags for sending messages
    private string carMessage;
    private string carName;
    private int flagSendMessage;

    // Private variable to track the current time for message intervals
    private float currentTime;
    
    // -----------------------------------------------------------------------------------------------------
    // Awake is called when the script instance is being loaded:

    void Awake()
    {
        carName = gameObject.name; // The name of the car GameObject
    }

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        // Initialize the car's WiFi signal strength, message, and mission list
        carWifiSignal = 1f;
        carMessage = "";
        missionReceivedGeneralList = new List<string>();

        // Initialize the current time for message intervals
        currentTime = messageIntervalTime;

        // Initialize the flag for sending messages
        flagSendMessage = 0;

    }

    // -----------------------------------------------------------------------------------------------------
    // FixedUpdate is called at a fixed interval:

    void FixedUpdate()
    {

        // Check if the car has a message to send
        if (carMessage != "")
        {

            // If the car has a message, send it to the base station
            Debug.Log($"UNITY-UNITY: {carName} Received message: {carMessage}");
            missionReceivedGeneralList.Add(carMessage);
            carMessage = "";
            flagSendMessage = 1;

        }

        // Update the current time for message intervals
        currentTime += Time.fixedDeltaTime;

        // If the current time exceeds the message interval or if a message is flagged to be sent
        if ((currentTime >= messageIntervalTime) || (flagSendMessage == 1))
        {
            currentTime = 0f;
            flagSendMessage = 0;
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to send a message to the base station:

    public static void SendMessageToBaseStation(string baseStationName, string message, string playerName)
    {

        // Check if the base station has a message handler for the car
        if (baseStationMessageHandlers.ContainsKey(baseStationName))
        {
            Debug.Log($"UNITY-UNITY: Message sent to {baseStationName}: {message}");
            baseStationMessageHandlers[baseStationName]?.Invoke(message);
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to receive a message from the base station when the script is enabled:

    void OnEnable()
    {
        if (!BaseStationUnity.carMessageHandlers.ContainsKey(carName))
        {
            BaseStationUnity.carMessageHandlers[carName] = UnityToUnityReceiveMessageFromBaseStationToCar;
        }
    }

    // -----------------------------------------------------------------------------------------------------
    // Method to receive a message from the base station when the script is disabled:

    void OnDisable()
    {
        if (BaseStationUnity.carMessageHandlers.ContainsKey(carName))
        {
            BaseStationUnity.carMessageHandlers.Remove(carName);
        }
    }

    // -----------------------------------------------------------------------------------------------------
    // Method to handle receiving a message from the base station:

    void UnityToUnityReceiveMessageFromBaseStationToCar(string message)
    {
        carMessage = message; // The message received from the base station
    }

}
