// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes

// ----------------------------------------------------------------------
// Class that implements a cascaded PID control architecture to control the drone's position and velocity
// Requires DroneControlInputs to write the output commands
// Requires DroneDynamics for physical parameters
// Requires DroneSetTarget for the target position and orientation
[RequireComponent(typeof(DroneControlInputs))]
[RequireComponent(typeof(DroneDynamics))]
[RequireComponent(typeof(DroneSetTarget))]
public class DronePositionVelocityController : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]
    
    [Tooltip("PID gains for the position loop. Maps position error [m] to desired velocity [m/s] in the drone's local frame")]
    public setPIDValue positionKvalue = new setPIDValue
    {
        Kp = 1.5f,
        Ki = 0.0f,
        Kd = 0.0f
    };

    [Tooltip("PID gains for the velocity loop. Maps velocity error [m/s] to desired acceleration [m/s²] in the drone's local frame")]
    public setPIDValue speedKvalue = new setPIDValue
    {
        Kp = 1.5f,
        Ki = 0.0f,
        Kd = 0.0f
    };

    [Tooltip("PID gains for the altitude loop. Maps vertical position error [m] to a normalized throttle command [-1, 1]")]
    public setPIDValue altitudeKvalue = new setPIDValue
    {
        Kp = 0.5f,
        Ki = 0.1f,
        Kd = 0.15f
    };

    [Tooltip("PID gains for the yaw heading loop. Maps heading error [deg] to a normalized yaw rate command [-1, 1]")]
    public setPIDValue yawHeadingKvalue = new setPIDValue
    {
        Kp = 0.02f,
        Ki = 0.0f,
        Kd = 0.005f
    };

    [Tooltip("Yaw heading error deadband in [degrees]. Within this range, yaw command is forced to zero")]
    public float yawHeadingDeadbandDeg = 2.0f;

    [Tooltip("Minimum absolute yaw command. Commands below this threshold are suppressed to avoid micro-torque oscillations")]
    public float yawCommandDeadband = 0.01f;

    [Tooltip("Maximum yaw rate in degrees per second that the controller can command. This is used to limit the rate of change of the smoothed target")]
    public float yawRateCurrentLimit = 50f;

    [Tooltip("If the target position is below this altitude, the controller will allow commanding a slight negative altitude to ensure proper landing")]
    public float minDesiredAltitude = 0.5f;

    [Tooltip("Minimum squared magnitude of the horizontal position error for auto-heading to be active")]
    public float minSqrMagnitudeForAutoHeading = 1.0f;

    [Tooltip("Factor to determine if we're in a landing scenario based on the target altitude")]
    public float isLandingAltitudeFactor = 1.5f;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]

    [Tooltip("Position error expressed in the drone's local frame [m]")]
    public Vector3 posErrorLocal;

    [Tooltip("Desired velocity in the drone's local frame [m/s] output by the position PID")]
    public Vector3 velDesiredLocal;

    [Tooltip("Desired acceleration in the drone's local frame [m/s²] output by the velocity PID")]
    public Vector3 accDesiredLocal;

    [Tooltip("Target pitch angle computed from the desired forward acceleration [rad]")]
    public float targetPitchRad;

    [Tooltip("Target roll angle computed from the desired lateral acceleration [rad]")]
    public float targetRollRad;

    [Tooltip("Normalized throttle command written to DroneControlInputs")]
    public float outputThrottle;

    [Tooltip("Normalized pitch command written to DroneControlInputs")]
    public float outputPitch;

    [Tooltip("Normalized roll command written to DroneControlInputs")]
    public float outputRoll;

    [Tooltip("Normalized yaw command written to DroneControlInputs")]
    public float outputYaw;

    // ----------------------------------------------------------------------
    // Private variables

    // Smoothed target heading used for yaw control to avoid oscillations when the target orientation changes abruptly
    private float smoothedTargetHeading;

    // Reference to the Rigidbody component for velocity calculations
    private Rigidbody rb;

    // Reference to the DroneDynamics component for physical parameters
    private DroneDynamics droneDynamics;

    // Reference to the DroneControlInputs component to write the output commands
    private DroneControlInputs droneControlInputs;

    // Reference to the DroneSetTarget component to read the target position and orientation
    private DroneSetTarget droneSetTarget;

    // PID controllers for position, velocity, and yaw control
    private VectorPID positionPID;
    private VectorPID speedPID;
    private PID yawPID;

    // Gravity magnitude used in the tilt-to-acceleration model
    private float gravity;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get references to required components
        rb = GetComponent<Rigidbody>();
        droneDynamics = GetComponent<DroneDynamics>();
        droneControlInputs = GetComponent<DroneControlInputs>();
        droneSetTarget = GetComponent<DroneSetTarget>();

        // Initialize PID controllers
        positionPID = new VectorPID();
        speedPID = new VectorPID();
        yawPID = new PID();

        // Get the gravity magnitude (positive value) for use in the tilt-to-acceleration conversion
        gravity = Mathf.Abs(Physics.gravity.y);

        // Initialize the smoothed target heading to the current drone heading at startup
        smoothedTargetHeading = transform.eulerAngles.y;
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // If the control mode is not set to Automatic, switch it to Automatic to ensure the controller can take over the commands
        if (droneControlInputs.controlMode != DroneControlMode.Automatic)
        {
            droneControlInputs.controlMode = DroneControlMode.Automatic;
        }

        // Calculate the position error in the drone's local frame (ignoring vertical error)
        CalculatePositionError();

        // Calculate the desired velocity in the drone's local frame based on the position error
        CalculateDesiredVelocity();

        // Calculate the desired acceleration in the drone's local frame based on the velocity error
        CalculateDesiredAcceleration();

        // Calculate the desired pitch and roll angles based on the desired acceleration using a simple tilt-to-acceleration model
        CalculateDesiredAttitude();

        // Apply the desired pitch, roll, throttle, and yaw commands to the DroneControlInputs component
        ApplyControl();
    }

    // ----------------------------------------------------------------------
    // Method to calculate the position error in the drone's local frame, ignoring the vertical component
    void CalculatePositionError()
    {
        // Calculate the world-space horizontal position error
        Vector3 worldError = droneSetTarget.target.position - transform.position;
        worldError.y = 0f;

        // Project the world-space error onto the drone's local forward and right axes
        float forwardError = Vector3.Dot(worldError, transform.forward);
        float lateralError = Vector3.Dot(worldError, transform.right);

        // Store as a local-frame vector (Y = 0 because altitude is controlled separately)
        posErrorLocal = new Vector3(lateralError, 0f, forwardError);
    }

    // ----------------------------------------------------------------------
    // Method to calculate the desired velocity in the drone's local frame based on the position error
    void CalculateDesiredVelocity()
    {
        // Run the position PID on the local-frame position error
        velDesiredLocal = positionPID.Compute(
            posErrorLocal,
            positionKvalue.Kp,
            positionKvalue.Ki,
            positionKvalue.Kd
        );

        // Clamp horizontal speed to the cruise speed defined in DroneSetTarget
        float cruiseSpeed = droneSetTarget.target.cruiseSpeed;
        velDesiredLocal = Vector3.ClampMagnitude(velDesiredLocal, cruiseSpeed);
    }

    // ----------------------------------------------------------------------
    // Method to calculate the desired acceleration in the drone's local frame based on the velocity error
    void CalculateDesiredAcceleration()
    {
        // Get the current forward and lateral velocity components by projecting the Rigidbody's velocity onto the drone's local axes
        float currentForward = Vector3.Dot(rb.velocity, transform.forward);
        float currentLateral = Vector3.Dot(rb.velocity, transform.right);

        // Create a local-frame vector for the current velocity
        Vector3 velCurrentLocal = new Vector3(currentLateral, 0f, currentForward);

        // Calculate the velocity error in the drone's local frame
        Vector3 velErrorLocal = velDesiredLocal - velCurrentLocal;

        // Run the velocity PID on the local-frame velocity error to get the desired acceleration
        accDesiredLocal = speedPID.Compute(
            velErrorLocal,
            speedKvalue.Kp,
            speedKvalue.Ki,
            speedKvalue.Kd
        );
    }

    // ----------------------------------------------------------------------
    // Method to calculate the desired pitch and roll angles based on the desired acceleration
    void CalculateDesiredAttitude()
    {
        // Use a tilt-to-acceleration model where the desired forward and lateral accelerations are achieved by tilting the drone
        targetPitchRad =  Mathf.Atan2(accDesiredLocal.z, gravity);
        targetRollRad  =  Mathf.Atan2(accDesiredLocal.x, gravity);
    }

    // ----------------------------------------------------------------------
    // Method to apply the desired pitch and roll angles to DroneControlInputs
    void ApplyControl()
    {
        // Convert the max tilt angle from degrees to radians for normalization
        float maxTiltRad = droneDynamics.maxTiltAngle * Mathf.Deg2Rad;

        // Normalize the desired pitch and roll angles to [-1, 1]
        float pitchCommand = Mathf.Clamp(targetPitchRad / maxTiltRad, -1f, 1f);
        float rollCommand  = Mathf.Clamp(targetRollRad  / maxTiltRad, -1f, 1f);
        
        // Get the desired altitude from the target position
        float desiredDroneAltitude = droneSetTarget.target.position.y;

        // If the desired altitude is very low, allow the controller to command a slight negative altitude
        if(desiredDroneAltitude < minDesiredAltitude)
        {
            // If we're close to the ground or pad, allow commanding a negative altitude to ensure we can land properly
            if(!droneDynamics.isCollidingWithGroundOrPad)
                desiredDroneAltitude = -minDesiredAltitude;
        }

        // Calculate the altitude error
        float altitudeError = desiredDroneAltitude - transform.position.y;

        // Calculate the desired vertical speed using the altitude PID. The output is clamped to the climb and descent speeds defined in DroneSetTarget
        float desiredVerticalSpeed;
        if (altitudeError >= 0f)
            desiredVerticalSpeed = Mathf.Min(altitudeError * altitudeKvalue.Kp, droneSetTarget.target.climbSpeed);
        else
            desiredVerticalSpeed = Mathf.Max(altitudeError * altitudeKvalue.Kp, -droneSetTarget.target.descentSpeed);

        // Convert the desired vertical speed to a normalized throttle command
        float throttleCommand;
        if (desiredVerticalSpeed >= 0f)
            throttleCommand = desiredVerticalSpeed / droneDynamics.maxClimbSpeedAllowed;
        else
            throttleCommand = desiredVerticalSpeed / droneDynamics.maxDescentSpeedAllowed;

        // Clamp the throttle command to [-1, 1]
        throttleCommand = Mathf.Clamp(throttleCommand, -1f, 1f);

        // If we're colliding with the ground or pad, prevent commanding a negative throttle that could cause the drone to try to go underground
        if (droneDynamics.isCollidingWithGroundOrPad && throttleCommand < 0f)
            throttleCommand = 0f;

        // Calculate the current yaw and the target yaw heading based on the target orientation mode
        float currentYaw = transform.eulerAngles.y;
        float finalTargetHeading;

        // Calculate the world-space horizontal error vector from the drone to the target position
        Vector3 worldError = droneSetTarget.target.position - transform.position;
        worldError.y = 0f;

        // Calculate the squared magnitude of the horizontal error for use in the auto-heading logic
        float sqrDistXZ = worldError.sqrMagnitude;

        // Determine if we're in a landing scenario based on the target altitude
        bool isLanding = droneSetTarget.target.position.y < minDesiredAltitude * isLandingAltitudeFactor;

        // If autoOrientation is enabled and we're not in a landing scenario, calculate the target heading using atan2 based on the horizontal error vector
        if (droneSetTarget.autoOrientation && !isLanding)
        {
            // Use atan2 to calculate the target heading, but only if the error is significant to avoid atan2(0,0) which can cause erratic behavior
            if (sqrDistXZ > minSqrMagnitudeForAutoHeading)
                finalTargetHeading = Mathf.Atan2(worldError.x, worldError.z) * Mathf.Rad2Deg;
            else
                finalTargetHeading = smoothedTargetHeading;
        }
        else
        {
            // Get the target yaw from the DroneSetTarget's target orientation
            float targetYaw = droneSetTarget.target.orientation;

            // Smoothly interpolate the smoothedTargetHeading towards the targetYaw with a maximum rate 
            finalTargetHeading = Mathf.MoveTowardsAngle(
                smoothedTargetHeading,
                targetYaw,
                yawRateCurrentLimit * Time.fixedDeltaTime
            );
        }

        // Smoothly interpolate the smoothedTargetHeading towards the finalTargetHeading using Mathf.MoveTowardsAngle
        smoothedTargetHeading = Mathf.MoveTowardsAngle(
            smoothedTargetHeading,
            finalTargetHeading,
            droneDynamics.maxYawRateAllowed * Time.fixedDeltaTime
        );

        // Calculate the yaw error using the smoothed heading
        float yawError = Mathf.DeltaAngle(currentYaw, smoothedTargetHeading);

        // Avoid constant sign changes around zero heading error
        if (Mathf.Abs(yawError) <= Mathf.Max(0f, yawHeadingDeadbandDeg))
        {
            // If the yaw error is within the deadband, suppress the yaw command and reset the yaw PID to avoid integral windup and oscillations
            yawPID.Reset();

            // Write the pitch, roll, and throttle commands to DroneControlInputs, but set yaw command to zero
            droneControlInputs.pitchInput = pitchCommand;
            droneControlInputs.rollInput = rollCommand;
            droneControlInputs.throttleInput = throttleCommand;
            droneControlInputs.yawInput = 0f;

            // Set the output variables for debugging/visualization
            outputPitch = pitchCommand;
            outputRoll = rollCommand;
            outputThrottle = throttleCommand;
            outputYaw = 0f;
            return;
        }

        // Calculate the yaw command using the yaw PID controller based on the yaw error, and clamp it to [-1, 1]
        float yawCommand = Mathf.Clamp(yawPID.Compute(
            yawError,
            yawHeadingKvalue.Kp,
            yawHeadingKvalue.Ki,
            yawHeadingKvalue.Kd
        ), -1f, 1f);

        // Apply a deadband to the yaw command to prevent micro-torque oscillations when the command is very small
        if (Mathf.Abs(yawCommand) < yawCommandDeadband)
            yawCommand = 0f;

        // Write the pitch, roll, throttle, and yaw commands to the DroneControlInputs component
        droneControlInputs.pitchInput = pitchCommand;
        droneControlInputs.rollInput = rollCommand;
        droneControlInputs.throttleInput = throttleCommand;
        droneControlInputs.yawInput = yawCommand;

        // Set the output variables for debugging/visualization
        outputPitch = pitchCommand;
        outputRoll = rollCommand;
        outputThrottle = throttleCommand;
        outputYaw = yawCommand;
    }
}
