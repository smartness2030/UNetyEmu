using UnityEngine;
using System.Text;
using System;

public class CommandsHandler : MonoBehaviour
{
    public Transform[] cubes;
    public bool printDebug;

    public string GetPositions()
    {
        
        // Create a StringBuilder to build the response message
        StringBuilder sb = new StringBuilder();

        // Loop through each cube and append its position
        for (int i = 0; i < cubes.Length; i++)
        {
            // Get the position of the cube and round to 2 decimal places
            Vector3 pos = cubes[i].position;

            // Append the position to the StringBuilder
            sb.Append($"{(float)Math.Round((double)pos.x,2)},{(float)Math.Round((double)pos.y,2)},{(float)Math.Round((double)pos.z,2)}");

            // Append a semicolon if not the last cube
            if (i < cubes.Length - 1) sb.Append(";");
        }

        // Append newline to mark end of message
        sb.Append("\n");

        return sb.ToString();
    }

    public void SetPositions(string data)
    {
        
        // Extract and split the position data
        string[] parts = data.Split(';');

        // Loop through each part and assign new X value
        for (int i = 0; i < parts.Length && i < cubes.Length; i++)
        {
            // Split the part into coordinates
            string[] coords = parts[i].Split(',');

            // Parse coordinates and apply only the X position (just for test purposes)
            if (coords.Length == 3 &&
                float.TryParse(coords[0], out float x) &&
                float.TryParse(coords[1], out float y) &&
                float.TryParse(coords[2], out float z))
            {
                // Get the current position of the cube
                Vector3 current = cubes[i].position;

                // Set the new position with the updated X value
                cubes[i].position = new Vector3(x, current.y, current.z); 

                // Log new position
                if(printDebug) Debug.Log($"Cube{i + 1} new X: {x}");
            }
        }
    }

    public void StopUnityEditor()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
