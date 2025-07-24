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

// Class to set missions to vehicles from the base station
public class BaseStationSetMissionsToVehicles : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // Flag to indicate if the connection with MininetWifi has been established
    public bool flagConnectionEstablished;

    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // BaseStationMininetWifi script to manage the connection with MininetWifi
    private BaseStationMininetWifi baseStationMininetWifi;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        // Get the BaseStationMininetWifi script
        baseStationMininetWifi = GetComponent<BaseStationMininetWifi>();

        // Initialize the flag to false
        flagConnectionEstablished = false;

    }

    // -----------------------------------------------------------------------------------------------------
    // FixedUpdate is called at a fixed interval:

    void FixedUpdate()
    {

        // Check if the baseStationMininetWifi is not null
        if (baseStationMininetWifi != null)
        {

            // Check if the MininetWifi is ready and the connection has not been established yet
            if (baseStationMininetWifi.flagMininetWifiReady && (!flagConnectionEstablished))
            {

                // Debug log to indicate that the connection is being established
                Debug.Log("BaseStationSetMissionsToVehicles: Connection established with MininetWifi.");

                // Set the flag to true to indicate that the connection has been established
                flagConnectionEstablished = true;

            }

        }

    }

}
