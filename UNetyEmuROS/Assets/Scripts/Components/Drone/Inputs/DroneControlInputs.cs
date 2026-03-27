// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute

// ----------------------------------------------------------------------
// Enum to define the control mode of the drone: Automatic or Manual
[Serializable] public enum DroneControlMode { 
    Automatic,
    Manual 
}

// -------------------------------------------------------
// Class to read the user inputs for throttle, pitch, roll and yaw and pass them to the DroneStabilizationController, which will interpret them as target vertical speed, pitch angle, roll angle and yaw rate respectively
// Input semantics (matching DroneControlInputs / DroneStabilizationController conventions):
//   throttleInput : [-1, +1] → [-maxDescentSpeedAllowed, maxClimbSpeedAllowed]
//   pitchInput    : [-1, +1] → [-maxTiltAngle, +maxTiltAngle] → backward/forward cruise speed
//   rollInput     : [-1, +1] → [-maxTiltAngle, +maxTiltAngle] → left/right cruise speed
//   yawInput      : [-1, +1] → [-maxYawRateAllowed, +maxYawRateAllowed] in deg/s
// Requires a DroneStabilizationController component to set the target vertical speed, pitch angle, roll angle and yaw rate based on the user inputs
// Requires a DroneDynamics component to know if the drone is colliding with the ground or the pad, in which case the throttle input will be ignored to prevent applying downward forces that could cause unrealistic behavior
[RequireComponent(typeof(DroneStabilizationController))]
[RequireComponent(typeof(DroneDynamics))]
public class DroneControlInputs : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Throttle input to control the vertical speed of the drone, where -1 is maximum descent speed and +1 is maximum climb speed allowed, as defined in DroneDynamics")]
    [Range(-1f, 1f)] public float throttleInput;

    [Tooltip("Pitch input to control the forward/backward tilt of the drone, where -1 is maximum backward tilt and +1 is maximum forward tilt allowed, as defined in DroneDynamics")]
    [Range(-1f, 1f)] public float pitchInput;

    [Tooltip("Roll input to control the left/right tilt of the drone, where -1 is maximum left tilt and +1 is maximum right tilt allowed, as defined in DroneDynamics")]
    [Range(-1f, 1f)] public float rollInput;

    [Tooltip("Yaw input to control the rotation of the drone around its vertical axis, where -1 is maximum left rotation and +1 is maximum right rotation allowed, as defined in DroneDynamics")]
    [Range(-1f, 1f)] public float yawInput;

    [Tooltip("Control mode of the drone. This is used to determine whether the drone should be controlled automatically by a script like DroneWaypointFollower or manually by the user through the inputs")]
    public DroneControlMode controlMode = DroneControlMode.Automatic;

    // ----------------------------------------------------------------------
    // Private variables

    // To set the target vertical speed, pitch angle, roll angle and yaw rate based on the user inputs
    private DroneStabilizationController droneStabilizationController;

    // To check if the drone is colliding with the ground or the pad, in which case the throttle input will be ignored to prevent applying downward forces that could cause unrealistic behavior
    private DroneDynamics droneDynamics;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get the DroneDynamics component
        droneDynamics = GetComponent<DroneDynamics>();

        // Get the DroneStabilizationController component
        droneStabilizationController = GetComponent<DroneStabilizationController>();
    }

    // ----------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // Set the pitch and roll targets based on the user inputs
        droneStabilizationController.setPitchTarget = pitchInput;
        droneStabilizationController.setRollTarget = rollInput;

        // Set the vertical speed target based on the user input. If the drone is colliding, set the vertical speed target to 0 to prevent unrealistic behavior
        if (droneDynamics.isCollidingWithGroundOrPad && throttleInput < 0f)
            droneStabilizationController.setVerticalSpeed = 0f;
        else
            droneStabilizationController.setVerticalSpeed = throttleInput;
              
        // Set the yaw target based on the user input
        droneStabilizationController.setYawTarget = yawInput;
    }
}
