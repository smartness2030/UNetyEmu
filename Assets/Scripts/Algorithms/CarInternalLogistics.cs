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
using UnityEngine; // Unity Engine library to use in MonoBehaviour classes

// Class to handle internal logistics of the car
public class CarInternalLogistics : MonoBehaviour
{
    public bool missionCompletedFlagCommunication = false; // Flag to indicate if the mission is completed
    public bool missionDeliverPackageFlagCommunication = false; // Flag to indicate if the package delivery mission is active
    public string currentMission = ""; // Current mission description
    public string currentMissionStatus = ""; // Status of the current mission
}
