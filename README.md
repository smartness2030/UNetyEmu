# UNetyEmuROS

> **Paper submitted:** "UNetyEmuROS: A Unity-Based Multi-Vehicle Simulator with Physically-Grounded Dynamics and ROS2 Sensor Integration"  
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
UNetyEmu-main/
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
├── launchDemo.sh                                # Launches the complete demo
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
- **`gnome-terminal`** must be available, as `launchDemo.sh` uses it to open separate terminal windows.

### Hardware Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 4 cores | 8 cores |
| RAM | 8 GB | 16 GB |
| GPU | Integrated | Dedicated GPU (NVIDIA or AMD) |
| Disk | 5 GB free | 10 GB free |

> **Note:** A dedicated GPU is recommended for smooth rendering of the Unity scene with multiple active vehicles and sensors.


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
| pip | 22.0.0+ |

### Required (for displaying detected objects and teleoperation via keyboard)

| Dependency | Version |
|------------|---------|
| numpy | 1.26.4 |
| opencv-python | 4.8.1.78 |
| ultralytics (YOLOv8) | 8.4.36 |
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

Follow the [ROS2 Installation Guide](https://github.com/intrig-unicamp/UNetyEmu/wiki) with step-by-step instructions.


### Step 2 — Make sure you have gnome-terminal and pip installed

```bash
sudo apt update
sudo apt install gnome-terminal python3-pip
```


### Step 3 — Install Python libraries

This will install all dependencies with their required versions:

```bash
pip install numpy==1.26.4
pip install opencv-python==4.8.1.78 --no-deps
pip install ultralytics==8.4.36 --no-deps
pip install readchar
```

> **Note:** If, during installation, you see warnings that Ubuntu couldn't find the PATH to the folder where pip placed the executables, add that folder to the PATH:
> ```bash
> echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
> source ~/.bashrc
> ```


### Step 4 — Clone this repository

```bash
git clone https://github.com/intrig-unicamp/UNetyEmu.git
cd UNetyEmu
```

or  download the [zipped project](https://github.com/intrig-unicamp/UNetyEmu/archive/refs/heads/main.zip) and navigate to the project's root folder `UNetyEmu-main/`.


The installation is complete to run the quick demo using the pre-built Unity file.



### Step 5 (optional) — Install Unity Hub and Unity Editor

To edit the scene and open the Unity project, follow the [Unity Installation Guide](https://github.com/intrig-unicamp/UNetyEmu/wiki) with step-by-step instructions.




# Minimum Test

To run the quick demo, make sure you're in the project root directory `UNetyEmu`, and execute the following: 

```bash
./launchDemo.sh
```

This bash script will build the ROS 2 workspace and open 3 terminals:

- **Terminal 1** — Download and launch the Unity executable
- **Terminal 2** — Run ROS-Unity bridge
- **Terminal 3** — Launch RViz

In summary, the connection between ROS and Unity has been established, and you will see both the Unity scene window and the rviz window, where you can view the sensor output.

<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/demoLaunched.png" height="500" alt="demoLaunched.png">
</p>



# Experiments

In this demonstration, you can perform 4 different experiments in which various drones and cars interact within the same scene. 

First, add ROS2 to your `.bashrc` so that RViz2 work in any terminal:

```bash
echo "source /opt/ros/humble/setup.bash" >> ~/.bashrc
source ~/.bashrc
```


### 1. Autonomous delivery with 360-LiDAR: Sending a mission to drone001 and check the response from the Lidar sensor

Open a new terminal. Go to the root folder of our project `UNetyEmu`, and run the following:

```bash
cd ros2_ws
source install/setup.bash
ros2 run examplePackage missionPublisher drone001Lidar
```

This commands will allow you to send a list of steps to follow (pick up the package, take off, cruise mode, etc.) so that `drone001` can autonomously deliver a package. 

In addition, in RViz2 you can see the point cloud updating in real-time as the drone makes the delivery.

> **IMPORTANT NOTE:** This command will behave as expected if it is executed **ONLY ONCE** at any point during the simulation. For a better understanding of how the drone executes this mission, please refer to our [documentation](https://github.com/intrig-unicamp/UNetyEmu/wiki).



### 2. Depth camera and position control: Set target position and orientation to the drone002

To view `drone002`, click within the open Unity scene and then click the menu button in the bottom right corner. A list of all available keyboard commands will appear. 

Switch the view in Unity until `dron002` appears. For that, press the `C` key once.

Open a new terminal. Go to the root folder of our project `UNetyEmu`, and run the following:

```bash
cd ros2_ws
source install/setup.bash
ros2 run examplePackage waypointPublisher drone002Camera
```

This terminal will stay open to send new target positions to `drone002`. For example, an input of `5 10 5 90 3` will send a command to `drone002` to fly to the position longitude `x=5`, altitude `y=10`, latitude `z=5`, with an orientation of `90 degrees` and a speed of `3 m/s`.

> **Note:** Follow our [documentation](https://github.com/intrig-unicamp/UNetyEmu/wiki) for a better understanding of how to view the depth camera output of `drone002` on `RViz2`.




### 3. RGB camera, object detection, and teleoperated: Remote Control and object detection with the drone003

Switch the view in Unity until `dron003` appears (pressing the `C` or `V` key).

For this last experiment, you will need to open two terminals. 

In the first terminal, go to the root folder of our project `UNetyEmu`, and run the following:

```bash
cd ros2_ws
source install/setup.bash
ros2 run examplePackage droneKeyboardControl drone003Camera
```
This will allow you to enable remote control of `drone003`. Be sure to check the available control keys, which can be found in the Unity scene menu.

Then, in the second terminal, go to the root folder of our project `UNetyEmu`, and run the following:


```bash
cd ros2_ws
source install/setup.bash
ros2 run examplePackage yolo_detector drone003Camera
```

This will allow you to run and observe object detection in real-time using a YOLOv8 node trained to detect trees and ground vehicles.

> **Note:** Since this is a demonstration, this script will run the YOLO node using only the CPU to avoid compatibility issues between the GPU and PyTorch/CUDA.



### 4. Ground vehicles: Teleoperation of car001

Switch the view in Unity until `car001` appears (pressing the `C` or `V` key).

Open a new terminal. Go to the root folder of our project `UNetyEmu`, and run the following:

```bash
cd ros2_ws
source install/setup.bash
ros2 run examplePackage carKeyboardControl car001
```

The terminal will be enabled to accept keyboard input and allow you to remotely control `car001`, which is currently in the scene. 

> **Note:** Follow our [documentation](https://github.com/intrig-unicamp/UNetyEmu/wiki) for a better understanding of how to use the keys to properly control the car.



# LICENSE

Apache License
Version 2.0, January 2004
[http://www.apache.org/licenses/](http://www.apache.org/licenses/)
