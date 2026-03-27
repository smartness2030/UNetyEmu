// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes

// ----------------------------------------------------------------------
// Class to model the energy consumption of the drone based on the physical relationship between motor thrust and power: P ∝ F^(3/2)
// Requires DroneDynamics to read per-motor forces and motor state
[RequireComponent(typeof(DroneDynamics))]
public class DroneEnergyConsumption : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]

    [Tooltip("Total usable battery capacity in [Wh]")]
    public float batteryCapacityWh = 500f;

    [Tooltip("Rotor disk radius in [meters], used to calculate the rotor disk area")]
    public float rotorRadius = 0.14f;

    [Tooltip("Air density in [kg/m³]")]
    public float airDensity = 1.225f;

    [Tooltip("Constant power draw of avionics, sensors and ESCs regardless of throttle, in [W]")]
    public float idlePowerDraw = 15f;

    [Tooltip("Motor efficiency factor in [0, 1]")]
    [Range(0f, 1f)] public float motorEfficiency = 0.90f;

    [Tooltip("Whether to reset the battery to full capacity when the simulation starts")]
    public bool resetBatteryOnStart = true;

    [Tooltip("Minimum thrust in [N] to consider for power calculation to avoid numerical issues")]
    public float minThrustForPowerCalculation = 0.0001f; 

    [Tooltip("Minimum instantaneous power in [W] to consider for flight time estimation to avoid numerical issues")]
    public float minInstantaneousPowerW = 0.001f;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]

    [Tooltip("Current battery charge remaining in [Wh]")]
    public float batteryRemainingWh;

    [Tooltip("Current battery charge remaining as a percentage [0-100%]")]
    [Range(0f, 100f)] public float batteryRemainingPercent;

    [Tooltip("Total instantaneous power consumed by all motors plus idle draw, in [W]")]
    public float instantaneousPowerW;

    [Tooltip("Power consumed by the front left motor, in [W]")]
    public float powerFrontLeftW;

    [Tooltip("Power consumed by the front right motor, in [W]")]
    public float powerFrontRightW;

    [Tooltip("Power consumed by the back right motor, in [W]")]
    public float powerBackRightW;

    [Tooltip("Power consumed by the back left motor, in [W]")]
    public float powerBackLeftW;

    [Tooltip("Total energy consumed since the simulation started or the battery was last reset, in [Wh]")]
    public float totalEnergyConsumedWh;

    [Tooltip("Estimated remaining flight time at the current power draw, in [minutes]")]
    public float estimatedFlightTimeRemainingMin;

    [Tooltip("Whether the battery has been fully depleted")]
    public bool batteryDepleted = false;

    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the DroneDynamics component to read per-motor forces
    private DroneDynamics droneDynamics;

    // Precomputed denominator for the thrust-to-power calculation
    private float thrustToPowerDenominator;    

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get reference to the DroneDynamics component on the same GameObject
        droneDynamics = GetComponent<DroneDynamics>();

        // Initialize battery state
        if (resetBatteryOnStart)
        {
            batteryRemainingWh = batteryCapacityWh;
            totalEnergyConsumedWh = 0f;
            batteryDepleted = false;
        }

        // Precompute the denominator for the thrust-to-power calculation
        ComputeThrustToPowerDenominator();
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // If the battery is already depleted, skip calculations to save performance
        if (batteryDepleted)
            return;

        // Compute the instantaneous power draw of each motor
        ComputeInstantaneousPower();

        // Drain the battery based on the instantaneous power and elapsed time
        DrainBattery();

        // Update the monitoring output variables
        UpdateMonitoringOutputs();
    }

    // ----------------------------------------------------------------------
    // Method to precompute the thrust-to-power denominator
    void ComputeThrustToPowerDenominator()
    {
        // Calculate the rotor disk area: A = π * r^2
        float rotorArea = Mathf.PI * rotorRadius * rotorRadius;
        
        // Calculate the denominator for the thrust-to-power relationship: sqrt(2 * rho * A)
        thrustToPowerDenominator = Mathf.Sqrt(2f * airDensity * rotorArea);

        // Add a safety check to prevent division by zero or very small numbers in the MotorPower calculation
        if (thrustToPowerDenominator < minThrustForPowerCalculation)
            thrustToPowerDenominator = minThrustForPowerCalculation;
    }

    // ----------------------------------------------------------------------
    // Method to compute the instantaneous power draw of each motor and the total power draw of the drone
    void ComputeInstantaneousPower()
    {
        // Calculate per-motor power
        powerFrontLeftW = MotorPower(droneDynamics.forceFrontLeft);
        powerFrontRightW = MotorPower(droneDynamics.forceFrontRight);
        powerBackRightW = MotorPower(droneDynamics.forceBackRight);
        powerBackLeftW = MotorPower(droneDynamics.forceBackLeft);

        // Total power = sum of motor powers + constant idle draw
        instantaneousPowerW = powerFrontLeftW + powerFrontRightW + powerBackRightW + powerBackLeftW + idlePowerDraw;
    }

    // ----------------------------------------------------------------------
    // Method to calculate the power consumed by a single motor
    float MotorPower(float thrustNewtons)
    {
        // Ensure thrust is non-negative and above a minimum threshold to avoid numerical issues
        if (thrustNewtons <= 0f)
            return 0f;

        // Calculate power using the relationship P = F^(3/2) / (k * eta)
        return Mathf.Pow(thrustNewtons, 1.5f) / (thrustToPowerDenominator * motorEfficiency);
    }

    // ----------------------------------------------------------------------
    // Method to drain the battery based on the instantaneous power draw
    void DrainBattery()
    {
        // Convert power [W] and time [s] to energy [Wh]: E = P * t / 3600
        float energyConsumedThisStepWh = instantaneousPowerW * Time.fixedDeltaTime / 3600f;

        // Accumulate total energy consumed
        totalEnergyConsumedWh += energyConsumedThisStepWh;

        // Drain the battery
        batteryRemainingWh -= energyConsumedThisStepWh;

        // Clamp to zero and flag depletion
        if (batteryRemainingWh <= 0f)
        {
            batteryRemainingWh = 0f;
            batteryDepleted    = true;
        }
    }

    // ----------------------------------------------------------------------
    // Method to update the monitoring output variables
    void UpdateMonitoringOutputs()
    {
        // Update battery remaining percentage
        batteryRemainingPercent = (batteryCapacityWh > 0f) ? (batteryRemainingWh / batteryCapacityWh) * 100f : 0f;

        // Estimated flight time remaining in minutes
        if (instantaneousPowerW > minInstantaneousPowerW)
            estimatedFlightTimeRemainingMin = (batteryRemainingWh / instantaneousPowerW) * 60f;
        else
            estimatedFlightTimeRemainingMin = 0f;
    }

    // ----------------------------------------------------------------------
    // Public method to recharge the battery to a given percentage [0-100]
    public void RechargeBattery(float targetPercent = 100f)
    {
        // Clamp target percentage to valid range
        targetPercent = Mathf.Clamp(targetPercent, 0f, 100f);

        // Recharge the battery to the target percentage of capacity
        batteryRemainingWh = batteryCapacityWh * (targetPercent / 100f);

        // Reset total energy consumed and depletion flag
        batteryDepleted = batteryRemainingWh <= 0f;
    }

    // ----------------------------------------------------------------------
    // Public method to recalculate the thrust-to-power denominator at runtime
    public void RefreshAerodynamicParameters()
    {
        ComputeThrustToPowerDenominator();
    }
}
