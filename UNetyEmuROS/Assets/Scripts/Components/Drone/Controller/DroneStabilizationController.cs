// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes

// ----------------------------------------------------------------------
// Class to control the drone's stabilization using PID and RatePD controllers for throttle, pitch, roll and yaw, which will be used to maintain the drone's position and orientation
// Requires a Rigidbody component to read the drone's current velocity and angular velocity for the control calculations
// Requires a DroneDynamics component to set the desired throttle, pitch, roll and yaw values for the physics calculations
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DroneDynamics))]
public class DroneStabilizationController : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Desired vertical speed command [-1=maxDescentSpeedAllowed, +1=+maxClimbSpeedAllowed]. Interpreted by DroneDynamics as target vertical speed")]
    [Range(-1f, 1f)] public float setVerticalSpeed;

    [Tooltip("Desired pitch angle command [-1=−maxTiltAngle, +1=+maxTiltAngle]. Interpreted by DroneDynamics as target angle")]
    [Range(-1f, 1f)] public float setPitchTarget;

    [Tooltip("Desired roll angle command [-1=−maxTiltAngle, +1=+maxTiltAngle]. Interpreted by DroneDynamics as target angle")]
    [Range(-1f, 1f)] public float setRollTarget;

    [Tooltip("Desired yaw angle command [-1=−maxYawRateAllowed, +1=+maxYawRateAllowed]. Interpreted by DroneDynamics as target yaw rate in deg/s")]
    [Range(-1f, 1f)] public float setYawTarget;

    [Tooltip("PID settings for throttle control (Kp, Ki, Kd)")]
    public setPIDValue throttleKvalue = new setPIDValue
    {
        Kp = 0.8f,
        Ki = 0.4f,
        Kd = 0.1f
    };

    [Tooltip("RatePD settings for pitch control (Kp, Kd)")]
    public setRatePDValue pitchKvalue = new setRatePDValue
    {
        Kp = 0.02f,
        Kd = 0.2f
    };

    [Tooltip("RatePD settings for roll control (Kp, Kd)")]
    public setRatePDValue rollKvalue = new setRatePDValue
    {
        Kp = 0.02f,
        Kd = 0.2f
    };

    [Tooltip("PID settings for yaw control (Kp, Ki, Kd)")]
    public setPIDValue yawKvalue = new setPIDValue
    {
        Kp = 5.0f,
        Ki = 0.0f,
        Kd = 0.0f
    };

    [Tooltip("When enabling this component, synchronize targets with current drone state to avoid abrupt transients")]
    [SerializeField] private bool syncTargetsOnEnable = true;

    [Tooltip("When disabling this component, clear the commands written into DroneDynamics")]
    [SerializeField] private bool resetDynamicsCommandsOnDisable = true;
    
    // ----------------------------------------------------------------------
    // Private variables

    // DroneDynamics component of the drone, used to set the desired throttle, pitch, roll and yaw values for the physics calculations and to read the collision state for the altitude control
    private DroneDynamics droneDynamics;

    // To read the drone's velocity and angular velocity for the control calculations
    private Rigidbody rb;

    // To store the PID and RatePD controllers for throttle, pitch, roll and yaw
    private PID throttlePID;
    private RatePD pitchRatePD;
    private RatePD rollRatePD;
    private PID yawPID;

    // To store the last yaw command for bumpless transfer when enabling/disabling the controller at runtime
    private float lastYawCommand = 0f;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Cache references to components, initialize controllers and reset controller state at the start of the simulation
        CacheReferences();

        // Initialize controllers lazily to ensure they are ready if Start is called after enabling the component at runtime
        InitializeControllers();

        // Reset controller state at the start of the simulation
        ResetControllers();
    }

    // ----------------------------------------------------------------------
    // Apply a bumpless transfer when the controller is enabled at runtime
    void OnEnable()
    {
        // Cache references to components, initialize controllers and reset controller state at the start of the simulation
        CacheReferences();

        // Initialize controllers lazily to ensure they are ready if Start is called after enabling the component at runtime
        InitializeControllers();

        // Reset controller state at the start of the simulation
        ResetControllers();

        // If not synchronizing targets on enable or if required components are missing, do nothing to avoid errors
        if (!syncTargetsOnEnable || rb == null || droneDynamics == null)
            return;

        // Keep hover/level targets when re-enabling to avoid an abrupt control jump
        setVerticalSpeed = 0f;
        setPitchTarget = 0f;
        setRollTarget = 0f;

        // Get the current yaw rate of the drone from the Rigidbody's angular velocity
        float currentYawRateNormalized = transform.InverseTransformDirection(rb.angularVelocity).y * Mathf.Rad2Deg / droneDynamics.maxYawRateAllowed;
        
        // Set the yaw target to the current yaw rate 
        setYawTarget = Mathf.Clamp(currentYawRateNormalized, -1f, 1f);

        // Set the last yaw command to the current yaw rate
        lastYawCommand = setYawTarget;

        // Set the yaw command in the DroneDynamics component to the current yaw rate
        droneDynamics.yaw = lastYawCommand;
    }

    // ----------------------------------------------------------------------
    // Clear commands when disabling the controller so DroneDynamics is not left with stale values
    void OnDisable()
    {
        // If not resetting dynamics commands on disable or if DroneDynamics component is missing, do nothing to avoid errors
        if (!resetDynamicsCommandsOnDisable || droneDynamics == null)
            return;

        // Reset the commands in the DroneDynamics component to zero to avoid leaving stale commands
        droneDynamics.throttle = 0f;
        droneDynamics.pitch = 0f;
        droneDynamics.roll = 0f;
        droneDynamics.yaw = 0f;
    }

    // ----------------------------------------------------------------------
    // Cache required components once and reuse references
    void CacheReferences()
    {
        //  Cache the Rigidbody component to read the drone's velocity and angular velocity for the control calculations
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // Cache the DroneDynamics component to set the desired throttle, pitch, roll and yaw values
        if (droneDynamics == null)
            droneDynamics = GetComponent<DroneDynamics>();
    }

    // ----------------------------------------------------------------------
    // Instantiate controllers lazily so Start and OnEnable are both safe
    void InitializeControllers()
    {
        // Instantiate the PID and RatePD controllers if they are null
        if (throttlePID == null)
            throttlePID = new PID();
        if (pitchRatePD == null)
            pitchRatePD = new RatePD();
        if (rollRatePD == null)
            rollRatePD = new RatePD();
        if (yawPID == null)
            yawPID = new PID();
    }

    // ----------------------------------------------------------------------
    // Reset controller internal state between enable/disable cycles
    void ResetControllers()
    {
        // Reset the internal state of the PID and RatePD controllers
        throttlePID.Reset();
        yawPID.Reset();
        
        // Reset the last yaw command to zero to avoid abrupt changes when enabling the controller again
        lastYawCommand = 0f;
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // Call the methods to stabilize the drone's altitude, attitude and yaw based on the current inputs and the drone's state
        StabilizeAltitude();
        StabilizeAttitude();
        StabilizeYaw();
    }

    // ----------------------------------------------------------------------
    // Method to stabilize the drone's altitude using a PID controller
    void StabilizeAltitude()
    {
        // Get the desired vertical speed from the setVerticalSpeed input, which is a normalized value between -1 and 1
        float desiredVerticalSpeed = setVerticalSpeed;
        
        // Scale the desired vertical speed to the actual speed range defined in DroneDynamics, where positive values correspond to climbing and negative values correspond to descending
        if (desiredVerticalSpeed >= 0f)
            desiredVerticalSpeed = desiredVerticalSpeed * droneDynamics.maxClimbSpeedAllowed;
        else
            desiredVerticalSpeed = desiredVerticalSpeed * droneDynamics.maxDescentSpeedAllowed;

        // If the drone is colliding with the ground or the pad and the desired vertical speed is negative (descending), set the desired vertical speed to 0 to prevent unrealistic behavior
        if (droneDynamics.isCollidingWithGroundOrPad && desiredVerticalSpeed < 0f)
        {
            desiredVerticalSpeed = 0f;
            throttlePID.Reset(); 
        }

        // Get the current vertical speed of the drone from the Rigidbody's velocity
        float currentVerticalSpeed = rb.velocity.y;

        // Calculate the speed error as the difference between the desired vertical speed and the current vertical speed
        float speedError = desiredVerticalSpeed - currentVerticalSpeed;

        // Compute the throttle command using the PID controller based on the speed error and the PID settings defined in throttleKvalue
        float throttleCommand = throttlePID.Compute(
            speedError,
            throttleKvalue.Kp,
            throttleKvalue.Ki,
            throttleKvalue.Kd
        );

        // Calculate a tilt compensation factor based on the drone's current tilt angle, to compensate for the loss of vertical lift when the drone is tilted
        float tiltCompensation = Vector3.Dot(transform.up, Vector3.up);
        tiltCompensation = Mathf.Clamp(tiltCompensation, 0.1f, 1f);

        // Compensate the throttle command by dividing it by the tilt compensation factor
        throttleCommand /= tiltCompensation;

        // If the drone is colliding with the ground or the pad and the throttle command is negative (descending), set the throttle command to 0 to prevent unrealistic behavior
        if (droneDynamics.isCollidingWithGroundOrPad && throttleCommand < 0f)
        {
            throttleCommand = 0f;
            throttlePID.Reset();
        }

        // Clamp the throttle command to the range [-1, 1] to ensure it stays within the valid input range for DroneDynamics
        throttleCommand = Mathf.Clamp(throttleCommand, -1f, 1f);

        // Set the throttle command in the DroneDynamics component to control the vertical speed of the drone
        droneDynamics.throttle = throttleCommand;
    }

    // ----------------------------------------------------------------------
    // Method to stabilize the drone's attitude (pitch and roll) using a Rate PD controller
    void StabilizeAttitude()
    {
        // Get the desired pitch and roll angles from the setPitchTarget and setRollTarget inputs
        float desiredPitch = setPitchTarget;
        float desiredRoll = setRollTarget;

        // Scale the desired pitch and roll angles to the actual angle range defined in DroneDynamics
        float targetPitch = desiredPitch * droneDynamics.maxTiltAngle;
        float targetRoll = desiredRoll * droneDynamics.maxTiltAngle;

        // Get the current pitch and roll angles of the drone from its local Euler angles, and normalize them to the range [-180, 180] degrees for correct error calculation
        Vector3 euler = transform.localEulerAngles;
        float currentPitch = NormalizeAngle(euler.x);
        float currentRoll  = - NormalizeAngle(euler.z);

        // Calculate the pitch and roll errors as the difference between the target angles and the current angles
        float pitchError = targetPitch - currentPitch;
        float rollError = targetRoll  - currentRoll;

        // Get the current pitch and roll rates of the drone from the Rigidbody's angular velocity, transforming it to the local frame of the drone to get the correct rates for the control calculations
        Vector3 localAngularSpeed = transform.InverseTransformDirection(rb.angularVelocity);

        // The pitch rate corresponds to the rotation around the local x-axis and the roll rate corresponds to the rotation around the local z-axis
        float pitchRate = localAngularSpeed.x;
        float rollRate = - localAngularSpeed.z;

        // Compute the pitch and roll corrections using the Rate PD controllers based on the pitch and roll errors, the current pitch and roll rates, and the Rate PD settings defined in pitchKvalue and rollKvalue
        float pitchCorrection = pitchRatePD.Compute(
            pitchError,
            pitchRate,
            pitchKvalue.Kp,
            pitchKvalue.Kd
        );
        float rollCorrection = rollRatePD.Compute(
            rollError,
            rollRate,
            rollKvalue.Kp,
            rollKvalue.Kd
        );

        // Clamp the pitch and roll corrections to the range [-1, 1] to ensure they stay within the valid input range for DroneDynamics
        pitchCorrection = Mathf.Clamp(pitchCorrection, -1f, 1f);
        rollCorrection = Mathf.Clamp(rollCorrection, -1f, 1f);

        // Set the pitch and roll corrections in the DroneDynamics component to control the attitude of the drone        
        droneDynamics.pitch = pitchCorrection;
        droneDynamics.roll = rollCorrection;
    }

    // ----------------------------------------------------------------------
    // Method to stabilize the drone's yaw using a PID controller
    void StabilizeYaw()
    {
        // Get the desired yaw rate from the setYawTarget input
        float desiredYawRate = Mathf.Clamp(setYawTarget, -1f, 1f);

        // Get the current pitch and roll rates of the drone from the Rigidbody's angular velocity, transforming it to the local frame of the drone to get the correct rates for the control calculations   
        Vector3 localAngularSpeed = transform.InverseTransformDirection(rb.angularVelocity);

        // Get the current yaw rate of the drone from the local angular velocity around the local y-axis, and convert it from radians per second to degrees per second for the control calculations
        float currentYawRate = localAngularSpeed.y;
        currentYawRate *= Mathf.Rad2Deg;

        // Scale the current yaw rate by dividing it by the maximum yaw rate allowed defined in DroneDynamics, to get a normalized value for the PID controller
        currentYawRate = currentYawRate / droneDynamics.maxYawRateAllowed;

        // Calculate the yaw rate error as the difference between the desired yaw rate and the current yaw rate
        float yawRateError = desiredYawRate - currentYawRate;

        // Compute the yaw command using the PID controller based on the yaw rate error and the PID settings defined in yawKvalue
        float yawCommand = yawPID.Compute(
            yawRateError,
            yawKvalue.Kp,
            yawKvalue.Ki,
            yawKvalue.Kd
        );

        // Clamp the yaw command to the range [-1, 1] to ensure it stays within the valid input range for DroneDynamics
        yawCommand = Mathf.Clamp(yawCommand, -1f, 1f);

        lastYawCommand = yawCommand;

        // Set the yaw command in the DroneDynamics component to control the yaw of the drone
        droneDynamics.yaw = yawCommand;
    }

    // ----------------------------------------------------------------------
    // Method to normalize an angle to the range [-180, 180] degrees
    float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
