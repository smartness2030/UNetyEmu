// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes

// ----------------------------------------------------------------------
// Class to rotate the drone propellers based on the forces applied by the DroneDynamics script
// Requires a DroneDynamics component to read the forces applied to each motor and rotate the propellers accordingly
[RequireComponent(typeof(DroneDynamics))]
public class DronePropellersRotation : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Private input variables

    [Header("• PRIVATE INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector
    
    [Tooltip("Maximum rotation speed in degrees per second at maximum force")]
    [SerializeField] private float maxRotationSpeed = 1000f;

    [Tooltip("Minimum force to start rotating the propellers")]
    [SerializeField] private float minForceToRotate = 0.01f;

    [Tooltip("Minimum rotation speed when minimum force is applied")]
    [SerializeField] private float minRotationSpeed = 100f;

    [Tooltip("Propellers transforms in the order: FrontLeft, FrontRight, BackRight, BackLeft")]
    [SerializeField] private Transform[] propellers;

    // ----------------------------------------------------------------------
    // Private variables

    // Current forces applied to each propeller
    private float[] currentForces;

    // Current rotation speeds of each propeller in degrees per second
    private float[] rotationSpeeds;

    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the DroneDynamics component to read the forces applied to each motor
    private DroneDynamics droneDynamics;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get reference to DroneDynamics component on the same GameObject
        droneDynamics = GetComponent<DroneDynamics>();

        // Initialize arrays to store current forces and rotation speeds for each propeller
        currentForces = new float[propellers.Length];
        rotationSpeeds = new float[propellers.Length];
    }

    // ----------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // Get the time elapsed since the last frame
        float deltaTime = Time.deltaTime;

        // Rotate each propeller based on its current rotation speed and direction
        for (int i = 0; i < propellers.Length; i++)
        {
            // Get the transform of the current propeller
            Transform propeller = propellers[i];
            
            // Validate that the propeller transform is not null before trying to rotate it
            if (propeller == null) continue;

            // Determine the direction of rotation for the current propeller (clockwise or counterclockwise) based on its index (even or odd)
            float direction = (i % 2 == 0) ? 1f : -1f;

            // Rotate the propeller around its local up axis (Y-axis) by the calculated rotation speed, multiplied by the direction and deltaTime to make it frame rate independent
            propeller.Rotate(Vector3.up, rotationSpeeds[i] * direction * deltaTime, Space.Self);
        }
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // Validate that the DroneDynamics component is available and that there are propellers assigned before trying to read forces and calculate rotation speeds
        if (droneDynamics == null || propellers.Length == 0) return;

        // If the drone motors are not turned on, set all rotation speeds to zero
        if (!droneDynamics.turnOnDroneMotors)
        {
            // Set all rotation speeds to zero
            for (int i = 0; i < rotationSpeeds.Length; i++)
                rotationSpeeds[i] = 0f;
            
            // Exit the method early since there are no forces to read and propellers should not rotate
            return;
        }

        // Read the forces applied to each motor from the DroneDynamics component and store them in an array for easier access
        float[] motorForces = new float[] {
            droneDynamics.forceFrontLeft,
            droneDynamics.forceFrontRight,
            droneDynamics.forceBackRight,
            droneDynamics.forceBackLeft
        };

        // For each propeller, calculate its rotation speed based on the current force applied to its corresponding motor
        for (int i = 0; i < propellers.Length; i++)
        {
            // Store the current force applied to the motor corresponding to the current propeller
            currentForces[i] = motorForces[i];

            // Calculate the rotation speed for the current propeller based on the current force applied to its motor using a square root function
            rotationSpeeds[i] = Mathf.Sqrt(currentForces[i]) * maxRotationSpeed;

            // If the current force applied to the motor is below the minimum threshold, set the rotation speed to a minimum value to simulate a slow rotation
            if (currentForces[i] < minForceToRotate)
                rotationSpeeds[i] = minRotationSpeed;
        }
    }
}
