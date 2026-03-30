// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

// Class to test ROS connection and topic subscribing
public class TestSubscriber : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<Float32Msg>("/firstPublisher", ReceiverUpdate);
    }
    
    void ReceiverUpdate(Float32Msg msg)
    {
        Debug.Log($"[TestSubscriber] Received message from ROS2: {msg.data}");
    }

}
