/*******************************************************************************
* Copyright 2025 INTRIG
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
******************************************************************************/

// Libraries
using System; // Library to use Serializable classes
using System.Collections.Generic; // Library to use List<T>
using UnityEngine; // Library to use MonoBehaviour classes
using System.IO;
using System.Collections;

// Class to create all objects/components/features according to the data in the JSON file:
public class ObjectSetupPathPlanningScript : MonoBehaviour
{
    
    // -----------------------------------------------------------------------------------------------------
    // Classes added to extract the data from JSON file:

    // Internal variable according to the data structure in the JSON file
    [System.Serializable] public class ObjectData
    {
        public string prefabName;
        public string group;
        public string type;
        public PlayerFeatures playerFeatures;
        public bool addTargetGameObject;
        public List<string> dynamicScripts;
        public List<string> algorithmScripts;
        public List<string> otherInternalScripts;
        public LidarFeatures lidarFeatures;
        public DepthCameraFeatures depthCameraFeatures;
        public CommunicationFeatures communicationFeatures;

    }

    // Internal variable of the structure called "playerFeatures"
    [System.Serializable] public class PlayerFeatures
    {
        public float unladenWeight;
        public float approxMaxFlightTime;
        public float maxBatteryCapacity;
        public float batteryVoltage;
        public float batteryStartPercentage;
        public float maxAltitude;
        public float maxThrust;
        public float maxSpeedManufacturer;
        public float maximumTiltAngle;
        public float propellerMaxRPM;
    }

    // Internal variable of the structure called "lidarFeatures"
    [System.Serializable] public class LidarFeatures
    {
        public string scriptName;
        public float lidarRange;
        public int numRaysHorizontal;
        public int numRaysVertical;
        public float verticalFOV;
        public float pointsPerSecond;
    }

    // Internal variable of the structure called "depthCameraFeatures"
    [System.Serializable] public class DepthCameraFeatures
    {
        public string scriptName;
        public float nearClipPlane;
        public float farClipPlane;
        public float fieldOfView;
        public int pixelWidth;
        public int pixelHeight;
    }

    // Internal variable of the structure called "communicationFeatures"
    [System.Serializable] public class CommunicationFeatures
    {
        public string scriptName;
    }

    // Internal variable to store all the data structure coming from the JSON file
    [System.Serializable] public class GetSetupPlayers
    {
        public ObjectData[] players;
    }
    
    public static List<string> dronesNameInstantiated = new List<string>{};

    public Material trailMaterial;

    public float maxBuildingsHeightWithMargin;
    
    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:
    
    // Variable to store the name of the JSON file
    [Header("JSON Setup File Name")]
    public string jsonFile;

    // List of objects to add colliders
    [Header("Objects to add Box or Mesh Collider")]
    public List<string> parentObjectNameToAddCollider;
    public List<bool> TrueForBoxColliderFalseForMeshCollider;

    // List of objects to instantiate players
    [Header("Object General Features")]
    public List<string> playerGroupNames;
    public List<string> parentObjectNameToInstantiatePlayersPerGroup;

    [Header("DronePad Customers - Objects Features")]
    public string customerGroup = "Customers"; // Name of the parent object for end positions
    public string prefabNameDronePadEnd = "prefabDronePad_red"; // Name of the prefab for drone pads at the end

    [Header("DronePad Group A - Objects Features")]
    public string playerStartGroupA = "PlayerStartGroupA"; // Name of the parent object for start positions
    public string prefabNameDronePadStartA = "prefabDronePad_green"; // Name of the prefab for drone pads
    
    [Header("DronePad Group B - Objects Features")]
    public string playerStartGroupB = "PlayerStartGroupB"; // Name of the parent object for start positions
    public string prefabNameDronePadStartB = "prefabDronePad_blue"; // Name of the prefab for drone pads

    [Header("DronePad Group C - Objects Features")]
    public string playerStartGroupC = "PlayerStartGroupC"; // Name of the parent object for start positions
    public string prefabNameDronePadStartC = "prefabDronePad_orange"; // Name of the prefab for drone pads

    public static bool allInstantiatedObjectsReady;

    // -----------------------------------------------------------------------------------------------------
    // Private variables of the ObjectSetupScript class:

    private GetSetupPlayers getSetupPlayers;  // Variable to store all the data

    private int currentID = 0; // Variable to store the ID of the player

    private GameObject prefabDronePadStartA;
    private GameObject prefabDronePadStartB;
    private GameObject prefabDronePadStartC;

    private GameObject prefabDronePadEnd;

    private GameObject prefabPackage;

    // Game object to store the instantiated package
    private GameObject instantiatedPackage;

    // Variable to get the package's rigidbody component
    private GameObject packages;

    private List<float> maxHeightsWithMarginList = new List<float>();

    private GameObject prefab;

    private GameObject playerStart;

    private GameObject playerStartChildObject;

    private ObjectData objNew;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        string path = Path.Combine(Application.streamingAssetsPath, jsonFile + ".json");

        if (File.Exists(path))
        {

            string json = File.ReadAllText(path);
            getSetupPlayers = JsonUtility.FromJson<GetSetupPlayers>(json);

            if (parentObjectNameToAddCollider.Count != TrueForBoxColliderFalseForMeshCollider.Count)
            {
                Debug.LogWarning("The number of elements in the lists ´parentObjectNameToAddCollider´, and ´TrueForBoxColliderFalseForMeshCollider´ must be the same to add the colliders");
            }
            else
            {
                AddColliders(); // Add colliders to the objects
            }

        }
        else
        {
            Debug.LogWarning("JSON file ´" + jsonFile + "´ not found. No objects can be instantiated without a JSON file.");
        }

        packages = GameObject.Find("Packages");

        allInstantiatedObjectsReady = false;

        // Start the sequence when the game begins (or when you want)
        StartCoroutine(InstantiateSequence());

    }

    IEnumerator InstantiateSequence()
    {

        // Wait 1 second, then call first
        yield return new WaitForSeconds(1f);
        InstantiatePrefabs();

        // Wait 1 second, then call second
        yield return new WaitForSeconds(1f);
        InstantiatePrefabsCustomers();

        // Wait 1 second, then call third
        yield return new WaitForSeconds(1f);
        InstantiatePrefabsPackages();

        // Wait 1 second, then call last
        yield return new WaitForSeconds(1f);
        InstantiateObjects();

        yield return new WaitForSeconds(1f);
        allInstantiatedObjectsReady = true;

    }


    void InstantiatePrefabs()
    {
        
        // Encontrar el objeto en la jerarquía dentro de PlayerStartGroup
        Transform parentObjectStartA = GameObject.Find(playerStartGroupA)?.transform;
        Transform parentObjectStartB = GameObject.Find(playerStartGroupB)?.transform;
        Transform parentObjectStartC = GameObject.Find(playerStartGroupC)?.transform;

        if (parentObjectStartA != null && parentObjectStartB != null && parentObjectStartC != null)
        {

            prefabDronePadStartA = parentObjectStartA.Find(prefabNameDronePadStartA)?.gameObject;
            prefabDronePadStartA.name = "DronePadStartA_000";

            prefabDronePadStartB = parentObjectStartB.Find(prefabNameDronePadStartB)?.gameObject;
            prefabDronePadStartB.name = "DronePadStartB_000";

            prefabDronePadStartC = parentObjectStartC.Find(prefabNameDronePadStartC)?.gameObject;
            prefabDronePadStartC.name = "DronePadStartC_000";

            if (prefabDronePadStartA != null && prefabDronePadStartB != null && prefabDronePadStartC != null)
            {

                int countAgents = 0;
                int countAgentsA = 0;
                int countAgentsB = 0;
                int countAgentsC = 0;

                int countAllAgents = LoadPath.agentsScenarioData.Count;
                Debug.Log("Total number of agents in the scenario: " + countAllAgents);

                int[] resultSplitIntoGroups = SplitIntoGroups(countAllAgents, 3);

                // Accessing agent data after parsing
                foreach (var agent in LoadPath.agentsScenarioData)
                {

                    if (countAgents >= LoadPath.numberOfAgentsInPath)
                    {
                        break;
                    }

                    Vector3 startPosition = agent.Value.StartPosition;

                    // Raycast from above to adjust Y position
                    Vector3 rayOrigin = new Vector3(startPosition.x, maxBuildingsHeightWithMargin, startPosition.z);
                    Ray ray = new Ray(rayOrigin, Vector3.down);

                    RaycastHit[] hits = Physics.RaycastAll(ray, maxBuildingsHeightWithMargin + 5);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    foreach (RaycastHit hit in hits)
                    {
                        if (hit.collider.CompareTag("Buildings"))
                        {
                            startPosition.y = maxBuildingsHeightWithMargin - hit.distance + 0.01f;
                            break;
                        }
                    }

                    // Determine which group (0 = A, 1 = B, 2 = C)
                    int groupIndex = countAgents % 3;

                    if (groupIndex == 0)
                    {

                        if (countAgents == 0)
                        {
                            // First agent in group A → move prefab
                            prefabDronePadStartA.transform.position = startPosition;
                        }
                        else
                        {
                            // Others → instantiate copies
                            GameObject newDronePad = Instantiate(prefabDronePadStartA, startPosition, Quaternion.identity, parentObjectStartA);
                            newDronePad.name = "DronePadStartA_" + countAgentsA.ToString("D3");
                        }

                        countAgentsA++;

                    }
                    else if (groupIndex == 1)
                    {

                        if (countAgents == 1)
                        {
                            prefabDronePadStartB.transform.position = startPosition;
                        }
                        else
                        {
                            GameObject newDronePad = Instantiate(prefabDronePadStartB, startPosition, Quaternion.identity, parentObjectStartB);
                            newDronePad.name = "DronePadStartB_" + countAgentsB.ToString("D3");
                        }

                        countAgentsB++;

                    }
                    else if (groupIndex == 2)
                    {

                        if (countAgents == 2)
                        {
                            prefabDronePadStartC.transform.position = startPosition;
                        }
                        else
                        {
                            GameObject newDronePad = Instantiate(prefabDronePadStartC, startPosition, Quaternion.identity, parentObjectStartC);
                            newDronePad.name = "DronePadStartC_" + countAgentsC.ToString("D3");
                        }

                        countAgentsC++;

                    }

                    countAgents++;

                }
                
            }

        }

    }

    public static int[] SplitIntoGroups(int number, int groupCount)
    {
        int baseValue = number / groupCount;   // Integer division
        int remainder = number % groupCount;   // Leftover after division

        int[] groups = new int[groupCount];

        for (int i = 0; i < groupCount; i++)
        {
            groups[i] = baseValue;

            if (i < remainder)
            {
                groups[i]++; // Distribute the remainder
            }
        }

        return groups;
    }


    void InstantiatePrefabsCustomers()
    {
        
        Transform parentObjectEnd = GameObject.Find(customerGroup)?.transform;

        if (parentObjectEnd != null)
        {
            prefabDronePadEnd = parentObjectEnd.Find(prefabNameDronePadEnd)?.gameObject;
            prefabDronePadEnd.name = "DronePadEnd_000";

            if (prefabDronePadEnd != null)
            {

                int countAgentsCustomers = 0;
                
                // Accessing agentCustomer data after parsing
                foreach (var agentCustomer in LoadPath.agentsScenarioData)
                {

                    if (countAgentsCustomers >= LoadPath.numberOfAgentsInPath)
                    {
                        break;
                    }


                    if (agentCustomer.Key == 0)
                    {

                        Vector3 EndPosition = agentCustomer.Value.EndPosition;

                        Vector3 rayOrigin = new Vector3(EndPosition.x, maxBuildingsHeightWithMargin, EndPosition.z);
                        Ray ray = new Ray(rayOrigin, Vector3.down);

                        // Lanza el raycast que detecta TODOS los objetos en su camino
                        RaycastHit[] hits = Physics.RaycastAll(ray, maxBuildingsHeightWithMargin + 5); // Usa un rango suficiente

                        // Ordenar los impactos por distancia (de más cercano a más lejano)
                        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                        foreach (RaycastHit hit in hits)
                        {
                            if (hit.collider.CompareTag("Buildings"))
                            {
                                //Debug.Log($"Hit building: {hit.collider.gameObject.name} at distance {hit.distance:F2}");

                                EndPosition.y = maxBuildingsHeightWithMargin - hit.distance + 0.1f;

                                break;
                            }
                        }

                        prefabDronePadEnd.transform.position = EndPosition;

                    }
                    else
                    {

                        Vector3 EndPosition = agentCustomer.Value.EndPosition;

                        Vector3 rayOrigin = new Vector3(EndPosition.x, maxBuildingsHeightWithMargin, EndPosition.z);
                        Ray ray = new Ray(rayOrigin, Vector3.down);

                        // Lanza el raycast que detecta TODOS los objetos en su camino
                        RaycastHit[] hits = Physics.RaycastAll(ray, maxBuildingsHeightWithMargin + 5); // Usa un rango suficiente

                        // Ordenar los impactos por distancia (de más cercano a más lejano)
                        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                        foreach (RaycastHit hit in hits)
                        {
                            if (hit.collider.CompareTag("Buildings"))
                            {
                                //Debug.Log($"Hit building: {hit.collider.gameObject.name} at distance {hit.distance:F2}");

                                EndPosition.y = maxBuildingsHeightWithMargin - hit.distance + 0.01f;

                                break;
                            }
                        }

                        GameObject newDronePad = Instantiate(prefabDronePadEnd, EndPosition, Quaternion.identity, parentObjectEnd);
                        newDronePad.name = "DronePadEnd_" + countAgentsCustomers.ToString("D3");

                    }

                    countAgentsCustomers++;

                    // Debug.Log($"Agent {agentCustomer.Key}: Start Position: {agentCustomer.Value.StartPosition}, End Position: {agentCustomer.Value.EndPosition}, Start Time: {agentCustomer.Value.StartTime}, Safety Radius: {agentCustomer.Value.SafetyRadius}, Speed: {agentCustomer.Value.Speed}");

                }

            }
        }

    }

    void InstantiatePrefabsPackages()
    {
        
        Transform parentObjectPackages = GameObject.Find("Packages")?.transform;
        
        if (parentObjectPackages != null)
        {
            
            int countAgentsPackages = 0;

            // Accessing agentPackage data after parsing
            foreach (var agentPackage in LoadPath.agentsScenarioData)
            {

                if (countAgentsPackages >= LoadPath.numberOfAgentsInPath)
                {
                    break;
                }

                // Choose package prefab depending on group (A=0, B=1, C=2)
                int groupIndex = countAgentsPackages % 3;
                string prefabName = "";

                if (groupIndex == 0)
                    prefabName = "prefabPackage1"; // Group A
                else if (groupIndex == 1)
                    prefabName = "prefabPackage2"; // Group B
                else if (groupIndex == 2)
                    prefabName = "prefabPackage2"; // Group C

                prefabPackage = Resources.Load<GameObject>("Prefabs/" + prefabName);

                Vector3 positionPackage = agentPackage.Value.StartPosition;

                Vector3 rayOrigin = new Vector3(positionPackage.x, maxBuildingsHeightWithMargin, positionPackage.z);
                Ray ray = new Ray(rayOrigin, Vector3.down);

                RaycastHit[] hits = Physics.RaycastAll(ray, maxBuildingsHeightWithMargin + 5);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.CompareTag("Buildings"))
                    {
                        positionPackage.y = maxBuildingsHeightWithMargin - hit.distance;
                        break;
                    }
                }

                // Instantiate the package game object
                instantiatedPackage = Instantiate(prefabPackage, positionPackage, Quaternion.identity, parentObjectPackages);

                // Set the package game object properties
                instantiatedPackage.transform.SetParent(packages.transform);
                instantiatedPackage.transform.name = "Package_" + countAgentsPackages.ToString("D3");
                instantiatedPackage.transform.tag = "Package";

                countAgentsPackages++;

                // Debug.Log($"Agent {agentPackage.Key}: Start Position: {agentPackage.Value.StartPosition}, End Position: {agentPackage.Value.EndPosition}, Start Time: {agentPackage.Value.StartTime}, Safety Radius: {agentPackage.Value.SafetyRadius}, Speed: {agentPackage.Value.Speed}");
                
            }

        }

    }


    // -----------------------------------------------------------------------------------------------------
    // Method to add colliders to the objects:

    void AddColliders()
    {

        // For each object in the list
        for (int i = 0; i < parentObjectNameToAddCollider.Count; i++)
        {
            
            // Find the object in the scene
            GameObject objectToAddCollider = GameObject.Find(parentObjectNameToAddCollider[i]);

            if(objectToAddCollider != null)
            {

                // For each child object of the object
                for (int j = 0; j < objectToAddCollider.transform.childCount; j++)
                {

                    // Find the child object
                    GameObject objectChildToAddCollider = objectToAddCollider.transform.GetChild(j).gameObject;

                    objectChildToAddCollider.tag = "Buildings";
                    
                    //StartCoroutine(AddCollidersAndRemoveBox(objectChildToAddCollider));
                   

                    // // Add the collider according to the boolean value
                    // if (TrueForBoxColliderFalseForMeshCollider[i] == true)
                    // {
                    //     if (objectChildToAddCollider.GetComponent<BoxCollider>() == null)
                    //     {
                    //         objectChildToAddCollider.AddComponent<BoxCollider>();
                    //     }
                    // }
                    // else if (TrueForBoxColliderFalseForMeshCollider[i] == false)
                    // {
                    //     if (objectChildToAddCollider.GetComponent<MeshCollider>() == null)
                    //     {
                    //         objectChildToAddCollider.AddComponent<MeshCollider>();
                    //     }
                    // }

                }

            }

            float maxHeight = 0f;

            Transform linkoping = objectToAddCollider.transform;

            foreach (Transform building in linkoping)
            {
                MeshCollider collider = building.GetComponent<MeshCollider>();
                if (collider != null)
                {
                    float height = collider.bounds.size.y;

                    if (height > maxHeight)
                    {
                        maxHeight = height;
                    }
                }
            }

            // Add 5 meters margin to the tallest building
            maxBuildingsHeightWithMargin = maxHeight + 5f;

            //Debug.Log("Max height of the building: " + maxHeight.ToString("F2") + " meters");

            maxBuildingsHeightWithMargin = (float)Math.Round(maxBuildingsHeightWithMargin, 2);

            maxHeightsWithMarginList.Add(maxBuildingsHeightWithMargin);
            
        }

        maxBuildingsHeightWithMargin = Mathf.Max(maxHeightsWithMarginList.ToArray());

    }


    IEnumerator AddCollidersAndRemoveBox(GameObject obj)
    {
        // Añadir el BoxCollider temporal
        BoxCollider box = obj.AddComponent<BoxCollider>();

        // Esperar 2 segundos
        yield return new WaitForSeconds(2f);

        // Destruir el BoxCollider
        if (box != null)
            Destroy(box);

        // Esperar un frame para que la física se estabilice
        yield return null;

        // Forzar MeshCollider con actualización
        MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = obj.AddComponent<MeshCollider>();

        meshCollider.convex = true; // o true si es necesario

        // (opcional) forzar recalcular si el mesh viene dinámico
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf != null && mf.mesh != null)
        {
            meshCollider.sharedMesh = null; // limpiar primero
            meshCollider.sharedMesh = mf.mesh; // volver a asignar
        }
    }



    // -----------------------------------------------------------------------------------------------------
    // Method for instantiating players in the scene:

    void InstantiateObjects()
    {

        int countAgentObjects = 0;       
        
        int countAgentObjectsA = 0;
        int countAgentObjectsB = 0;
        int countAgentObjectsC = 0;

        // Accessing agentPackage data after parsing
        foreach (var agentObjects in LoadPath.agentsScenarioData)
        {

            if (countAgentObjects >= LoadPath.numberOfAgentsInPath)
            {
                break;
            }

            // Choose package prefab depending on group (A=0, B=1, C=2)
            int groupIndex = countAgentObjects % 3;
            string prefabName = "";
            string prefabGroup = "";

            if (groupIndex == 0)
            {
                prefabName = getSetupPlayers.players[0].prefabName; // Group A
                prefabGroup = parentObjectNameToInstantiatePlayersPerGroup[0];

                // Find the child object
                playerStartChildObject = GameObject.Find(prefabGroup).transform.GetChild(countAgentObjectsA).gameObject;

                objNew = getSetupPlayers.players[0];

                countAgentObjectsA++;
            }
            else if (groupIndex == 1)
            {
                prefabName = getSetupPlayers.players[1].prefabName; // Group B
                prefabGroup = parentObjectNameToInstantiatePlayersPerGroup[1];

                // Find the child object
                playerStartChildObject = GameObject.Find(prefabGroup).transform.GetChild(countAgentObjectsB).gameObject;

                objNew = getSetupPlayers.players[1];

                countAgentObjectsB++;
            }
            else if (groupIndex == 2)
            {
                prefabName = getSetupPlayers.players[2].prefabName; // Group C
                prefabGroup = parentObjectNameToInstantiatePlayersPerGroup[2];

                // Find the child object
                playerStartChildObject = GameObject.Find(prefabGroup).transform.GetChild(countAgentObjectsC).gameObject;

                objNew = getSetupPlayers.players[2];

                countAgentObjectsC++;
            }

            prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);          

            // Create a vector for the initial position
            Vector3 position = new Vector3(
                playerStartChildObject.transform.position.x,
                playerStartChildObject.transform.position.y,
                playerStartChildObject.transform.position.z
            );

            // Create a vector for initial orientation
            Quaternion rotation = Quaternion.Euler(
                playerStartChildObject.transform.eulerAngles.x,
                playerStartChildObject.transform.eulerAngles.y,
                playerStartChildObject.transform.eulerAngles.z
            );





            Vector3 rayOrigin = new Vector3(position.x, maxBuildingsHeightWithMargin, position.z);
            Ray ray = new Ray(rayOrigin, Vector3.down);

            // Lanza el raycast que detecta TODOS los objetos en su camino
            RaycastHit[] hits = Physics.RaycastAll(ray, maxBuildingsHeightWithMargin + 5); // Usa un rango suficiente

            // Ordenar los impactos por distancia (de más cercano a más lejano)
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag("Buildings"))
                {
                    //Debug.Log($"Hit building: {hit.collider.gameObject.name} at distance {hit.distance:F2}");

                    position.y = maxBuildingsHeightWithMargin - hit.distance;

                    break;
                }
            }

            playerStartChildObject.transform.position = position;


            // Instantiate the player in the specified position and orientation
            GameObject instantiatedObject = Instantiate(prefab, position, rotation);




            // // Obtener el prefab de la esfera de seguridad
            // GameObject safetyCirclePrefab = Resources.Load<GameObject>("Prefabs/prefabSafetyCircle"); // Asume que lo guardaste allí

            // // Instanciar la esfera
            // GameObject safetyCircle = Instantiate(safetyCirclePrefab);
            // safetyCircle.name = "SafetyCircle";

            // // Hacerla hija del dron
            // safetyCircle.transform.SetParent(instantiatedObject.transform);

            // // Centrarla en el dron
            // safetyCircle.transform.localPosition = Vector3.zero;




            // Obtener el prefab de la esfera de seguridad
            GameObject safetySpherePrefab = Resources.Load<GameObject>("Prefabs/prefabSafetySphere"); // Asume que lo guardaste allí

            // Instanciar la esfera
            GameObject safetySphere = Instantiate(safetySpherePrefab);
            safetySphere.name = "SafetySphere";

            // Hacerla hija del dron
            safetySphere.transform.SetParent(instantiatedObject.transform);

            // Centrarla en el dron
            safetySphere.transform.localPosition = Vector3.zero;

            // Desactivarla al inicio
            safetySphere.transform.gameObject.SetActive(false);







            // 1. Agregar LineRenderer
            LineRenderer line = instantiatedObject.AddComponent<LineRenderer>();
            line.material = trailMaterial;
            line.widthMultiplier = 0.5f;
            line.useWorldSpace = true;
            line.numCornerVertices = 10;

            // Lista fija de colores
            Color[] colorPalette = new Color[]
            {
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow,
                new Color(1f, 0.4f, 0.7f), // Rosado
                new Color(1f, 0.5f, 0f),   // Naranja
                Color.cyan,
                Color.magenta
            };

            // Elegir un color aleatorio de la lista
            Color chosenColor = colorPalette[UnityEngine.Random.Range(0, colorPalette.Length)];

            // Crear gradiente
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(chosenColor, 0f),
                    new GradientColorKey(chosenColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.4f, 0f),
                    new GradientAlphaKey(0.4f, 1f)
                }
            );
            line.colorGradient = gradient;

            TrailLine trailScript = instantiatedObject.AddComponent<TrailLine>();
            trailScript.trailColor = chosenColor;
            trailScript.minDistance = 0.05f;


            // Set all the features of the player
            SetObjectFeatures(instantiatedObject, objNew);

            // If the player is a drone
            if (objNew.type == "Drone")
            {
                playerStartChildObject.tag = "DronePadStart";
                instantiatedObject.name = "Agent_" + currentID.ToString("D3");
                //instantiatedObject.name = "DRO"+currentID.ToString("D3")+playerGroupNames[i];
                SetDroneFeatures(instantiatedObject, objNew);
            }

            dronesNameInstantiated.Add(instantiatedObject.name);

            // If the player has a target game object
            if (objNew.addTargetGameObject == true) SetTargetGameObject(instantiatedObject);

            // If the player has internal scripts
            SetInternalScripts(instantiatedObject, objNew);

            // If the player has a lidar sensor
            SetLidarFeatures(instantiatedObject, objNew);

            // If the player has a depth camera sensor
            SetDepthCameraFeatures(instantiatedObject, objNew);

            // If the player has communication features
            SetCommunicationFeatures(instantiatedObject, objNew);

            // Add new ID number for the next player
            currentID++;

            countAgentObjects++;

        }



        // // For each player in the getSetupPlayers
        // foreach (ObjectData obj in getSetupPlayers.players)
        // {

        //     // Load the prefab from the Resources folder
            

        //     if (prefab != null)
        //     {

        //         // For each group in the list
        //         for (int i = 0; i < playerGroupNames.Count; i++)
        //         {

        //             // If the group of the player is the same as the group in the list
        //             if (obj.group == playerGroupNames[i])
        //             {

                        

        //             }

        //         }

        //     }
        //     else
        //     {
        //         // If the prefab is not found
        //         Debug.LogWarning("PrefabName: ´" + obj.prefabName + "´ not found. No player can be instantiated without a prefab object.");
        //     }

        // }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the features of the player:

    void SetObjectFeatures(GameObject instantiatedObject, ObjectData obj)
    {

        // Set the tag of the instantiated player
        instantiatedObject.tag = obj.type;

        // Add a reading component of the player features. NOTE: See the GetObjectFeatures script to unify variables
        GetObjectFeatures getObjectFeatures = instantiatedObject.AddComponent<GetObjectFeatures>();
        getObjectFeatures.group = obj.group;
        getObjectFeatures.objectID = currentID.ToString("D3");
        getObjectFeatures.prefabName = obj.prefabName;
        
    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the features of the drone:

    void SetDroneFeatures(GameObject instantiatedObject, ObjectData obj)
    {
        
        // Set the mass in the Rigidbody
        Rigidbody rb = instantiatedObject.GetComponent<Rigidbody>();
        if(obj.playerFeatures.unladenWeight == 0) rb.mass = 15.0f;
        else rb.mass = obj.playerFeatures.unladenWeight; 
        
        // Add a reading component of the drone features. NOTE: See the GetDroneFeatures script to unify variables
        GetDroneFeatures getDroneFeatures = instantiatedObject.AddComponent<GetDroneFeatures>();
        getDroneFeatures.unladenWeight = rb.mass;

        if(obj.playerFeatures.approxMaxFlightTime == 0) getDroneFeatures.approxMaxFlightTime = 20.0f;
        else getDroneFeatures.approxMaxFlightTime = obj.playerFeatures.approxMaxFlightTime;

        if(obj.playerFeatures.maxBatteryCapacity == 0) getDroneFeatures.maxBatteryCapacity = 3000.0f;
        else getDroneFeatures.maxBatteryCapacity = obj.playerFeatures.maxBatteryCapacity;

        if(obj.playerFeatures.batteryVoltage == 0) getDroneFeatures.batteryVoltage = 11.1f;
        else getDroneFeatures.batteryVoltage = obj.playerFeatures.batteryVoltage;
        
        if(obj.playerFeatures.batteryStartPercentage == 0) getDroneFeatures.batteryStartPercentage = 100.0f;
        else getDroneFeatures.batteryStartPercentage = obj.playerFeatures.batteryStartPercentage;
        
        getDroneFeatures.maxAltitude = obj.playerFeatures.maxAltitude;

        // If the maxThrust is not set, calculate it based on the mass of the drone
        if(obj.playerFeatures.maxThrust == 0) getDroneFeatures.maxThrust = rb.mass * 2.0f * Mathf.Abs(Physics.gravity.y);
        else getDroneFeatures.maxThrust = obj.playerFeatures.maxThrust;

        getDroneFeatures.maxSpeedManufacturer = obj.playerFeatures.maxSpeedManufacturer;

        // If the maximumTiltAngle is not set, set it to 30 degrees
        if(obj.playerFeatures.maximumTiltAngle == 0) getDroneFeatures.maximumTiltAngle = 30.0f;
        else getDroneFeatures.maximumTiltAngle = obj.playerFeatures.maximumTiltAngle;
        
        getDroneFeatures.propellerMaxRPM = obj.playerFeatures.propellerMaxRPM;

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the features of the car:

    void SetCarFeatures(GameObject instantiatedObject, ObjectData obj)
    {
        
        // Add a reading component of the car features. NOTE: See the GetCarFeatures script to unify variables
        GetCarFeatures getCarFeatures = instantiatedObject.AddComponent<GetCarFeatures>();
        getCarFeatures.unladenWeight = obj.playerFeatures.unladenWeight;
        getCarFeatures.maxSpeedManufacturer = obj.playerFeatures.maxSpeedManufacturer;

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the target game object:

    void SetTargetGameObject(GameObject instantiatedObject)
    {

        // Create a new object (sphere) to be the target of the player
        GameObject sphereTargetHolder = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // Set the name and scale of the target object
        sphereTargetHolder.name = "ID_" + currentID.ToString("D3") + "_Target";
        sphereTargetHolder.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Set the position and orientation of the target object
        sphereTargetHolder.transform.position = instantiatedObject.transform.position;
        sphereTargetHolder.transform.eulerAngles = instantiatedObject.transform.eulerAngles;

        // Eliminate the collider of the target object
        Destroy(sphereTargetHolder.GetComponent<Collider>());
        
        MeshRenderer meshRenderer = sphereTargetHolder.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false; // Disable the mesh renderer to make it invisible
        }
    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the internal scripts:

    void SetInternalScripts(GameObject instantiatedObject, ObjectData obj)
    {
        
        // If the player has internal scripts
        if(obj.dynamicScripts != null)
        {

            // For each script name in the list
            foreach (string dynamic in obj.dynamicScripts)
            {
                
                // Add the component related to the script operation based on the script name
                System.Type scriptDynamic = System.Type.GetType(dynamic);

                // If the script is found, add it to the instantiated object
                if (scriptDynamic != null) instantiatedObject.AddComponent(scriptDynamic);
                else Debug.LogWarning("Player name: "+instantiatedObject.name+" | dynamicScript: ´" + dynamic + "´ not found.");

            }

        }

        // If the player has algorithm scripts
        if(obj.algorithmScripts != null)
        {

            // For each script name in the list
            foreach (string algorithm in obj.algorithmScripts)
            {
                
                // Add the component related to the algorithm operation based on the script name
                System.Type scriptAlgorithm = System.Type.GetType(algorithm);

                // If the script is found, add it to the instantiated object
                if (scriptAlgorithm != null) instantiatedObject.AddComponent(scriptAlgorithm);
                else Debug.LogWarning("Player name: "+instantiatedObject.name+" | algorithmScripts: ´" + algorithm + "´ not found.");

            }

        }
        
        // If the player has other internal scripts (e.g. animations)
        if(obj.otherInternalScripts != null)
        {

            // For each script name in the list
            foreach (string otherInternal in obj.otherInternalScripts)
            {
                
                // Add the component related to the other internal operation based on the script name
                System.Type scriptOtherInternal = System.Type.GetType(otherInternal);

                // If the script is found, add it to the instantiated object
                if (scriptOtherInternal != null) instantiatedObject.AddComponent(scriptOtherInternal);
                else Debug.LogWarning("Player name: "+instantiatedObject.name+" | otherInternalScripts: ´" + otherInternal + "´ not found.");

            }

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the lidar features:

    void SetLidarFeatures(GameObject instantiatedObject, ObjectData obj)
    {
        
        // Find the LidarHolder object in the instantiated object
        Transform lidarHolderTransform = instantiatedObject.transform.Find("LidarHolder");

        // If the LidarHolder object is found
        if (lidarHolderTransform != null)
        {
            
            // Create a new object to be the holder of the lidar sensor
            GameObject lidarHolder = lidarHolderTransform.gameObject;

            // Add a reading component of the lidar features. NOTE: See the GetLidarFeatures script to unify variables
            GetLidarFeatures getLidarFeatures = instantiatedObject.AddComponent<GetLidarFeatures>();
            getLidarFeatures.scriptName = obj.lidarFeatures.scriptName;
            
            // If the lidar range is not set, set it to 50 meters
            if(obj.lidarFeatures.lidarRange == 0) getLidarFeatures.lidarRange = 50.0f;
            else getLidarFeatures.lidarRange = obj.lidarFeatures.lidarRange;

            // If the number of horizontal rays is not set, set it to 360
            if(obj.lidarFeatures.numRaysHorizontal == 0) getLidarFeatures.numRaysHorizontal = 360;
            else getLidarFeatures.numRaysHorizontal = obj.lidarFeatures.numRaysHorizontal;

            // If the number of vertical rays is not set, set it to 1
            if(obj.lidarFeatures.numRaysVertical == 0) getLidarFeatures.numRaysVertical = 1;
            else getLidarFeatures.numRaysVertical = obj.lidarFeatures.numRaysVertical;

            getLidarFeatures.verticalFOV = obj.lidarFeatures.verticalFOV;

            // If the number of points per second is not set, set it to 1
            if(obj.lidarFeatures.pointsPerSecond == 0) getLidarFeatures.pointsPerSecond = 1;
            else getLidarFeatures.pointsPerSecond = obj.lidarFeatures.pointsPerSecond;
            
            // If the script name is not null
            if(obj.lidarFeatures.scriptName != null){

                // Add the component related to the lidar operation based on the script name
                System.Type scriptLidar = System.Type.GetType(obj.lidarFeatures.scriptName);

                // If the script is found, add it to the instantiated object
                if (scriptLidar != null) lidarHolder.AddComponent(scriptLidar);
                else Debug.LogWarning("Player name: "+instantiatedObject.name+" | lidarFeatures / scriptName: ´" + obj.lidarFeatures.scriptName + "´ not found. No lidar sensor can be created without a reference script for its operation");

            }

        }
 
    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the depth camera features:

    void SetDepthCameraFeatures(GameObject instantiatedObject, ObjectData obj)
    {

        // Find the DepthCameraHolder object in the instantiated object
        Transform depthCameraHolderTransform = instantiatedObject.transform.Find("DepthCameraHolder");

        // If the DepthCameraHolder object is found
        if (depthCameraHolderTransform != null)
        {
            
            // Add a reading component of the depth camera features. NOTE: See the GetDepthCameraFeatures script to unify variables
            GetDepthCameraFeatures getDepthCameraFeatures = instantiatedObject.AddComponent<GetDepthCameraFeatures>();
            getDepthCameraFeatures.scriptName = obj.depthCameraFeatures.scriptName;
            
            // If the near clip plane is not set, set it to 0.3 meters
            if(obj.depthCameraFeatures.nearClipPlane == 0) getDepthCameraFeatures.nearClipPlane = 0.3f;
            else getDepthCameraFeatures.nearClipPlane = obj.depthCameraFeatures.nearClipPlane;

            // If the far clip plane is not set, set it to 200 meters
            if(obj.depthCameraFeatures.farClipPlane == 0) getDepthCameraFeatures.farClipPlane = 200.0f;
            else getDepthCameraFeatures.farClipPlane = obj.depthCameraFeatures.farClipPlane;

            // If the field of view is not set, set it to 60 degrees
            if(obj.depthCameraFeatures.fieldOfView == 0) getDepthCameraFeatures.fieldOfView = 60.0f;
            else getDepthCameraFeatures.fieldOfView = obj.depthCameraFeatures.fieldOfView;

            // If the pixel width is not set, set it to 256 pixels
            if(obj.depthCameraFeatures.pixelWidth == 0) getDepthCameraFeatures.pixelWidth = 256;
            else getDepthCameraFeatures.pixelWidth = obj.depthCameraFeatures.pixelWidth;

            // If the pixel height is not set, set it to 256 pixels
            if(obj.depthCameraFeatures.pixelHeight == 0) getDepthCameraFeatures.pixelHeight = 256;
            else getDepthCameraFeatures.pixelHeight = obj.depthCameraFeatures.pixelHeight;
            
            // Create a new object to be the holder of the depth camera sensor
            GameObject depthCameraHolder = depthCameraHolderTransform.gameObject;

            // Add the depth camera to the child player
            Camera depthCamera = depthCameraHolder.AddComponent<Camera>();

            // Add depth camera features
            depthCamera.nearClipPlane = getDepthCameraFeatures.nearClipPlane;
            depthCamera.farClipPlane = getDepthCameraFeatures.farClipPlane;
            depthCamera.fieldOfView = getDepthCameraFeatures.fieldOfView;

            // Create a RenderTexture and assign it to the depth camera
            RenderTexture renderTexture = new RenderTexture(
                getDepthCameraFeatures.pixelWidth, 
                getDepthCameraFeatures.pixelHeight,
                16 // 16 bits for the depth + 8 bits for the stencil
            );
            depthCamera.targetTexture = renderTexture;
    
            // If the script name is not null
            if(getDepthCameraFeatures.scriptName != null){

                // Add the component related to the camera operation based on the script name
                System.Type scriptCamera = System.Type.GetType(obj.depthCameraFeatures.scriptName);

                // If the script is found, add it to the instantiated object
                if (scriptCamera != null) depthCameraHolder.AddComponent(scriptCamera);
                else Debug.LogWarning("Player name: "+instantiatedObject.name+" | depthCameraFeatures / scriptName: ´" + obj.depthCameraFeatures.scriptName + "´ not found. No Depth Camera sensor can be created without a reference script for its operation");

            }
            
            // Disable the camera until an image needs to be captured
            depthCamera.enabled = false;

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the communication features:

    void SetCommunicationFeatures(GameObject instantiatedObject, ObjectData obj)
    {
        
        // Add a reading component of the communication features. NOTE: See the GetCommunicationFeatures script to unify variables
        GetCommunicationFeatures getCommunicationFeatures = instantiatedObject.AddComponent<GetCommunicationFeatures>();
        getCommunicationFeatures.scriptName = obj.communicationFeatures.scriptName;
        
        // If the script name is not null
        if(obj.communicationFeatures.scriptName != null){

            // Add the component related to the camera operation based on the script name
            System.Type scriptCommunication = System.Type.GetType(obj.communicationFeatures.scriptName);

            // If the script is found, add it to the instantiated object
            if (scriptCommunication != null) instantiatedObject.AddComponent(scriptCommunication);
            else Debug.LogWarning("Player name: "+instantiatedObject.name+" | communicationFeatures / scriptName: ´" + obj.communicationFeatures.scriptName + "´ not found. No Communication features can be created without a reference script for its operation");

        }

    }

}
