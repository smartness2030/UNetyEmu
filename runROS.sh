#!/usr/bin/env bash

# Exit immediately if a command exits with a non-zero status
set -e

# Get the absolute path of the project
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Enter the ROS 2 workspace
echo "Entering ros2_ws..."
cd "$ROOT_DIR/ros2_ws"

# Launch the ROS 2 nodes
echo "Launching ROS2 connection..."
./connect.sh
