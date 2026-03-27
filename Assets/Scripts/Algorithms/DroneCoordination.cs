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

// Class to set a single route planning for the drone
public class DroneCoordination : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // Variables to control the drone speed, rotation speed, and minimum error to target
    public float speed2 = 5.0f;
    public float rotationSpeed2 = 5f;

    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // Get the coordination script
    private Coordination coordination;

    // Get the object features and the object ID
    private GetObjectFeatures getObjectFeatures;
    private string objectID;

    // Sphere target object related to the drone
    private GameObject targetObj;

    // Variables to store the target position, rotation, direction, and distance to target
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 direction;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {
    
        // Get the object features and the object ID
        getObjectFeatures = GetComponent<GetObjectFeatures>();
        objectID = getObjectFeatures.objectID;

        // Get the sphere target object related to the drone
        targetObj = GameObject.Find("ID" + objectID + "Target");

        // Get the coordination component
        coordination = FindObjectOfType<Coordination>();

    }

    // -----------------------------------------------------------------------------------------------------
    // FixedUpdate is called at fixed time intervals, ideal for physical changes in objects:

    void FixedUpdate()
    {
        
        if (coordination != null){

            // Call the method to follow the altitude
            FollowAltitude();

        }
        else
        {
            
            // Set the target position using the same sphere
            targetPosition = targetObj.transform.position;

        }	

    }

    // -----------------------------------------------------------------------------------------------------
    // Class to follow the altitude:

    void FollowAltitude()
    {

        // Check if the coordination is active
        if (coordination.activeCoordination)
        {
            
            // Set the altitude of the target object using the global altitude by the coordination script
            targetPosition = new Vector3(
                targetObj.transform.position.x + coordination.MoveTowardsX, 
                coordination.setGlobalAltitude, 
                targetObj.transform.position.z + coordination.MoveTowardsZ
            );

        }
        else
        {
            
            // Set the target position using the same sphere
            targetPosition = targetObj.transform.position;

        }

        // Move the target object towards the target position
        targetObj.transform.position = Vector3.MoveTowards(targetObj.transform.position, targetPosition, speed2 * Time.deltaTime);

        // Calculate the direction to the target
        direction = (targetPosition - targetObj.transform.position).normalized;

        // Rotate the target object towards the target position
        if (direction != Vector3.zero)
        {
            
            // Set the target rotation
            targetRotation = Quaternion.LookRotation(direction);

            // Rotate the target object towards the target position
            targetObj.transform.rotation = Quaternion.Slerp(targetObj.transform.rotation, targetRotation, rotationSpeed2 * Time.deltaTime);

        }

    }

}
