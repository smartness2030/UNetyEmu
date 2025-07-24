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

// Class to add a read component about the car's features, according to the data in the setupObjects.json file:
public class GetCarFeatures : MonoBehaviour
{
    // Car Features:
    [Header("Car Features")]
    [ReadOnly] public float weight;
    [ReadOnly] public float motorForce;
    [ReadOnly] public float brakeForce;
    [ReadOnly] public float maxSteerAngle;
    [ReadOnly] public float approxMaxDrivingTime;
    [ReadOnly] public float maxBatteryCapacity;
    [ReadOnly] public float batteryVoltage;
    [ReadOnly] public float batteryStartPercentage;
    [ReadOnly] public float maxSpeedAllowed;
}
