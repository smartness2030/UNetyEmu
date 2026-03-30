// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

// Class to publish IMU information of the vehicle in a ROS topic.
public class IMUROS : MonoBehaviour
{
    private ROSConnection ros;
    private Rigidbody rb;

    private Vector3 prevVelocity;
    private string topicName;
    private string vehicleID;
    
    // Start is called before the first frame update
    void Start()
    {
        vehicleID = gameObject.name;
        topicName = vehicleID + "_IMU";

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImuMsg>(topicName);

        rb = GetComponent<Rigidbody>();
    }

    // FixedUpdate is called at a fixed time interval, ideal for physics calculations
    void FixedUpdate()
    {
        Vector3 velocity = rb.velocity;
        Vector3 angularVelocity = rb.angularVelocity;

        Vector3 acceleration = (velocity - prevVelocity) / Time.fixedDeltaTime;
        prevVelocity = velocity;

        ImuMsg outputMsg = new ImuMsg();
        outputMsg.header = new HeaderMsg
        {
            stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg
            {
                sec = (int)Time.time,
                nanosec = (uint)((Time.time - Mathf.Floor(Time.time)) * 1e-9)
            }
        };

        outputMsg.orientation = new QuaternionMsg(
            transform.rotation.x,
            transform.rotation.y,
            transform.rotation.z,
            transform.rotation.w
        );

        outputMsg.angular_velocity = new Vector3Msg(
            angularVelocity.x,
            angularVelocity.y,
            angularVelocity.z
        );

        outputMsg.linear_acceleration = new Vector3Msg(
            acceleration.x,
            acceleration.y,
            acceleration.z
        );

        ros.Publish(topicName, outputMsg);
    }
}
