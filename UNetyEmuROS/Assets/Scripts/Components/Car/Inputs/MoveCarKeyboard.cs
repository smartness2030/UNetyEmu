// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine;

// ----------------------------------------------------------------------
// Class to handle keyboard input for controlling the car's movement, including speed limiting and input smoothing
public class MoveCarKeyboard : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Private input variables

    [Header("• PRIVATE INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Key to move the car forward")]
    [SerializeField] private KeyCode forwardKey = KeyCode.UpArrow;

    [Tooltip("Key to move the car backward")]
    [SerializeField] private KeyCode backwardKey = KeyCode.DownArrow;

    [Tooltip("Key to steer the car to the right")]
    [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;

    [Tooltip("Key to steer the car to the left")]
    [SerializeField] private KeyCode leftKey = KeyCode.LeftArrow;

    [Tooltip("Key to apply the brake")]
    [SerializeField] private KeyCode brakeKey = KeyCode.Space;

    [Tooltip("The speed limit in m/s")]
    [SerializeField] private float setSpeedLimit = 10f;

    [Tooltip("The range before reaching the speed limit where the throttle will start to be reduced")]
    [SerializeField] private float speedLimitSoftZone = 1f;

    [Tooltip("The speed at which the throttle input will be smoothed")]
    [SerializeField] private float throttleSmooth = 5f;

    [Tooltip("The speed at which the steering input will be smoothed")]
    [SerializeField] private float steeringSmooth = 5f;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("The current speed of the car in m/s")]
    public float currentSpeed;

    [Tooltip("The final throttle input after applying limits")]
    public float finalThrottle;

    [Tooltip("The raw steering input from the keyboard, smoothed over time")]
    public float steeringInput;
    
    [Tooltip("Whether the brake input is currently active")]
    public bool brakeInput;

    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the CarDynamics component to apply the inputs to the car's movement
    private CarDynamics carDynamics;

    // Reference to the Rigidbody component to get the car's velocity for speed calculations
    private Rigidbody rb;

    // Internal variable to keep track of the smoothed throttle input
    private float throttleInput;   

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get references to the CarDynamics and Rigidbody components on the same GameObject
        carDynamics = GetComponent<CarDynamics>();
        rb = GetComponent<Rigidbody>();
    }

    // ----------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // Initialize target throttle and steering values
        float targetThrottle = 0f;
        float targetSteering = 0f;

        // Check for forward and backward input and set the target throttle accordingly
        if (Input.GetKey(forwardKey))
            targetThrottle = 1f;
        else if (Input.GetKey(backwardKey))
            targetThrottle = -1f;

        // Check for right and left input and set the target steering accordingly
        if (Input.GetKey(rightKey))
            targetSteering = 1f;
        else if (Input.GetKey(leftKey))
            targetSteering = -1f;

        // Smoothly interpolate the throttle and steering inputs towards the target values for smoother control
        throttleInput = Mathf.Lerp(throttleInput, targetThrottle, Time.deltaTime * throttleSmooth);
        steeringInput = Mathf.Lerp(steeringInput, targetSteering, Time.deltaTime * steeringSmooth);

        // Check if the brake key is pressed and set the brake input accordingly
        brakeInput = Input.GetKey(brakeKey);
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // Calculate the current speed of the car by taking the magnitude of the velocity in the XZ plane (ignoring vertical velocity)
        Vector3 velocityXZ = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        currentSpeed = velocityXZ.magnitude;

        // Calculate the throttle limit factor based on the current speed and the defined speed limit, applying a soft zone for smoother limiting
        float throttleLimitFactor = 1f;
        if (currentSpeed >= setSpeedLimit)
        {
            throttleLimitFactor = 0f;
        }
        else if (currentSpeed >= (setSpeedLimit - speedLimitSoftZone))
        {
            // Calculate how close the current speed is to the speed limit and reduce the throttle accordingly within the soft zone
            float t = (setSpeedLimit - currentSpeed) / speedLimitSoftZone;
            throttleLimitFactor = Mathf.Clamp01(t);
        }

        // Apply the throttle limit factor to the throttle input to get the final throttle value that will be applied to the car
        finalThrottle = throttleInput * throttleLimitFactor;

        // Apply the final throttle, steering, and brake inputs to the CarDynamics component to control the car's movement
        carDynamics.throttle = finalThrottle;
        carDynamics.steering = steeringInput;
        carDynamics.isBraking = brakeInput;
    }
}
