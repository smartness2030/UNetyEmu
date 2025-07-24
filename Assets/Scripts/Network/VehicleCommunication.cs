using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleCommunication : MonoBehaviour
{

    // Get the BaseStationMininetWifi script to get the flag from MininetWifi
    public LogisticCenterWithVehicles logisticCenterWithVehicles;

    // Variable to store the base station game object name
    public string logisticCenterGameObjectName = "LogisticCenter";

    public bool flagMessageReceived = false; // Flag to indicate if a message has been received

    public bool flagOnlineCommunication = true; // Flag to indicate if the vehicle is online or offline. Variable controlled by Unity/MininetWiFi

    public List<string> setMessageData = new List<string>(); // Lista de objetivos

    public List<Vector3> setRoutePoints = new List<Vector3>(); 
    public List<float> setRouteOrientations = new List<float>(); 
    
    private Vector3 checkPoint1;
    private Vector3 checkPoint2;
    private Vector3 checkPoint3;
    private Vector3 checkPoint4;
    private Vector3 checkPoint5;
    private Vector3 checkPoint6;
    private Vector3 checkPoint7;
    private Vector3 checkPoint8;
    private Vector3 checkPoint9;

    private float checkOrientation1;
    private float checkOrientation2;
    private float checkOrientation3;
    private float checkOrientation4;
    private float checkOrientation5;
    private float checkOrientation6;
    private float checkOrientation7;
    private float checkOrientation8;
    private float checkOrientation9;

    // Variable to store the distance and azimuth between two points
    private Vector3 distanceAzimuth;

    // Variable to store the angle in radians
    private float angleInRadians;

    private bool flatSetMission = false;

    private float droneMaxAltitude = 100f;

    private string typeOfVehicle = "DRO";

    private DronePathPlanning dronePathPlanning;
    private CarPathPlanning carPathPlanning;

    public bool messageConnection;

    public Vector3 carVehiclePosition;
    public float carVehicleOrientation;

    // Variable to store the drone's wifi signal
    public float vehicleWifiSignal; 

    public string baseStationGameObjectName = "BaseStation";

    private BaseStationMininetWifi baseStationMininetWifi;
    private float timeElapsed = 0f;

    // Start is called before the first frame update
    void Start()
    {

        vehicleWifiSignal = 1f;

        // Get the BaseStationMininetWifi script
        logisticCenterWithVehicles = GameObject.Find(logisticCenterGameObjectName).GetComponent<LogisticCenterWithVehicles>();

        if (gameObject.name.StartsWith("DRO"))
        {
            GetDroneFeatures getDroneFeatures = GetComponent<GetDroneFeatures>();
            droneMaxAltitude = getDroneFeatures.maxAltitude;
            typeOfVehicle = "DRO";

            dronePathPlanning = GetComponent<DronePathPlanning>();
        }

        if (gameObject.name.StartsWith("CAR"))
        {
            GetCarFeatures getCarFeatures = GetComponent<GetCarFeatures>();
            typeOfVehicle = "CAR";

            carPathPlanning = GetComponent<CarPathPlanning>();
        }

        messageConnection = true;
        
        GameObject baseStation = GameObject.Find(baseStationGameObjectName);
        baseStationMininetWifi = baseStation.GetComponent<BaseStationMininetWifi>();
        
    }


    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= 1f)
        {
            timeElapsed = 0f;

            Vector3 dronePosition = transform.position;
            Debug.Log("Posición del dron: " + dronePosition);

            if (baseStationMininetWifi != null)
            {

                if (baseStationMininetWifi.radius > 1)
                {

                    Vector3 basePosition = baseStationMininetWifi.transform.position;

                    // Distancia en el plano XZ
                    Vector2 droneXZ = new Vector2(dronePosition.x, dronePosition.z);
                    Vector2 baseXZ = new Vector2(basePosition.x, basePosition.z);
                    float distance = Vector2.Distance(droneXZ, baseXZ);

                    // Obtener radio de cobertura desde la estación base
                    float coverageRadius = baseStationMininetWifi.radius;

                    // Valor entre 0 y 1
                    vehicleWifiSignal = Mathf.Clamp01(1f - (distance / coverageRadius));

                }
                
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {




        // Check if the flag from MininetWifi is true
        if (logisticCenterWithVehicles.flagConnectionEstablished && (!flagMessageReceived))
        {

            Debug.Log("[Unity Vehicle] VehicleCommunication: Connection established with " + gameObject.name);
            flagMessageReceived = true; // Set the flag to true to indicate that the connection has been established

        }

        if (flagMessageReceived)
        {

            if ((setMessageData.Count == 4) && !flatSetMission)
            {

                (Vector3 hubPos, float hubRot) = ParsePositionAndRotation(setMessageData[0]);
                (Vector3 packagePos, float packageRot) = ParsePositionAndRotation(setMessageData[1]);
                (Vector3 customerPos, float customerRot) = ParsePositionAndRotation(setMessageData[2]);
                (Vector3 carPos, float carRot) = ParsePositionAndRotation(setMessageData[3]);

                carVehiclePosition = carPos;
                carVehicleOrientation = carRot;

                createCheckPoints(hubPos, hubRot, packagePos, packageRot, customerPos, customerRot);

                setRouteToVehicle();

                flatSetMission = true;

            }

        }

        if (!flagOnlineCommunication && messageConnection)
        {
            Debug.Log($"[Vehicle {gameObject.name}] Lost connection with the Base Station. Vehicle is offline.");
            messageConnection = false;
        }

    }

    void createCheckPoints(Vector3 hubPos, float hubRot, Vector3 packagePos, float packageRot, Vector3 customerPos, float customerRot)
    {

        if (typeOfVehicle == "DRO")
        {

            checkPoint1 = new Vector3(hubPos.x, droneMaxAltitude, hubPos.z);
            checkPoint2 = new Vector3(packagePos.x, droneMaxAltitude, packagePos.z);
            checkPoint3 = new Vector3(packagePos.x, packagePos.y, packagePos.z);
            checkPoint4 = new Vector3(packagePos.x, droneMaxAltitude, packagePos.z);
            checkPoint5 = new Vector3(customerPos.x, droneMaxAltitude, customerPos.z);
            checkPoint6 = new Vector3(customerPos.x, customerPos.y, customerPos.z);
            checkPoint7 = new Vector3(customerPos.x, droneMaxAltitude, customerPos.z);
            checkPoint8 = new Vector3(hubPos.x, droneMaxAltitude, hubPos.z);
            checkPoint9 = new Vector3(hubPos.x, hubPos.y, hubPos.z);

            setRoutePoints.Add(checkPoint1);
            setRoutePoints.Add(checkPoint2);
            setRoutePoints.Add(checkPoint3);
            setRoutePoints.Add(checkPoint4);
            setRoutePoints.Add(checkPoint5);
            setRoutePoints.Add(checkPoint6);
            setRoutePoints.Add(checkPoint7);
            setRoutePoints.Add(checkPoint8);
            setRoutePoints.Add(checkPoint9);

            checkOrientation1 = GetAzimuth(checkPoint2, checkPoint1);
            checkOrientation2 = checkOrientation1;
            checkOrientation3 = packageRot;
            checkOrientation4 = GetAzimuth(checkPoint5, checkPoint4);
            checkOrientation5 = checkOrientation4;
            checkOrientation6 = customerRot;
            checkOrientation7 = GetAzimuth(checkPoint8, checkPoint7);
            checkOrientation8 = checkOrientation7;
            checkOrientation9 = hubRot;

            setRouteOrientations.Add(checkOrientation1);
            setRouteOrientations.Add(checkOrientation2);
            setRouteOrientations.Add(checkOrientation3);
            setRouteOrientations.Add(checkOrientation4);
            setRouteOrientations.Add(checkOrientation5);
            setRouteOrientations.Add(checkOrientation6);
            setRouteOrientations.Add(checkOrientation7);
            setRouteOrientations.Add(checkOrientation8);
            setRouteOrientations.Add(checkOrientation9);

        }

        if (typeOfVehicle == "CAR")
        {

            // Specific route to reach customer 1 in 473.55, 0.2, 384.8
            checkPoint1 = new Vector3(291.73f, 0f, 245.42f);
            checkPoint2 = new Vector3(444.1f, 0f, 417.3f);
            checkPoint3 = new Vector3(478.24f, 0f, 391.28f);

            setRoutePoints.Add(checkPoint1);
            setRoutePoints.Add(checkPoint2);
            setRoutePoints.Add(checkPoint3);

        }

    }


    void setRouteToVehicle()
    {

        if (gameObject.name.StartsWith("DRO"))
        {
            dronePathPlanning.routePoints = setRoutePoints;
            dronePathPlanning.routeOrientations = setRouteOrientations;
        }

        if (gameObject.name.StartsWith("CAR"))
        {
            carPathPlanning.routePoints = setRoutePoints;
            carPathPlanning.routeOrientations = setRouteOrientations;
        }

    }


    (Vector3, float) ParsePositionAndRotation(string data)
    {

        string[] parts = data.Split(':');
        if (parts.Length != 2)
        {
            return (Vector3.zero, 0f);
        }

        string[] values = parts[1].Split(';');
        if (values.Length != 4)
        {
            return (Vector3.zero, 0f);
        }

        float x = float.Parse(values[0]);
        float y = float.Parse(values[1]);
        float z = float.Parse(values[2]);
        float rotationY = float.Parse(values[3]);

        return (new Vector3(x, y, z), rotationY);
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
