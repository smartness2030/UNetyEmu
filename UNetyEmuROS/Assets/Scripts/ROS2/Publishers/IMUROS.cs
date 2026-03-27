using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using Unity.VisualScripting;
using RosMessageTypes.Std;
using System.Numerics;

public class IMUROS : MonoBehaviour
{
    // Start is called before the first frame update
    private ROSConnection ros;
    private Rigidbody rb;

    private UnityEngine.Vector3 prevVelocity, prevAngularVelocity;
    private string topicName;
    private string droneID;

    
    void Start()
    {
        droneID = gameObject.name;
        topicName = droneID + "_IMU";

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImuMsg>(topicName);

        rb = GetComponent<Rigidbody>();


    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        UnityEngine.Vector3 velocity = rb.velocity;
        UnityEngine.Vector3 angularVelocity = rb.angularVelocity;

        UnityEngine.Vector3 acceleration = (velocity - prevVelocity) / Time.fixedDeltaTime;
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

      //  outputMsg.orientation_covariance = new double[9];
       // outputMsg.angular_velocity_covariance = new double[9];
        //outputMsg.linear_acceleration_covariance = new double[9];

        ros.Publish(topicName, outputMsg);
    }
}
