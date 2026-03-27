using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class LoadPath : MonoBehaviour
{
    
    public string fileScenario = "ScenarioToLoad.3dscen";
    
    public string filePathOutbound = "PathsOutboundToUse.txt";

    public string filePathReturn = "PathsReturnToUse.txt";

    public static int numberOfAgentsInPath = 0; // Total number of agents in the path data
    

    // Dictionary to store agent data, where key = agent ID, value = AgentPathInfo object
    public static Dictionary<int, AgentPathInfo> agentsPathOutboundData;
    public static Dictionary<int, AgentPathInfo> agentsPathReturnData;

    public static Dictionary<int, AgentScenarioInfo> agentsScenarioData;

    void Start()
    {
        
        agentsPathOutboundData = new Dictionary<int, AgentPathInfo>();
        agentsPathReturnData = new Dictionary<int, AgentPathInfo>();

        agentsScenarioData = new Dictionary<int, AgentScenarioInfo>();

        LoadScenario(); 

        LoadRoutes();

    }

    void LoadScenario()
    {

        string scenarioFullPath = Path.Combine(Application.streamingAssetsPath, fileScenario);

        string[] lines = File.ReadAllLines(scenarioFullPath); // Read all lines from the scenario file
        ParseScenarioLine(lines);
        
    }

    void ParseScenarioLine(string[] lines)
    {

        // Extract the size and number of agents from the scenario file
        string[] firstLineValues = lines[0].Split(' ');
        int sizeX = int.Parse(firstLineValues[0]);
        int sizeY = int.Parse(firstLineValues[2]); // In Unity, Y is the second value in the match
        int sizeZ = int.Parse(firstLineValues[1]); // In Unity, Z is the first value in the match

        int totalAgents = int.Parse(lines[1]);

        Debug.Log($"Scenario Size: {sizeX} X {sizeY} X {sizeZ}");
        Debug.Log($"Total DronePads in the Scenario: {totalAgents}");

        for (int i = 2; i < lines.Length; i++)
        {
            
            string[] values = lines[i].Split(' ');
            
            if (values.Length == 10)
            {

                float startX = float.Parse(values[0]);
                float startY = float.Parse(values[2]); // In Unity, Y is the second value in the match
                float startZ = float.Parse(values[1]); // In Unity, Z is the first value in the match

                float endX = float.Parse(values[3]);
                float endY = float.Parse(values[5]); // In Unity, Y is the second value in the match
                float endZ = float.Parse(values[4]); // In Unity, Z is the first value in the match

                float startTime = float.Parse(values[6]);
                float safetyRadius = float.Parse(values[7]);
                float speed = float.Parse(values[8]);
                float extraVariable = float.Parse(values[9]); // Extra variable for future use

                Vector3 startPosition = new Vector3(startX, startY, startZ);
                Vector3 endPosition = new Vector3(endX, endY, endZ);

                // Create and store agent information
                AgentScenarioInfo agentScenarioInfo = new AgentScenarioInfo(startPosition, endPosition, startTime, safetyRadius, speed, extraVariable);
                agentsScenarioData[i - 2] = agentScenarioInfo; // Store in dictionary

            }
            else
            {
                Debug.LogWarning("Line format is incorrect: " + values);
            }

        }

    }

    // Reads the file and extracts drone routes and properties
    void LoadRoutes()
    {
        
        string pathFileFullPathOutbound = Path.Combine(Application.streamingAssetsPath, filePathOutbound);
        string pathFileFullPathReturn = Path.Combine(Application.streamingAssetsPath, filePathReturn);

        // Read all lines from the file
        string[] linesOutbound = File.ReadAllLines(pathFileFullPathOutbound);
        
        // Process each line (each line represents a route for an agent)
        foreach (string lineOutbound in linesOutbound)
        {
            ParsePathOutboundLine(lineOutbound);
        }

        // -----------------------------------------------------------------------------------------------------

        if (agentsPathOutboundData.Count > 0)
        {
            numberOfAgentsInPath = agentsPathOutboundData.Count; // Outbound agents
            //numberOfAgentsInPath = agentsScenarioData.Count; // Full agents
            //numberOfAgentsInPath = 301; // Agents
        }

        // -----------------------------------------------------------------------------------------------------

        Debug.Log($"Total Agents in the Path: {numberOfAgentsInPath}");

        if (File.Exists(pathFileFullPathReturn))
        {

            string[] linesReturn = File.ReadAllLines(pathFileFullPathReturn);

            if (linesReturn.Length > 0)
            {

                // Process each line (each line represents a route for an agent)
                foreach (string lineReturn in linesReturn)
                {
                    ParsePathReturnLine(lineReturn);
                }
                
            }

        }

        SetPath.flagSetPath = true; // Set the flag to true to indicate that the path has been set
        
    }


    void ParsePathOutboundLine(string line)
    {

        // Regular expression pattern to extract data
        string pattern;

        if (line.Contains("OUTBOUND"))
        {

            pattern = @"Agent\s(\d+)\s\[Radius:\s([\d.]+),\sSpeed:\s([\d.]+),\sStart\sTime:\s([\d.]+)\]\s(\w+):(.+)";

            Match match = Regex.Match(line, pattern); // Match the pattern in the line

            // Extract values from the matched groups
            if (match.Success)
            {
                int agentId = int.Parse(match.Groups[1].Value); // Extract agent ID
                float radius = float.Parse(match.Groups[2].Value); // Extract radius
                float speed = float.Parse(match.Groups[3].Value); // Extract speed
                float startTime = float.Parse(match.Groups[4].Value); // Extract start time
                string direction = match.Groups[5].Value;
                string positionsData = match.Groups[6].Value; // Extract position data as a string

                List<Vector3> positions = ExtractPositions(positionsData); // Convert position data into a list of Vector3

                // Create and store agent information
                AgentPathInfo agentPathInfo = new AgentPathInfo(radius, speed, startTime, positions);
                agentsPathOutboundData[agentId] = agentPathInfo; // Store in dictionary

            }
            else
            {
                Debug.LogWarning("Line format is incorrect: " + line);
            }

        }
        else
        {

            pattern = @"Agent\s(\d+)\s\[Radius:\s([\d.]+),\sSpeed:\s([\d.]+),\sStart\sTime:\s([\d.]+)\]:(.+)";

            Match match = Regex.Match(line, pattern); // Match the pattern in the line

            // Extract values from the matched groups
            if (match.Success)
            {
                int agentId = int.Parse(match.Groups[1].Value); // Extract agent ID
                float radius =  float.Parse(match.Groups[2].Value); // Extract radius
                float speed = float.Parse(match.Groups[3].Value); // Extract speed
                float startTime = float.Parse(match.Groups[4].Value); // Extract start time
                string positionsData = match.Groups[5].Value; // Extract position data as a string

                List<Vector3> positions = ExtractPositions(positionsData); // Convert position data into a list of Vector3

                // Create and store agent information
                AgentPathInfo agentPathInfo = new AgentPathInfo(radius, speed, startTime, positions);
                agentsPathOutboundData[agentId] = agentPathInfo; // Store in dictionary

            }
            else
            {
                Debug.LogWarning("Line format is incorrect: " + line);
            }
            
        }

    }


    void ParsePathReturnLine(string line)
    {
        // Regular expression pattern to extract data
        string pattern = @"Agent\s(\d+)\s\[Radius:\s([\d.]+),\sSpeed:\s([\d.]+),\sStart\sTime:\s([\d.]+)\]\s(\w+):(.+)";

        Match match = Regex.Match(line, pattern); // Match the pattern in the line

        // Extract values from the matched groups
        if (match.Success)
        {
            int agentId = int.Parse(match.Groups[1].Value); // Extract agent ID
            float radius =  float.Parse(match.Groups[2].Value); // Extract radius
            float speed = float.Parse(match.Groups[3].Value); // Extract speed
            float startTime = float.Parse(match.Groups[4].Value); // Extract start time
            string direction = match.Groups[5].Value;
            string positionsData = match.Groups[6].Value; // Extract position data as a string

            List<Vector3> positions = ExtractPositions(positionsData); // Convert position data into a list of Vector3

            // Create and store agent information
            AgentPathInfo agentPathInfo = new AgentPathInfo(radius, speed, startTime, positions);
            agentsPathReturnData[agentId] = agentPathInfo; // Store in dictionary

        }
        else
        {
            Debug.LogWarning("Line format is incorrect: " + line);
        }
        
    }


    List<Vector3> ExtractPositions(string data)
    {
        List<Vector3> positions = new List<Vector3>(); // List to store extracted positions
        string positionPattern = @"\((\d+),(\d+),(\d+)\)"; // Pattern to match (x,y,z) coordinates
        Vector3? lastPosition = null; // Variable para rastrear la última posición añadida

        foreach (Match match in Regex.Matches(data, positionPattern))
        {
            float x = float.Parse(match.Groups[1].Value); // Extract X coordinate
            float y = float.Parse(match.Groups[3].Value) + 2f; // Extract Z coordinate (y coordinate in Unity is the third value in the match)
            float z = float.Parse(match.Groups[2].Value); // Extract Y coordinate (z coordinate in Unity is the second value in the match)

            Vector3 currentPosition = new Vector3(x, y, z);
        
            if (!lastPosition.HasValue || currentPosition != lastPosition.Value)
            {
                positions.Add(currentPosition);
                lastPosition = currentPosition;
            }

        }

        return positions; // Return the list of positions
    }

}

// Class to store agent information
public class AgentPathInfo
{
    public float Radius { get; private set; }
    public float Speed { get; private set; }
    public float StartTime { get; private set; }
    public List<Vector3> Positions { get; private set; }

    public AgentPathInfo(float radius, float speed, float startTime, List<Vector3> positions)
    {
        Radius = radius;
        Speed = speed;
        StartTime = startTime;
        Positions = positions;
    }
}

// Class to store agent information
public class AgentScenarioInfo
{
    public Vector3 StartPosition { get; private set; } 
    public Vector3 EndPosition { get; private set; } 
    public float StartTime { get; private set; }
    public float SafetyRadius { get; private set; }
    public float Speed { get; private set; }
    public float ExtraVariable { get; private set; } 

    public AgentScenarioInfo(Vector3 startPosition, Vector3 endPosition, float startTime, float safetyRadius, float speed, float extraVariable)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
        StartTime = startTime;
        SafetyRadius = safetyRadius;
        Speed = speed;
        ExtraVariable = extraVariable;
    }
}
