// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes

// Class to manage the car dynamics such as motor force, brake force, steering angle, and battery consumption
public class CarDynamics : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Throttle input for the car, ranging from -1 (full reverse) to 1 (full forward)")]
    [Range(-1f, 1f)] public float throttle;

    [Tooltip("Steering input for the car, ranging from -1 (full left) to 1 (full right)")]
    [Range(-1f, 1f)] public float steering;

    [Tooltip("Brake input for the car, true to apply brakes and false to release brakes")]
    public bool isBraking;

    // ----------------------------------------------------------------------
    // Private input variables

    [Header("• PRIVATE INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("WheelCollider for the front left wheel of the car, which is used to apply motor torque, brake force, and steering angle")]
    [SerializeField] private WheelCollider wheelFrontLeft;

    [Tooltip("WheelCollider for the front right wheel of the car, which is used to apply motor torque, brake force, and steering angle")]
    [SerializeField] private WheelCollider wheelFrontRight;

    [Tooltip("WheelCollider for the back left wheel of the car, which is used to apply motor torque and brake force")]
    [SerializeField] private WheelCollider wheelBackLeft;

    [Tooltip("WheelCollider for the back right wheel of the car, which is used to apply motor torque and brake force")]
    [SerializeField] private WheelCollider wheelBackRight;

    [Tooltip("Transform for the front left wheel of the car, used for visual representation")]
    [SerializeField] private Transform frontLeftWheelTransform;

    [Tooltip("Transform for the front right wheel of the car, used for visual representation")]
    [SerializeField] private Transform frontRightWheelTransform;

    [Tooltip("Transform for the back left wheel of the car, used for visual representation")]
    [SerializeField] private Transform backLeftWheelTransform;

    [Tooltip("Transform for the back right wheel of the car, used for visual representation")]
    [SerializeField] private Transform backRightWheelTransform;

    [Tooltip("Maximum motor torque applied to the wheels when throttle is at maximum (1)")]
    [SerializeField] private float motorTorque = 3000f;

    [Tooltip("Brake force applied to the wheels when brakes are engaged")]
    [SerializeField] private float brakeForce = 2000f;

    [Tooltip("Percentage of brake force applied when throttle is not at maximum")]
    [SerializeField] private float percentageBrakeForceForNonThrottle = 0.5f;

    [Tooltip("Maximum steering angle in degrees")]
    [SerializeField] private float maxSteerAngle = 30f;

    [Tooltip("Unladen weight of the car in kg")]
    [SerializeField] private float unladenWeight = 1500f;

    [Tooltip("Maximum speed of the car in m/s, which should be set according to the manufacturer's specifications")]
    [SerializeField] private float maxSpeedManufacturer = 25f;
    
    [Tooltip("Minimum Drag coefficient of the car for aerodynamics, which can be set based on the car's design")]
    [SerializeField] private float randomDragMin = 0.1f;

    [Tooltip("Maximum Drag coefficient of the car for aerodynamics, which can be set based on the car's design")]
    [SerializeField] private float randomDragMax = 0.3f;

    [Tooltip("Minimum Angular Drag coefficient of the car for aerodynamics, which can be set based on the car's design")]
    [SerializeField] private float randomAngularDragMin = 1.0f;

    [Tooltip("Maximum Angular Drag coefficient of the car for aerodynamics, which can be set based on the car's design")]
    [SerializeField] private float randomAngularDragMax = 2.0f;

    [Tooltip("Vertical offset for the center of mass of the car, which can be adjusted to improve stability and handling")]
    [SerializeField] private float centerOfMassOffsetY = -0.1f;

    [Tooltip("Minimum speed in m/s below which the car's speed will be considered as 0 for display purposes")]
    [SerializeField] private float minSpeedToShow = 0.01f;

    [Tooltip("Minimum speed in m/s below which the wheels will not visually move, to prevent unrealistic wheel movement")]
    [SerializeField] private float minSpeedToMoveWheels = 0.1f;

    [Tooltip("Factor for smoothing the steering input, higher values will result in smoother steering")]
    [SerializeField] private float smoothSteeringFactor = 5f;

    [Tooltip("Animation curve to limit the steering angle based on the car's speed")]
    [SerializeField] private  AnimationCurve steerBySpeed = AnimationCurve.Linear(0f, 1f, 50f, 0.3f);

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Current speed of the car in m/s, calculated from the Rigidbody's velocity")]    
    public float speed;

    [Tooltip("Current motor torque applied to the wheels, calculated based on the throttle input and motor torque settings")]
    public float currentMotorTorque;
    
    [Tooltip("Current brake force applied to the wheels, calculated based on the brake input and brake force settings")]
    public float currentBrakeForce;

    [Tooltip("Current steering angle applied to the front wheels in degrees, calculated based on the steering input and max steer angle settings")]
    public float currentSteerAngle;

    [Tooltip("Flag to indicate if the car is currently colliding with an object, which can be used to trigger collision responses")]
    public bool isColliding = false;
    
    // ----------------------------------------------------------------------
    // Private variables
    
    // Rigidbody component of the car
    private Rigidbody rb;

    // Allowed steering angle after considering speed-based limitations
    private float allowedSteering;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get the Rigidbody component of the car
        rb = GetComponent<Rigidbody>();

        // Set the center of mass of the car
        rb.centerOfMass = new Vector3(0, centerOfMassOffsetY, 0);

        // Set the mass of the car
        rb.mass = unladenWeight;

        // Random drag between min and max for aerodynamics
        rb.drag = Random.Range(randomDragMin, randomDragMax);

        // Random angular drag between min and max for aerodynamics
        rb.angularDrag = Random.Range(randomAngularDragMin, randomAngularDragMax);

        // Enable gravity for the car's Rigidbody
        rb.useGravity = true;
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // Get the forces/torques of the car
        GetForcesTorques();

        // Apply the motor force, steering angle, and brake force
        ApplyHandleMotor();
        ApplyHandleSteering();

        // Update the wheel visuals based on the WheelCollider's position and rotation
        UpdateWheelVisuals(wheelFrontLeft, frontLeftWheelTransform);
        UpdateWheelVisuals(wheelFrontRight, frontRightWheelTransform);
        UpdateWheelVisuals(wheelBackLeft, backLeftWheelTransform);
        UpdateWheelVisuals(wheelBackRight, backRightWheelTransform);

        // Limit the speed of the car to the maximum speed specified by the manufacturer
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeedManufacturer);

        // Show the current speed of the car, and set it to 0 if it's below the minimum speed to show
        if(rb.velocity.magnitude < minSpeedToShow)
            speed = 0f;
        else
            speed = rb.velocity.magnitude;

        // Apply the brake force when throttle is at 0, to help the car come to a stop when the player releases the throttle
        if (throttle == 0f)
        {
            Vector3 brakeForce = -rb.velocity * percentageBrakeForceForNonThrottle;
            rb.AddForce(brakeForce, ForceMode.Acceleration);
        }
    }

    // ----------------------------------------------------------------------
    // Method to get the forces and torques based on input values
    void GetForcesTorques()
    {
        // Calculate the allowed steering angle based on the current speed
        float maxSteerAllowed = steerBySpeed.Evaluate(rb.velocity.magnitude);

        // Update the allowed steering angle
        allowedSteering = steering * maxSteerAllowed;

        // Apply the motor torque, steering angle, and brake force based on input values
        currentMotorTorque = throttle * motorTorque;
        currentSteerAngle = allowedSteering * maxSteerAngle;
        currentBrakeForce = isBraking ? brakeForce : 0f;
    }

    // ----------------------------------------------------------------------
    // Methods to handle the force applied to the wheels
    void ApplyHandleMotor()
    {
        // Apply the motor torque to the back wheels
        wheelBackLeft.motorTorque = currentMotorTorque;
        wheelBackRight.motorTorque = currentMotorTorque;

        // Apply the brake force to all wheels
        wheelBackLeft.brakeTorque = currentBrakeForce;
        wheelBackRight.brakeTorque = currentBrakeForce;
        wheelFrontLeft.brakeTorque = currentBrakeForce;
        wheelFrontRight.brakeTorque = currentBrakeForce;
    }

    // ----------------------------------------------------------------------
    // Method to handle steering angle for the front wheels
    void ApplyHandleSteering()
    {         
        // Smoothly interpolate the steering angle of the front wheels towards the target steering angle based on the allowed steering input
        wheelFrontLeft.steerAngle = Mathf.Lerp(wheelFrontLeft.steerAngle, currentSteerAngle, Time.fixedDeltaTime * smoothSteeringFactor);
        wheelFrontRight.steerAngle = Mathf.Lerp(wheelFrontRight.steerAngle, currentSteerAngle, Time.fixedDeltaTime * smoothSteeringFactor);
    }

    // ----------------------------------------------------------------------
    // Method to update the wheel visuals based on the WheelCollider's position and rotation
    void UpdateWheelVisuals(WheelCollider collider, Transform wheelTransform)
    {
        // Get the world position and rotation of the wheel from the WheelCollider
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);

        // Apply the position and rotation to the wheel's Transform for visual representation
        wheelTransform.rotation = rot;
        
        // Move the wheel visual only if the car is moving above a certain speed
        if (rb.velocity.magnitude > minSpeedToMoveWheels)
            wheelTransform.position = pos;
    }

    // ----------------------------------------------------------------------
    // Methods to detect collisions and set the isColliding flag accordingly
    void OnCollisionStay(Collision collision)
    {
        // Set the collision flag to true
        isColliding = true;
    }

    // ----------------------------------------------------------------------
    // Method to reset the collision states when the car exits a collision
    void OnCollisionExit(Collision collision)
    {
        // Set the collision flag to false
        isColliding = false;
    }
}
