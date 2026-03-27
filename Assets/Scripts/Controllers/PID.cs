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

// Class to create a PID controller to control the drone's position:
public class PID
{
    
    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    private float previousError = 0f;
    private float integral = 0f;

    // -----------------------------------------------------------------------------------------------------
    // Method to compute the PID controller:
    
    public float Compute(float error, float Kp, float Ki, float Kd)
    {
        
        // Proportional term:
        float proportional = Kp * error;

        // Integral term:        
        integral += error * Time.fixedDeltaTime;
        float integralTerm = Ki * integral;

        // Derivative term:
        float derivative = (error - previousError) / Time.fixedDeltaTime;
        float derivativeTerm = Kd * derivative;

        // Update the previous error:
        previousError = error;

        // Return the PID controller output:
        return proportional + integralTerm + derivativeTerm;

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to reset the integral value of the controller:
    
    public void Reset()
    {
        integral = 0f; // Reset the integral value
    }

}
