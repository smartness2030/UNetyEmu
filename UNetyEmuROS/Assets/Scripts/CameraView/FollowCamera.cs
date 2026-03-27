// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute

// Enumeration to define the types of objects that the camera can follow, currently used types: Drone and Car
[Serializable] public enum ObjectType
{
    Drone,
    Car
}

// ----------------------------------------------------------------------
// Class to control the main camera that follows the players
public class FollowCamera : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Private input variables

    [Header("• PRIVATE INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Key to switch to player view mode (following the players)")]
    [SerializeField] private KeyCode keyPlayerViewMode = KeyCode.N;

    [Tooltip("Key to switch to the next player")]
    [SerializeField] private KeyCode keyNextPlayer = KeyCode.C;

    [Tooltip("Key to switch to the previous player")]
    [SerializeField] private KeyCode keyPreviousPlayer = KeyCode.V;

    [Tooltip("Key to switch to map view mode")]
    [SerializeField] private KeyCode keyMapViewMode = KeyCode.M;

    [Tooltip("Key to toggle mouse rotation in map view mode")]
    [SerializeField] private KeyCode keyMouseRotation = KeyCode.R;

    [Tooltip("Key to sprint in map view mode (holding this key while moving will increase the movement speed)")]
    [SerializeField] private KeyCode keySprint = KeyCode.LeftShift;

    [Tooltip("Key to move forward in map view mode")]
    [SerializeField] private KeyCode keyForward = KeyCode.W;

    [Tooltip("Key to move backward in map view mode")]
    [SerializeField] private KeyCode keyBackward = KeyCode.S;

    [Tooltip("Key to move left in map view mode")]
    [SerializeField] private KeyCode keyLeft = KeyCode.A;

    [Tooltip("Key to move right in map view mode")]
    [SerializeField] private KeyCode keyRight = KeyCode.D;

    [Tooltip("Key to move up in map view mode")]
    [SerializeField] private KeyCode keyUp = KeyCode.Q;

    [Tooltip("Key to move down in map view mode")]
    [SerializeField] private KeyCode keyDown = KeyCode.E;

    [Tooltip("Name of the game object type (TAG) that the user wants to follow. Currently used types: 'Drone' and 'Car'")]
    [SerializeField] private ObjectType selectedType;

    [Tooltip("Desired position of the camera with an offset relative to the drone's rotation on all axes")]
    [SerializeField] private Vector3[] offsetPosition = { new Vector3(0f, 1.5f, -3f), new Vector3(0f, 3.5f, -7f) };
    
    [Tooltip("Desired rotation of the camera with an offset relative to the drone's rotation on all axes")]
    [SerializeField] private float offsetXRotation = 15f;

    [Tooltip("Smoothness of camera movement when following the player")]
    [SerializeField] private float smoothSpeed = 0.2f;

    [Tooltip("Smoothness of camera rotation when following the player")]
    [SerializeField] private float rotationSmoothSpeed = 5f;

    [Tooltip("Position of the camera in map view mode, with an offset relative to the center of the map")]
    [SerializeField] private Vector3 mapViewPosition = new Vector3(0f, 10f, 0f);

    [Tooltip("Desired rotation of the camera in map view mode on X axis")]
    [SerializeField] private float mapViewXRotation = 30f;

    [Tooltip("Desired rotation of the camera in map view mode on Y axis")]
    [SerializeField] private float mapViewYRotation = 45f;

    [Tooltip("Sensitivity of the mouse for camera rotation in map view mode")]
    [SerializeField] private float mouseSensitivity = 500f;

    [Tooltip("Base speed of camera movement in map view mode")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Multiplier for camera movement speed when sprinting in map view mode (holding Left Shift)")]
    [SerializeField] private float sprintMultiplier = 10f;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Name of the player currently being followed by the camera (automatically updated)")]
    public string playerName;

    // ----------------------------------------------------------------------
    // Private variables
    
    // Reference to the target game object to follow
    private Transform target;

    // Index of the drone to follow
    private int numVehicle = 0; 
    
    // Offset for the map view camera position
    private Vector3 mapViewPositionOffset = Vector3.zero;

    // Accumulated rotation on the X axis for map view
    private float rotationX = 0f;

    // Accumulated rotation on the Y axis for map view
    private float rotationY = 0f;

    // Minimum rotation on the X axis for mouse rotation in map view mode
    private float minRotationX = -90f;

    // Maximum rotation on the X axis for mouse rotation in map view mode
    private float maxRotationX = 90f;

    // Control if the mouse rotation is enabled
    private bool enableMouseRotation = false; 

    // Array of game objects with the specified tag
    private GameObject[] targetObjects;

    // Reference to the target object to follow
    private GameObject targetObject; 

    // Current index in objectTypes array
    private int currentTypeIndex = 0;

    // Desired position of the camera
    private Vector3 desiredPosition;

    // Smoothed position of the camera
    private Vector3 smoothedPosition;

    // Final rotation of the camera
    private Quaternion finalRotation;

    // Smoothed rotation of the camera
    private Quaternion smoothedRotation;

    // Current speed of camera movement
    private float currentMoveSpeed;

    // Adjusted speed of camera movement
    private float adjustedMoveSpeed;
    
    // Mouse movement on the X axis
    private float mouseX;

    // Mouse movement on the Y axis
    private float mouseY; 

    // Internal tag name (kept for compatibility)
    private string typeObjectName; 

    // Array of object types (TAGs) that the camera can follow. Currently used types: 'Drone' and 'Car'
    private string[] objectTypes = { "Drone", "Car" };

    // Flag to control if the camera is in map view mode
    private bool inMapViewMode = false;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Keep the cursor unlocked and visible at the start
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Synchronize the selected type from the enum to the internal string
        SyncTypeFromEnum();
    }

    // ----------------------------------------------------------------------
    // Method to synchronize the selected type from the enum to the internal string used for finding game objects by tag, and to update the current type index accordingly
    void SyncTypeFromEnum()
    {
        currentTypeIndex = (int)selectedType;
        typeObjectName = objectTypes[currentTypeIndex];
    }

    // ----------------------------------------------------------------------
    // Method called when a value is changed in the Inspector, to keep the internal string and current type index synchronized with the selected enum value
    void OnValidate()
    {
        SyncTypeFromEnum();
    }

    // ----------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // Change to player view mode
        if (Input.GetKeyDown(keyPlayerViewMode))
        {
            // Flag to indicate that the camera is not in map view mode
            inMapViewMode = false;

            // Keep the cursor unlocked and visible
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Next or previous player to follow
        if (Input.GetKeyDown(keyNextPlayer))
        {
            // Move to next player of current type
            numVehicle++;

            // Find all game objects with the specified tag and sort them by name
            targetObjects = GameObject.FindGameObjectsWithTag(typeObjectName);
            Array.Sort(targetObjects, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // If we've reached the end of current type's objects
            if (numVehicle >= targetObjects.Length)
            {
                // Move to next type
                currentTypeIndex = (currentTypeIndex + 1) % objectTypes.Length;
                typeObjectName = objectTypes[currentTypeIndex];

                // Find all game objects with the new type's tag and sort them by name
                targetObjects = GameObject.FindGameObjectsWithTag(typeObjectName);
                Array.Sort(targetObjects, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

                // If there are objects of the new type
                if (targetObjects.Length > 0)
                {
                    // Start from the first object of the new type
                    numVehicle = 0;
                }
                else
                {
                    // No objects of this type found, move to the next type
                    currentTypeIndex = (currentTypeIndex - 1 + objectTypes.Length) % objectTypes.Length;
                    typeObjectName = objectTypes[currentTypeIndex];
                    
                    // No objects of this type found, reset to -1 to avoid out of bounds
                    numVehicle = -1; 
                }
            }
        }

        // Previous player to follow
        if (Input.GetKeyDown(keyPreviousPlayer))
        {
            // Move to previous player of current type
            numVehicle--;

            // Find all game objects with the specified tag and sort them by name
            targetObjects = GameObject.FindGameObjectsWithTag(typeObjectName);
            Array.Sort(targetObjects, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // If we've gone below the first index of the current type
            if (numVehicle < 0)
            {
                // Move to the previous type
                currentTypeIndex = (currentTypeIndex - 1 + objectTypes.Length) % objectTypes.Length;
                typeObjectName = objectTypes[currentTypeIndex];

                // Find all game objects with the new type's tag and sort them by name
                targetObjects = GameObject.FindGameObjectsWithTag(typeObjectName);
                Array.Sort(targetObjects, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

                // If there are objects of the new type
                if (targetObjects.Length > 0)
                {
                    // Start from the last object of the new type
                    numVehicle = targetObjects.Length - 1;
                }
                else
                {
                    // No objects of this type found, return to the original type
                    currentTypeIndex = (currentTypeIndex + 1) % objectTypes.Length;
                    typeObjectName = objectTypes[currentTypeIndex];

                    // Find all game objects with the original type's tag and sort them by name
                    targetObjects = GameObject.FindGameObjectsWithTag(typeObjectName);
                    Array.Sort(targetObjects, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                    
                    // Reset to -1 if no objects found
                    numVehicle = targetObjects.Length > 0 ? targetObjects.Length - 1 : -1;
                }
            }
        }

        // Change to map view mode
        if (Input.GetKeyDown(keyMapViewMode))
        {
            // Flag to indicate that the camera is in map view mode
            inMapViewMode = true;

            // Set the configuration of the map view
            SetMapView();

            // Keep the cursor unlocked and visible
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Toggle mouse rotation
        if (Input.GetKeyDown(keyMouseRotation)) enableMouseRotation = !enableMouseRotation;

        // Handle map view controls
        if (inMapViewMode) HandleMapViewControls();
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // If the camera is in map view mode, do not follow the player
        if (inMapViewMode)
        {
            playerName = "";
            return;
        }

        // Try to find the target game object to follow
        try
        {
            // Find all game objects with the specified tag
            targetObjects = GameObject.FindGameObjectsWithTag(typeObjectName);
            Array.Sort(targetObjects, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // If there are objects with the specified tag
            if (targetObjects.Length > 0)
            {
                // Limit the index to the range of the array
                if (numVehicle >= targetObjects.Length) numVehicle = targetObjects.Length - 1;
                if (numVehicle < 0) numVehicle = 0;
                
                // Get the target game object to follow
                targetObject = targetObjects[numVehicle];
                
                // Get the name of the player
                playerName = targetObject.name;

                // Set the target game object and move the camera to the desired position
                target = targetObject.transform;
                moveToPosition();
            }
        }
        catch (Exception)
        {
            // Reset the index
            numVehicle = 0;

            // Set the player name to empty
            playerName = "";
        }
    }

    // ----------------------------------------------------------------------
    // Method to move the camera to the desired position
    void moveToPosition()
    {
        // Get the offset position based on the current type index 
        Vector3 selectedOffsetPosition = offsetPosition[currentTypeIndex];
        
        // Get the target's yaw rotation 
        float targetYaw = target.eulerAngles.y;

        // Get the horizontal offset by rotating the selected offset position around the Y axis
        Quaternion yawRotation = Quaternion.Euler(0f, targetYaw, 0f);

        // Calculate the desired position by adding the horizontal offset to the target's position
        Vector3 horizontalOffset = yawRotation * selectedOffsetPosition;
        desiredPosition = target.position + horizontalOffset;

        // Set the desired height of the camera by adding the vertical offset to the target's height
        desiredPosition.y = target.position.y + selectedOffsetPosition.y;

        // Smoothly move the camera to the desired position using Lerp for a smooth transition
        smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Calculate the final rotation of the camera by adding the offset rotation to the target's yaw rotation
        finalRotation = Quaternion.Euler(offsetXRotation, targetYaw, 0f);
        smoothedRotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            rotationSmoothSpeed * Time.deltaTime
        );

        // Apply the smoothed rotation to the camera
        transform.rotation = smoothedRotation;
    }

    // ----------------------------------------------------------------------
    // Method for setting the view of the map
    void SetMapView()
    {
        // Set the position and rotation of the camera to the map view object
        transform.position = mapViewPosition;
        transform.rotation = Quaternion.Euler(mapViewXRotation, mapViewYRotation, 0f);

        // Initializes the accumulated rotations based on the current camera rotation
        rotationX = transform.eulerAngles.x;
        rotationY = transform.eulerAngles.y;
    }

    // ----------------------------------------------------------------------
    // Method to handle map view controls
    void HandleMapViewControls()
    {
        // Determines the speed of movement: applies the multiplier if the Key Shift is pressed
        currentMoveSpeed = moveSpeed * (Input.GetKey(keySprint) ? sprintMultiplier : 1f);
        adjustedMoveSpeed = currentMoveSpeed * Time.deltaTime;

        // Camera movements in map view using local axes
        if (Input.GetKey(keyForward))
        {
            mapViewPositionOffset += transform.forward * adjustedMoveSpeed;  // Zoom In
        }
        if (Input.GetKey(keyBackward))
        {
            mapViewPositionOffset -= transform.forward * adjustedMoveSpeed;  // Zoom Out
        }
        if (Input.GetKey(keyLeft))
        {
            mapViewPositionOffset -= transform.right * adjustedMoveSpeed;  // Left
        }
        if (Input.GetKey(keyRight))
        {
            mapViewPositionOffset += transform.right * adjustedMoveSpeed;  // Right
        }
        if (Input.GetKey(keyUp))
        {
            mapViewPositionOffset += transform.up * adjustedMoveSpeed;  // Up
        }
        if (Input.GetKey(keyDown))
        {
            mapViewPositionOffset -= transform.up * adjustedMoveSpeed;  // Down
        }

        // Applies the offset to the camera position
        transform.position = mapViewPosition + mapViewPositionOffset;

        // Camera rotation using mouse movement, only if enableMouseRotation is true
        if (enableMouseRotation)
        {
            // Get the mouse movement
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            // Accumulate the rotation on the X and Y axes
            rotationX -= mouseY;
            rotationY += mouseX;

            // Limit the rotation on the X axis
            rotationX = Mathf.Clamp(rotationX, minRotationX, maxRotationX);

            // Apply the accumulated rotation to the camera
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
    }
}
