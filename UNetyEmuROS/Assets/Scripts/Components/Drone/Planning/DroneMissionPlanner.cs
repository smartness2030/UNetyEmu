// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute

// ----------------------------------------------------------------------
// Enum to define the type of each mission step
[Serializable] public enum MissionStepType
{
    Idle,
    Takeoff,
    Cruise,
    Landing,
    RechargingBattery,
    PickingUpPackage,
    DeliveringPackage
}

// ----------------------------------------------------------------------
// Enum to represent the current state of the mission controller
[Serializable] public enum MissionState
{
    idle,
    waitingBefore,
    executingStep,
    waitingAfter,
    waitingForIdle,
    missionComplete
}

// ----------------------------------------------------------------------
// Serializable class representing a single mission step
[Serializable] public class MissionStep
{
    [Tooltip("Type of the mission step, which determines the drone's behavior and which parameters are used")]
    public MissionStepType stepType;

    [Tooltip("Time in seconds the drone waits BEFORE starting this step")]
    public float waitBefore = 0f;

    [Tooltip("Time in seconds the drone waits AFTER completing this step")]
    public float waitAfter = 0f;

    [Tooltip("Waypoints for a stepType")]
    public SetDroneTarget[] waypoints;
}

// ----------------------------------------------------------------------
// Class that manages the execution of a sequence of mission steps for the drone
// Requires the DroneWaypointFollower to control the drone's movement along waypoints for navigation steps
// Requires the DronePickUpPackage to control the arm state for pick-up and delivery steps
// Requires the AttachObject to update the package state when picking up or delivering
// Requires the DroneControlInputs to check the control mode and avoid conflicts with manual control
[RequireComponent(typeof(DroneWaypointFollower))]
[RequireComponent(typeof(DronePickUpPackage))]
[RequireComponent(typeof(AttachObject))]
[RequireComponent(typeof(DroneControlInputs))]
public class DroneMissionPlanner : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]

    [Tooltip("Set to true to start executing the mission steps from the beginning. " +
             "Automatically reset to false once the mission starts")]
    public bool startMission = false;

    [Tooltip("Ordered list of mission steps. The drone executes them sequentially from index 0")]
    public MissionStep[] missionSteps;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]

    [Tooltip("Current state of the mission controller, for monitoring")]
    public MissionState currentState = MissionState.idle;

    [Tooltip("Index of the step currently being executed, for monitoring")]
    public int currentStepIndex = 0;

    [Tooltip("Type of the step currently being executed, for monitoring")]
    public MissionStepType currentStepType;

    [Tooltip("Remaining hold time after the current step completes, in [seconds], for monitoring")]
    public float remainingWaitTime = 0f;

    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the DroneWaypointFollower component to control the drone's movement along waypoints
    private DroneWaypointFollower droneWaypointFollower;

    // Timer variable used for counting down wait times before and after steps
    private float waitTimer = 0f;

    // Reference to the DronePickUpPackage component to control the drone's arm for picking up and delivering packages
    private DronePickUpPackage dronePickUpPackage;

    // Reference to the AttachObject component to update the package state
    private AttachObject attachObject;

    // Reference to the DroneControlInputs component to check the control mode and avoid conflicts with manual control
    private DroneControlInputs droneControlInputs; 

    // Flight phase that indicates the drone is idle and not currently executing a route, used to detect when a step is complete
    private FlightPhase idleFlightPhase = FlightPhase.idle;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get references to the required components on the same GameObject
        droneWaypointFollower = GetComponent<DroneWaypointFollower>();
        dronePickUpPackage = GetComponent<DronePickUpPackage>();
        attachObject = GetComponent<AttachObject>();
        droneControlInputs = GetComponent<DroneControlInputs>();
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // Do not execute the route if not in Automatic mode, to avoid conflicts with manual control
        if (droneControlInputs.controlMode != DroneControlMode.Automatic)
            return;

        // Ensure autoOrientation is enabled for all steps by default
        if (droneWaypointFollower.autoOrientationCurrentWaypoint != true)
            droneWaypointFollower.autoOrientationCurrentWaypoint = true; 

        // State machine to manage the execution flow of the mission steps based on the current state of the controller
        switch (currentState)
        {
            // Wait for the startMission flag to be set to true, then start executing the first step
            case MissionState.idle:
                UpdateIdle(); 
                break;

            // Count down the waitBefore timer for the current step, then start executing the step
            case MissionState.waitingBefore:
                UpdateWaitingBefore(); 
                break;

            // Wait until the droneWaypointFollower returns to idle, indicating the step is complete, then start the waitAfter timer
            case MissionState.executingStep:
                UpdateExecutingStep();
                break;

            // Count down the waitAfter timer for the current step, then advance to the next step
            case MissionState.waitingAfter:
                UpdateWaitingAfter();
                break;
            
            // One-frame buffer state to ensure the droneWaypointFollower has fully settled into idle before advancing to the next step
            case MissionState.waitingForIdle:
                UpdateWaitingForIdle();
                break;
        }
    }

    // ----------------------------------------------------------------------
    // Method to handle the idle state that waits for the startMission flag to be set, then starts executing the first step of the mission
    void UpdateIdle()
    {
        // Wait for the startMission flag to be set to true, which can be triggered from the Inspector or programmatically via LoadMission()
        if (!startMission)
            return;

        // Start the mission by executing the first step
        startMission = false;
        currentStepIndex = 0;

        // Check if there are mission steps defined before trying to execute them
        if (missionSteps == null || missionSteps.Length == 0)
        {
            Debug.LogWarning("DroneMissionController: no mission steps defined.");
            return;
        }

        // Execute the current step which will handle loading the waypoints into the droneWaypointFollower
        ExecuteCurrentStep();
    }

    // ----------------------------------------------------------------------
    // Method to handle the waitingBefore statet that counts down the waitBefore timer for the current step
    void UpdateWaitingBefore()
    {
        // Count down the wait timer and update the remainingWaitTime
        waitTimer -= Time.fixedDeltaTime;
        remainingWaitTime = Mathf.Max(0f, waitTimer);

        // Once the wait timer reaches 0, start executing the current step by loading its waypoints into the droneWaypointFollower
        if (waitTimer <= 0f)
            StartStepExecution(missionSteps[currentStepIndex]);
    }

    // ----------------------------------------------------------------------
    // Method to handle the executingStep state that waits until the droneWaypointFollower returns to idle, indicating the step is complete
    void UpdateExecutingStep()
    {        
        // Check if the droneWaypointFollower has returned to the idle flight phase and then start the waitAfter timer for the current step
        if (droneWaypointFollower.currentPhase == idleFlightPhase)
            StartWaitAfter(missionSteps[currentStepIndex]);
    }

    // ----------------------------------------------------------------------
    // Method to handle the waitingAfter state that counts down the waitAfter timer for the current step
    void UpdateWaitingAfter()
    {
        // Count down the wait timer and update the remainingWaitTime
        waitTimer -= Time.fixedDeltaTime;
        remainingWaitTime = Mathf.Max(0f, waitTimer);

        // Once the wait timer reaches 0, advance to the next step in the mission
        if (waitTimer <= 0f)
            currentState = MissionState.waitingForIdle;
    }

    // ----------------------------------------------------------------------
    // Method to handle the waitingForIdle state that waits for one frame to ensure the droneWaypointFollower has fully settled into idle
    void UpdateWaitingForIdle()
    {
        AdvanceToNextStep();
    }

    // ----------------------------------------------------------------------
    // Method to execute the current step by loading its waypoints into the droneWaypointFollower
    void ExecuteCurrentStep()
    {
        // Get the current step and update the currentStepType for monitoring
        MissionStep step = missionSteps[currentStepIndex];
        currentStepType = step.stepType;

        // Start the waitBefore timer for the current step if specified, otherwise start executing the step immediately
        if (step.waitBefore > 0f)
        {
            // Initialize the wait timer and remaining wait time for monitoring
            waitTimer = step.waitBefore;
            remainingWaitTime = waitTimer;
            
            // Enter the waitingBefore state to count down the waitBefore timer before starting the step execution
            currentState = MissionState.waitingBefore;

            // Log the wait time for monitoring
            Debug.Log($"DroneMissionController: waiting {step.waitBefore}s before '{step.stepType}'");
            return;
        }
        else
        {
            // Log that there is no wait time before the step for monitoring
            Debug.Log($"DroneMissionController: no wait before '{step.stepType}', starting immediately");
        }

        // Start the execution of the current step
        StartStepExecution(step);
    }

    // ----------------------------------------------------------------------
    // Method to start the execution of a step by loading its waypoints into the droneWaypointFollower 
    void StartStepExecution(MissionStep step)
    {
        // Handle the arm state for pick-up and delivery steps based on the step type
        HandleArmState(step.stepType);

        // If the step type is a wait step that has no waypoints, start the waitAfter timer immediately
        if (IsWaitStep(step.stepType))
        {
            StartWaitAfter(step);
            return;
        }

        // For navigation steps that require waypoints, check if waypoints are defined before trying to load them into the droneWaypointFollower
        if (step.waypoints == null || step.waypoints.Length == 0)
        {
            // If no waypoints are defined for a navigation step, log a warning
            AdvanceToNextStep();
            Debug.LogWarning($"Step [{currentStepIndex}] '{step.stepType}' has no waypoints, skipping.");
            return;
        }

        // Load the waypoints for the current step into the droneWaypointFollower to start following the route
        droneWaypointFollower.currentWaypoints = step.waypoints;

        // Set the startRoute flag to true to trigger the droneWaypointFollower to start following the loaded waypoints
        droneWaypointFollower.startRoute = true;
        
        // Enter the executingStep state to wait until the droneWaypointFollower returns to idle, indicating the step is complete
        currentState = MissionState.executingStep;
    }

    // ----------------------------------------------------------------------
    // Method to handle the arm state for pick-up and delivery steps based on the step type
    void HandleArmState(MissionStepType stepType)
    {
        // If we don't have the required components to control the arm state for pick-up and delivery steps, return
        if (dronePickUpPackage == null || attachObject.packageObject == null)
            return;

        // Check if the current step type is a pick-up or delivery step, and if so, set the arm state in the dronePickUpPackage accordingly
        switch (stepType)
        {
            // For pick-up steps, set the arm state to PickUp to trigger the pick-up mechanism and logic in the DronePickUpPackage component
            case MissionStepType.PickingUpPackage:
                dronePickUpPackage.currentState = ArmState.PickUp;
                break;

            // For delivery steps, set the arm state to Drop to trigger the drop mechanism and logic in the DronePickUpPackage component
            case MissionStepType.DeliveringPackage:
                dronePickUpPackage.currentState = ArmState.Drop;
                break;

            // For all other step types that are not pick-up or delivery, set the arm state to Idle
            default:
                dronePickUpPackage.currentState = ArmState.Idle;
                break;
        }
    }

    // ----------------------------------------------------------------------
    // Method to start the waitAfter timer for the current step if specified, otherwise advance to the next step immediately
    void StartWaitAfter(MissionStep step)
    {
        // If the step has a waitAfter time specified, initialize the wait timer
        if (step.waitAfter > 0f)
        {
            // Initialize the wait timer and remaining wait time for monitoring
            waitTimer = step.waitAfter;
            remainingWaitTime = waitTimer;

            // Enter the waitingAfter state to count down the waitAfter timer before advancing to the next step
            currentState = MissionState.waitingAfter;

            // Log the waiting time and the step type for monitoring
            Debug.Log($"DroneMissionController: waiting {step.waitAfter}s after '{step.stepType}'");
        }
        else
        {
            // If no waitAfter time is specified for the step, advance to the next step immediately without waiting
            currentState = MissionState.waitingForIdle;

            // Log that there is no wait time after the step for monitoring
            Debug.Log($"DroneMissionController: no wait after '{step.stepType}', advancing immediately");
        }
    }

    // ----------------------------------------------------------------------
    // Method to advance to the next step in the mission sequence, or mark the mission as complete if we have reached the end of the steps
    void AdvanceToNextStep()
    {
        // Increment the current step index to move to the next step in the mission sequence
        currentStepIndex++;

        // Check if we have reached the end of the mission steps
        if (currentStepIndex >= missionSteps.Length)
        {
            // If we have completed all the steps, reset the current step index and mark the mission as complete
            remainingWaitTime = 0f;
            currentState = MissionState.missionComplete;            
            Debug.Log("DroneMissionController: mission complete.");
            return;
        }

        // If there are more steps to execute, start executing the next step by loading its waypoints into the droneWaypointFollower
        ExecuteCurrentStep();
    }

    // ----------------------------------------------------------------------
    // Method to determine if a step type is a wait step that has no waypoints and only requires waiting
    bool IsWaitStep(MissionStepType type)
    {
        // Return true if the step type is a wait step that do not require waypoints
        return type == MissionStepType.Idle || type == MissionStepType.RechargingBattery ||
               type == MissionStepType.PickingUpPackage  || type == MissionStepType.DeliveringPackage;
    }

    // ----------------------------------------------------------------------
    // Method to load a new mission sequence by providing an array of MissionStep objects, and optionally start the mission immediately
    public void LoadMission(MissionStep[] steps, bool autoStart = false)
    {
        // Assign the provided mission steps to the missionSteps variables
        missionSteps = steps;
        currentStepIndex = 0;
        currentState = MissionState.idle;
        startMission = autoStart;
    }
}
