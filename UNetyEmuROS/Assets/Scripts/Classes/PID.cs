// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use Time.fixedDeltaTime
using System; // To use Serializable attribute

// Public class to hold PID settings for tuning
[Serializable] public class setPIDValue
{
    public float Kp;
    public float Ki;
    public float Kd;
}

// ----------------------------------------------------------------------
// Class to create a PID controller to control
public class PID
{
    // ----------------------------------------------------------------------
    // Private variables
    private float previousError = 0f;
    private float integral = 0f;

    // ----------------------------------------------------------------------
    // Method to compute the PID controller
    public float Compute(float error, float Kp, float Ki, float Kd)
    {
        // Proportional term
        float proportional = Kp * error;

        // Integral term 
        integral += error * Time.fixedDeltaTime;
        float integralTerm = Ki * integral;

        // Derivative term
        float derivative = (error - previousError) / Time.fixedDeltaTime;
        float derivativeTerm = Kd * derivative;

        // Update the previous error
        previousError = error;

        // Return the PID controller output
        return proportional + integralTerm + derivativeTerm;
    }

    // ----------------------------------------------------------------------
    // Method to reset the integral value of the controller
    public void Reset()
    {
        integral = 0f;
        previousError = 0f;
    }
}
