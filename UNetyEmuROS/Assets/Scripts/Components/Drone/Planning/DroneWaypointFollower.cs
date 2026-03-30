// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute

// ----------------------------------------------------------------------
// Enum to define the type of mission the drone will execute, used by DroneMissionPlanner
[Serializable] public enum FlightPhase { 
    idle, 
    following 
}

// ----------------------------------------------------------------------
// Class to follow a sequence of waypoints with a drone, using a Pure Pursuit approach for smooth path following
// Requires a DroneSetTarget component to send position and speed commands to the drone
// Requires a DroneControlInputs component to check the current control mode and avoid conflicts with manual control
[RequireComponent(typeof(DroneSetTarget))]
[RequireComponent(typeof(DroneControlInputs))]
public class DroneWaypointFollower : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]

    [Tooltip("Set to true by DroneMissionController to start executing the current waypoints. Automatically reset to false once the follower transitions to following")]
    public bool startRoute = false;

    [Tooltip("Look-ahead distance for the Pure Pursuit algorithm in [meters]. Only used when the route has more than 1 waypoint")]
    public float lookAheadDistance = 3f;

    [Tooltip("Radius around the final waypoint within which the drone is considered to have arrived, in [meters]")]
    public float arrivalRadius = 0.3f;

    [Tooltip("Distance to the final waypoint at which the ARRIVING phase begins and the look-ahead starts shrinking, in [meters]")]
    public float arrivingDistance = 5f;

    [Tooltip("Minimum segment length to consider when projecting the drone onto the path, in [meters]")]
    public float segLenMin = 0.0001f;

    [Tooltip("Whether to use auto orientation towards the current waypoint during the route execution")]
    public bool autoOrientationCurrentWaypoint = true;

    [Tooltip("Array of waypoints to follow. Each waypoint includes position, orientation and speed settings")]
    public SetDroneTarget[] currentWaypoints;

    [Tooltip("Radius of the sphere gizmo for waypoint visualization in the scene view")]
    [SerializeField] private float sphereGizmoRadius = 0.15f;

    [Tooltip("Whether to show gizmos in the scene view to visualize the waypoints and the virtual target")]
    public bool showGizmos = true;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)]

    [Tooltip("Current execution phase, read by DroneMissionController to detect when the route is finished")]
    public FlightPhase currentPhase = FlightPhase.idle;

    [Tooltip("Current position of the virtual target on the path")]
    public Vector3 virtualTargetPosition;

    [Tooltip("Distance from the drone to the final waypoint")]
    public float distanceToFinalWaypoint;

    [Tooltip("Current cruise speed being commanded")]
    public float currentCruiseSpeed;

    [Tooltip("Current climb speed being commanded")]
    public float currentClimbSpeed;

    [Tooltip("Current descent speed being commanded")]
    public float currentDescentSpeed;

    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the DroneSetTarget component to send commands to the drone
    private DroneSetTarget droneSetTarget;

    // Reference to the DroneControlInputs component to check control mode
    private DroneControlInputs droneControlInputs; 

    // Accumulated arc-length distances from waypoint 0 to each waypoint
    private float[] waypointDistances;
    private float totalPathLength;
    private float virtualTargetArcLength;
 
    // Flag to indicate if we have entered the arriving sub-phase
    private bool isArriving = false;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get references to required components
        droneSetTarget = GetComponent<DroneSetTarget>();
        droneControlInputs = GetComponent<DroneControlInputs>();
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // Do not execute the route if not in Automatic mode, to avoid conflicts with manual control
        if (droneControlInputs.controlMode != DroneControlMode.Automatic)
            return; 
        
        // Detect the rising edge of startRoute to begin execution
        if (currentPhase == FlightPhase.idle && startRoute)
        {
            // Consume the startRoute flag to avoid re-triggering
            startRoute = false;

            // Initialize the route following process
            BeginRoute();

            // If we failed to start the route (e.g. no waypoints), stay in idle
            return;
        }

        // If we are currently following a route, update the drone's target based on the waypoints and Pure Pursuit logic
        if (currentPhase == FlightPhase.following)
            UpdateFollowing();
    }

    // ----------------------------------------------------------------------
    // Method to initialize the route following process, called when startRoute is triggered
    void BeginRoute()
    {
        // Validate that we have waypoints to follow
        if (currentWaypoints == null || currentWaypoints.Length == 0)
            return;
        
        // Initialize state for route following
        currentPhase = FlightPhase.following;
        
        // Reset arriving flag and virtual target arc length
        isArriving = false;

        // Start with the virtual target at the beginning of the path
        virtualTargetArcLength = 0f;

        // Set auto orientation mode based on the public variable from DroneSetTarget
        if (autoOrientationCurrentWaypoint)
            droneSetTarget.autoOrientation = true;
        else
            droneSetTarget.autoOrientation = false;

        // If there's only one waypoint, we can directly apply it to the drone without needing Pure Pursuit logic
        if (currentWaypoints.Length == 1)
        {
            droneSetTarget.autoOrientation = false; // Ensure auto orientation is disabled when we only have one waypoint 
            ApplyWaypointToDroneSetTarget(currentWaypoints[0]);
        }            
        else
        {
            // For multiple waypoints, we need to calculate the path distances for Pure Pursuit
            BuildPathDistances();
        } 
    }

    // ----------------------------------------------------------------------
    // Method to update the drone's target while following the route, called in FixedUpdate when in following phase
    void UpdateFollowing()
    {
        // If there's only one waypoint, we just check if we've arrived at it without needing Pure Pursuit logic
        if (currentWaypoints.Length == 1)
            UpdateSingleWaypoint();
        else // For multiple waypoints, we use Pure Pursuit to calculate the virtual target and command the drone towards it
            UpdatePurePursuit();
    }

    // ----------------------------------------------------------------------
    // Method to update the drone's target when there's only a single waypoint, simply check distance to it and finish when arrived
    void UpdateSingleWaypoint()
    {
        // Calculate distance to the single waypoint
        distanceToFinalWaypoint = Vector3.Distance(
            transform.position,
            currentWaypoints[0].position
        );

        // If we are within the arrival radius, consider the route finished
        if (distanceToFinalWaypoint <= arrivalRadius)
            FinishRoute();
    }

    // ----------------------------------------------------------------------
    // Method to update the drone's target while following the route with Pure Pursuit logic
    void UpdatePurePursuit()
    {
        // Calculate the distance to the final waypoint to determine if we are in the arriving phase and when to finish
        distanceToFinalWaypoint = Vector3.Distance(
            transform.position,
            currentWaypoints[currentWaypoints.Length - 1].position
        );

        // Switch to arriving sub-phase when close enough to the final waypoint
        if (!isArriving && distanceToFinalWaypoint <= arrivingDistance)
            isArriving = true;

        // If we have arrived at the final waypoint, finish the route
        if (distanceToFinalWaypoint <= arrivalRadius)
        {
            FinishRoute();
            return;
        }

        // Check if the final segment is a descent, which requires special handling to ensure the drone can properly land
        bool isFinalDescent = IsFinalSegmentDescent();

        // If we are in the arriving phase and the final segment is a descent, we want to directly command the drone towards the final waypoint
        if (isFinalDescent && isArriving)
        {
            ApplyWaypointToDroneSetTarget(currentWaypoints[currentWaypoints.Length - 1]);
            return;
        }

        // Project the drone's current position onto the path to find the closest point and corresponding arc length along the path
        float droneArcLength = ProjectDroneOnPath();

        // Calculate the effective look-ahead distance, which shrinks as we approach the final waypoint during the arriving phase
        float effectiveLookAhead;
        if (isArriving)
        {
            // Linearly shrink the look-ahead distance from the configured value down to 0 as we go from arrivingDistance to arrivalRadius
            float t = Mathf.Clamp01(distanceToFinalWaypoint / arrivingDistance);
            effectiveLookAhead = Mathf.Lerp(0f, lookAheadDistance, t);
        }
        else
        {
            // During the normal following phase, use the full look-ahead distance
            effectiveLookAhead = lookAheadDistance;
        }

        // Advance virtual target arc-length (never goes backwards)
        virtualTargetArcLength = Mathf.Max(
            virtualTargetArcLength,
            droneArcLength + effectiveLookAhead
        );

        // Clamp the virtual target arc length to the total path length to avoid overshooting
        virtualTargetArcLength = Mathf.Min(virtualTargetArcLength, totalPathLength);

        // Resolve the virtual target position based on the calculated arc length
        ResolveVirtualTarget(virtualTargetArcLength);
    }

    // ----------------------------------------------------------------------
    // Method to apply the final waypoint's position, orientation and speeds to the drone and transition to idle phase
    void FinishRoute()
    {
        // Apply the final waypoint's exact position, orientation and speeds
        SetDroneTarget last = currentWaypoints[currentWaypoints.Length - 1];
        
        // Ensure auto orientation is disabled when finishing the route to allow the drone to properly orient towards the final waypoint
        droneSetTarget.autoOrientation = false;

        // Apply the final waypoint's settings to the drone
        ApplyWaypointToDroneSetTarget(last);

        // Transition to idle phase to indicate that we have finished the route
        currentPhase = FlightPhase.idle;
    }

    // ----------------------------------------------------------------------
    // Method to calculate the accumulated arc-length distances along the path defined by the waypoints, used for Pure Pursuit calculations
    void BuildPathDistances()
    {
        // Initialize the waypointDistances array to store the accumulated distances from the first waypoint to each subsequent waypoint
        waypointDistances = new float[currentWaypoints.Length];
        waypointDistances[0] = 0f;

        // Calculate the accumulated distances along the path by summing the distances between consecutive waypoints
        for (int i = 1; i < currentWaypoints.Length; i++)
            waypointDistances[i] = waypointDistances[i - 1] + Vector3.Distance(currentWaypoints[i - 1].position, currentWaypoints[i].position);

        // Store the total path length for easy access when clamping the virtual target arc length
        totalPathLength = waypointDistances[currentWaypoints.Length - 1];
    }

    // ----------------------------------------------------------------------
    // Method to project the drone's current position onto the path and find the closest point and corresponding arc length
    float ProjectDroneOnPath()
    {
        // Initialize variables to keep track of the best projection found
        float bestArcLength = 0f;
        float bestDistSq    = float.MaxValue;

        // Iterate through each segment defined by consecutive waypoints to find the closest point on the path to the drone's current position
        for (int i = 0; i < currentWaypoints.Length - 1; i++)
        {
            // Get the start and end points of the current segment of the path defined by waypoints i and i+1
            Vector3 segStart = currentWaypoints[i].position;
            Vector3 segEnd = currentWaypoints[i + 1].position;
            
            // Create a vector representing the current segment of the path
            Vector3 seg = segEnd - segStart;

            // Get the length of the segment to use for normalization
            float segLen = seg.magnitude;

            // Ignore segments that are too short to avoid numerical issues
            if (segLen < segLenMin)
                continue;

            // Calculate the projection of the drone's position onto the line defined by the segment, normalized by the segment length
            float t = Vector3.Dot(transform.position - segStart, seg) / (segLen * segLen);
            t = Mathf.Clamp01(t);

            // Calculate the closest point on the segment to the drone's position using the projection parameter t
            Vector3 closest = segStart + t * seg;
            float distSq  = (transform.position - closest).sqrMagnitude;

            // If this is the closest point found so far, update the best arc length and distance squared
            if (distSq < bestDistSq)
            {
                // Update the best distance squared to the closest point found so far
                bestDistSq = distSq;
                
                // Calculate the arc length along the path to this closest point
                bestArcLength = waypointDistances[i] + t * segLen;
            }
        }

        // Return the arc length of the closest point found
        return bestArcLength;
    }

    // ----------------------------------------------------------------------
    // Method to resolve the virtual target's position, orientation and speeds based on a given arc length along the path
    void ResolveVirtualTarget(float arcLength)
    {
        // Clamp the arc length to ensure it stays within the bounds of the path
        arcLength = Mathf.Clamp(arcLength, 0f, totalPathLength);

        // Find the segment of the path that corresponds to the given arc length by iterating through the waypointDistances array
        int seg = currentWaypoints.Length - 2;

        // Iterate through the waypoint distances to find the segment that corresponds to the given arc length
        for (int i = 0; i < currentWaypoints.Length - 1; i++)
        {
            // If the arc length is less than or equal to the distance at the next waypoint, we have found the correct segment
            if (arcLength <= waypointDistances[i + 1])
            {
                // Store the index of the segment and break out of the loop
                seg = i;
                break;
            }
        }

        // Get the start and end arc lengths of the identified segment to use for interpolation
        float segStart = waypointDistances[seg];
        float segEnd = waypointDistances[seg + 1];

        // Calculate the length of the segment to use for normalization
        float segLength = segEnd - segStart;

        // Calculate the interpolation parameter t for the segment
        float t = (segLength > segLenMin) ? (arcLength - segStart) / segLength : 1f;

        // Interpolate position between the two waypoints of the segment using the parameter t to find the virtual target's position
        virtualTargetPosition = Vector3.Lerp(
            currentWaypoints[seg].position,
            currentWaypoints[seg + 1].position, t
        );

        // Interpolate cruise speed between the two waypoints of the segment using the parameter t to find the virtual target's cruise speed
        currentCruiseSpeed = Mathf.Lerp(
            currentWaypoints[seg].cruiseSpeed,
            currentWaypoints[seg + 1].cruiseSpeed, t
        );

        // Interpolate climb speed between the two waypoints of the segment using the parameter t to find the virtual target's climb speed
        currentClimbSpeed = Mathf.Lerp(
            currentWaypoints[seg].climbSpeed,
            currentWaypoints[seg + 1].climbSpeed, t
        );

        // Interpolate descent speed between the two waypoints of the segment using the parameter t to find the virtual target's descent speed
        currentDescentSpeed = Mathf.Lerp(
            currentWaypoints[seg].descentSpeed,
            currentWaypoints[seg + 1].descentSpeed, t
        );

        // Determine the orientation to command to the drone based on the autoOrientationCurrentWaypoint flag
        if (!autoOrientationCurrentWaypoint)
        {
            // Always follow waypoint orientation
            droneSetTarget.autoOrientation = false;

            // Interpolate orientation between the two waypoints of the segment using the parameter t to find the virtual target's orientation
            float orientation = Mathf.LerpAngle(
                currentWaypoints[seg].orientation,
                currentWaypoints[seg + 1].orientation,
                t
            );

            // Apply the calculated orientation to the drone's target
            droneSetTarget.target.orientation = orientation;
        }
        else
        {
            // Use auto orientation during the route
            droneSetTarget.autoOrientation = true;
        }

        // Finally, apply the calculated virtual target position and speeds to the drone's target
        droneSetTarget.target.position = virtualTargetPosition;
        droneSetTarget.target.cruiseSpeed = currentCruiseSpeed;
        droneSetTarget.target.climbSpeed = currentClimbSpeed;
        droneSetTarget.target.descentSpeed = currentDescentSpeed;
    }

    // ----------------------------------------------------------------------
    // Method to check if the final segment of the path is a descent, which requires special handling during the arriving phase to ensure proper landing
    bool IsFinalSegmentDescent()
    {
        // If we don't have at least 2 waypoints, we cannot determine if it's a descent, so we return false by default
        if (currentWaypoints.Length < 2)
            return false;

        // Calculate the delta vector of the final segment defined by the last two waypoints to determine if it's mostly vertical or mostly horizontal
        Vector3 last = currentWaypoints[currentWaypoints.Length - 1].position;
        Vector3 previous = currentWaypoints[currentWaypoints.Length - 2].position;

        // Calculate the horizontal and vertical distances of the final segment to determine if it's a descent or not
        Vector3 delta = last - previous;

        // Calculate the horizontal distance as the magnitude of the projection of the delta vector onto the horizontal plane (x-z plane)
        float horizontalDist = new Vector2(delta.x, delta.z).magnitude;
        float verticalDist = Mathf.Abs(delta.y);

        // If the vertical distance is greater than the horizontal distance, we consider it a descent segment
        return verticalDist > horizontalDist;
    }

    // ----------------------------------------------------------------------
    // Method to apply a given waypoint's position, orientation and speed settings directly to the drone's target
    void ApplyWaypointToDroneSetTarget(SetDroneTarget wp)
    {
        droneSetTarget.target.position = wp.position;
        droneSetTarget.target.orientation = wp.orientation;
        droneSetTarget.target.cruiseSpeed = wp.cruiseSpeed;
        droneSetTarget.target.climbSpeed = wp.climbSpeed;
        droneSetTarget.target.descentSpeed = wp.descentSpeed;
    }

    // ----------------------------------------------------------------------
    // Method to draw gizmos in the scene view to visualize the waypoints, the path and the virtual target for debugging and tuning purposes
    void OnDrawGizmos()
    {
        // Only draw gizmos if the flag is enabled and we have waypoints to visualize
        if (!showGizmos)
        return;

        // If we don't have any waypoints to visualize, return
        if (currentWaypoints == null || currentWaypoints.Length == 0)
            return;

        // Use a random seed based on the instance ID of the game object to generate a consistent random color for the path visualization
        int id = gameObject.GetInstanceID();
        UnityEngine.Random.InitState(id);

        // Generate a random base color for the path visualization to differentiate between multiple waypoint followers in the scene
        Color baseColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);

        // Draw spheres at each waypoint position and lines between consecutive waypoints to visualize the path
        for (int i = 0; i < currentWaypoints.Length; i++)
        {
            // Use different colors for the first and last waypoints to easily identify the start and end of the path
            if (i == 0)
                Gizmos.color = Color.green;
            else if (i == currentWaypoints.Length - 1)
                Gizmos.color = Color.red;
            else
                Gizmos.color = baseColor;

            // Draw a sphere at the waypoint position with the specified radius to visualize the waypoint
            Gizmos.DrawSphere(currentWaypoints[i].position, sphereGizmoRadius);

            // Draw a line from this waypoint to the next one to visualize the path segment, using the base color for the lines
            if (i < currentWaypoints.Length - 1)
            {
                Gizmos.color = baseColor;
                Gizmos.DrawLine(currentWaypoints[i].position, currentWaypoints[i + 1].position);
            }
        }

        // Draw the virtual target position if we are playing and following the waypoints
        if (Application.isPlaying && currentPhase == FlightPhase.following)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(virtualTargetPosition, sphereGizmoRadius);
        }
    }
}
