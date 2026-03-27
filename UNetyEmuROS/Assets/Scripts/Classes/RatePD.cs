// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using System; // To use Serializable attribute

// Public class to hold Rate PD settings for tuning
[Serializable] public class setRatePDValue
{
    public float Kp;
    public float Kd;
}

// ----------------------------------------------------------------------
// Class to create a Rate PD controller to control the drone's angular rates (pitch and roll)
public class RatePD
{
    // ----------------------------------------------------------------------
    // Method to compute the PD controller for the angular rates
    public float Compute(float error, float rate, float Kp, float Kd)
    {
        // Compute the proportional and derivative terms of the PD controller
        float proportional = Kp * error;        
        float derivative = Kd * rate;

        // Return the sum of the proportional and derivative terms as the control output
        return proportional - derivative;
    }
}
