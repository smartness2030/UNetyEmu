using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;




public class GPSROS : MonoBehaviour
{

    private ROSConnection ros;
    private Rigidbody rb;

    private UnityEngine.Vector3 globalPosition, localPosition;
    private string topicName;
    private string droneID;
    // Start is called before the first frame update
    void Start()
    {
        droneID=gameObject.name;
        topicName = droneID+"_GPS";

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<NavSatFixMsg>(topicName);

        rb = GetComponent<Rigidbody>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        UnityEngine.Vector3 global_pos = transform.position;

        double global_latitude = global_pos.z;
        double global_longitude = global_pos.x;
        double global_altitude = global_pos.y;

        NavSatFixMsg globalGps = new NavSatFixMsg();

        globalGps.header = new HeaderMsg
        {
            stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg
            {
                sec = (int)Time.time,
                nanosec = (uint)((Time.time - Mathf.Floor(Time.time)) * 1e-9)

            }
        };

        globalGps.latitude = global_latitude;
        globalGps.longitude = global_longitude;
        globalGps.altitude = global_altitude;

        ros.Publish(topicName,globalGps);
    }
}
