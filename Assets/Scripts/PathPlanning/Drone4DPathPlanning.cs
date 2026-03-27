// Libraries
using System.Collections.Generic; // Library to use lists
using UnityEngine; // Unity Engine library to use in MonoBehaviour classes
using System.Collections; // Library to use in IEnumerator class
using System;

// Class to set a single route planning for the drone
public class Drone4DPathPlanning : MonoBehaviour
{


    public float speed = 1;
    public float minSpeed = 1f;

    public float radius = 0f;
    public float startTimeOutbound = 0f;

    public float minErrorToFinalTarget = 0.3f;

    public float acceleration = 0.5f;
    public float deceleration = 12.0f;

    public float rotationSpeed = 10f;
    public float minErrorToTarget = 0.5f;
    public int lookaheadRange = 20;

    public int currentPointIndex = 0;
    public float currentTargetSpeed = 0f;

    public float maxFollowDistance = 1.5f;

    public float maxAngle = 60f;

    

    public static bool flagSphereMain = false; // Flag to check if the sphere is the main one







    public float startTimeReturn = 0f; // Start time for the return path

    public int currentPointIndexReturn = 0;

    public float currentTargetSpeedReturn = 0f;





    // List of route points
    [SerializeField] public List<Vector3> routePointsOutbound = new List<Vector3> { };
    [SerializeField] public List<Vector3> routePointsReturn = new List<Vector3> { };




    public bool flagStartCoroutineReturn = false; // Flag to check if the coroutine has started for the outbound path

    

    


    

    public bool flagFullPathFinished = false; // Flag to check if the full path has been finished


    //public bool flagReverseTest = true; // Flag to check if the route points should be reversed for the return path

    





    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // Get the object features and the object ID
    private GetObjectFeatures getObjectFeatures;
    private string objectID;

    // Sphere target object related to the drone
    private GameObject targetObj;

    // Variables to store the target position, rotation, direction, and distance to target
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 direction;
    private float distanceToNextPoint;



    private float minDistance;

    private bool flagStartCoroutineOutbound = false;
    

    
    private bool flagTargetReachedReturn = false; // Flag to check if the target has been reached
    private bool flagStandbyReturn = false; // Flag to check if the drone is in standby mode

    
    

    private Transform safetySphereTransform;
    private Transform safetyCircleTransform;

    private Rigidbody rb;

    private DroneJoinedToPackage droneJoinedToPackage;

    private GameObject package;

    private Drone1Animation drone1Animation;
    private Drone2Animation drone2Animation;

    



    public bool flagStartPath = false;
    public bool flagStartPathReturn = false; // Flag to check if the coroutine has started for the return path

    public bool flagPathReturn = false;

    // Internal state
    public int currentIndex;

    public Vector3 startPoint;
    public Vector3 endPoint;

    public float segmentDuration;
    public float elapsedTime;

    public bool flagTargetReached = false; // Flag to check if the target has been reached
    public bool flagStandby = false; // Flag to check if the drone is in standby mode
    public bool flagDeliverPackage = false; // Flag to check if the package is being delivered


    public Vector3 liftStartPosition;
    public Vector3 liftTargetPosition;
    public float liftElapsedTime;
    public float liftDuration = 2f;

    public bool isTakeOff = false;
    public bool isTakeOffEnded = false;

    public bool isLanding = false;
    public bool isLandingEnded = false;

    public Vector3 finalTargetPosition; // Final target position
    public GameObject found;

    public bool flagFirstTakeOff = false;

    // Collision detection flag
    [Header("Collision Detection")]
    public bool isColliding = false; // Flag to indicate if the drone is colliding with an object
    
    Collision collisionName;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        // Get the object features and the object ID
        getObjectFeatures = GetComponent<GetObjectFeatures>();
        objectID = getObjectFeatures.objectID;

        // Get the sphere target object related to the drone
        targetObj = GameObject.Find("ID_" + objectID + "_Target");

        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component of the drone

        droneJoinedToPackage = GetComponent<DroneJoinedToPackage>(); // Get the DroneJoinedToPackage component

        // Get the Drone1Animation and Drone2Animation components
        drone1Animation = GetComponent<Drone1Animation>();
        drone2Animation = GetComponent<Drone2Animation>();

        // Identify the package to be anchored to the drone
        package = GameObject.Find("Package_" + objectID);

        // If the package is found. Anchor the package to the drone
        if (package != null)
        {

            droneJoinedToPackage.objectPackage = package.transform;

            droneJoinedToPackage.isPackageAttached = true;

            // Set the pick up package flag to true for the type of the Drone1
            if (drone1Animation != null) drone1Animation.isPickingUpPackage = true;

            // Set the pick up package flag to true for the type of the Drone2
            if (drone2Animation != null) drone2Animation.isPickingUpPackage = true;

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // FixedUpdate is called at fixed time intervals, ideal for physical changes in objects:

    void FixedUpdate()
    {
        
        // if(isColliding)
        // {
        //     Debug.Log("Agent " + objectID + " is colliding with an object.");
        // }
        
        if (safetySphereTransform != null)
        {
            safetySphereTransform.gameObject.SetActive(flagSphereMain);
        }

        if (routePointsOutbound.Count > 1)
        {

            if (flagStartCoroutineOutbound == false)
            {

                SetRadius();

                // Limpiar la ruta de puntos duplicados
                RemoveConsecutiveDuplicates();

                flagStartCoroutineOutbound = true;

                flagStartPath = false;

                StartCoroutine(StartRoutePlanning());

            }

        }

        if ((flagStartCoroutineOutbound) && isTakeOffEnded == false)
        {
            TakeOff();
        }

        if (flagStartPath && (flagStartPathReturn == false) && isTakeOffEnded)
        {
            
            // Call the function to follow the route
            FollowRoute();

        }

        if (flagPathReturn)
        {

            if (routePointsReturn.Count > 0)
            {

                // if (flagReverseTest == true)
                // {

                //     /*routePointsReturn.Reverse();*/
                //     flagReverseTest = false; 

                // }
                
                if (flagStartCoroutineReturn == false)
                {

                    // Limpiar la ruta de puntos duplicados
                    RemoveConsecutiveDuplicatesReturn();

                    Debug.Log("Starting return path for Agent " + objectID);

                    Vector3 newStartPosition = routePointsReturn[0];
                    newStartPosition.y = newStartPosition.y - 2f; // Adjust the height for the return path
                    
                    targetObj.transform.position = newStartPosition;
                    currentIndex = 0;

                    PrepareNextSegmentReturn();

                    flagStartPathReturn = true; // Start the path after the specified time

                    flagStartPath = false;

                    flagStartCoroutineReturn = true;

                    isTakeOffEnded = false;

                    flagTargetReached = false;
                }

            }

            if ((flagStartCoroutineReturn) && isTakeOffEnded == false)
            {
                TakeOff();
            }

            if (flagStartPathReturn && (flagStartPath == false) && isTakeOffEnded)
            {
                
                // Call the function to follow the route
                FollowReturnRoute();

            }

        }



    }

    void TakeOff()
    {

        if (isTakeOff == false)
        {
            liftStartPosition = targetObj.transform.position;
            liftTargetPosition = liftStartPosition + Vector3.up * 2f;
            isTakeOff = true;
            //Debug.Log("Agent " + objectID + " is taking off at start position: " + liftStartPosition + " to target position: " + liftTargetPosition);
        }

        liftElapsedTime += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(liftElapsedTime / liftDuration);

        targetObj.transform.position = Vector3.Lerp(liftStartPosition, liftTargetPosition, t);

        if (t >= 1f)
        {
            targetObj.transform.position = liftTargetPosition;
            isTakeOffEnded = true;
            isTakeOff = false;
            liftElapsedTime = 0f;
            flagFirstTakeOff = true;
        }

    }   

    IEnumerator StartRoutePlanning()
    {

        yield return new WaitForSeconds(startTimeOutbound);

        targetObj.transform.position = routePointsOutbound[0];
        currentIndex = 0;

        PrepareNextSegmentOutbound();

        flagStartPath = true; // Start the path after the specified time

    }

    IEnumerator WaitUntilStartReturn()
    {

        yield return new WaitForSeconds(startTimeReturn - 1f);

        flagPathReturn = true; // Set the flag to true to start the return path

        //flagDeliverPackage = true; // Start the path after the specified time

    }

    IEnumerator WaitUntilLand(float timeDeliver)
    {

        yield return new WaitForSeconds(timeDeliver);

        flagDeliverPackage = true; // Start the path after the specified time

    }

    IEnumerator WaitUntilLandReturn(float timeDeliver)
    {

        yield return new WaitForSeconds(timeDeliver);

        flagFullPathFinished = true; // Start the path after the specified time

        Debug.Log("Agent " + objectID + " has reached the final target position.");

    }

    IEnumerator DestroyPackage(float timeDeliver)
    {

        yield return new WaitForSeconds(timeDeliver);

        if (package != null)
        {
            Destroy(package);
        }

    }

    void PrepareNextSegmentOutbound()
    {
        
        if (currentIndex >= routePointsOutbound.Count - 1)
        {
            currentIndex = -1;
            return;
        }
        
        if (currentIndex < 0)
        {
            return;
        }

        startPoint = routePointsOutbound[currentIndex];
        endPoint = routePointsOutbound[currentIndex + 1];

        // --- CHANGE HERE: use Euclidean distance instead of Manhattan ---
        float distance = Vector3.Distance(startPoint, endPoint);

        //float manhattanDistance = CalculateManhattanDistance(startPoint, endPoint);

        segmentDuration = distance / speed;
        if (segmentDuration < 0.0001f)
            segmentDuration = 0.0001f;

        elapsedTime = 0f;

        // Calculate target rotation (Y axis only)
        Vector3 direction = endPoint - startPoint;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    void PrepareNextSegmentReturn()
    {
        
        if (currentIndex >= routePointsReturn.Count - 1)
        {

            return;
        }
        
        if (currentIndex < 0)
        {
            return;
        }

        startPoint = routePointsReturn[currentIndex];
        endPoint = routePointsReturn[currentIndex + 1];

        float manhattanDistance = CalculateManhattanDistance(startPoint, endPoint);

        segmentDuration = manhattanDistance / speed;
        if (segmentDuration < 0.0001f)
            segmentDuration = 0.0001f;

        elapsedTime = 0f;

        // Calculate target rotation (Y axis only)
        Vector3 direction = endPoint - startPoint;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    float CalculateManhattanDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x)
             + Mathf.Abs(a.y - b.y)
             + Mathf.Abs(a.z - b.z);
    }

    void SetRadius()
    {

        //LineRenderer line = gameObject.GetComponent<LineRenderer>();

        //line.widthMultiplier = radius;

        // safetyCircleTransform = transform.Find("SafetyCircle");

        // if (safetyCircleTransform != null)
        // {
        //     float scale = radius * 0.01f;
        //     safetyCircleTransform.transform.localScale = new Vector3(scale, scale, scale);
        //     
        // }

        safetySphereTransform = transform.Find("SafetySphere");

        if (safetySphereTransform != null)
        {
            float scale = radius * 0.01f * 2f;
            safetySphereTransform.transform.localScale = new Vector3(scale, scale, scale);
            safetySphereTransform.gameObject.SetActive(flagSphereMain);
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Class to follow the route:

    void FollowRoute()
    {

        if (flagTargetReached == false)
        {
            
            elapsedTime += Time.fixedDeltaTime;

            float t = elapsedTime / segmentDuration;
            t = Mathf.Clamp01(t);

            targetObj.transform.position = Vector3.Lerp(startPoint, endPoint, t);

            targetObj.transform.rotation = Quaternion.Slerp(
                targetObj.transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );

            if (t >= 1f)
            {
                //transform.position = endPoint;

                targetObj.transform.position = endPoint;

                currentIndex++;

                if (currentIndex >= routePointsOutbound.Count - 1)
                {
                    flagTargetReached = true;
                    currentIndex = -1;
                    elapsedTime = 0f;
                    StartCoroutine(WaitUntilStartReturn());
                    return;
                }

                PrepareNextSegmentOutbound();
            }

        }
        
        







        // if ((currentPointIndex < routePointsOutbound.Count) && (flagTargetReached == false))
        // {
        //     // Set the target position to the current point index
        //     targetPosition = routePointsOutbound[currentPointIndex];

        //     Vector3 currentPos = targetObj.transform.position;
        //     Vector3 toTarget = targetPosition - currentPos;
        //     Vector3 currentDir = toTarget.normalized;

        //     // Calcular dirección promedio hacia adelante (lookahead)
        //     Vector3 avgDir = Vector3.zero;
        //     for (int i = 1; i <= lookaheadRange; i++)
        //     {
        //         int index = Mathf.Clamp(currentPointIndex + i, 0, routePointsOutbound.Count - 1);
        //         avgDir += (routePointsOutbound[index] - currentPos).normalized;
        //     }
        //     avgDir.Normalize();

        //     // Calcular curvatura
        //     float angle = Vector3.Angle(currentDir, avgDir);
        //     float turnFactor = Mathf.InverseLerp(0f, maxAngle, angle);
        //     float targetSpeed = Mathf.Lerp(speed, minSpeed, turnFactor);

        //     // Chequeo anticipado de curvas en próximos puntos
        //     bool curvaFuertePorDelante = false;
        //     for (int i = 1; i < lookaheadRange - 1; i++)
        //     {
        //         int a = Mathf.Clamp(currentPointIndex + i - 1, 0, routePointsOutbound.Count - 1);
        //         int b = Mathf.Clamp(currentPointIndex + i, 0, routePointsOutbound.Count - 1);
        //         int c = Mathf.Clamp(currentPointIndex + i + 1, 0, routePointsOutbound.Count - 1);

        //         Vector3 dir1 = (routePointsOutbound[b] - routePointsOutbound[a]).normalized;
        //         Vector3 dir2 = (routePointsOutbound[c] - routePointsOutbound[b]).normalized;
        //         float ang = Vector3.Angle(dir1, dir2);

        //         if (ang > 20f)
        //         {
        //             curvaFuertePorDelante = true;
        //             break;
        //         }
        //     }

        //     // --- NUEVO BLOQUE: CONTROL DE DISTANCIA Y ORIENTACIÓN DEL DRON ---
        //     float droneToTargetDistance = Vector3.Distance(transform.position, targetObj.transform.position);
        //     bool droneIsTooFar = droneToTargetDistance > maxFollowDistance;

        //     float targetObjectRotationY = targetObj.transform.rotation.eulerAngles.y;
        //     float objectRotationY = transform.rotation.eulerAngles.y;
        //     float angleDiff = Mathf.DeltaAngle(targetObjectRotationY, objectRotationY);
        //     bool droneIsMisaligned = Mathf.Abs(angleDiff) > maxAngle;

        //     // Si el dron está lejos o desorientado, aplicar "modo espera"
        //     bool droneNeedsCatchup = droneIsTooFar || droneIsMisaligned;


        //     if (curvaFuertePorDelante || droneNeedsCatchup)
        //     {
        //         targetSpeed *= 0.5f; // anticipar frenado
        //     }

        //     // Desacelerar si nos acercamos al final de la trayectoria
        //     int remainingPoints = routePointsOutbound.Count - currentPointIndex;
        //     if (remainingPoints <= lookaheadRange)
        //     {
        //         // Distancia hasta el último punto
        //         float distanceToEnd = Vector3.Distance(currentPos, routePointsOutbound[routePointsOutbound.Count - 1]);
        //         float slowdownFactor = Mathf.InverseLerp(20f, 0f, distanceToEnd); // entre 10m y 0m desacelera
        //         float finalApproachSpeed = Mathf.Lerp(speed, minSpeed, slowdownFactor);

        //         targetSpeed = Mathf.Min(targetSpeed, finalApproachSpeed);
        //     }




        //     // Transición suave de velocidad con desaceleración más fuerte
        //     float deltaSpeed = targetSpeed - currentTargetSpeed;
        //     float decelFactor = Mathf.Max(1f, currentTargetSpeed / minSpeed); // mayor velocidad, mayor freno

        //     if (deltaSpeed > 0)
        //     {
        //         // Acelerando
        //         currentTargetSpeed += acceleration * Time.fixedDeltaTime;
        //     }
        //     else
        //     {
        //         // Desacelerando (más fuerte y proporcional a la velocidad)
        //         currentTargetSpeed += deceleration * decelFactor * Time.fixedDeltaTime * Mathf.Sign(deltaSpeed);
        //     }

        //     currentTargetSpeed = Mathf.Clamp(currentTargetSpeed, minSpeed, speed);

        //     // Movimiento
        //     targetObj.transform.position += currentDir * currentTargetSpeed * Time.fixedDeltaTime;

        //     // Rotación
        //     if (currentDir != Vector3.zero)
        //     {
        //         Quaternion targetRot = Quaternion.LookRotation(currentDir);
        //         targetObj.transform.rotation = targetRot;
        //     }

        //     // También podés mantener esta rotación extra si tenés lógica adicional
        //     direction = (targetPosition - targetObj.transform.position).normalized;
        //     if (direction != Vector3.zero)
        //     {
        //         targetRotation = Quaternion.LookRotation(direction);
        //         targetObj.transform.rotation = targetRotation;
        //     }

        //     // Ajustar el umbral según si es el último punto
        //     if (currentPointIndex != (routePointsOutbound.Count - 1))
        //         minDistance = minErrorToTarget;
        //     else
        //         minDistance = minErrorToFinalTarget;

        //     distanceToNextPoint = Vector3.Distance(targetObj.transform.position, targetPosition);

        //     // Avanzar al siguiente punto si estamos cerca
        //     if (distanceToNextPoint < minDistance)
        //     {
        //         if (currentPointIndex < routePointsOutbound.Count - 1)
        //         {
        //             currentPointIndex++;
        //         }
        //         else
        //         {
        //             currentTargetSpeed = 0;
        //             flagTargetReached = true;
        //         }
        //     }

        // }



        if (flagTargetReached && (flagStandby == false))
        {


            finalTargetPosition = routePointsOutbound[routePointsOutbound.Count - 1];

            // Check if the distance to the target is less than the minimum error to target
            if (Vector3.Distance(transform.position, finalTargetPosition) < minErrorToFinalTarget)
            {

                if (rb.velocity.magnitude < 0.1f)
                {

                    Debug.Log("Agent " + objectID + " has reached the final target position.");

                    StartCoroutine(WaitUntilLand(2.5f));
                    flagStandby = true; // Set the flag to true when the final target is reached
                    flagFirstTakeOff = false;

                }


            }

        }

        if (flagStandby)
        {

            finalTargetPosition = routePointsOutbound[routePointsOutbound.Count - 1];

            string numberStr = objectID;
            int number = int.Parse(numberStr);
            string padded = number.ToString("D3");

            GameObject finalDronePad = GameObject.Find("DronePadEnd_" + padded);
            finalTargetPosition.y = finalDronePad.transform.position.y;

            targetObj.transform.position = Vector3.MoveTowards(targetObj.transform.position, finalTargetPosition, 0.8f * Time.deltaTime);

            if (rb.velocity.magnitude < 0.01f)
            {

                if (flagDeliverPackage)
                {

                    Debug.Log("Agent " + objectID + " has delivered the package.");

                    // Set the deliver package flag to true for the type of the Drone1
                    if (drone1Animation != null) drone1Animation.isDeliveringPackage = true;

                    // Set the deliver package flag to true for the type of the Drone2
                    if (drone2Animation != null) drone2Animation.isDeliveringPackage = true;

                    droneJoinedToPackage.isPackageDelivered = true;

                    flagDeliverPackage = false; // Reset the flag after delivering the package
                    
                    StartCoroutine(DestroyPackage(3f));

                }


            }


        }





    }

    void FollowReturnRoute()
    {

        if (flagTargetReached == false)
        {
            
            elapsedTime += Time.fixedDeltaTime;

            float t = elapsedTime / segmentDuration;
            t = Mathf.Clamp01(t);

            targetObj.transform.position = Vector3.Lerp(startPoint, endPoint, t);

            targetObj.transform.rotation = Quaternion.Slerp(
                targetObj.transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );

            if (t >= 1f)
            {
                //transform.position = endPoint;

                targetObj.transform.position = endPoint;

                currentIndex++;

                if (currentIndex >= routePointsReturn.Count - 1)
                {
                    flagTargetReached = true;
                    currentIndex = -1;
                    elapsedTime = 0f;
                    flagTargetReachedReturn = true;
                    return;
                }

                PrepareNextSegmentReturn();
            }

        }





        // if ((currentPointIndexReturn < routePointsReturn.Count) && (flagTargetReachedReturn == false))
        // {
        //     // Set the target position to the current point index
        //     targetPosition = routePointsReturn[currentPointIndexReturn];

        //     Vector3 currentPos = targetObj.transform.position;
        //     Vector3 toTarget = targetPosition - currentPos;
        //     Vector3 currentDir = toTarget.normalized;

        //     // Calcular dirección promedio hacia adelante (lookahead)
        //     Vector3 avgDir = Vector3.zero;
        //     for (int i = 1; i <= lookaheadRange; i++)
        //     {
        //         int index = Mathf.Clamp(currentPointIndexReturn + i, 0, routePointsReturn.Count - 1);
        //         avgDir += (routePointsReturn[index] - currentPos).normalized;
        //     }
        //     avgDir.Normalize();

        //     // Calcular curvatura
        //     float angle = Vector3.Angle(currentDir, avgDir);
        //     float turnFactor = Mathf.InverseLerp(0f, maxAngle, angle);
        //     float targetSpeed = Mathf.Lerp(speed, minSpeed, turnFactor);

        //     // Chequeo anticipado de curvas en próximos puntos
        //     bool curvaFuertePorDelante = false;
        //     for (int i = 1; i < lookaheadRange - 1; i++)
        //     {
        //         int a = Mathf.Clamp(currentPointIndexReturn + i - 1, 0, routePointsReturn.Count - 1);
        //         int b = Mathf.Clamp(currentPointIndexReturn + i, 0, routePointsReturn.Count - 1);
        //         int c = Mathf.Clamp(currentPointIndexReturn + i + 1, 0, routePointsReturn.Count - 1);

        //         Vector3 dir1 = (routePointsReturn[b] - routePointsReturn[a]).normalized;
        //         Vector3 dir2 = (routePointsReturn[c] - routePointsReturn[b]).normalized;
        //         float ang = Vector3.Angle(dir1, dir2);

        //         if (ang > 20f)
        //         {
        //             curvaFuertePorDelante = true;
        //             break;
        //         }
        //     }

        //     // --- NUEVO BLOQUE: CONTROL DE DISTANCIA Y ORIENTACIÓN DEL DRON ---
        //     float droneToTargetDistance = Vector3.Distance(transform.position, targetObj.transform.position);
        //     bool droneIsTooFar = droneToTargetDistance > maxFollowDistance;

        //     float targetObjectRotationY = targetObj.transform.rotation.eulerAngles.y;
        //     float objectRotationY = transform.rotation.eulerAngles.y;
        //     float angleDiff = Mathf.DeltaAngle(targetObjectRotationY, objectRotationY);
        //     bool droneIsMisaligned = Mathf.Abs(angleDiff) > maxAngle;

        //     // Si el dron está lejos o desorientado, aplicar "modo espera"
        //     bool droneNeedsCatchup = droneIsTooFar || droneIsMisaligned;


        //     if (curvaFuertePorDelante || droneNeedsCatchup)
        //     {
        //         targetSpeed *= 0.5f; // anticipar frenado
        //     }

        //     // Desacelerar si nos acercamos al final de la trayectoria
        //     int remainingPoints = routePointsReturn.Count - currentPointIndexReturn;
        //     if (remainingPoints <= lookaheadRange)
        //     {
        //         // Distancia hasta el último punto
        //         float distanceToEnd = Vector3.Distance(currentPos, routePointsReturn[routePointsReturn.Count - 1]);
        //         float slowdownFactor = Mathf.InverseLerp(20f, 0f, distanceToEnd); // entre 10m y 0m desacelera
        //         float finalApproachSpeed = Mathf.Lerp(speed, minSpeed, slowdownFactor);

        //         targetSpeed = Mathf.Min(targetSpeed, finalApproachSpeed);
        //     }




        //     // Transición suave de velocidad con desaceleración más fuerte
        //     float deltaSpeed = targetSpeed - currentTargetSpeedReturn;
        //     float decelFactor = Mathf.Max(1f, currentTargetSpeedReturn / minSpeed); // mayor velocidad, mayor freno

        //     if (deltaSpeed > 0)
        //     {
        //         // Acelerando
        //         currentTargetSpeedReturn += acceleration * Time.fixedDeltaTime;
        //     }
        //     else
        //     {
        //         // Desacelerando (más fuerte y proporcional a la velocidad)
        //         currentTargetSpeedReturn += deceleration * decelFactor * Time.fixedDeltaTime * Mathf.Sign(deltaSpeed);
        //     }

        //     currentTargetSpeedReturn = Mathf.Clamp(currentTargetSpeedReturn, minSpeed, speed);

        //     // Movimiento
        //     targetObj.transform.position += currentDir * currentTargetSpeedReturn * Time.fixedDeltaTime;

        //     // Rotación
        //     if (currentDir != Vector3.zero)
        //     {
        //         Quaternion targetRot = Quaternion.LookRotation(currentDir);
        //         targetObj.transform.rotation = targetRot;
        //     }

        //     // También podés mantener esta rotación extra si tenés lógica adicional
        //     direction = (targetPosition - targetObj.transform.position).normalized;
        //     if (direction != Vector3.zero)
        //     {
        //         targetRotation = Quaternion.LookRotation(direction);
        //         targetObj.transform.rotation = targetRotation;
        //     }

        //     // Ajustar el umbral según si es el último punto
        //     if (currentPointIndexReturn != (routePointsReturn.Count - 1))
        //         minDistance = minErrorToTarget;
        //     else
        //         minDistance = minErrorToFinalTarget;

        //     distanceToNextPoint = Vector3.Distance(targetObj.transform.position, targetPosition);

        //     // Avanzar al siguiente punto si estamos cerca
        //     if (distanceToNextPoint < minDistance)
        //     {
        //         if (currentPointIndexReturn < routePointsReturn.Count - 1)
        //         {
        //             currentPointIndexReturn++;
        //         }
        //         else
        //         {
        //             currentTargetSpeedReturn = 0;
        //             flagTargetReachedReturn = true;
        //         }
        //     }

        // }



        if (flagTargetReachedReturn && (flagStandbyReturn == false))
        {

            finalTargetPosition = routePointsReturn[routePointsReturn.Count - 1];

            // Check if the distance to the target is less than the minimum error to target
            if (Vector3.Distance(transform.position, finalTargetPosition) < minErrorToFinalTarget)
            {

                if (rb.velocity.magnitude < 0.1f)
                {

                    Debug.Log("Agent " + objectID + " has reached the hub position.");

                    StartCoroutine(WaitUntilLandReturn(2.5f));
                    flagStandbyReturn = true; // Set the flag to true when the final target is reached
                    flagFirstTakeOff = false;
                }


            }

        }

        if (flagStandbyReturn)
        {

            finalTargetPosition = routePointsReturn[routePointsReturn.Count - 1];

            string numberStr = objectID;
            int number = int.Parse(numberStr);
            string padded = number.ToString("D3");
            
            float tolerance = 0.1f;

            // Busca todos los objetos con Tag "DronePadStart"
            GameObject[] allPads = GameObject.FindGameObjectsWithTag("DronePadStart");
            found = null;

            foreach (GameObject obj in allPads)
            {
                // Filtrar por nombre que empiece con DronePadStartA_, B_ o C_
                if (obj.name.StartsWith("DronePadStartA_") ||
                    obj.name.StartsWith("DronePadStartB_") ||
                    obj.name.StartsWith("DronePadStartC_"))
                {
                    Vector3 pos = obj.transform.position;

                    if (Mathf.Abs(pos.x - finalTargetPosition.x) <= tolerance &&
                        Mathf.Abs(pos.z - finalTargetPosition.z) <= tolerance)
                    {
                        found = obj;
                        break; // encontramos el primero que coincide
                    }
                }
            }

            //GameObject finalDronePad = GameObject.Find("DronePadStart_" + padded);
            finalTargetPosition.y = found.transform.position.y;

            //Debug.Log("Agent " + objectID + " landing at: " + finalTargetPosition.y);

            targetObj.transform.position = Vector3.MoveTowards(targetObj.transform.position, finalTargetPosition, 0.8f * Time.deltaTime);

            if (targetObj.transform.position.y == finalTargetPosition.y)
            {

                if (flagFullPathFinished)
                {

                    Debug.Log("Agent " + objectID + " has returned to the hub");

                    flagFullPathFinished = false; // Reset the flag after returning to the hub

                }

            }

        }

    }





    void RemoveConsecutiveDuplicates()
    {
        if (routePointsOutbound.Count <= 1) return;

        List<Vector3> cleanedRoute = new List<Vector3>();
        cleanedRoute.Add(routePointsOutbound[0]);

        for (int i = 1; i < routePointsOutbound.Count; i++)
        {
            if (routePointsOutbound[i] != routePointsOutbound[i - 1])
            {
                cleanedRoute.Add(routePointsOutbound[i]);
            }
        }

        routePointsOutbound = cleanedRoute;
    }
    
    void RemoveConsecutiveDuplicatesReturn()
    {
        if (routePointsReturn.Count <= 1) return;

        List<Vector3> cleanedRoute = new List<Vector3>();
        cleanedRoute.Add(routePointsReturn[0]);

        for (int i = 1; i < routePointsReturn.Count; i++)
        {
            if (routePointsReturn[i] != routePointsReturn[i - 1])
            {
                cleanedRoute.Add(routePointsReturn[i]);
            }
        }

        routePointsReturn = cleanedRoute;
    }


    // -----------------------------------------------------------------------------------------------------
    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider:

    void OnCollisionStay(Collision collision)
    {
        // Comprobamos que el objeto NO sea ninguno de los tres tags
        if (!collision.gameObject.CompareTag("DronePadStart") &&
            !collision.gameObject.CompareTag("DronePadCustomer") &&
            !collision.gameObject.CompareTag("Package") &&
            !collision.gameObject.CompareTag("Ground"))
        {
            isColliding = true;

            // if (flagFirstTakeOff)
            // {
            //     Debug.Log("Agent " + objectID + " is colliding with object: " + collision.gameObject.name);
            // }
        }
    }

    // -----------------------------------------------------------------------------------------------------
    // OnCollisionExit is called when this collider/rigidbody has stopped touching another rigid

    void OnCollisionExit(Collision collision)
    {
        // Set the collision flag to false
        isColliding = false;
    }

}
