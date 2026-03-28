# UNetyEmuROS

> **Paper:** "UNetyEmuROS: A Unity-Based Multi-Vehicle Simulator with Physically-Grounded Dynamics and ROS2 Sensor Integration"  
> **Venue:** SBRC 2026 — Salão de Ferramentas

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![ROS2](https://img.shields.io/badge/ROS2-Humble-green.svg)](https://docs.ros.org/en/humble/)
[![Unity](https://img.shields.io/badge/Unity-2022.3.62f2-black.svg)](https://unity.com/es/releases/editor/whats-new/2022.3.62f2)
[![Platform](https://img.shields.io/badge/Platform-Linux-orange.svg)]()

We present UNetyEmuROS, a Unity-based multi-vehicle simulator that extends our previous work [UNetyEmu](https://github.com/intrig-unicamp/UNetyEmu/tree/sbrc25) with two key contributions: (i) a physically-grounded dynamics engine featuring per-motor forces, cascaded PID attitude control, and actuator disk energy modeling, where picking up a package physically alters thrust demand, inertia, and battery drain as emergent behavior; and (ii) a modular ROS2 sensor bridge publishing 360-LiDAR, RGB and depth camera, IMU, and GPS as standard sensor msgs topics, each independently attachable to any vehicle. We validate both contributions in an urban scenario with heterogeneous drones and ground vehicles operating concurrently on package delivery tasks, object detection, and teleoperation through ROS.

<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/yolo.gif?raw=true" height="220">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/lidar.gif?raw=true" height="220">
</p>

