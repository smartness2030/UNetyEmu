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


public class MoveCarROS : MonoBehaviour
{
    private string carId;
    private CarDynamics carDynamics;

    void Start()
    {   
        carId = gameObject.name;
        carDynamics = GetComponent<CarDynamics>();
        string topicName = carId + "_keyboardInput";
        ROSConnection.GetOrCreateInstance().Subscribe<Float32MultiArrayMsg>(topicName,carstateUpdate); //Receive keyboard command from ROS.
        
    }

    void carstateUpdate(Float32MultiArrayMsg msg){ //Apply ROS received information in car.
        bool braking;
        carDynamics.throttle = msg.data[0];
        braking = msg.data[1] == 1 ? true : false;
        carDynamics.isBraking = braking;
        carDynamics.steering = msg.data[2];
    }

    
}