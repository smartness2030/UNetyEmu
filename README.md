# UNetyEmuROS

> **Paper:** "UNetyEmuROS: A Unity-Based Multi-Vehicle Simulator with Physically-Grounded Dynamics and ROS2 Sensor Integration"  
> **Venue:** SBRC 2026 — Salão de Ferramentas

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![ROS2](https://img.shields.io/badge/ROS2-Humble-green.svg)](https://docs.ros.org/en/humble/)
[![Unity](https://img.shields.io/badge/Unity-2022.3.62f2-black.svg)](https://unity.com/es/releases/editor/whats-new/2022.3.62f2)
[![Platform](https://img.shields.io/badge/Platform-Linux-orange.svg)]()

We present UNetyEmuROS, a Unity-based multi-vehicle simulator that extends our previous work [UNetyEmu](https://github.com/intrig-unicamp/UNetyEmu/tree/sbrc25) with two key contributions: (i) a physically-grounded dynamics engine featuring per-motor forces, cascaded PID attitude control, and actuator disk energy modeling, where picking up a package physically alters thrust demand, inertia, and battery drain as emergent behavior; and (ii) a modular ROS2 sensor bridge publishing 360-LiDAR, RGB and depth camera, IMU, and GPS as standard sensor msgs topics, each independently attachable to any vehicle. We validate both contributions in an urban scenario with heterogeneous drones and ground vehicles operating concurrently on package delivery tasks, object detection, and teleoperation through ROS.

<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/yolo.gif?raw=true" height="500">
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/lidar.gif?raw=true" height="500">
</p>



# Repository structure

```
UNetyEmu/
├── UNetyEmuROS/                                  # Unity project
│   └── Assets/Scripts/
│       ├── CameraView/                           # Follow camera and HUD
│       ├── Classes/                              # PID, VectorPID, RatePD, SetLogs
│       ├── Components/
│       │   ├── Car/
│       │   │   ├── Dynamic/CarDynamics.cs
│       │   │   ├── Inputs/MoveCarKeyboard.cs     # Move vehicle from Unity
│       │   │   └── SaveLogs/SaveCarLogs.cs
│       │   └── Drone/
│       │       ├── Actuator/                     # DronePickUpPackage, DronePropellersRotation
│       │       ├── Controller/                   # DronePositionVelocityController, DroneStabilizationController
│       │       ├── Dynamic/              ← Key contribution 1
│       │       ├── EnergyConsumption/            # DroneEnergyConsumption
│       │       ├── Inputs/                       # DroneSetTarget, MoveDroneKeyboar (from Unity)
│       │       ├── Interaction/                  # AttachObject
│       │       ├── Planning/                     # DroneMissionPlanner, DroneWaypointFollower
│       │       └── SaveLogs/                     # SaveDroneLogs
│       ├── MenuInfo/                             # InfoPanelController
│       ├── ROS2/                         ← Key contribution 2
│       │   ├── Publishers/                       # LidarROS, RGBCameraROS, DepthCameraROS, GPSROS, IMUROS
│       │   └── Subscribers/                      # MissionReceiver, WaypointReceiver, MoveDroneROS (from ROS), MoveCarROS (from ROS)
│       └── SensorsConfig/                        # LinearDepthShader.shader
├── ros2_ws/
│   ├── src/
│   │   ├── ROS-TCP-Endpoint/                     # TCP bridge package (Unity ↔ ROS2)
│   │   └── examplePackage/                       # ROS2 nodes
│   │       └── examplePackage/
│   │           ├── applyYolo.py                  # YOLOv8 on RGB camera stream
│   │           ├── cameraReceiver.py             # Displays RGB camera feed
│   │           ├── carKeyboardControl.py         # Real-time teleoperation of the car
│   │           ├── droneKeyboardControl.py       # Real-time teleoperation of the drone
│   │           ├── missionPublisher.py           # Send a sequence of steps to follow during a mission
│   │           ├── waypointPublisher.py          # Send new target
│   │           └── missions/mission.json         # Define the list of steps for the mission
│   ├── config/
│   │   └── unity_ros2_visualTest.rviz
│   └── connect.sh                                # Sources setup.bash and runs ros_tcp_endpoint
├── rviz/
│   └── UNetyEmuROS_sensors.rviz                  # RViz2 sensor visualization config
├── runFullDemo.sh                                # Launches the complete demo
├── runROS.sh                                     # Starts the ROS2–Unity TCP bridge
├── buildWorkspace.sh                             # Builds the ROS2 workspace with colcon
└── loadUNetyEmu.py                               # Downloads and launches the Unity build
```



# Badges considered (Selos Considerados)

The authors of this work consider applying to the following badges: "Artefatos Disponíveis (SeloD)", "Artefatos Funcionais (SeloF)", "Artefatos Sustentáveis (SeloS)", and "Experimentos Reprodutíveis (SeloR)".



# Basic Information

To validate our contributions, we designed an urban delivery scenario that simultaneously tests the flight dynamics engine and the ROS2 sensor integration. The scene represents a simple urban environment with roads, buildings, and trees, in which 4 ground vehicles and 3 drones of different types operate simultaneously within the same Unity scene. The drones differ in size, mass, and equipped components, as summarized in the next Table. The first 3 ground vehicles are parked and remain stationary, using their dynamic components to interact with the scene. Meanwhile, the last ground vehicle, equipped with GPS and IMU sensors, is teleoperated from ROS2 using keyboard inputs.

| Vehicle | Size  | Sensor | Control | ROS2 topic |
|---------|-------|--------|---------|------------|
| **Drone 1** | Medium | 360-LiDAR | `LoadMission()` | `PointCloud2` |
| **Drone 2** | Large  | Depth Camera | `SetTarget()` | `Image (32FC1)` |
| **Drone 3** | Small  | RGB Camera | Keyboard teleop | `Image (rgb8)` |
| **Car 1**   | Medium | GPS + IMU | Keyboard teleop | `NavSatFix, Imu` |

### Operating System

- **Ubuntu 22.04 LTS (Linux x86\_64)**
- **`gnome-terminal`** must be available, as `runFullDemo.sh` uses it to open separate terminal windows.

### Hardware Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 4 cores | 8 cores |
| RAM | 8 GB | 16 GB |
| GPU | Integrated | Dedicated GPU (NVIDIA or AMD) |
| Disk | 5 GB free | 10 GB free |

> A dedicated GPU is recommended for smooth rendering of the Unity scene with multiple active vehicles and sensors.


### Execution Modes

- **Run quick demo (no Unity Editor):** uses a pre-built Linux executable downloaded automatically on first run.
- **Full project (with Unity Editor):** allows opening, modifying, and recompiling the scene. Additionally requires Unity 2022.3.62f2.


# Dependencies

### Required (quick demo)

| Dependency | Version |
|------------|---------|
| Ubuntu | 22.04 LTS |
| ROS2 | Humble |
| ROS-TCP-Connector | Included in the Unity packages |
| ROS-TCP-Endpoint | Included in the workspace `ros2_ws/src/` |
| RViz2 | Included with ROS2 Desktop |
| Python | 3.10+ |
| gnome-terminal | Any |

### Required (for displaying detected objects and teleoperation via keyboard)

| Dependency | Version |
|------------|---------|
| ultralytics (YOLOv8) | Latest |
| readchar | Latest |

### Optional (Unity Editor — to modify the project)

| Dependency | Version |
|------------|---------|
| Unity Hub | Latest |
| Unity Editor | 2022.3.62f2 |

### Internal ROS2 packages (included in the workspace `ros2_ws/src/` )

| Package | Description |
|---------|-------------|
| `examplePackage` | ROS2 nodes: keyboard control, mission publisher, waypoint publisher, camera receiver, YOLOv8 detection |



# Security concerns

The execution of this artifact is risk-free for evaluators. UNetyEmuROS uses as its core operation, documented frameworks and openly available online such as [Unity](https://unity.com/) and [ROS2](https://docs.ros.org/en/humble/index.html).



# Installation

### Step 1 — Install ROS2 Humble

Follow the complete [ROS2 Installation Guide](https://docs.ros.org/en/humble/Installation/Ubuntu-Install-Debs.html) for a clean and successful installation.


### Step 2 — Make sure you have gnome-terminal

```bash
sudo apt update
sudo apt install gnome-terminal
```


### Step 3 — Clone the repository

```bash
git clone https://github.com/intrig-unicamp/UNetyEmu.git
cd UNetyEmu
```


### Step 4 — Install Python libraries

This will automatically install the dependencies (ultralytics, numpy, readchar): 

```bash
pip install -r requirements.txt
```

The installation is complete to run the quick demo using the pre-built Unity file.



### Step 5 (optional) — Install Unity Hub and Unity Editor

To edit the scene and open the full project, refer to the [Documentation](https://github.com/intrig-unicamp/UNetyEmu/wiki) for step-by-step installation instructions.




# Minimum Test

To run the quick demo, make sure you're in the project root directory `UNetyEmu`, and enter the following. This will open several terminal windows that will be used to download the executable version of the Unity project, enable communication with ROS2, and open a saved instance of RViz2:

```bash
./runDemo.sh
```

**Build the ROS2 workspace:**

```bash
./buildWorkspace.sh
```

This runs `colcon build` inside `ros2_ws/`, compiling both the `ROS-TCP-Endpoint` bridge and the `examplePackage` nodes. 


**Terminal 1 — Download and launch the Unity executable:**

```bash
python3 loadUNetyEmu.py
```

On the first run, the build is downloaded from the [GitHub Release](https://github.com/intrig-unicamp/UNetyEmu/releases/tag/sbrc26) and extracted to `built_up_UNetyEmuROS/`. Subsequent runs skip the download.


**Terminal 2 — Run ROS-Unity bridge:**

```bash
./runROS.sh
```

This runs `source install/setup.bash` from the `ros2_ws` workspace and launches `ros_tcp_endpoint default_server_endpoint`. Wait for: `Starting ROS TCP server on 127.0.0.1:10000`


**Terminal 3 — Launch RViz:**

```bash
rviz2 -d ../rviz/UNetyEmuROS_sensors.rviz
```

This opens a previously saved RViz window, configured to display the response from the camera and lidar sensors.






# Experiments






# LICENSE

Apache License
Version 2.0, January 2004
[http://www.apache.org/licenses/](http://www.apache.org/licenses/)


