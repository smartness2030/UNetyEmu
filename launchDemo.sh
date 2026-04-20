#!/bin/bash

# Exit immediately if a command fails
set -e

# Configuration variables
ROS_WS="ros2_ws"
RVIZ_CONFIG="rviz/UNetyEmuROS_sensors.rviz"
UNITY_EXEC="built_up_UNetyEmuROS/smallcity1.x86_64"
TCP_PORT=10000
STARTUP_DELAY=3

# Ensure script is run from project root
if [ ! -d "$ROS_WS" ]; then
  echo "Error: This script must be run from the project root directory."
  exit 1
fi

# Terminal launcher
open_terminal() {
    local cmd="$1"
    if command -v gnome-terminal &>/dev/null && gnome-terminal --version &>/dev/null 2>&1; then
        gnome-terminal -- bash -ic "$cmd" &
    elif command -v xterm &>/dev/null; then
        xterm -e bash -ic "$cmd" &
    elif command -v x-terminal-emulator &>/dev/null; then
        x-terminal-emulator -e bash -ic "$cmd" &
    elif command -v konsole &>/dev/null; then
        konsole -e bash -ic "$cmd" &
    else
        echo "Error: No terminal emulator found. Install xterm: sudo apt install xterm"
        exit 1
    fi
}

# Build the ROS2 workspace
echo "\n----- Build ROS2 workspace -----"
./buildWorkspace.sh

# Wait after building the workspace
sleep $STARTUP_DELAY

# Launch processes in separate terminals
echo "\n----- Launching processes in separate terminals -----"

# Terminal 1: Launch Unity executable
open_terminal "
echo -e '\nStarting Unity executable...';
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
open_terminal "
echo '\nStarting ROS bridge...';
# Kill any process using the TCP port to avoid conflicts
fuser -k $TCP_PORT/tcp || true
# Start the ROS-Unity bridge
source /opt/ros/humble/setup.bash
source $PWD/$ROS_WS/install/setup.bash
cd $PWD
./runROS.sh;
exec bash
"

# Wait before launching RViz
sleep $STARTUP_DELAY

# Terminal 3: Launch RViz
open_terminal "
echo '\nLaunching RViz2...';
source /opt/ros/humble/setup.bash
source $PWD/$ROS_WS/install/setup.bash
cd $PWD/$ROS_WS
source install/setup.bash;
rviz2 -d ../$RVIZ_CONFIG;
exec bash
"

echo "\n----- All processes launched -----\n"
