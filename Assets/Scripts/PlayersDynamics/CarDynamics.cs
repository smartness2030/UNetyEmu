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
using UnityEngine; // Library to use in MonoBehaviour classes

// Class to manage the car dynamics such as motor force, brake force, steering angle, and battery consumption
[RequireComponent(typeof(Rigidbody))]
public class CarDynamics : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // Car features
    [Header("Car Features")]
    public float motorForce = 0f;
    public float brakeForce = 0f;
    public float maxSteerAngle = 0f;

    // Battery features
    [Header("Battery Features")]
    public float batteryLevel = 100f; // Initial battery level of the car in percentage (just to initialize the variable)
    public float simulationBatterySpeedFactor = 0.0001f; // Speed factor to simulate the battery consumption
    public float idlePowerConsumptionPerSecond = 0.01f; // Power consumption of the car when idle

    // Wheel colliders and transforms
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider backLeftWheel;
    public WheelCollider backRightWheel;
    public Transform frontLeftWheelTransform;
    public Transform frontRightWheelTransform;
    public Transform backLeftWheelTransform;
    public Transform backRightWheelTransform;

    // Initialization variables for the car dynamics
    public float currentMotorTorque = 0f;
    public float currentSteerAngle = 0f;
    public float currentBrakeForce = 0f;
    
    // Variable to manage the car state
    public bool flagCarShutdown = false;

    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // Flag to check if the car is initialized
    private bool isInitialized = false;

    // Initialization variables to get car features
    private GetCarFeatures getCarFeatures;
    private float gravity;
    private Rigidbody rb;
    private float carWeight;
    private float energyTotal;
    private float hoverPower;
    private float batteryCapacity;
    private float batteryVoltage;
    private float maxDrivingTime;

    // Variables for the battery consumption of the drone
    private float thrustUsed;
    private float powerConsumption;
    private float timeBasedConsumption;
    private float batteryUsed;
    private float batteryUsed_percent;

    // Time active variable to calculate the time-based consumption
    private float timeActive = 0f;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        // Initialize the Rigidbody component
        rb = GetComponent<Rigidbody>();

        // Initialize the car dynamics
        InitializeCar();

        // Get the GetCarFeatures component to get the car features
        getCarFeatures = GetComponent<GetCarFeatures>();
        if (getCarFeatures != null)
        {
            motorForce = getCarFeatures.motorForce;
            brakeForce = getCarFeatures.brakeForce;
            maxSteerAngle = getCarFeatures.maxSteerAngle;
            batteryLevel = getCarFeatures.batteryStartPercentage; // Get the initial battery level of the drone
            batteryCapacity = getCarFeatures.maxBatteryCapacity; // Get the maximum battery capacity of the drone
            batteryVoltage = getCarFeatures.batteryVoltage; // Get the battery voltage of the drone
            maxDrivingTime = getCarFeatures.approxMaxDrivingTime * 60f; // Get the maximum flight time of the drone in seconds
        }

        // Get the gravity of Unity
        gravity = Mathf.Abs(Physics.gravity.y);

        // Calculate the weight of the drone using the gravity of Unity
        carWeight = rb.mass * gravity;

        // Calculate the power required to hover the drone
        energyTotal = (batteryCapacity * batteryVoltage) / 1000f;
        hoverPower = energyTotal / (maxDrivingTime / 3600f);

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to initialize the car dynamics, checking if all references are assigned:

    void InitializeCar()
    {

        // Check that all necessary references are assigned
        if (frontLeftWheel == null || frontRightWheel == null ||
            backLeftWheel == null || backRightWheel == null ||
            frontLeftWheelTransform == null || frontRightWheelTransform == null ||
            backLeftWheelTransform == null || backRightWheelTransform == null)
        {
            enabled = false;
            return;
        }

        // Set the initial values for the car dynamics
        rb.centerOfMass = new Vector3(0f, -0.1f, 0f);
        isInitialized = true;

    }

    // -----------------------------------------------------------------------------------------------------
    // FixedUpdate is called at a fixed interval:

    void FixedUpdate()
    {

        // Check the battery level
        CheckBatteryLevel();

        // If the car is not shutdown, update the car dynamics
        if (!flagCarShutdown)
        {

            // Update the current motor torque, steer angle, and brake force
            UpdateBatteryConsumption();

            // If the car is not initialized, return            
            if (!isInitialized) return;

            // If the car is initialized, apply the motor force, steering angle, and brake force
            HandleMotor();
            HandleSteering();

            // Update the wheel visuals based on the WheelCollider's position and rotation
            UpdateWheelVisuals(frontLeftWheel, frontLeftWheelTransform);
            UpdateWheelVisuals(frontRightWheel, frontRightWheelTransform);
            UpdateWheelVisuals(backLeftWheel, backLeftWheelTransform);
            UpdateWheelVisuals(backRightWheel, backRightWheelTransform);

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Methods to handle the force applied to the wheels:

    void HandleMotor()
    {

        // Apply the motor torque to the back wheels
        backLeftWheel.motorTorque = currentMotorTorque;
        backRightWheel.motorTorque = currentMotorTorque;

        // Apply the brake force to all wheels
        backLeftWheel.brakeTorque = currentBrakeForce;
        backRightWheel.brakeTorque = currentBrakeForce;
        frontLeftWheel.brakeTorque = currentBrakeForce;
        frontRightWheel.brakeTorque = currentBrakeForce;

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to handle steering angle for the front wheels:

    void HandleSteering()
    {

        // Apply the steering angle to the front wheels
        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to update the wheel visuals based on the WheelCollider's position and rotation:

    void UpdateWheelVisuals(WheelCollider collider, Transform wheelTransform)
    {

        // Get the position and rotation of the WheelCollider and apply it to the wheel transform
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to check the battery level and set the flag to shutdown the car if the battery is low:

    void CheckBatteryLevel()
    {

        // If the battery level is less than 0, stop the car
        if (batteryLevel <= 1f)
        {
            flagCarShutdown = true; // Set the flag to shutdown the car
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to update the battery consumption of the car:

    void UpdateBatteryConsumption()
    {

        // Get the total force applied to the drone
        thrustUsed = currentMotorTorque;

        // Calculate the power consumption of the drone
        powerConsumption = hoverPower * (thrustUsed / carWeight);

        // If the drone is idle, increase the power consumption
        timeActive += Time.deltaTime;

        // Calculate the time-based consumption of the drone
        timeBasedConsumption = timeActive * idlePowerConsumptionPerSecond;

        // Sum the power consumption of the drone
        powerConsumption += timeBasedConsumption;

        // Calculate the battery used in the simulation
        batteryUsed = (powerConsumption / batteryVoltage) * 3600 * Time.deltaTime * simulationBatterySpeedFactor;

        // Calculate the battery used in percentage
        batteryUsed_percent = (batteryUsed / batteryCapacity) * 100f;

        // Update the battery level of the drone
        batteryLevel = Mathf.Clamp(batteryLevel - batteryUsed_percent, 0f, 100f);

    }

}
