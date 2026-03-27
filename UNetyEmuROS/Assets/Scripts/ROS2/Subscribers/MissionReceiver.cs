// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------



// Libraries
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

[RequireComponent(typeof(DroneMissionPlanner))]
public class MissionReceiver : MonoBehaviour
{

    private string droneID;
    private DroneMissionPlanner planner;

    [Serializable]
    private class WaypointData
    {
        public float x, y, z;
        public float cruiseSpeed;
        public float climbSpeed;
        public float descentSpeed;
    }

    [Serializable]
    private class StepData
    {
        public int movementId;
        public float waitBefore;
        public float waitAfter;
        public List<WaypointData> waypoints;
    }

    [Serializable]
    private class MissionData
    {
        public List<StepData> steps;
    }

    void Start()
    {
        droneID = gameObject.name;
        planner = GetComponent<DroneMissionPlanner>();

        string topic = droneID + "_Missionreceiver";
        
        ROSConnection.GetOrCreateInstance().Subscribe<StringMsg>(topic, missionUpdate);

    }

    void missionUpdate(StringMsg receivedMsg)
    {   
        MissionData missionData = JsonUtility.FromJson<MissionData>(receivedMsg.data);

        if (missionData == null || missionData.steps == null || missionData.steps.Count == 0)
        {
            Debug.LogWarning("WaypointsReceiver: JSON error.");
            return;
        }

        MissionStep[] steps = BuildMissionSteps(missionData);
        planner.LoadMission(steps, autoStart: true); 
    }

    MissionStep[] BuildMissionSteps(MissionData data)
    {
        MissionStep[] steps = new MissionStep[data.steps.Count];

        for (int k = 0; k < data.steps.Count; k++)
        {
            StepData sd = data.steps[k];
            steps[k] = new MissionStep
            {
                stepType   = IdToStepType(sd.movementId), 
                waitBefore = sd.waitBefore,
                waitAfter  = sd.waitAfter,
                waypoints  = BuildWaypoints(sd.waypoints)
            };
        }

        return steps;
    }

    SetDroneTarget[] BuildWaypoints(List<WaypointData> wps) //Build the drone route.
    {
        SetDroneTarget[] targets = new SetDroneTarget[wps.Count];

        for (int k = 0; k < wps.Count; k++)
        {
            WaypointData w = wps[k];
            targets[k] = new SetDroneTarget
            {
                position     = new Vector3(w.x, w.y, w.z),
                orientation  = 0, 
                cruiseSpeed  = w.cruiseSpeed,
                climbSpeed   = w.climbSpeed,
                descentSpeed = w.descentSpeed
            };
        }

        return targets;
    }

    MissionStepType IdToStepType(int id)
    {
        switch (id) // All mission stlyes possibilities.
        {
            case 1:  return MissionStepType.Cruise;
            case 2:  return MissionStepType.Takeoff;
            case 3:  return MissionStepType.Landing;
            case 4:  return MissionStepType.PickingUpPackage;
            case 5:  return MissionStepType.DeliveringPackage;
            default: return MissionStepType.Idle;
        }
    }
}