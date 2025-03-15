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

// Class to join the package object with the drone object 
public class DroneJoinedToPackage : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // Package object to be attached to the drone
    public Transform objectPackage;

    // Variables to identify the drone's state
    [ReadOnly] public bool isPackageAttached = false;
    [ReadOnly] public bool isPackageDelivered = false;

    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // Variables to join the package object with the drone object
    private FixedJoint joint; 
    private Rigidbody rb;

    // -----------------------------------------------------------------------------------------------------
    // Update is called once per frame:

    void Update()
    {
        
        // If the package object is not null
        if (objectPackage != null)
        {
            
            // Get the Rigidbody component of the package object
            rb = objectPackage.GetComponent<Rigidbody>();

            // If the package object is attached to the drone, anchor the package
            if (isPackageAttached) AnchorPackage();
        
            // If the package object is delivered, release the package
            if (isPackageDelivered) RealesePackage();

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to anchor the package to the drone:

    void AnchorPackage()
    {
        
        // If the joint is null and the Rigidbody component is not null
        if (joint == null && rb != null)
        {
            
            // Create a FixedJoint component and connect the package object with the drone object
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = rb;  

        }

        // Set the package object as attached to the drone
        isPackageAttached = false;

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to release the package from the drone:

    void RealesePackage()
    {

        // If the joint is not null
        if (joint != null)
        {
            
            // Destroy the joint and wake up the Rigidbody component
            Destroy(joint);
            rb.WakeUp();

        }

        // Set the package object as delivered
        isPackageDelivered = false;

    }

}
