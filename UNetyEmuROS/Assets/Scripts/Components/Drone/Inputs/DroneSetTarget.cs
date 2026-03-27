// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute

// Public class to define the target position, orientation and speeds for the drone
[Serializable] public class SetDroneTarget
{
    public Vector3 position;
    public float orientation;
    public float cruiseSpeed;
    public float climbSpeed;
    public float descentSpeed;
}

// ----------------------------------------------------------------------
// Class to set the target for the drone, which will be used by the controllers to calculate the necessary control inputs to reach that target
public class DroneSetTarget : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Target position for the drone to reach")]
    public SetDroneTarget target = new SetDroneTarget();

    [Tooltip("Whether to automatically orient the drone towards the target position")]
    public bool autoOrientation;

    // ----------------------------------------------------------------------
    // Awake is called when the script instance is being loaded
    void Awake()
    {
        target.position = transform.position;
        target.orientation = transform.localEulerAngles.y;
    }
}
