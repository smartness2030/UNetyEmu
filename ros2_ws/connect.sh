#!/usr/bin/env bash

# Load workspace
source install/setup.bash

# Run node
ros2 run ros_tcp_endpoint default_server_endpoint
