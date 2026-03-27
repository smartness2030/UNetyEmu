// ----------------------------------------------------------------------
// Copyright 2025 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use Vector3

// ----------------------------------------------------------------------
// Class to create a Vector PID controller to control the position and velocity in 3D space
public class VectorPID
{
    // ----------------------------------------------------------------------
    // Private variables

    // PID controllers for each axis
    private PID pidX = new PID();
    private PID pidY = new PID();
    private PID pidZ = new PID();

    // ----------------------------------------------------------------------
    // Method to compute the PID controller for each axis independently
    public Vector3 Compute(Vector3 error, float Kp, float Ki, float Kd)
    {
        // Compute PID for each axis
        float outputX = pidX.Compute(error.x, Kp, Ki, Kd);
        float outputY = pidY.Compute(error.y, Kp, Ki, Kd);
        float outputZ = pidZ.Compute(error.z, Kp, Ki, Kd);

        // Return the PID outputs as a Vector3
        return new Vector3(outputX, outputY, outputZ);
    }

    // ----------------------------------------------------------------------
    // Method to reset the integral value of the controller
    public void Reset()
    {
        pidX.Reset();
        pidY.Reset();
        pidZ.Reset();
    }
}
