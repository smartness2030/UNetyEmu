// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes

// ----------------------------------------------------------------------
// Class to control the drone with a package from the keyboard simulating joystick behavior
// Requires DroneControlInputs to write the user inputs to be read by downstream controllers
// Requires DroneDynamics to check collision state for throttle limiting
// Requires DronePickUpPackage to pick up and drop packages with keyboard input
[RequireComponent(typeof(DroneControlInputs))]
[RequireComponent(typeof(DroneDynamics))]
[RequireComponent(typeof(DronePickUpPackage))]
public class MoveDroneKeyboardWithPackage : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Private input variables

    [Header("• PRIVATE INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Key to increase altitude (throttle up)")]
    [SerializeField] private KeyCode altitudeUpKey = KeyCode.T;

    [Tooltip("Key to decrease altitude (throttle down)")]
    [SerializeField] private KeyCode altitudeDownKey = KeyCode.G;

    [Tooltip("Key to yaw left")]
    [SerializeField] private KeyCode yawLeftKey = KeyCode.F;

    [Tooltip("Key to yaw right")]
    [SerializeField] private KeyCode yawRightKey = KeyCode.H;

    [Tooltip("Key to pitch forward")]
    [SerializeField] private KeyCode pitchForwardKey = KeyCode.I;

    [Tooltip("Key to pitch backward")]
    [SerializeField] private KeyCode pitchBackwardKey = KeyCode.K;

    [Tooltip("Key to roll left")]
    [SerializeField] private KeyCode rollLeftKey = KeyCode.J;

    [Tooltip("Key to roll right")]
    [SerializeField] private KeyCode rollRightKey = KeyCode.L;

    [Tooltip("Key to pick up a package when nearby (only in Manual control mode)")]
    [SerializeField] private KeyCode pickUpPackageKey = KeyCode.U;

    [Tooltip("Key to drop the currently attached package (only in Manual control mode)")]
    [SerializeField] private KeyCode dropPackageKey = KeyCode.O;

    [Tooltip("Rate at which pitch, roll and yaw ramp up to their maximum value when a key is held, in [units/second]")]
    [SerializeField] private float inputRampUpRate = 2.0f;

    [Tooltip("Rate at which pitch, roll and yaw return to zero when the key is released, in [units/second]")]
    [SerializeField] private float inputRampDownRate = 3.0f;

    [Tooltip("Rate at which the throttle ramps up or down when the altitude keys are held, in [units/second]")]
    [SerializeField] private float throttleRampRate = 1.5f;

    [Tooltip("Factor to slow down the throttle return to zero when neither altitude key is held")]
    [SerializeField] private float slowReturnRampDownRateMultiplier = 1.0f;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]

    [Tooltip("Current throttle value being written to DroneControlInputs [-1, 1]")]
    public float currentThrottle;

    [Tooltip("Current pitch value being written to DroneControlInputs [-1, 1]")]
    public float currentPitch;

    [Tooltip("Current roll value being written to DroneControlInputs [-1, 1]")]
    public float currentRoll;

    [Tooltip("Current yaw value being written to DroneControlInputs [-1, 1]")]
    public float currentYaw;

    // ----------------------------------------------------------------------
    // Private variables

    // References to the DroneControlInputs components on the same GameObject
    private DroneControlInputs droneControlInputs;

    // Reference to the DroneDynamics component to check collision state for throttle limiting
    private DroneDynamics droneDynamics;

    // Reference to the DronePickUpPackage component
    private DronePickUpPackage dronePickUpPackage;

    // Reference to the AttachObject component to check if there is a package nearby to pick up
    private AttachObject attachObject;

    // Internal state variables to track the current throttle, pitch, roll, and yaw values 
    private float throttleValue = 0f;
    private float pitchValue = 0f;
    private float rollValue = 0f;
    private float yawValue = 0f;

    // Internal variable to track if the drone was colliding with the ground or pad in the last frame
    private bool wasCollidingLastFrame = false;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get the droneControlInputs component to write the user inputs to be read by downstream controllers
        droneControlInputs = GetComponent<DroneControlInputs>();
        
        // Set the control mode to Manual in the DroneControlInputs component to be used by downstream controllers
        droneControlInputs.controlMode = DroneControlMode.Manual;

        // Get the DroneDynamics component to check collision state for throttle limiting
        droneDynamics = GetComponent<DroneDynamics>();

        // Get the DronePickUpPackage component to attach/detach packages
        dronePickUpPackage = GetComponent<DronePickUpPackage>();

        // Get the AttachObject component to check if there is a package nearby to pick up
        attachObject = GetComponent<AttachObject>();

        // Reset all axis values to zero to avoid applying stale inputs from the previous mode when switching
        ResetAxes();
    }

    // ----------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // Check if the control mode has changed at runtime and apply the corresponding settings
        if (droneControlInputs.controlMode != DroneControlMode.Manual)
        {
            // Set the control mode to Manual in the DroneControlInputs component to be used by downstream controllers
            droneControlInputs.controlMode = DroneControlMode.Manual;

            // Reset all axis values to zero to avoid applying stale inputs from the previous mode when switching
            ResetAxes();
        }

        // Update the throttle, pitch, roll, and yaw values based on the current keyboard input and the defined ramp rates
        UpdateThrottle();
        UpdatePitchRollYaw();

        // Apply the updated control values to the drone
        ApplyToDroneControlInputs();

        // Check for package pick up or drop input and update the DronePickUpPackage component state accordingly
        PickUpOrDropAPackage();
    }

    // ----------------------------------------------------------------------
    // Method to update the throttle value based on the altitude up/down keys
    void UpdateThrottle()
    {
        // Check if the drone is currently colliding with the ground or pad using the DroneDynamics component
        bool isColliding = droneDynamics.isCollidingWithGroundOrPad;

        // If the drone just started colliding this frame (rising edge), set the throttle value to zero
        if (isColliding && !wasCollidingLastFrame)
            throttleValue = 0f;

        // Update the wasCollidingLastFrame variable for the next frame
        wasCollidingLastFrame = isColliding;

        // Get the current state of the altitude up and down keys
        bool upHeld = Input.GetKey(altitudeUpKey);
        bool downHeld = Input.GetKey(altitudeDownKey);

        // If only the up key is held, ramp up the throttle value
        if (upHeld && !downHeld)
        {
            throttleValue += throttleRampRate * Time.deltaTime;
        }
        else if (downHeld && !upHeld) // If only the down key is held and the drone is not colliding, ramp down the throttle value
        {
            if (!isColliding)
                throttleValue -= throttleRampRate * Time.deltaTime;
        }
        else // If both or neither key is held, slowly return the throttle value towards zero
        {
            // Use a slower ramp down rate for throttle to create a more gradual return to zero
            throttleValue = Mathf.MoveTowards(throttleValue, 0f, throttleRampRate * slowReturnRampDownRateMultiplier * Time.deltaTime);
        }

        // Clamp the throttle value to the range [-1, 1]
        throttleValue = Mathf.Clamp(throttleValue, -1f, 1f);
    }

    // ----------------------------------------------------------------------
    // Method to update the pitch, roll, and yaw values based on the corresponding keys
    void UpdatePitchRollYaw()
    {
        pitchValue = UpdateSpringAxis(pitchValue, pitchForwardKey, pitchBackwardKey);
        rollValue = UpdateSpringAxis(rollValue, rollRightKey, rollLeftKey);
        yawValue = UpdateSpringAxis(yawValue, yawRightKey, yawLeftKey);
    }

    // ----------------------------------------------------------------------
    // Helper method to update an axis value (pitch, roll, or yaw) based on the corresponding positive and negative keys
    float UpdateSpringAxis(float currentValue, KeyCode positiveKey, KeyCode negativeKey)
    {
        // Get the current state of the positive and negative keys
        bool positiveHeld = Input.GetKey(positiveKey);
        bool negativeHeld = Input.GetKey(negativeKey);

        // If only the positive key is held, ramp up towards +1
        if (positiveHeld && !negativeHeld)
            currentValue = Mathf.MoveTowards(currentValue, 1f, inputRampUpRate * Time.deltaTime);
        else if (negativeHeld && !positiveHeld) // If only the negative key is held, ramp down towards -1
            currentValue = Mathf.MoveTowards(currentValue, -1f, inputRampUpRate * Time.deltaTime);
        else // If both or neither key is held, return towards zero
            currentValue = Mathf.MoveTowards(currentValue, 0f, inputRampDownRate * Time.deltaTime);

        // Return the updated axis value, clamped to the range [-1, 1]
        return Mathf.Clamp(currentValue, -1f, 1f);
    }

    // ----------------------------------------------------------------------
    // Method to write the computed axis values to DroneControlInputs and update the monitoring output variables
    void ApplyToDroneControlInputs()
    {
        // Write the current control values to the DroneControlInputs component to be used by downstream controllers
        droneControlInputs.throttleInput = throttleValue;
        droneControlInputs.pitchInput = pitchValue;
        droneControlInputs.rollInput = rollValue;
        droneControlInputs.yawInput = yawValue;

        // Apply the current control values to the monitoring output variables for visualization in the Inspector
        currentThrottle = throttleValue;
        currentPitch = pitchValue;
        currentRoll = rollValue;
        currentYaw = yawValue;
    }

    // ----------------------------------------------------------------------
    // Method to reset all internal axis values to zero
    void ResetAxes()
    {
        // Reset the internal state variables to zero to avoid applying stale inputs when switching control modes
        throttleValue = 0f;
        pitchValue    = 0f;
        rollValue     = 0f;
        yawValue      = 0f;

        // Reset the output monitoring variables to zero as well
        currentThrottle = 0f;
        currentPitch    = 0f;
        currentRoll     = 0f;
        currentYaw      = 0f;
    }

    // ----------------------------------------------------------------------
    // Method to check for package pick up or drop input and update the DronePickUpPackage component state
    void PickUpOrDropAPackage()
    {
        // Only allow picking up a package if there is a nearby package to pick up
        if (attachObject.packageObject == null)
            return;

        // Get the current state of the pick up and drop keys
        bool pickUpKey = Input.GetKey(pickUpPackageKey);
        bool dropKey = Input.GetKey(dropPackageKey);

        // If the pick up key is pressed set the state to PickUp
        if (pickUpKey && !dropKey)
        {
            dronePickUpPackage.currentState = ArmState.PickUp;
        }
        else if (dropKey && !pickUpKey) // If the drop key is pressed set the state to Drop
        {
            dronePickUpPackage.currentState = ArmState.Drop;
        }
    }
}
