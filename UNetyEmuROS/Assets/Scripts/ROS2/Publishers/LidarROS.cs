using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;



public class LidarROS : MonoBehaviour
{   
    
    [Header("Sensor Offset")]
    public Vector3 sensorOffset = Vector3.zero;

    [Header("Sensor configuration")]
    public int horizontalResolution =360;
    public int verticalChannels = 16;
    public int verticalFOV = 45;
    public int maxDistance = 30;
    public int scansPerSecond = 10;


    private NativeArray<RaycastCommand> commands;
    private NativeArray<RaycastHit> results;

    [Header("ROS Configuration")]
    private ROSConnection ros;
    private string topicName;
    private float timer;

    void Start(){
        topicName = gameObject.name + "_lidar";
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PointCloud2Msg>(topicName);
    }

    // Update is called once per frame
    void Update()
    {
        timer+=Time.deltaTime;
        if(timer >=1f / scansPerSecond){
            Scan();
            timer=0;
        }
        
    }

    void Scan()
    {
        int totalBeams = horizontalResolution * verticalChannels;

        commands = new NativeArray<RaycastCommand>(totalBeams, Allocator.TempJob);
        results = new NativeArray<RaycastHit>(totalBeams, Allocator.TempJob);

        float vStep = verticalFOV / (verticalChannels - 1);

        var queryParameters = new QueryParameters
        {
            layerMask = Physics.DefaultRaycastLayers,
            hitTriggers = QueryTriggerInteraction.Ignore
        };

        for(int k = 0; k < verticalChannels; k++){
            float vAngle = -verticalFOV / 2 + (k * vStep);

            for (int h = 0; h < horizontalResolution; h++){
                float hAngle = h * (360f / horizontalResolution);
                Vector3 direction = Quaternion.Euler(vAngle, hAngle, 0) * transform.forward;

                int index = k * horizontalResolution + h;

                Vector3 rayOrigin = transform.position + transform.rotation * sensorOffset;
                commands[index] = new RaycastCommand(
                    rayOrigin,
                    direction,
                    queryParameters,
                    maxDistance
                );
            }
        }

        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 20);
        handle.Complete();

        ProcessPC2(results);

        commands.Dispose();
        results.Dispose();
    }

    void ProcessPC2(NativeArray<RaycastHit> results){
        int hitCount=0;
        foreach(var hit in results) if(hit.collider !=null)hitCount++;

        PointCloud2Msg msg = new PointCloud2Msg();
        msg.header = new HeaderMsg{
            frame_id = gameObject.name + "_lidar_link",
            stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg{
                sec = (int)Time.time,
                nanosec = (uint)((Time.time%1)*1e9)
            }
        };
    

        msg.height = 1;
        msg.width = (uint)hitCount;
        msg.is_bigendian=false;
        msg.is_dense=true;
        msg.point_step=12;
        msg.row_step = (uint)(msg.point_step*hitCount);

        msg.fields = new PointFieldMsg[3];
        string[] names = {"x","y","z"};
        for(int k=0;k<3;k++){
            msg.fields[k]=new PointFieldMsg(names[k],(uint)(k*4),PointFieldMsg.FLOAT32,1);

        }

        byte[] data = new byte[msg.row_step];
        int offset=0;
        foreach(var hit in results){
            if(hit.collider !=null){
                Vector3 localPoint = transform.InverseTransformPoint(hit.point);
                float rosX = localPoint.z;
                float rosY = -localPoint.x;
                float rosZ = localPoint.y;

                Buffer.BlockCopy(BitConverter.GetBytes(rosX),0,data,offset,4);
                Buffer.BlockCopy(BitConverter.GetBytes(rosY),0,data,offset+4,4);
                Buffer.BlockCopy(BitConverter.GetBytes(rosZ),0,data,offset+8,4);
                offset+=12;
            }
        }
        msg.data=data;

        ros.Publish(topicName,msg);
    }
}


