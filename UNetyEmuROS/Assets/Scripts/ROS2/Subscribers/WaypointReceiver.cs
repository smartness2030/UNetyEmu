// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;


[RequireComponent(typeof(DroneSetTarget))]
public class WaypointReceiver : MonoBehaviour
{
    private string droneId;
    private DroneSetTarget waypointTarget;
 
    void Start()
    {
        droneId = gameObject.name;
        waypointTarget = GetComponent<DroneSetTarget>();
        string topicName = droneId + "_waypointReceiver";
        ROSConnection.GetOrCreateInstance().Subscribe<Float32MultiArrayMsg>(topicName,waypointUpdate); //Receive waypoints from ROS connection.
        
    }

    void waypointUpdate(Float32MultiArrayMsg msg){ //Update the waypoint goal.
        waypointTarget.target.position = new Vector3(msg.data[0],msg.data[1],msg.data[2]);
        waypointTarget.target.orientation = msg.data[3];
        waypointTarget.target.cruiseSpeed = msg.data[4];
        waypointTarget.target.descentSpeed = msg.data[4];
        waypointTarget.target.climbSpeed = msg.data[4];
    }

    
}
