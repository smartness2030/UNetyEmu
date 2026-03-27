// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine; // To use in MonoBehaviour classes
using System; // To use Serializable attribute

// Enum to define the package states
[Serializable] public enum PackageState
{
    Idle,       // Package is not attached or delivered
    Attached,   // Package is attached to the drone
    Delivered   // Package has been delivered
}

// ----------------------------------------------------------------------
// Class to join the package object with the drone object, which will be used to manage the package attachment and delivery process
// Requires a DroneControlInputs component to check the control mode of the drone
[RequireComponent(typeof(DroneControlInputs))]
public class AttachObject : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Public input variables

    [Header("• PUBLIC INPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Current state of the package (Idle, Attached, Delivered)")]
    public PackageState packageState = PackageState.Idle;

    [Tooltip("Package object transform to be attached to the drone")]
    public Transform packageObject;

    [Tooltip("Distance threshold to consider a package nearby for attachment only in Manual control mode")]
    public float distanceToPackageToAttach = 1f;

    // ----------------------------------------------------------------------
    // Output variables

    [Header("• OUTPUT VARIABLES OF THIS CLASS:")]
    [Space(8)] // Space for better organization in the Inspector

    [Tooltip("Boolean to indicate if there is a nearby package to attach")]
    public bool isPackageNearby = false;
   
    // ----------------------------------------------------------------------
    // Private variables

    // Reference to the DroneControlInputs component to check the control mode
    private DroneControlInputs droneControlInputs;

    // To store all possible package objects in the scene
    private GameObject[] possiblePackages;

    // Variables to join the package object with the drone object
    private FixedJoint packageJoint; 
    private Rigidbody packageRigidbody;

    // ----------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        // Get the DroneControlInputs component to check the control mode if needed
        droneControlInputs = GetComponent<DroneControlInputs>();
    }

    // ----------------------------------------------------------------------
    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        // Check if we moved away from the current nearby package
        CheckIfStillNearby();
        
        // Only try to find and attach a nearby package if not in Automatic control mode
        if (droneControlInputs.controlMode != DroneControlMode.Automatic && !isPackageNearby)
            FindANearbyPackage();
        
        // If the package object is not null
        if (packageObject != null)
        {
            // Get the Rigidbody component of the package object
            packageRigidbody = packageObject.GetComponent<Rigidbody>();

            // Handle package behavior based on its state
            switch (packageState)
            {
                case PackageState.Attached:
                    AttachPackage();
                    break;

                case PackageState.Delivered:
                    ReleasePackage();
                    break;

                case PackageState.Idle:
                    break; // Do nothing in Idle state

                default:
                    break;
            }
        }
    }

    // ----------------------------------------------------------------------
    // Method to find a nearby package to attach, only in Manual control mode
    void FindANearbyPackage()
    {
        // Find all game objects with the tag "Package" in the scene
        possiblePackages = GameObject.FindGameObjectsWithTag("Package");

        // Check the distance to each package and set the first one within the threshold as the nearby package to attach
        foreach (GameObject package in possiblePackages)
        {
            // Calculate the distance between the drone and the package            
            float distance = Vector3.Distance(transform.position, package.transform.position);

            // If the distance is less than the threshold, set this package as the nearby package to attach and break the loop
            if (distance < distanceToPackageToAttach)
            {
                packageObject = package.transform;
                isPackageNearby = true;
                break; // Stop searching after finding the first nearby package
            }
        }
    }

    // ----------------------------------------------------------------------
    // Method to check if we moved away from the current nearby package, in which case we clear the reference to it
    void CheckIfStillNearby()
    {
        // If the package object is null, there is no nearby package to check
        if (packageObject == null) 
            return;

        // Calculate the distance between the drone and the package
        float distance = Vector3.Distance(transform.position, packageObject.position);

        // If drone moved away, clear reference
        if (distance > distanceToPackageToAttach)
        {
            packageObject = null;
            isPackageNearby = false;
        }
    }

    // ----------------------------------------------------------------------
    // Method to attach the package to the drone
    void AttachPackage()
    {
        // Create a FixedJoint if not already created and Rigidbody exists
        if (packageJoint == null && packageRigidbody != null)
        {
            packageJoint = gameObject.AddComponent<FixedJoint>();
            packageJoint.connectedBody = packageRigidbody;
        }

        // After attaching, return state to Idle to prevent repeated attachment
        packageState = PackageState.Idle;
    }

    // ----------------------------------------------------------------------
    // Method to release the package from the drone
    void ReleasePackage()
    {
        // Destroy the joint if it exists
        if (packageJoint != null  && packageRigidbody != null)
        {
            Destroy(packageJoint);
            packageRigidbody.WakeUp();
        }

        // Reset the package joint reference after releasing the package
        packageJoint = null;

        // After releasing, return state to Idle
        packageState = PackageState.Idle;
    }
}
