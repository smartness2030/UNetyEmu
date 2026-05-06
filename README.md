# UNetyEmuROS: A Unity-Based Multi-Vehicle Simulator with Physically-Grounded Dynamics and ROS2 Sensor Integration

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![ROS2](https://img.shields.io/badge/ROS2-Humble-green.svg)](https://docs.ros.org/en/humble/)
[![Unity](https://img.shields.io/badge/Unity-2022.3.62f2-black.svg)](https://unity.com/es/releases/editor/whats-new/2022.3.62f2)
[![Platform](https://img.shields.io/badge/Platform-Linux-orange.svg)]()
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/intrig-unicamp/UNetyEmu)

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
├── launchDemo.sh                                 # Launches the complete demo
├── runROS.sh                                     # Starts the ROS2–Unity TCP bridge
├── buildWorkspace.sh                             # Builds the ROS2 workspace with colcon
├── loadUNetyEmu.py                               # Downloads and launches the Unity build for Linux systems
└── loadUNetyEmu_windows.py                       # (NEW) Downloads and launches the Unity build for Windows systems
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

### Required — Quick Demo (pre-built Unity executable)

| Dependency | Version |
|------------|---------|
| Ubuntu | 22.04 LTS |
| ROS2 | Humble |
| ROS-TCP-Connector | Included in the Unity packages |
| ROS-TCP-Endpoint | Included in the workspace `ros2_ws/src/` |
| RViz2 | Included with ROS2 Desktop |
| Python | 3.10+ |
| gnome-terminal + xterm | Any |
| pip | 22.0.0+ |

### Python packages — Simulator core

| Package | Version | 
|---------|--------|
| numpy | 1.26.4 | 
| opencv-python | 4.8.1.78 |
| readchar | Latest |

### Other Python packages — For the demo with object detection using YOLO

| Package | Version |
|---------|--------|
| torch | 2.11.0+cpu | 
| torchvision | 0.26.0+cpu |
| ultralytics | 8.4.36 |
| polars | Latest |
| ultralytics-thop | Latest |

### Internal ROS2 packages (included in the workspace `ros2_ws/src/` )

| Package | Description |
|---------|-------------|
| `examplePackage` | ROS2 nodes: keyboard control, mission publisher, waypoint publisher, camera receiver, YOLOv8 detection |

### Optional — Unity Editor (to modify the scene settings)

| Dependency | Version |
|------------|---------|
| Unity Hub | Latest |
| Unity Editor | 2022.3.62f2 |



# Security concerns

The execution of this artifact is risk-free for evaluators. UNetyEmuROS uses as its core operation, documented frameworks and openly available online such as [Unity](https://unity.com/) and [ROS2](https://docs.ros.org/en/humble/index.html).







# Installation

## Option A — Preconfigured Virtual Machine (VirtualBox)

For your convenience, we provide a preconfigured Virtual Machine (VM) image `.ova` that already includes all the dependencies needed to run our simulator.

Also, to avoid conflicts with the 3D acceleration that Unity requires to display the simulation correctly within a VM, we have chosen to run Unity directly on the host PC either on Linux or Windows (no need to install dependencies) and run ROS2 from the VM. 


### System Requirements for the host PC

To run the virtual machine, you need a host PC (either Linux or Windows) with sufficient resources to run Unity on the host PC and ROS2 on the virtual machine. We recommend the following **minimum** specifications:

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| RAM | 12 GB | 16 GB or more |
| CPU | 4 cores | 6 cores or more |
| Disk Space | 40 GB free | 60 GB free (Full version) |
| GPU | Not required | Dedicated GPU for better Unity performance |


In addition, to run Unity executable files, you only need **Python 3.9+** on the host PC. There is no need to install any dependencies; simply clone our repository on the host PC and download our `.ova` image to use it in VirtualBox.

> **Note:** Allocating fewer resources than recommended may result in lower performance or instability during simulation. If your machine does not have enough available resources, we recommend installing the simulator directly on your host computer instead.


### Available Image

**Full Project** — ROS2 + Unity Editor included (estimated size: 23 GB)  
https://drive.google.com/file/d/13UP_8Rpum-1CtK6TanjSYZfj_YwJsiSM/view?usp=sharing

Once you've downloaded the VM in `.zip` format from the link above, we recommend unzipping it and keeping only the `.ova` file. That is, delete the download file to avoid wasting space on your computer.


### VirtualBox

This image was exported using **VirtualBox 7.2.6**. To ensure a successful import, download **VirtualBox 7.2.6 or newer** for your platform (Windows or Linux) from the official website:
https://www.virtualbox.org/wiki/Downloads

Make sure you not only install VirtualBox but also include the **VirtualBox Extension Pack**, which is compatible with your version of **VirtualBox**. This extension pack is also available on the official website linked above.


#### Instructions

1. Once VirtualBox and its extension pack have been installed correctly, open it and go to:

`File → Import Appliance`

Then select the downloaded `.ova` file and click **Finish** to start the import.

2. Once the VM has finished importing, go to:

`File → Tools → Network`

Make sure you have a virtual network adapter for **“Host-only Networks”** available. If you don't have one, just click the **Create** button to add one.

3. Click on the **UNetyEmuROS** VM and go to the network settings: 

`Settings / Network`

Make sure that **“Host-only Adapter”** is selected under `Attached to` for Adapter 1. Then select the `Name` that is available in your VirtualBox (it should show the adapter that was created in the previous step). Then, in `Promiscuous Mode`, select **“Allow All”**, and **refresh** the `MAC Address` to ensure a new one is generated when you open the VM.

4. Finally, open the VM and log in using the password:

```text
unetyemuros
```

Inside the virtual machine, the project is located at:

```bash
/home/unetyemuros/git/UNetyEmu
```

Or just open a terminal and run:

```bash
cd git/UNetyEmu/
```




### Clone the repository on the host PC

From any location on the host computer, clone our repository:

```bash
git clone https://github.com/intrig-unicamp/UNetyEmu.git
cd UNetyEmu
```

or  download the [zipped project](https://github.com/intrig-unicamp/UNetyEmu/archive/refs/heads/main.zip), unzip it, and navigate to the project's root folder `UNetyEmu-main/`.



The installation is now complete to run the quick demo. 





## Option B — Native Installation

### Step 1 — Install ROS2 Humble

Follow our documentation in the [Installation](https://github.com/intrig-unicamp/UNetyEmu/wiki/SBRC26-Home) section, with step-by-step instructions.


### Step 2 — Make sure you have gnome-terminal, xterm, and pip installed

```bash
sudo apt update
sudo apt install gnome-terminal xterm python3-pip
```


### Step 3 — Install Python libraries

This will install all dependencies with their required versions.

- Simulator core:

```bash
pip install numpy==1.26.4
pip install opencv-python==4.8.1.78 --no-deps
pip install readchar
```

- For the demo with object detection using YOLO:

```bash
pip install torch torchvision --index-url https://download.pytorch.org/whl/cpu
pip install ultralytics==8.4.36 --no-deps
pip install polars ultralytics-thop
```

> **Note:** `ultralytics` is installed with `--no-deps` to avoid version conflicts with `numpy` and `opencv`. `torch` and its companions are installed separately for the same reason. In addition, if you see a warning that the install path is not in `PATH`:
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


The installation is now complete to run the quick demo using the pre-built Unity file.



### Step 5 (optional) — Install Unity Hub and Unity Editor

To edit the scene and open the Unity project, follow our documentation in the [Installation](https://github.com/intrig-unicamp/UNetyEmu/wiki/SBRC26-Home) section, with step-by-step instructions.

Then check out our [Basic-Information](https://github.com/intrig-unicamp/UNetyEmu/wiki/SBRC26-Basic-Information) section in the documentation, to better understand how to run the experiments using the scene in the Unity Editor.





# Minimum Test

## Option A — If you chose the Preconfigured Virtual Machine Installation

To run the quick demo, keep in mind that you will be launching a Unity application on the host PC while ROS2 is running on the VM. To do this, follow these steps in order:


### In the Virtual Machine

1. Start up the VM and log in. Open a new terminal and run the following:

```bash
hostname -I
```

This command will display the IP address to which the VM is connected within the host PC's network. 

Copy this IP address to use it from the host computer.

> **Note:** If no IP address appears, make sure the VM’s network settings are configured correctly using the **“Host-only Adapter”** with the name available in your VirtualBox. If you still don’t see the IP address, shut down the VM and change the network adapter to **“Bridge Adapter”**. Start the VM and verify that you can browse the internet. Then, try again to obtain the VM's IP address.


### In the host PC

2. Open a new terminal, go to the project root directory `UNetyEmu`, and run the following (replace `<IP_ADDRESS>` with the IP address you obtained from the VM):

**For Linux host PCs:**
```bash
python3 loadUNetyEmu.py --ip <IP_ADDRESS>
```
> **Example:** python3 loadUNetyEmu.py --ip 192.168.56.109

**For Windows host PCs:**
```bash
python loadUNetyEmu_windows.py --ip <IP_ADDRESS>
```
> **Example:** python loadUNetyEmu_windows.py --ip 192.168.56.105

This will open a window displaying our **Unity demo scene**. Note that in the upper-left corner, there are **RED** bidirectional arrows, right before **“ROS2 IP”**. Following that, you’ll see the **IP** address that Unity expects to use to connect to the VM (which runs ROS2) via port **10000**.

> **Note:** On the first run, the build is downloaded from the [GitHub Release](https://github.com/intrig-unicamp/UNetyEmu/releases/tag/sbrc26-release) and extracted to `built_up_UNetyEmuROS/` or `built_up_Windows/`, respectively. 


### In the Virtual Machine

3. Open a new terminal, go to the project root directory `UNetyEmu`, and build the ROS2 workspace:

```bash
./buildWorkspace.sh
```

4. After that, start the ROS-TCP-Endpoint by executing the following:

```bash
fuser -k 10000/tcp || true
source /opt/ros/humble/setup.bash
source $HOME/git/UNetyEmu/ros2_ws/install/setup.bash
cd $HOME/git/UNetyEmu
./runROS.sh
```

At this point, you will notice that, in the **Unity** window on the **host PC**, the bidirectional arrows have turned **BLUE**. This means that the connection has been established successfully and that both Unity and ROS2 can exchange messages.


5. Finally, open a new terminal and Start RViz2:

```bash
source /opt/ros/humble/setup.bash
source $HOME/git/UNetyEmu/ros2_ws/install/setup.bash
rviz2 -d $HOME/git/UNetyEmu/rviz/UNetyEmuROS_sensors.rviz
```

In summary, on the host PC, you’ll see the drones and cars available in this demo (you can switch views from there), while on the VM side, you’ll be viewing the sensor output data arriving via ROS2 and displayed in RViz2. Additionally, from the VM, you’ll be able to execute all the commands for the next experiments.

<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/demoLaunchedVM.png" height="500" alt="demoLaunchedVM.png">
</p>






## Option B — If you chose the Native Installation

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

For more details, please refer to the relevant section in our documentation: [Minimum Test](https://github.com/intrig-unicamp/UNetyEmu/wiki/SBRC26-Minimum-Test).





# Experiments

In this demonstration, you can perform 4 different experiments in which various drones and cars interact within the same scene. 

> **Note:** If you chose to use the virtual machine, remember that all of the following commands must be run inside the virtual machine, while you can watch what’s happening with the drones and cars on the host PC.

Before you start, if this is the first time you're running the simulator, add ROS2 to your `.bashrc` so that RViz2 work in any terminal:

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

> **IMPORTANT NOTE:** This command will behave as expected if it is executed **ONLY ONCE** at any point during the simulation. For a better understanding of how the drone executes this mission, please refer to our [documentation](https://github.com/intrig-unicamp/UNetyEmu/wiki/SBRC26-Home).



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

> **Note:** Follow our [documentation](https://github.com/intrig-unicamp/UNetyEmu/wiki/SBRC26-Home) for a better understanding of how to view the depth camera output of `drone002` on `RViz2`.




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

> **Note:** Follow our [documentation](https://github.com/intrig-unicamp/UNetyEmu/wiki/SBRC26-Home) for a better understanding of how to use the keys to properly control the car.



# LICENSE

Apache License
Version 2.0, January 2004
[http://www.apache.org/licenses/](http://www.apache.org/licenses/)
