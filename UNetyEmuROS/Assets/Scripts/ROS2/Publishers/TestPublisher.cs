// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------


// Libraries
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using Unity.VisualScripting.FullSerializer;
using RosMessageTypes.Rosgraph;

public class TestPublisher : MonoBehaviour
{
    ROSConnection ros; //Ros connection object.
    public string topicName = "/hello_world";
    float timer;

    void Start()
    {
        //Instanciate and start ROS connection.
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<StringMsg>(topicName);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 1.0f)
        {
            timer = 0.0f;
            StringMsg msg = new StringMsg("Hello from Unity!");
            ros.Publish(topicName, msg); //Publish msg in ros topic.

        }
        
    }
}
