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

// Class to manage the drone landing pad
public class DroneLandingPad : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    public bool isDroneLanded = false;

    // -----------------------------------------------------------------------------------------------------
    // Method called when another collider enters the trigger collider attached to this object

    private void OnTriggerEnter(Collider other)
    {

        // Check if the collider belongs to a drone
        if (other.CompareTag("Drone"))
        {

            // Set the drone as landed
            isDroneLanded = true;

            // Set the drone as a child of the landing pad
            other.transform.SetParent(transform);

            // Get the drone's ID from its name
            string droneName = other.name;
            string idDrone = droneName.Substring(3, 3);

            // Find the target object for the drone and set it as a child of the landing pad
            GameObject targetObject = GameObject.Find("ID" + idDrone + "Target");
            targetObject.transform.SetParent(transform);

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method called when another collider exits the trigger collider attached to this object

    private void OnTriggerExit(Collider other)
    {

        // Check if the collider belongs to a drone
        if (other.CompareTag("Drone"))
        {

            // Reset the drone's landing state
            isDroneLanded = false;

            // Remove the drone from being a child of the landing pad
            FixedJoint joint = other.GetComponent<FixedJoint>();
            if (joint != null)
            {
                Destroy(joint); // Remove the FixedJoint from the drone
            }

        }

    }

}
