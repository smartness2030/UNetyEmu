using UnityEngine;
using System.Collections.Generic;

public class CarPathPlanning : MonoBehaviour
{
        
    [Header("Movement Parameters")]
    public float maxSpeed = 40.0f;
    public float acceleration = 2.0f;
    public float distanceBetweenPoints = 1.0f;

    [Header("Deceleration Control")]
    [Tooltip("Factor > 1.0 starts deceleration earlier")]
    public float anticipationFactor = 1.5f;

    [Header("Movement State (Debug)")]
    public float currentSpeed = 0f;

    public List<Vector3> routePoints = new List<Vector3>(); // Lista de objetivos
    public List<float> routeOrientations = new List<float>(); // Lista de orientaciones

    public List<Vector3> points = new List<Vector3>(); // Puntos intermedios actuales
    public int currentIndex = 0;
    private int destinationIndex = 0;

    private Vector3 endPosition;

    public bool flagStartPath;

    public GameObject targetObj;
    private string objectID;

    private DroneLandingPad landingPadScript;









    void Start()
    {

        flagStartPath = false;

        GetObjectFeatures getObjectFeatures = GetComponent<GetObjectFeatures>();
        objectID = getObjectFeatures.objectID;
        targetObj = GameObject.Find("ID" + objectID + "Target");

        Transform padTransform = transform.Find("prefabPadTruck_003");
        landingPadScript = padTransform.GetComponent<DroneLandingPad>();
        
    }

    void FixedUpdate()
    {

        if (landingPadScript != null)
        {

            if (landingPadScript.isDroneLanded)
            {

                if (routePoints.Count == 0)
                {
                    return;
                }
                else
                {
                    
                    if(!flagStartPath)
                    {

                        // Iniciar movimiento hacia el primer destino
                        destinationIndex = 0;
                        SetNextDestination();

                        flagStartPath = true;

                    }
                    
                }
                
                if(flagStartPath)
                {

                    if (currentIndex >= points.Count)
                    {
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

                    if (targetObj.transform.position == destination)
                    {
                        currentIndex++;
                    }

                }

            }
            
        }
        
    }

    private void SetNextDestination()
    {
        endPosition = routePoints[destinationIndex];
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

}
