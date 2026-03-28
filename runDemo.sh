#!/bin/bash

# Exit immediately if a command fails
set -e

# Configuration variables
ROS_WS="ros2_ws"
RVIZ_CONFIG="rviz/UNetyEmuROS_sensors.rviz"
UNITY_EXEC="built_up_UNetyEmuROS/smallcity1.x86_64"

# Startup delay times (in seconds)
STARTUP_DELAY=3

# Ensure script is run from project root
if [ ! -d "ros2_ws" ]; then
  echo "Error: This script must be run from the project root directory."
  exit 1
fi

# Build the ROS2 workspace
echo "\n----- Build ROS2 workspace -----"
./buildWorkspace.sh

# Wait after building the workspace
sleep $STARTUP_DELAY

# Launch processes in separate terminals
echo "\n----- Launching processes in separate terminals -----"

# Terminal 1: Launch Unity executable
gnome-terminal -- bash -c "
echo '\nStarting Unity executable...';
python3 loadUNetyEmu.py;
exec bash
"

# Wait for Unity executable to start
echo "\nWaiting for Unity process..."

# Wait until the Unity process is running
while ! pgrep -f $UNITY_EXEC > /dev/null; do
    sleep 1
done

# Additional wait time to ensure Unity is fully initialized
echo "\nUnity started. Waiting extra time to launch RViz..."

# Wait after Unity starts
sleep $STARTUP_DELAY

# Terminal 2: Run ROS-Unity bridge
gnome-terminal -- bash -c "
echo '\nStarting ROS bridge...';
./runROS.sh;
exec bash
"

# Wait before launching RViz
sleep $STARTUP_DELAY

# Terminal 3: Launch RViz
gnome-terminal -- bash -c "
cd $ROS_WS;
echo '\nLaunching RViz...';
source install/setup.bash;
rviz2 -d ../$RVIZ_CONFIG;
exec bash
"

echo "\n----- All processes launched -----\n"
