using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPath : MonoBehaviour
{
    
    public static bool flagSetPath = false; 

    private GameObject droneObject;

    private int countDronesWithPath;
    

    // Update is called once per frame
    void FixedUpdate()
    {
        
        if(ObjectSetupPathPlanningScript.allInstantiatedObjectsReady == true)
        {
            
            if(flagSetPath)
            {
                
                //Debug.Log("Nombres de drones: "+ ObjectSetupPathPlanningScript.dronesNameInstantiated.Count);

                countDronesWithPath = 0;

                // Accessing agent data after parsing
                foreach (var agent in LoadPath.agentsPathOutboundData)
                {
                    
                    droneObject = GameObject.Find(ObjectSetupPathPlanningScript.dronesNameInstantiated[countDronesWithPath]);
                    Debug.Log("Drone Object: " + droneObject.name);

                    if (droneObject != null)
                    {

                        droneObject.GetComponent<Drone4DPathPlanning>().radius = agent.Value.Radius; // Set the radius of the drone
                        droneObject.GetComponent<Drone4DPathPlanning>().speed = agent.Value.Speed; // Set the speed of the drone
                        droneObject.GetComponent<Drone4DPathPlanning>().startTimeOutbound = agent.Value.StartTime + 2f; // Set the start time of the drone
                        droneObject.GetComponent<Drone4DPathPlanning>().routePointsOutbound = agent.Value.Positions; // Set the route points of the drone

                    }

                    Debug.Log($"Agent {agent.Key}: Radius: {agent.Value.Radius}, Speed: {agent.Value.Speed}, Start Time: {agent.Value.StartTime}, OUTBOUND Number of Positions: {agent.Value.Positions.Count}");
                    foreach (var position in agent.Value.Positions)
                    {
                        //Debug.Log($"Position: {position}");
                    }

                    countDronesWithPath++;

                }
                
                countDronesWithPath = 0;

                foreach (var agent in LoadPath.agentsPathReturnData)
                {

                    droneObject = GameObject.Find(ObjectSetupPathPlanningScript.dronesNameInstantiated[countDronesWithPath]);

                    if (droneObject != null)
                    {

                        // droneObject.GetComponent<Drone4DPathPlanning>().startTimeReturn = agent.Value.StartTime; // Set the start time of the drone
                        droneObject.GetComponent<Drone4DPathPlanning>().startTimeReturn = 10; // Set the start time of the drone
                        droneObject.GetComponent<Drone4DPathPlanning>().routePointsReturn = agent.Value.Positions; // Set the route points of the drone

                    }

                    Debug.Log($"Agent {agent.Key}: Radius: {agent.Value.Radius}, Speed: {agent.Value.Speed}, Start Time: {agent.Value.StartTime}, RETURN Number of Positions: {agent.Value.Positions.Count}");

                    foreach (var position in agent.Value.Positions)
                    {
                        //Debug.Log($"Position: {position}");
                    }

                    countDronesWithPath++;

                }
                
                flagSetPath = false; 
                
            }

        }

    }

}
