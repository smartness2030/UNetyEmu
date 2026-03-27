using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class TestSubscriber : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<Float32Msg>("/firstPublisher", ReceiverUpdate);
    }
    
    void ReceiverUpdate(Float32Msg msg)
    {
        Debug.Log($"Mensagem recebida de fora do ROS2 {msg.data}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
