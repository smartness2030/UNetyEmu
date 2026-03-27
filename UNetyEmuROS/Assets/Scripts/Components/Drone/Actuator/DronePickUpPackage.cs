// ----------------------------------------------------------------------
// Copyright 2025 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute

// Enumeration for arm states, can be expanded with more states if needed
[Serializable] public enum ArmState
{
    Idle,
    PickUp,
    Drop
}

// ----------------------------------------------------------------------
// Class to control the drone's package pickup mechanism, including moving supports to attach/detach packages
// Requires an AttachObject component to manage the package state and a Rigidbody component on the package for physical interaction
[RequireComponent(typeof(AttachObject))]
public class DronePickUpPackage : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Current state of the arm: Idle, PickUp, or Drop")]
    public ArmState currentState = ArmState.Idle;
    
    // ----------------------------------------------------------------------
    // Private input variables

    [Header("• PRIVATE INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("All supports distance along the arm's local X axis")]
    [SerializeField] private float moveDistanceSupportFrontLeftRightBack = 0.012f;

    [Tooltip("End support distance along the arm's local X axis")]
    [SerializeField] private float moveDistanceEndSupport = 0.013f;

    [Tooltip("Final step multiplier for all supports")]
    [SerializeField] private int finalStepDistanceSupportFrontLeftRightBack = 7;
    
    [Tooltip("Final step multiplier for end support")]
    [SerializeField] private int finalStepDistanceEndSupport = 7;

    [Tooltip("Time in seconds to complete the forward or return movement")]
    [SerializeField] private float moveDuration = 2.5f;

    [Tooltip("Number of supports allowed (must match the total number of transformers in packageEndSupport). Only 4 supports are allowed for the prefab_Drone 1 or 2")]
    [SerializeField] private int numSupports = 4;

    [Tooltip("End support transform array to move forward and attach the package")]
    [SerializeField] private Transform[] packageEndSupport;

    [Tooltip("Front left support Transforms")]
    [SerializeField] private Transform[] packageSupportFrontLeft;

    [Tooltip("Front right support Transforms")]
    [SerializeField] private Transform[] packageSupportFrontRight;

    [Tooltip("Back left support Transforms")]
    [SerializeField] private Transform[] packageSupportBackLeft;

    [Tooltip("Back right support Transforms")]
    [SerializeField] private Transform[] packageSupportBackRight;
    
    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the AttachObject component to update the package state
    private AttachObject attachObject;

    // Arrays to store initial positions and target positions for the end supports
    private Vector3[] startPositionsEndSupport;
    private Vector3[] forwardTargetsEndSupport;

    // Arrays to store initial positions and target positions for all supports
    private Vector3[] startPositionsSupportFrontLeft;
    private Vector3[] forwardTargetsSupportFrontLeft;
    private Vector3[] startPositionsSupportFrontRight;
    private Vector3[] forwardTargetsSupportFrontRight;
    private Vector3[] startPositionsSupportBackLeft;
    private Vector3[] forwardTargetsSupportBackLeft;
    private Vector3[] startPositionsSupportBackRight;
    private Vector3[] forwardTargetsSupportBackRight;
    
    // Progress variable to track the movement from start to target positions (0 to 1)
    private float progress;

    // Direction multiplier to determine movement direction based on support position
    private int directionMultiplier;

    // ----------------------------------------------------------------------
    // Awake is called when the script instance is being loaded
    void Awake()
    {
        // Get reference to the AttachObject component on the same GameObject
        attachObject = GetComponent<AttachObject>();

        // Validate that the number of supports matches the expected number for the prefabs
        if (packageEndSupport != null && packageEndSupport.Length != numSupports)
            Debug.LogWarning("Expected 4 transforms for the prefab_Drone 1 or 2");
        
        // Validate that all support arrays are not null
        if (packageEndSupport == null || packageSupportFrontLeft == null || 
            packageSupportFrontRight == null || packageSupportBackLeft == null || 
            packageSupportBackRight == null) return;

        // Get the initial and target positions for all supports to prepare for the movement during PickUp and Drop states
        GetStartAndTargetPositionsEndSupport();
        GetStartAndTargetPositionsFrontLeftSupport();
        GetStartAndTargetPositionsFrontRightSupport();
        GetStartAndTargetPositionsBackLeftSupport();
        GetStartAndTargetPositionsBackRightSupport();
    }

    // ----------------------------------------------------------------------
    // Method to calculate the initial and target positions for the end supports based on their local rotations and the defined move distances
    void GetStartAndTargetPositionsEndSupport()
    {
        // Initialize arrays to store start and target positions for the end supports
        startPositionsEndSupport = new Vector3[packageEndSupport.Length];
        forwardTargetsEndSupport = new Vector3[packageEndSupport.Length];

        // Calculate start and target positions for end supports based on their local rotations and the defined move distances
        for (int i = 0; i < packageEndSupport.Length; i++)
        {
            // Store the initial local position of each end support
            startPositionsEndSupport[i] = packageEndSupport[i].localPosition;

            // Determine the direction multiplier based on the support's position (front left and back left move in one direction, front right and back right move in the opposite direction)
            if ((i == 0) || (i == 2))
                directionMultiplier = 1;
            else if ((i == 1) || (i == 3))
                directionMultiplier = -1;

            // Calculate the target local position for each end support by moving it along its local X axis in the direction determined by the multiplier
            forwardTargetsEndSupport[i] = startPositionsEndSupport[i] + 
                packageEndSupport[i].localRotation * (Vector3.right * directionMultiplier * moveDistanceEndSupport * finalStepDistanceEndSupport);
        }
    }

    // ----------------------------------------------------------------------
    // Method to calculate the initial and target positions for the front left supports
    void GetStartAndTargetPositionsFrontLeftSupport()
    {
        // Initialize arrays to store start and target positions for the front left supports
        startPositionsSupportFrontLeft = new Vector3[packageSupportFrontLeft.Length];
        forwardTargetsSupportFrontLeft = new Vector3[packageSupportFrontLeft.Length];

        // Calculate start and target positions for front left supports based on their local rotations and the defined move distances
        for (int i = 0; i < packageSupportFrontLeft.Length; i++)
        {
            // Store the initial local position of each front left support
            startPositionsSupportFrontLeft[i] = packageSupportFrontLeft[i].localPosition;

            // Determine the direction multiplier for front left supports (move in one direction)
            directionMultiplier = 1;
            
            // Calculate the target local position for each front left support by moving it along its local X axis in the direction determined by the multiplier
            forwardTargetsSupportFrontLeft[i] = startPositionsSupportFrontLeft[i] + 
                packageSupportFrontLeft[i].localRotation * (Vector3.right * directionMultiplier * moveDistanceSupportFrontLeftRightBack * finalStepDistanceSupportFrontLeftRightBack);

            // Decrease the final step multiplier for the next support to create a staggered movement effect
            finalStepDistanceSupportFrontLeftRightBack--;
        }

        // Reset the final step multiplier for the next group of supports
        finalStepDistanceSupportFrontLeftRightBack = packageSupportFrontLeft.Length;
    }

    // ----------------------------------------------------------------------
    // Method to calculate the initial and target positions for the front right supports
    void GetStartAndTargetPositionsFrontRightSupport()
    {
        // Initialize arrays to store start and target positions for the front right supports
        startPositionsSupportFrontRight = new Vector3[packageSupportFrontRight.Length];
        forwardTargetsSupportFrontRight = new Vector3[packageSupportFrontRight.Length];

        // Calculate start and target positions for front right supports based on their local rotations and the defined move distances
        for (int i = 0; i < packageSupportFrontRight.Length; i++)
        {
            // Store the initial local position of each front right support
            startPositionsSupportFrontRight[i] = packageSupportFrontRight[i].localPosition;

            // Determine the direction multiplier for front right supports (move in the opposite direction)
            directionMultiplier = -1;

            // Calculate the target local position for each front right support by moving it along its local X axis in the direction determined by the multiplier
            forwardTargetsSupportFrontRight[i] = startPositionsSupportFrontRight[i] + 
                packageSupportFrontRight[i].localRotation * (Vector3.right * directionMultiplier * moveDistanceSupportFrontLeftRightBack * finalStepDistanceSupportFrontLeftRightBack);

            // Decrease the final step multiplier for the next support to create a staggered movement effect
            finalStepDistanceSupportFrontLeftRightBack--;
        }

        // Reset the final step multiplier for the next group of supports
        finalStepDistanceSupportFrontLeftRightBack = packageSupportFrontRight.Length;
    }

    // ----------------------------------------------------------------------
    // Method to calculate the initial and target positions for the back left supports
    void GetStartAndTargetPositionsBackLeftSupport()
    {
        // Initialize arrays to store start and target positions for the back left supports
        startPositionsSupportBackLeft = new Vector3[packageSupportBackLeft.Length];
        forwardTargetsSupportBackLeft = new Vector3[packageSupportBackLeft.Length];

        // Calculate start and target positions for back left supports based on their local rotations and the defined move distances
        for (int i = 0; i < packageSupportBackLeft.Length; i++)
        {
            // Store the initial local position of each back left support
            startPositionsSupportBackLeft[i] = packageSupportBackLeft[i].localPosition;

            // Determine the direction multiplier for back left supports (move in one direction)
            directionMultiplier = 1;

            // Calculate the target local position for each back left support by moving it along its local X axis in the direction determined by the multiplier
            forwardTargetsSupportBackLeft[i] = startPositionsSupportBackLeft[i] + 
                packageSupportBackLeft[i].localRotation * (Vector3.right * directionMultiplier * moveDistanceSupportFrontLeftRightBack * finalStepDistanceSupportFrontLeftRightBack);

            // Decrease the final step multiplier for the next support to create a staggered movement effect
            finalStepDistanceSupportFrontLeftRightBack--;
        }

        // Reset the final step multiplier for the next group of supports
        finalStepDistanceSupportFrontLeftRightBack = packageSupportBackLeft.Length;
    }

    // ----------------------------------------------------------------------
    // Method to calculate the initial and target positions for the back right supports
    void GetStartAndTargetPositionsBackRightSupport()
    {
        // Initialize arrays to store start and target positions for the back right supports
        startPositionsSupportBackRight = new Vector3[packageSupportBackRight.Length];
        forwardTargetsSupportBackRight = new Vector3[packageSupportBackRight.Length];

        // Calculate start and target positions for back right supports based on their local rotations and the defined move distances
        for (int i = 0; i < packageSupportBackRight.Length; i++)
        {
            // Store the initial local position of each back right support
            startPositionsSupportBackRight[i] = packageSupportBackRight[i].localPosition;

            // Determine the direction multiplier for back right supports (move in the opposite direction)
            directionMultiplier = -1;

            // Calculate the target local position for each back right support by moving it along its local X axis in the direction determined by the multiplier
            forwardTargetsSupportBackRight[i] = startPositionsSupportBackRight[i] + 
                packageSupportBackRight[i].localRotation * (Vector3.right * directionMultiplier * moveDistanceSupportFrontLeftRightBack * finalStepDistanceSupportFrontLeftRightBack);

            // Decrease the final step multiplier for the next support to create a staggered movement effect
            finalStepDistanceSupportFrontLeftRightBack--;
        }
    }

    // ----------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // Validate that all support arrays are not null before processing the state machine
        if (packageEndSupport == null || packageSupportFrontLeft == null || 
            packageSupportFrontRight == null || packageSupportBackLeft == null || 
            packageSupportBackRight == null) return;

        // State machine to handle the behavior of the supports based on the current state of the arm (Idle, PickUp, Drop)
        switch (currentState)
        {
            case ArmState.Idle:
                HandleIdleState();
                break;

            case ArmState.PickUp:
                HandlePickUpState();
                break;

            case ArmState.Drop:
                HandleDropState();
                break;
        }
    }

    // ----------------------------------------------------------------------
    // Method to handle the Idle state, where supports are in their initial positions and the package is not attached
    void HandleIdleState()
    {
        // If not null, update the package state to Idle
        if (attachObject != null)
            attachObject.packageState = PackageState.Idle;
        else // Return to prevent trying to update the package state without a reference to the AttachObject component
            return;
    }

    // ----------------------------------------------------------------------
    // Method to handle the PickUp state, where supports move forward to attach the package and update the package state to Attached
    void HandlePickUpState()
    {
        // If not null, update the package state to Attached
        if (attachObject != null)
            attachObject.packageState = PackageState.Attached;
        else // Return to prevent trying to update the package state without a reference to the AttachObject component
            return;
        
        // Calculate the delta progress for the movement based on the defined move duration and update the progress variable
        float delta = Time.deltaTime / moveDuration;

        // Increase the progress variable to move supports towards their target positions, and clamp it between 0 and 1 to ensure it doesn't exceed the defined range
        progress += delta;
        progress = Mathf.Clamp01(progress);

        // Update the positions of all supports based on the current progress of the movement
        UpdateAllSupports(progress);
    }

    // ----------------------------------------------------------------------
    // Method to handle the Drop state, where supports move back to their initial positions to detach the package and update the package state to Delivered
    void HandleDropState()
    {
        // If not null, update the package state to Delivered
        if (attachObject != null)
            attachObject.packageState = PackageState.Delivered;
        else // Return to prevent trying to update the package state without a reference to the AttachObject component
            return;

        // Calculate the delta progress for the movement based on the defined move duration and update the progress variable
        float delta = Time.deltaTime / moveDuration;

        // Decrease the progress variable to move supports back towards their initial positions, and clamp it between 0 and 1 to ensure it doesn't exceed the defined range
        progress -= delta;
        progress = Mathf.Clamp01(progress);

        // Update the positions of all supports based on the current progress of the movement
        UpdateAllSupports(progress);
    }

    // ----------------------------------------------------------------------
    // Method to update the positions of all supports
    void UpdateAllSupports(float elapsedTime)
    {
        UpdateSupportGroup(packageEndSupport, startPositionsEndSupport, forwardTargetsEndSupport, elapsedTime);
        UpdateSupportGroup(packageSupportFrontLeft, startPositionsSupportFrontLeft, forwardTargetsSupportFrontLeft, elapsedTime);
        UpdateSupportGroup(packageSupportFrontRight, startPositionsSupportFrontRight, forwardTargetsSupportFrontRight, elapsedTime);
        UpdateSupportGroup(packageSupportBackLeft, startPositionsSupportBackLeft, forwardTargetsSupportBackLeft, elapsedTime);
        UpdateSupportGroup(packageSupportBackRight, startPositionsSupportBackRight, forwardTargetsSupportBackRight, elapsedTime);
    }

    // ----------------------------------------------------------------------
    // Method to update the positions of the group of supports based on the current progress of the movement, using linear interpolation between start and target positions
    void UpdateSupportGroup(Transform[] supports, Vector3[] startPositions, Vector3[] targetPositions, float elapsedTime)
    {
        // For each support in the group update its local position
        for (int i = 0; i < supports.Length; i++)
        {
            // Validate that the support is not null before updating its position
            if (supports[i] == null) continue;

            // Update the local position of the support by linearly interpolating between its start position and target position based on the elapsed time of the movement
            supports[i].localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], elapsedTime);
        }
    }
}
