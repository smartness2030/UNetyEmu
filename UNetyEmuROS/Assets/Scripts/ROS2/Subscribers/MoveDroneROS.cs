// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

// Class to subscribe to ROS topic and move the drone in Unity according to the received commands
[RequireComponent(typeof(DroneControlInputs))]
public class MoveDroneROS : MonoBehaviour
{
    private DroneControlInputs droneControlInputs;

    void Start()
    {
        droneControlInputs = GetComponent<DroneControlInputs>();
        droneControlInputs.controlMode = DroneControlMode.Manual;

        Debug.Log($"[MoveDroneROS] Starting MoveDroneROS and subscribing to the drone commands topic....");

        string topicName = gameObject.name + "_keyboardInput";
        Debug.Log($"[MoveDroneROS] Subscribing to: '{topicName}'");
        ROSConnection.GetOrCreateInstance().Subscribe<Float32MultiArrayMsg>(topicName, OnCommandReceived);
    }

    void OnCommandReceived(Float32MultiArrayMsg msg)
    {
        Debug.Log($"[MoveDroneROS] Received: T={msg.data[0]:F2} P={msg.data[1]:F2} R={msg.data[2]:F2} Y={msg.data[3]:F2}");
        droneControlInputs.throttleInput = msg.data[0];
        droneControlInputs.pitchInput    = msg.data[1];
        droneControlInputs.rollInput     = msg.data[2];
        droneControlInputs.yawInput      = msg.data[3];
    }
}
