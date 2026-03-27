using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class NonFlyZoneReader : MonoBehaviour
{
    public string fileScenario = "NfzToUse.3dmap";
    public Material groupMaterial;

    private float startTimeSetupPath = -1f;

    private class GroupCube
    {
        public GameObject cube;
        public float startTime;
        public float endTime;
    }

    private List<GroupCube> groupCubes = new List<GroupCube>();

    void Start()
    {
        string scenarioFullPath = Path.Combine(Application.streamingAssetsPath, fileScenario);

        if (!File.Exists(scenarioFullPath))
        {
            Debug.LogError("File not found: " + scenarioFullPath);
            return;
        }

        string[] lines = File.ReadAllLines(scenarioFullPath);

        if (lines.Length == 0)
        {
            Debug.LogWarning("The file is empty: " + scenarioFullPath);
            return;
        }

        int lineIndex = 0;

        // Skip empty lines if present
        while (string.IsNullOrWhiteSpace(lines[lineIndex])) lineIndex++;

        // Read dimensions
        string[] dims = lines[lineIndex++].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int dimX = int.Parse(dims[0]);
        int dimY = int.Parse(dims[1]);
        int dimZ = int.Parse(dims[2]);

        Debug.Log($"Dimensions: {dimX} {dimY} {dimZ}");

        // Skip empty lines if present
        while (string.IsNullOrWhiteSpace(lines[lineIndex])) lineIndex++;

        int initialPointCount = int.Parse(lines[lineIndex++]);

        // Skip initial points
        lineIndex += initialPointCount;

        // Skip empty lines if present
        while (string.IsNullOrWhiteSpace(lines[lineIndex])) lineIndex++;

        int numGroups = int.Parse(lines[lineIndex++]);

        GameObject parentObject = new GameObject("GroupsParent");

        for (int i = 0; i < numGroups; i++)
        {
            // Skip empty lines if present
            while (string.IsNullOrWhiteSpace(lines[lineIndex])) lineIndex++;

            string[] groupInfo = lines[lineIndex++].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (groupInfo.Length < 4)
            {
                Debug.LogWarning($"Invalid group header at line {lineIndex - 1}");
                continue;
            }

            int groupId = int.Parse(groupInfo[0]);
            float startTime = float.Parse(groupInfo[1]) + 2f;
            float endTime = float.Parse(groupInfo[2]) + 2f;
            int groupPointCount = int.Parse(groupInfo[3]);

            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;

            for (int j = 0; j < groupPointCount; j++)
            {
                // Skip empty lines if present
                while (string.IsNullOrWhiteSpace(lines[lineIndex])) lineIndex++;

                string[] pointParts = lines[lineIndex++].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (pointParts.Length < 3)
                {
                    Debug.LogWarning($"Invalid point line at {lineIndex - 1}");
                    continue;
                }

                int x = int.Parse(pointParts[0]);
                int y = int.Parse(pointParts[1]);
                int z = int.Parse(pointParts[2]);

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                minZ = Mathf.Min(minZ, z);

                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
                maxZ = Mathf.Max(maxZ, z);
            }

            float sizeX = maxX - minX + 1;
            float sizeY = maxY - minY + 1;
            float sizeZ = maxZ - minZ + 1;

            float centerX = minX + sizeX / 2f;
            float centerY = minY + sizeY / 2f;
            float centerZ = minZ + sizeZ / 2f;

            // Swap Y and Z axes for Unity
            Vector3 unityPosition = new Vector3(centerX, centerZ + 2f, centerY);
            Vector3 unityScale = new Vector3(sizeX - 0.5f, sizeZ - 0.5f, sizeY - 0.5f);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cube.GetComponent<BoxCollider>());

            cube.name = $"GroupNonFlyZone_{groupId}";
            cube.transform.position = unityPosition;
            cube.transform.localScale = unityScale;
            cube.transform.parent = parentObject.transform;
            cube.SetActive(false); // Start as inactive

            Renderer renderer = cube.GetComponent<Renderer>();
            if (groupMaterial != null)
            {
                renderer.material = groupMaterial;
            }

            // Print position and size
            Debug.Log($"Group of NonFlyZone {groupId} | StartTime: {startTime} | EndTime: {endTime} | Pos: {unityPosition} | Size: {unityScale}");

            groupCubes.Add(new GroupCube
            {
                cube = cube,
                startTime = startTime,
                endTime = endTime
            });
        }
    }

    void Update()
    {

        if (ObjectSetupPathPlanningScript.allInstantiatedObjectsReady == true)
        {

            if (startTimeSetupPath < 0f)
            {
                startTimeSetupPath = Time.time;
            }

            float currentTime = Time.time - startTimeSetupPath;

            foreach (var group in groupCubes)
            {

                bool shouldBeActive = currentTime >= group.startTime && currentTime <= group.endTime;

                if (group.cube.activeSelf != shouldBeActive)
                {
                    group.cube.SetActive(shouldBeActive);
                }

            }
        
        }

    }

}
