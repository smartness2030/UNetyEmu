// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes

// -------------------------------------------------------
// Class to create a DroneDynamics script to apply forces and torques to the drone's Rigidbody based on the throttle, pitch, roll, and yaw inputs
// Requires a Rigidbody component to read the drone's velocity and apply forces
[RequireComponent(typeof(Rigidbody))]
public class DroneDynamics : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Throttle input to control the percentage of maximum thrust applied by the motors")]
    [Range(-1f, 1f)] public float throttle = 0f;

    [Tooltip("Pitch input to control the forward/backward tilt of the drone, where -1 is maximum backward tilt and +1 is maximum forward tilt")]
    [Range(-1f, 1f)] public float pitch = 0f;

    [Tooltip("Roll input to control the left/right tilt of the drone, where -1 is maximum left tilt and +1 is maximum right tilt")]
    [Range(-1f, 1f)] public float roll = 0f;

    [Tooltip("Yaw input to control the rotation of the drone around its vertical axis, where -1 is maximum left rotation and +1 is maximum right rotation")]
    [Range(-1f, 1f)] public float yaw = 0f;

    [Tooltip("Maximum climb speed allowed when the throttle input is positive, in [meters per second]")]
    public float maxClimbSpeedAllowed = 8f;

    [Tooltip("Maximum descent speed allowed when the throttle input is negative, in [meters per second]")]
    public float maxDescentSpeedAllowed = 5f;

    [Tooltip("Maximum yaw rate allowed when the yaw input is at its maximum value, in [degrees/second]. This is used to normalize the maximum yaw rate in DroneStabilizationController")]
    public float maxYawRateAllowed = 90f;

    [Tooltip("Maximum tilt angle the drone can reach, in [degrees]. This is used to limit the maximum pitch and roll angles of the drone and to control the maximum cruise speed of the drone, as higher tilt angles allow for higher horizontal speeds")]
    public float maxTiltAngle = 40f;

    [Tooltip("Maximum force that can be applied by each motor, in [Newtons]. This is used to limit the forces applied by the motors based on the throttle, pitch and roll inputs and simulate realistic physics behavior")]
    public float maxForcePerMotor = 32f;

    [Tooltip("Whether to turn on the drone's motors and apply forces based on the inputs, or to keep the drone stationary regardless of the inputs")]
    public bool turnOnDroneMotors = true;

    [Tooltip("Whether to show debug rays in the scene view to visualize the forces applied by each motor")]
    public bool showGizmos = true;

    // ----------------------------------------------------------------------
    // Private input variables

    [Header("• PRIVATE INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Number of motors on the drone, used to calculate the forces applied by each motor based on the throttle input. This is Script is set to 4 for a quadcopter configuration")]
    [SerializeField] private int numMotors = 4;

    [Tooltip("Transform GameObject belonging to the front left motor (where the force of the front left propeller is applied)")]
    [SerializeField] private Transform motorFrontLeft;

    [Tooltip("Transform GameObject belonging to the front right motor (where the force of the front right propeller is applied)")]
    [SerializeField] private Transform motorFrontRight;

    [Tooltip("Transform GameObject belonging to the back right motor (where the force of the back right propeller is applied)")]
    [SerializeField] private Transform motorBackRight;

    [Tooltip("Transform GameObject belonging to the back left motor (where the force of the back left propeller is applied)")]
    [SerializeField] private Transform motorBackLeft;

    [Tooltip("Maximum Torque that can be applied by the motors for yaw control, in [Newton-meters]. This parameter directly affects the maximum yaw rate of the drone and can be tuned to achieve the desired responsiveness to yaw inputs together with the maxYawRateAllowed parameter")]
    [SerializeField] private float torqueAppliedToControlYawRate = 5f;

    [Tooltip("Unladen weight of the drone in [kilograms], used to simulate realistic physics behavior when the drone is not carrying any package")]
    [SerializeField] public float unladenWeight = 8f;

    [Tooltip("Maximum package weight that the drone can support in [kilograms], used to simulate realistic physics behavior when the drone is carrying a package")]
    [SerializeField] public float maxPackageWeightSupported = 3f;

    [Tooltip("Linear drag coefficient to simulate air resistance, used for basic speed limiting and more realistic physics behavior at low speeds. This is set to a low value to use a quadratic drag model for more realistic speed limiting at higher speeds with the quadraticDragCoefficient")]
    [SerializeField] private float dragCoefficient = 0.1f;

    [Tooltip("Angular drag value to simulate air resistance that opposes the drone's rotation, used to prevent excessive spinning and improve stability, especially for yaw control")]
    [SerializeField] private float angularDragCoefficient = 2.8f;

    [Tooltip("Quadratic drag coefficient to simulate air resistance that increases with speed, used for more realistic speed limiting behavior at higher speeds")]
    [SerializeField] private float quadraticDragCoefficient = 0.4f;

    [Tooltip("Cross-Coupling Compensation Gain to compensate for the coupling effects between pitch, roll and yaw inputs")]
    [SerializeField] private float pitchRollCouplingGain = 2.0f;

    [Tooltip("Yaw input threshold to apply cross-coupling compensation to improve stability during yaw maneuvers")]
    [SerializeField] private float yawToleranceForCrossCoupling = 0.1f;

    [Tooltip("Pitch and Roll input magnitude threshold to apply cross-coupling compensation to avoid applying compensation when the pitch and roll inputs are very low and the coupling effects are negligible")]
    [SerializeField] private float pitchAndRollToleranceForCrossCoupling = 0.1f;

    [Tooltip("Minimum speed to show in the currentSpeed variable, in [meters per second]. This is used to avoid showing very low speeds due to physics simulation noise when the drone is almost stationary")]
    [SerializeField] private float minSpeedToShow = 0.01f;

    [Tooltip("Minimum yaw rate to show in the currentYawRate variable, in [degrees per second]. This is used to avoid showing very low yaw rates due to physics simulation noise when the drone is almost stationary in yaw")]
    [SerializeField] private float minYawRateToShow = 0.1f;

    [Tooltip("Minimum threshold for the sum of raw forces to apply normalization, to avoid applying excessive forces when the sum of raw forces is very low")]
    [SerializeField] private float minNormFactorThreshold = 0.0001f;

    [Tooltip("Debug scale factor for the debug rays that visualize the forces applied by each motor")]
    [SerializeField] private float debugScale = 0.02f;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Whether the drone is currently colliding with any object, used to implement collision response and prevent unrealistic behavior when the drone is in contact with the ground or other objects")]
    public bool isColliding = false;

    [Tooltip("Whether the drone is currently colliding only with the ground or the drone pad")]
    public bool isCollidingWithGroundOrPad = false;

    [Tooltip("Current speed of the drone in [meters per second], calculated from the Rigidbody's velocity and used for monitoring")]
    public float currentSpeed;

    [Tooltip("Current velocity of the drone in [meters per second], calculated from the Rigidbody's velocity and used for monitoring")]
    public Vector3 currentVelocity;

    [Tooltip("Current yaw rate of the drone in [degrees per second], calculated from the Rigidbody's angular velocity and used for monitoring")]
    public float currentYawRate;
    
    [Tooltip("Current total force applied by the motors in [Newtons], calculated from the individual forces applied by each motor and used for monitoring")]
    public float currentTotalForceAppliedToMotors;

    [Tooltip("Current force applied by the front left motor in [Newtons], calculated from the throttle, pitch and roll inputs and used for monitoring")]
    public float forceFrontLeft;

    [Tooltip("Current force applied by the front right motor in [Newtons], calculated from the throttle, pitch and roll inputs and used for monitoring")]
    public float forceFrontRight;

    [Tooltip("Current force applied by the back right motor in [Newtons], calculated from the throttle, pitch and roll inputs and used for monitoring")]
    public float forceBackRight;

    [Tooltip("Current force applied by the back left motor in [Newtons], calculated from the throttle, pitch and roll inputs and used for monitoring")]
    public float forceBackLeft;

    [Tooltip("Current total torque applied by the motors in [Newton-meters], calculated from the individual torques applied by each motor and used for monitoring")]
    public Vector3 currentTotalTorqueAppliedToMotors;

    [Tooltip("Current torque applied by the front left motor in [Newton-meters], calculated from the throttle, pitch and roll inputs and used for monitoring")]
    public Vector3 torqueFrontLeft;

    [Tooltip("Current torque applied by the front right motor in [Newton-meters], calculated from the throttle, pitch and roll inputs and used for monitoring")]
    public Vector3 torqueFrontRight;

    [Tooltip("Current torque applied by the back right motor in [Newton-meters], calculated from the throttle, pitch and roll inputs and used for monitoring")]
    public Vector3 torqueBackRight;

    [Tooltip("Current torque applied by the back left motor in [Newton-meters], calculated from the throttle, pitch and roll inputs and used for monitoring")]
    public Vector3 torqueBackLeft;

    // ----------------------------------------------------------------------
    // Private variables

    // Rigidbody component of the drone, used to read the drone's velocity and apply forces and torques for the physics simulation
    private Rigidbody rb;

    // To store the current throttle, pitch, roll and yaw inputs
    private float currentThrottle;
    private float currentPitch;
    private float currentRoll;
    private float currentYaw;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();

        // Set the Rigidbody properties for realistic drone physics behavior
        rb.centerOfMass = Vector3.zero;
        rb.mass = unladenWeight;
        rb.drag = dragCoefficient;
        rb.angularDrag = angularDragCoefficient;
        rb.useGravity = true;
        
        // Initialize the forces applied by the motors
        forceFrontLeft = 0f;
        forceFrontRight = 0f;
        forceBackRight = 0f;
        forceBackLeft = 0f;
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // Identify the current total mass of the drone including the package it is carrying and turn on the motors if the conditions are met
        IdentifyCurrentMassAndTurnOnMotors();

        // Apply quadratic drag for more realistic speed limiting at higher speeds, simulating the effect of air resistance that increases with speed
        ApplyQuadraticDrag();
        
        // Calculate and apply the forces and torques to the drone's Rigidbody based on the throttle, pitch, roll, and yaw inputs
        ApplyForceTorqueToMotors();

        // Apply cross-coupling compensation for the pitch and roll inputs based on the yaw input, to compensate for the coupling effects
        ApplyCrossCouplingCompensation(currentPitch, currentRoll, currentYaw);

        // Apply tilt limiting to limit the maximum tilt angle of the drone by applying a restoring torque when the drone exceeds the maximum tilt angle
        ApplyTiltLimiting();
        
        // Update the current speed and velocity of the drone based on the Rigidbody's velocity for monitoring purposes
        ShowCurrentSpeedAndVelocity();

        // Update the total forces and torques applied by the motors for monitoring purposes
        ShowTotalForcesAndTorquesApplied();

        // Draw debug rays to visualize the forces applied by each motor, with different colors for each motor and a scaling factor for better visibility
        if (showGizmos)
            showDrawRayGizmos();
    }

    // ----------------------------------------------------------------------
    // Method to identify the current total mass of the drone including the package it is carrying and to turn on the motors
    void IdentifyCurrentMassAndTurnOnMotors()
    {
        // Calculate the package mass by subtracting the unladen weight of the drone from the current total mass obtained from the FixedJoint component
        float packageMass = GetCurrentTotalMass() - unladenWeight;

        // Turn on the motors only if the turnOnDroneMotors variable is true and the package mass is within the maximum weight supported by the drone
        if(!turnOnDroneMotors || packageMass > maxPackageWeightSupported)
        {
            forceFrontLeft = 0f;
            forceFrontRight = 0f;
            forceBackRight = 0f;
            forceBackLeft = 0f;
            return;
        } 
    }

    // ----------------------------------------------------------------------
    // Method to calculate the current total mass of the drone including the package it is carrying
    float GetCurrentTotalMass()
    {
        // Start with the unladen weight of the drone
        float totalMass = unladenWeight;

        // Add the mass of the package being carried obtained from the FixedJoint component
        FixedJoint joint = GetComponent<FixedJoint>();
        if (joint != null && joint.connectedBody != null)
            totalMass += joint.connectedBody.mass;
        
        // Return the total mass of the drone including the package
        return totalMass;
    }

    // ----------------------------------------------------------------------
    // Method to apply quadratic drag for more realistic speed limiting at higher speeds, simulating the effect of air resistance that increases with speed
    void ApplyQuadraticDrag()
    {
        // Get the current velocity of the drone from the Rigidbody component
        Vector3 velocity = rb.velocity;

        // Calculate the quadratic drag force using the formula: F_drag = - C * v * |v|, where C is the quadratic drag coefficient and v is the velocity vector
        Vector3 quadraticDrag = - quadraticDragCoefficient * velocity * velocity.magnitude;

        // Apply the quadratic drag force to the Rigidbody to simulate air resistance
        rb.AddForce(quadraticDrag);
    }

    // ----------------------------------------------------------------------
    // Method to calculate and apply the forces and torques to the drone's Rigidbody based on the throttle, pitch, roll, and yaw inputs
    void ApplyForceTorqueToMotors()
    {
        // Get the current throttle, pitch, roll and yaw inputs and clamp them to the range [-1, 1] to ensure they are within the expected limits
        currentThrottle = Mathf.Clamp(throttle, -1f, 1f);
        currentPitch = Mathf.Clamp(pitch, -1f, 1f);
        currentRoll = Mathf.Clamp(roll, -1f, 1f);
        currentYaw = Mathf.Clamp(yaw, -1f, 1f);

        // Calculate the maximum total thrust the drone can generate
        float maxTotalThrust = numMotors * maxForcePerMotor;

        // Calculate the total positive thrust based on the throttle input, where a throttle of -1 corresponds to 0 total thrust and a throttle of +1 corresponds to maxTotalThrust
        float totalForce = Mathf.Clamp01((currentThrottle + 1f) * 0.5f) * maxTotalThrust;

        // Calculate the base force for each motor by dividing the total force by the number of motors
        float baseForce = totalForce / numMotors;

        // Calculate the raw forces for each motor based on the pitch, roll and yaw inputs using a simple mixing algorithm for a quadcopter configuration
        float rawFL = 1f - currentPitch + currentRoll - currentYaw;
        float rawFR = 1f - currentPitch - currentRoll + currentYaw;
        float rawBR = 1f + currentPitch - currentRoll - currentYaw;
        float rawBL = 1f + currentPitch + currentRoll + currentYaw;

        // Calculate the sum of the raw forces to use for normalization
        float sumRaw = rawFL + rawFR + rawBR + rawBL;

        // Calculate the normalization factor to scale the raw forces so that the maximum force applied by any motor does not exceed the maxForcePerMotor limit
        float normFactor;
        if (sumRaw > minNormFactorThreshold)
            normFactor = numMotors / sumRaw;
        else
            normFactor = 1f;

        // Calculate the final forces for each motor by multiplying the base force by the normalized raw forces
        forceFrontLeft  = baseForce * rawFL * normFactor;
        forceFrontRight = baseForce * rawFR * normFactor;
        forceBackRight  = baseForce * rawBR * normFactor;
        forceBackLeft   = baseForce * rawBL * normFactor;

        // Ensure that the forces applied by each motor are not negative to prevent applying downward forces that could cause unrealistic behavior
        forceFrontLeft = Mathf.Max(0f, forceFrontLeft);
        forceFrontRight = Mathf.Max(0f, forceFrontRight);
        forceBackRight = Mathf.Max(0f, forceBackRight);
        forceBackLeft = Mathf.Max(0f, forceBackLeft);

        // Check for NaN values in the forces and set them to 0 to prevent applying invalid forces that could cause unrealistic behavior
        if(float.IsNaN(forceFrontLeft) || float.IsNaN(forceFrontRight) || float.IsNaN(forceBackRight) || float.IsNaN(forceBackLeft))
        {
            forceFrontLeft = 0f;
            forceFrontRight = 0f;
            forceBackRight = 0f;
            forceBackLeft = 0f;
        }
        
        // Apply the calculated forces at the positions of the motors to create the appropriate torques for pitch and roll
        rb.AddForceAtPosition(transform.up * forceFrontLeft, motorFrontLeft.position);
        rb.AddForceAtPosition(transform.up * forceFrontRight, motorFrontRight.position);
        rb.AddForceAtPosition(transform.up * forceBackRight, motorBackRight.position);
        rb.AddForceAtPosition(transform.up * forceBackLeft, motorBackLeft.position);

        // Apply the torque for yaw control based on the current yaw input and the torqueAppliedToControlYawRate parameter, which directly affects the maximum yaw rate of the drone
        rb.AddTorque(transform.up * currentYaw * torqueAppliedToControlYawRate);        
    }

    // ----------------------------------------------------------------------
    // Method to apply cross-coupling compensation for the pitch and roll inputs based on the yaw input, to compensate for the coupling effects between pitch, roll and yaw inputs and improve stability during yaw maneuvers
    void ApplyCrossCouplingCompensation(float pitch, float roll, float yawInput)
    {
        // Apply cross-coupling compensation only if the yaw input is above the specified threshold to avoid applying compensation when the yaw input is very low and the coupling effects are negligible
        if (Mathf.Abs(yawInput) > yawToleranceForCrossCoupling)
        {
            // Calculate the magnitude of the pitch and roll inputs
            float pitchRollMagnitude = Mathf.Sqrt(pitch * pitch + roll * roll);
            
            // Apply cross-coupling compensation only if the magnitude of the pitch and roll inputs is above the specified threshold to avoid applying compensation when the pitch and roll inputs are very low
            if (pitchRollMagnitude > pitchAndRollToleranceForCrossCoupling)
            {
                // Calculate the correction torque to apply to the motors based on the pitch and roll inputs, the yaw input and the pitchRollCouplingGain parameter, which can be tuned to achieve the desired level of compensation
                Vector3 correctionTorque = new Vector3(
                    pitch * yawInput * pitchRollCouplingGain,
                    0f,
                    roll * yawInput * pitchRollCouplingGain
                );
                
                // Apply the correction torque to the Rigidbody to compensate for the coupling effects between pitch, roll and yaw inputs and improve stability during yaw maneuvers
                rb.AddRelativeTorque(correctionTorque);
            }
        }
    }

    // ----------------------------------------------------------------------
    // Method to limit the maximum tilt angle of the drone by applying a restoring torque when the drone exceeds the maximum tilt angle
    void ApplyTiltLimiting()
    {
        // Calculate the current tilt angle of the drone by finding the angle between the drone's up vector and the world up vector
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        // If the tilt angle exceeds the maximum tilt angle, apply a restoring torque to bring the drone back within the limits
        if (tiltAngle > maxTiltAngle)
        {
            // Calculate the axis of rotation for the restoring torque by finding the cross product between the drone's up vector and the world up vector
            Vector3 tiltAxis = Vector3.Cross(transform.up, Vector3.up);

            // Calculate the magnitude of the restoring torque based on how much the tilt angle exceeds the maximum tilt angle
            float restoreTorque = tiltAngle - maxTiltAngle;

            // Apply the restoring torque to the Rigidbody to limit the maximum tilt angle of the drone
            rb.AddTorque(tiltAxis.normalized * restoreTorque, ForceMode.Force);
        }
    }

    // ----------------------------------------------------------------------
    // Method to show the current speed and velocity of the drone
    void ShowCurrentSpeedAndVelocity()
    {
        // Get the current speed of the drone from the Rigidbody's velocity magnitude, and set it to 0 if it is below the minimum speed to show for monitoring purposes
        currentSpeed = rb.velocity.magnitude;
        currentVelocity = rb.velocity;
        if (rb.velocity.magnitude < minSpeedToShow)
        {
            currentSpeed = 0f;
            currentVelocity = Vector3.zero;
        }

        // Get the current yaw rate of the drone from the Rigidbody's angular velocity around the local y-axis, and convert it from radians per second to degrees per second for monitoring purposes
        currentYawRate = rb.angularVelocity.y * Mathf.Rad2Deg;
        if (Mathf.Abs(currentYawRate) < minYawRateToShow)
            currentYawRate = 0f;
    }

    // ----------------------------------------------------------------------
    // Method to calculate and show the total forces and torques applied by the motors, which can be useful for monitoring and debugging
    void ShowTotalForcesAndTorquesApplied()
    {
        // Get the current center of mass of the drone from the Rigidbody component
        Vector3 com = rb.worldCenterOfMass;

        // Calculate the position vectors from the center of mass to each motor
        Vector3 rFL = motorFrontLeft.position - com;
        Vector3 rFR = motorFrontRight.position - com;
        Vector3 rBR = motorBackRight.position - com;
        Vector3 rBL = motorBackLeft.position - com;

        // Calculate the force vectors for each motor based on the forces applied and the drone's up direction
        Vector3 fFL = transform.up * forceFrontLeft;
        Vector3 fFR = transform.up * forceFrontRight;
        Vector3 fBR = transform.up * forceBackRight;
        Vector3 fBL = transform.up * forceBackLeft;

        // Calculate the torque applied by each motor using the cross product of the position vector and the force vector
        torqueFrontLeft = Vector3.Cross(rFL, fFL);
        torqueFrontRight = Vector3.Cross(rFR, fFR);
        torqueBackRight = Vector3.Cross(rBR, fBR);
        torqueBackLeft = Vector3.Cross(rBL, fBL);

        // Calculate the total torque applied by the motors by summing the individual torques
        currentTotalTorqueAppliedToMotors = torqueFrontLeft + torqueFrontRight + torqueBackRight + torqueBackLeft;

        // Calculate the current total force applied by the motors by summing the individual forces
        currentTotalForceAppliedToMotors = forceFrontLeft + forceFrontRight + forceBackRight + forceBackLeft;
    }

    // ----------------------------------------------------------------------
    // Methods to detect collisions and set the isColliding and isCollidingWithGroundOrPad variables accordingly
    void OnCollisionStay(Collision collision)
    {
        // Set the isColliding variable to true when the drone is colliding with any object
        isColliding = true;

        // Set the isCollidingWithGroundOrPad variable to true when the drone is colliding with the ground or the drone pad
        if (collision.gameObject.CompareTag("DronePad") || collision.gameObject.CompareTag("Ground"))
            isCollidingWithGroundOrPad = true;
    }

    // ----------------------------------------------------------------------
    // Method to reset the collision states when the drone exits a collision
    void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        isCollidingWithGroundOrPad = false;
    }

    // ----------------------------------------------------------------------
    // Method to draw debug rays in the scene view to visualize the forces applied by each motor
    void showDrawRayGizmos()
    {
        Debug.DrawRay(motorFrontLeft.position, transform.up * forceFrontLeft * debugScale, Color.red);
        Debug.DrawRay(motorFrontRight.position, transform.up * forceFrontRight * debugScale, Color.green);
        Debug.DrawRay(motorBackRight.position, transform.up * forceBackRight * debugScale, Color.blue);
        Debug.DrawRay(motorBackLeft.position, transform.up * forceBackLeft * debugScale, Color.yellow);
    }
}
