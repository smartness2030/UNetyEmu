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

// Class to add a read component about the drone's features, according to the data in the setupObjects.json file:
public class GetDroneFeatures : MonoBehaviour
{
    // Drone Features:
    [Header("Drone Features")]
    [ReadOnly] public float unladenWeight;
    [ReadOnly] public float approxMaxFlightTime;
    [ReadOnly] public float maxBatteryCapacity;
    [ReadOnly] public float batteryVoltage;
    [ReadOnly] public float batteryStartPercentage;
    [ReadOnly] public float maxAltitude;
    [ReadOnly] public float maxThrust;
    [ReadOnly] public float maxSpeedManufacturer;
    [ReadOnly] public float maximumTiltAngle;
    [ReadOnly] public float propellerMaxRPM;
}
