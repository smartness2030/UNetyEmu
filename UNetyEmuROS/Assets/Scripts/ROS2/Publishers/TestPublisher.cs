// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

// Class to test ROS connection and topic publishing
public class TestPublisher : MonoBehaviour
{
    public string topicName = "/hello_world";

    private ROSConnection ros;
    private float timer;

    // Start is called before the first frame update
    void Start()
    {
        //Instanciate and start ROS connection
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
            ros.Publish(topicName, msg); //Publish msg in ros topic
        }
    }
}
