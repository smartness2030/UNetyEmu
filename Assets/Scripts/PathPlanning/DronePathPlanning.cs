using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using System.Collections;
using UnityEngine.Analytics; // Library to use in IEnumerator class

public class DronePathPlanning : MonoBehaviour
{

    [Header("Movement Parameters")]
    public float maxSpeed = 15.0f;
    public float acceleration = 1.0f;
    public float distanceBetweenPoints = 1.0f;

    [Header("Deceleration Control")]
    [Tooltip("Factor > 1.0 starts deceleration earlier")]
    public float anticipationFactor = 0.8f;

    [Header("Movement State (Debug)")]
    public float currentSpeed = 0f;

    public List<Vector3> routePoints = new List<Vector3>(); // Lista de objetivos
    public List<float> routeOrientations = new List<float>(); // Lista de orientaciones

    public List<Vector3> routeReplanningPoints = new List<Vector3>(); // Lista de objetivos
    public List<float> routeReplanningOrientations = new List<float>(); // Lista de orientaciones

    public float rotationSpeed = 5f;

    public List<Vector3> points = new List<Vector3>(); // Puntos intermedios actuales
    public int currentIndex = 0;
    public int destinationIndex = 0;

    private Vector3 endPosition;

    public bool flagStartPath;

    public GameObject targetObj;
    private string objectID;

    private GetDroneFeatures getDroneFeatures;

    private Rigidbody targetRb;

    private Vector3 lastTargetPosition;

    private VehicleCommunication vehicleCommunication;

    public int emergencyIndex = 0;

    private Vector3 emergencyDirection = Vector3.zero;
    private bool emergencyModeActive;

    public float brakingFactor = 2f;

    public bool setRouteReplanning;

    // Variable to store the distance and azimuth between two points
    private Vector3 distanceAzimuth;

    // Variable to store the angle in radians
    private float angleInRadians;

    public bool droneLandedInTruck;

    public float flagTime2;
    public float flagTime4;

    public bool coroutineFlagTime2;
    public bool coroutineFlagTime4;

    private DroneJoinedToPackage droneJoinedToPackage;


    // Get the Drone1Animation and Drone2Animation components to set the animations of the drone
    private Drone1Animation drone1Animation;
    private Drone2Animation drone2Animation;
    private int flagAnimation;

    public bool finishRoutePlanning;


    void Start()
    {

        finishRoutePlanning = false;

        flagStartPath = false;
        setRouteReplanning = false;

        emergencyModeActive = false;

        droneLandedInTruck = false;

        flagTime2 = 0f;
        flagTime4 = 0f;

        coroutineFlagTime2 = false;
        coroutineFlagTime4 = false;

        GetObjectFeatures getObjectFeatures = GetComponent<GetObjectFeatures>();
        objectID = getObjectFeatures.objectID;
        targetObj = GameObject.Find("ID" + objectID + "Target");

        getDroneFeatures = GetComponent<GetDroneFeatures>();
        if (getDroneFeatures != null)
        {
            maxSpeed = getDroneFeatures.maxSpeedManufacturer;
        }

        targetRb = targetObj.GetComponent<Rigidbody>();

        vehicleCommunication = GetComponent<VehicleCommunication>();

        // Get the DroneJoinedToPackage component
        droneJoinedToPackage = GetComponent<DroneJoinedToPackage>();

        flagAnimation = 0;

        // Get the Drone1Animation and Drone2Animation components
        drone1Animation = GetComponent<Drone1Animation>();
        drone2Animation = GetComponent<Drone2Animation>();


    }

    void FixedUpdate()
    {
        
        if (droneLandedInTruck)
        {
            return;
        }

        if (routePoints.Count == 0)
        {
            return;
        }
        else
        {

            if (!flagStartPath)
            {

                // Iniciar movimiento hacia el primer destino
                destinationIndex = 0;
                SetNextDestination();

                flagStartPath = true;

            }

        }

        if ((setRouteReplanning) && (!finishRoutePlanning))
        {

            if (currentIndex >= points.Count)
            {
                
                // Llegamos al final del camino actual, avanzar al siguiente destino si hay
                destinationIndex++;
                if (destinationIndex < routeReplanningPoints.Count)
                {
                    SetEmergencyDestination();
                }
                else
                {
                    // Movimiento completo
                    currentSpeed = 0f;

                    droneLandedInTruck = true;

                    return;
                }
            }

            float totalRemainingDistance = Vector3.Distance(targetObj.transform.position, endPosition);
            float anticipatedDistance = totalRemainingDistance * anticipationFactor;
            float requiredDeceleration = (currentSpeed * currentSpeed) / (2 * Mathf.Max(anticipatedDistance, 0.01f));

            if (requiredDeceleration > acceleration)
            {
                currentSpeed -= requiredDeceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Max(0, currentSpeed);
            }
            else
            {

                currentSpeed += acceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            }

            Vector3 destination = points[currentIndex];
            targetObj.transform.position = Vector3.MoveTowards(targetObj.transform.position, destination, currentSpeed * Time.fixedDeltaTime);

            // Aplicar rotación hacia la orientación deseada
            if (destinationIndex < routeReplanningOrientations.Count)
            {
                float targetYAngle = routeReplanningOrientations[destinationIndex];
                Quaternion targetRotation = Quaternion.Euler(
                    targetObj.transform.eulerAngles.x,
                    targetYAngle,
                    targetObj.transform.eulerAngles.z
                );

                targetObj.transform.rotation = Quaternion.Slerp(
                    targetObj.transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );
            }

            // Verificar si alcanzamos el destino
            if (targetObj.transform.position == destination)
            {
                currentIndex++;
            }

            return;
        }

        if ((!vehicleCommunication.flagOnlineCommunication) && (!setRouteReplanning))
        {
            if (!emergencyModeActive)
            {
                emergencyModeActive = true;

                // Determina la dirección actual si hay movimiento
                if (points.Count > currentIndex + 1)
                {
                    emergencyDirection = (points[currentIndex + 1] - points[currentIndex]).normalized;
                }
                else
                {
                    emergencyDirection = transform.forward; // Por defecto
                }
            }

            // Desacelerar suavemente la velocidad actual del target
            currentSpeed -= acceleration * brakingFactor * Time.fixedDeltaTime;
            currentSpeed = Mathf.Max(0, currentSpeed);

            // Avanza el target en la última dirección conocida
            targetObj.transform.position += emergencyDirection * currentSpeed * Time.fixedDeltaTime;

            if (currentSpeed < 0.1f)
            {

                setRouteReplanning = true;

                currentIndex = 0;

                destinationIndex = 0;

                points.Clear();

                Vector3 carPosition = vehicleCommunication.carVehiclePosition;
                float carOrientation = vehicleCommunication.carVehicleOrientation;

                Vector3 newCheckPoint1 = new Vector3(carPosition.x, targetObj.transform.position.y, carPosition.z);
                Vector3 newCheckPoint2 = new Vector3(carPosition.x, carPosition.y, carPosition.z);

                float newCheckOrientation1 = GetAzimuth(newCheckPoint1, targetObj.transform.position);
                float newCheckOrientation2 = carOrientation;

                routeReplanningPoints.Add(newCheckPoint1);
                routeReplanningPoints.Add(newCheckPoint2);

                routeReplanningOrientations.Add(newCheckOrientation1);
                routeReplanningOrientations.Add(newCheckOrientation2);

                SetEmergencyDestination();

            }

            return;
        }



        if ((flagStartPath && vehicleCommunication.flagOnlineCommunication) && (!setRouteReplanning))
        {

            if (currentIndex >= points.Count)
            {

                // Pick up the package
                if (destinationIndex == 2)
                {

                    if (!coroutineFlagTime2)
                    {
                        coroutineFlagTime2 = true; // Set the flag to true
                        StartCoroutine(WaitUntil2()); // Start the coroutine
                    }

                    if (!coroutineFlagTime4)
                    {
                        coroutineFlagTime4 = true; // Set the flag to true
                        StartCoroutine(WaitUntil4()); // Start the coroutine
                    }

                    if (flagTime2 == 0)
                    {
                        return;
                    }

                    if ((flagTime2 == 1) && (flagAnimation == 0))
                    {

                        // Identify the package to be anchored to the drone
                        GameObject package = GameObject.Find("prefabPackage_" + gameObject.name);

                        // If the package is found. Anchor the package to the drone
                        if (package != null) droneJoinedToPackage.objectPackage = package.transform;

                        droneAttachToPackage();

                    }

                    if (flagTime4 == 0)
                    {
                        return;
                    }

                    if (flagTime4 == 1)
                    {

                        flagAnimation = 0;

                        flagTime2 = 0; // Reset the flag
                        flagTime4 = 0; // Reset the flag
                        
                        coroutineFlagTime2 = false; // Reset the flag
                        coroutineFlagTime4 = false; // Reset the flag

                    }

                }

                // Deliver the package
                if (destinationIndex == 5)
                {

                    if (!coroutineFlagTime2)
                    {
                        coroutineFlagTime2 = true; // Set the flag to true
                        StartCoroutine(WaitUntil2()); // Start the coroutine
                    }

                    if (!coroutineFlagTime4)
                    {
                        coroutineFlagTime4 = true; // Set the flag to true
                        StartCoroutine(WaitUntil4()); // Start the coroutine
                    }

                    if (flagTime2 == 0)
                    {
                        return;
                    }
                    

                    if ((flagTime2 == 1) && (flagAnimation == 0))
                    {

                        // Identify the package to be anchored to the drone
                        GameObject package = GameObject.Find("prefabPackage_" + gameObject.name);

                        // If the package is found. Anchor the package to the drone
                        if (package != null) droneJoinedToPackage.objectPackage = package.transform;

                        droneDeliverPackage();

                    }

                    if (flagTime4 == 0)
                    {
                        return;
                    }

                    if (flagTime4 == 1)
                    {

                        flagAnimation = 0;

                        flagTime2 = 0; // Reset the flag
                        flagTime4 = 0; // Reset the flag

                        coroutineFlagTime2 = false; // Reset the flag
                        coroutineFlagTime4 = false; // Reset the flag

                        finishRoutePlanning = true;

                    }

                }


                // Llegamos al final del camino actual, avanzar al siguiente destino si hay
                destinationIndex++;

                if (destinationIndex < routePoints.Count)
                {
                    SetNextDestination();
                }
                else
                {
                    // Movimiento completo
                    currentSpeed = 0f;
                    return;
                }
            }

            float totalRemainingDistance = Vector3.Distance(targetObj.transform.position, endPosition);
            float anticipatedDistance = totalRemainingDistance * anticipationFactor;
            float requiredDeceleration = (currentSpeed * currentSpeed) / (2 * Mathf.Max(anticipatedDistance, 0.01f));

            if (requiredDeceleration > acceleration)
            {
                currentSpeed -= requiredDeceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Max(0, currentSpeed);
            }
            else
            {

                currentSpeed += acceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            }

            Vector3 destination = points[currentIndex];
            targetObj.transform.position = Vector3.MoveTowards(targetObj.transform.position, destination, currentSpeed * Time.fixedDeltaTime);

            // Aplicar rotación hacia la orientación deseada
            if (destinationIndex < routeOrientations.Count)
            {
                float targetYAngle = routeOrientations[destinationIndex];
                Quaternion targetRotation = Quaternion.Euler(
                    targetObj.transform.eulerAngles.x,
                    targetYAngle,
                    targetObj.transform.eulerAngles.z
                );

                targetObj.transform.rotation = Quaternion.Slerp(
                    targetObj.transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );
            }

            // Verificar si alcanzamos el destino
            if (targetObj.transform.position == destination)
            {
                currentIndex++;
            }


        }

    }

    void droneDeliverPackage()
    {
        
        // Set the package delivered flag to true and call the deliver animation method
        droneJoinedToPackage.isPackageDelivered = true; 
        deliverAnimation();

    }

    void deliverAnimation()
    {

        // Set the deliver package flag to true for the type of the Drone1
        if(drone1Animation != null) drone1Animation.isDeliveringPackage = true;

        // Set the deliver package flag to true for the type of the Drone2
        if(drone2Animation != null) drone2Animation.isDeliveringPackage = true;

        // Reset the flag of the animation
        flagAnimation = 1;

    }

    void droneAttachToPackage()
    {

        droneJoinedToPackage.isPackageAttached = true; // Set the package attached flag to true
        pickUpAnimation(); // Call the pick up animation method
        
    }

    void pickUpAnimation()
    {

        // Set the pick up package flag to true for the type of the Drone1
        if(drone1Animation != null) drone1Animation.isPickingUpPackage = true;

        // Set the pick up package flag to true for the type of the Drone2
        if(drone2Animation != null) drone2Animation.isPickingUpPackage = true;

        // Reset the flag of the animation
        flagAnimation = 1;

    }

    IEnumerator WaitUntil2()
    {
        flagTime2 = 0; // Set the flag to 0 to wait
        yield return new WaitForSeconds(2); // Wait for n seconds
        flagTime2 = 1; // Set the flag to 1 to continue
    }

    IEnumerator WaitUntil4()
    {
        flagTime4 = 0; // Set the flag to 0 to wait
        yield return new WaitForSeconds(4); // Wait for n seconds
        flagTime4 = 1; // Set the flag to 1 to continue
    }

    private void SetNextDestination()
    {
        endPosition = routePoints[destinationIndex];
        CalculatePoints(targetObj.transform.position, endPosition);
        currentIndex = 0;
    }

    private void SetEmergencyDestination()
    {
        endPosition = routeReplanningPoints[destinationIndex];
        CalculatePoints(targetObj.transform.position, endPosition);
        currentIndex = 0;
    }

    public void CalculatePoints(Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 direction = (endPosition - startPosition).normalized;
        float totalDistance = Vector3.Distance(startPosition, endPosition);
        int pointCount = Mathf.CeilToInt(totalDistance / distanceBetweenPoints);

        points.Clear();
        for (int i = 0; i <= pointCount; i++)
        {
            Vector3 point = startPosition + direction * distanceBetweenPoints * i;
            if (Vector3.Distance(startPosition, point) > totalDistance)
                point = endPosition;
            points.Add(point);
        }
    }

    float GetAzimuth(Vector3 from, Vector3 to)
    {

        //  Calculate the distance and azimuth between two points
        distanceAzimuth = from - to;

        // Calculate the angle in radians
        angleInRadians = Mathf.Atan2(distanceAzimuth.z, distanceAzimuth.x);

        // Return the angle in degrees
        return 90 - angleInRadians * Mathf.Rad2Deg;

    }

}
