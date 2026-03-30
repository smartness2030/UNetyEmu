// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

// Class to publish GPS information of the vehicle in a ROS topic.
public class GPSROS : MonoBehaviour
{

    private ROSConnection ros;
    private Rigidbody rb;

    private string topicName;
    private string vehicleID;

    // Start is called before the first frame update
    void Start()
    {
        vehicleID=gameObject.name;
        topicName = vehicleID+"_GPS";

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<NavSatFixMsg>(topicName);

        rb = GetComponent<Rigidbody>();
    }

    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
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
