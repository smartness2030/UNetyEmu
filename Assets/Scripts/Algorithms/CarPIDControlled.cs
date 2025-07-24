/*******************************************************************************
* Copyright 2025 INTRIG
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
******************************************************************************/

// Libraries
using UnityEngine; // Unity Engine library to use in MonoBehaviour classes

// Class to handle PID controlled car dynamics
public class CarPIDControlled : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:
    
    // Car features
    public float maxTorque = 1500f;
    public float minTorque = -1500f;
    public float distanceToStartBraking = 5f;
    public float initialTargetSpeed = 5f; // Initial target speed (lower value)
    public float brakeDistance = 2f; // Distance to start braking
    public float stopDistance = 0.5f; // Distance to stop completely
    public float deadzone = 10f; // Deadzone for steering control
    public float maxSpeed = 20.0f;

    // Target position and orientation
    public Vector3 targetPosition;
    public float targetOrientation;

    // -----------------------------------------------------------------------------------------------------
    // Private variables of the class:

    // Required components
    private CarDynamics carController;
    private GetObjectFeatures getObjectFeatures;
    private string objectID;
    private GameObject targetObj;
    private Rigidbody rb;

    // PID control variables
    private float KpThrottle = 0.5f;  // Reduced for smoother control
    private float KiThrottle = 0.05f; // Reduced to avoid initial accumulation
    private float KdThrottle = 0.2f;  // Reduced for smoother control
    private float KpSteer = 1.0f;
    private float KiSteer = 0.1f;
    private float KdSteer = 0.5f;

    // PID control objects
    private PID throttlePID;
    private PID steerPID;

    // Variables to calculate the error in the PID control
    private float currentSpeed;
    private float speedError;
    private float currentYaw;
    private float yawError;
    private float currentDistance;
    private float distanceError;
    private float timeSinceStart = 0f;
    private Vector3 directionToTarget;
    private float targetYaw;

    // Variables for stuck detection and recovery
    private float maxAllowedDistance = 20f; // Maximum allowed distance before stopping
    private float recoveryDistance = 10f;   // Distance at which the vehicle will resume movement
    private float lastDistance = 0f;        // Last recorded distance
    private float distanceCheckTime = 0f;   // Time to check if distance changes
    private bool isStuck = false;           // Flag to indicate if vehicle is stuck

    // Variable to get the car features
    private GetCarFeatures getCarFeatures;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        // Get required components
        rb = GetComponent<Rigidbody>();
        carController = GetComponent<CarDynamics>();
        getObjectFeatures = GetComponent<GetObjectFeatures>();

        // Check if GetObjectFeatures is available and get the object ID
        if (getObjectFeatures != null)
        {
            objectID = getObjectFeatures.objectID;
            targetObj = GameObject.Find("ID" + objectID + "Target");
        }

        // Initialize PID controllers
        throttlePID = new PID();
        steerPID = new PID();

        // Set initial center of mass
        rb.centerOfMass = new Vector3(0f, -0.1f, 0f);

        // Get the maximum speed allowed for the car
        getCarFeatures = GetComponent<GetCarFeatures>();
        if (getCarFeatures != null)
        {
            maxSpeed = getCarFeatures.maxSpeedAllowed; 
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // FixedUpdate is called at a fixed interval:

    void FixedUpdate()
    {

        // Ensure required components are available
        if (carController == null || getObjectFeatures == null || targetObj == null) return;

        // Update time since start
        timeSinceStart += Time.fixedDeltaTime;

        // Get target position and orientation
        targetPosition = targetObj.transform.position;
        targetOrientation = targetObj.transform.eulerAngles.y;

        // Calculate direction to target
        directionToTarget = (targetPosition - transform.position).normalized;
        targetYaw = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;

        // Apply PID controls
        ApplyThrottlePIDControl();
        ApplySteeringPIDControl();

    }

    // -----------------------------------------------------------------------------------------------------
    // Methods to apply PID control for throttle:

    void ApplyThrottlePIDControl()
    {

        // Calculate distance to target only in XZ plane
        Vector3 currentPos = transform.position;
        Vector3 targetPos = targetPosition;
        currentPos.y = 0;
        targetPos.y = 0;
        currentDistance = Vector3.Distance(currentPos, targetPos);

        // Check if vehicle is stuck or too far
        if (!isStuck)
        {

            // Calculate distance error
            distanceCheckTime += Time.fixedDeltaTime;

            // If stuck, check if distance has changed significantly
            if (distanceCheckTime >= 2f)
            {
                if (currentDistance > maxAllowedDistance ||
                    (Mathf.Abs(currentDistance - lastDistance) < 0.1f && currentDistance > 5f))
                {
                    isStuck = true;
                    carController.currentMotorTorque = 0f;
                    carController.currentBrakeForce = carController.brakeForce;
                }
                lastDistance = currentDistance;
                distanceCheckTime = 0f;
            }

        }
        else
        {

            // If stuck, check if target is close enough to resume
            if (currentDistance < recoveryDistance)
            {
                isStuck = false;
                carController.currentBrakeForce = 0f;
            }
            else
            {
                carController.currentMotorTorque = 0f;
                carController.currentBrakeForce = carController.brakeForce;
                return;
            }

        }

        // Calculate current speed in XZ plane
        Vector3 velocityXZ = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        currentSpeed = velocityXZ.magnitude;

        // Determine if we need to move forward or backward
        float dotProduct = Vector3.Dot(transform.forward, directionToTarget);
        bool shouldMoveForward = dotProduct > 0;

        // Apply brake if very close to target
        if (currentDistance < stopDistance)
        {
            carController.currentMotorTorque = 0f;
            carController.currentBrakeForce = carController.brakeForce;
            return;
        }

        // Calculate target speed based on distance and time
        float targetSpeed;

        // If within first 2 seconds, gradually increase target speed
        if (timeSinceStart < 2f)
        {
            // Gradually increase target speed
            targetSpeed = Mathf.Lerp(initialTargetSpeed, maxSpeed, timeSinceStart / 2f);
        }
        else
        {

            if (currentDistance > distanceToStartBraking)
            {
                targetSpeed = 20f;
            }
            else
            {
                targetSpeed = Mathf.Lerp(2f, maxSpeed, currentDistance / distanceToStartBraking);
            }

        }

        // Calculate speed error
        speedError = targetSpeed - currentSpeed;

        // Apply PID control to throttle if car is not shutting down
        if (!carController.flagCarShutdown)
        {

            // Calculate throttle using PID control
            float throttle = throttlePID.Compute(speedError, KpThrottle, KiThrottle, KdThrottle);

            // Calculate maximum allowed torque based on distance
            float maxAllowedTorque;
            if (currentDistance > distanceToStartBraking)
            {
                // When far away, use maximum torque
                maxAllowedTorque = maxTorque;
            }
            else
            {
                // When close, reduce torque proportionally
                maxAllowedTorque = Mathf.Lerp(0f, maxTorque, currentDistance / distanceToStartBraking);
            }

            // Apply torque limited by distance and direction
            float torque = Mathf.Clamp(throttle, -1f, 1f) * maxAllowedTorque;

            // Invert torque if we need to move backward
            if (!shouldMoveForward)
            {
                torque = -torque;
            }

            // Apply torque to car controller
            carController.currentMotorTorque = torque;

            // Apply brake if close to target and speed is higher than desired
            if (currentDistance < brakeDistance && currentSpeed > targetSpeed)
            {
                carController.currentBrakeForce = carController.brakeForce * (1f - (currentDistance / brakeDistance));
            }
            else
            {
                carController.currentBrakeForce = 0f;
            }

        }

        // Reset PID if error is small
        if (Mathf.Abs(speedError) < 0.5f) throttlePID.Reset();

    }

    // -----------------------------------------------------------------------------------------------------
    // Methods to apply PID control for steering:

    void ApplySteeringPIDControl()
    {

        // Get current yaw
        currentYaw = transform.localEulerAngles.y;

        // Calculate direction to target in local space
        Vector3 localDirectionToTarget = transform.InverseTransformPoint(targetPosition);
        float targetAngle = Mathf.Atan2(localDirectionToTarget.x, localDirectionToTarget.z) * Mathf.Rad2Deg;

        // Calculate yaw error
        yawError = targetAngle;
        if (yawError > 180) yawError -= 360;
        if (yawError < -180) yawError += 360;

        // Apply PID control to steering if car is not shutting down
        if (!carController.flagCarShutdown)
        {

            // Check if vehicle is stopped (using a small velocity threshold)
            bool isStopped = rb.velocity.magnitude < 0.1f;
            bool isBraking = carController.currentBrakeForce > 0f;

            // When stopped, use a larger deadzone for steering
            float currentDeadzone = isStopped ? deadzone * 2f : deadzone;

            // If braking, keep wheels straight
            if (isBraking)
            {
                carController.currentSteerAngle = 0f;
                steerPID.Reset();
                return;
            }

            // If stopped and angle error is significant, allow steering correction
            if (isStopped && Mathf.Abs(yawError) > currentDeadzone)
            {
                float steer = steerPID.Compute(yawError, KpSteer, KiSteer, KdSteer);
                float steeringFactor = Mathf.Clamp01((Mathf.Abs(yawError) - currentDeadzone) / (45f - currentDeadzone));
                float smoothedSteer = Mathf.Sign(steer) * Mathf.Pow(Mathf.Abs(steer), 1.5f) * steeringFactor;
                float targetSteerAngle = Mathf.Clamp(smoothedSteer, -1f, 1f) * carController.maxSteerAngle;
                carController.currentSteerAngle = Mathf.Lerp(carController.currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * 5f);
            }
            else if (!isStopped) // Normal steering control when moving
            {

                // If yaw error is within deadzone, stop steering
                if (Mathf.Abs(yawError) < deadzone)
                {
                    carController.currentSteerAngle = 0f;
                    steerPID.Reset();
                    return;
                }

                // Apply PID control for steering
                float steer = steerPID.Compute(yawError, KpSteer, KiSteer, KdSteer);
                float steeringFactor = Mathf.Clamp01((Mathf.Abs(yawError) - deadzone) / (45f - deadzone));
                float smoothedSteer = Mathf.Sign(steer) * Mathf.Pow(Mathf.Abs(steer), 1.5f) * steeringFactor;
                float targetSteerAngle = Mathf.Clamp(smoothedSteer, -1f, 1f) * carController.maxSteerAngle;
                carController.currentSteerAngle = Mathf.Lerp(carController.currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * 5f);

            }
            else
            {
                carController.currentSteerAngle = 0f;
            }

        }

        // Reset PID if error is small
        if (Mathf.Abs(yawError) < 0.5f) steerPID.Reset();
        
    }

}
