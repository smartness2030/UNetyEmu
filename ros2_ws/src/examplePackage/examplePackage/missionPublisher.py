# ------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ------------------------------------------------------



# Libraries
import rclpy
from rclpy.node import Node
from std_msgs.msg import String
import json
import os
import sys
from ament_index_python.packages import get_package_share_directory

class MissionPublisher(Node):
    def __init__(self, drone_id):
        super().__init__('mission_publisher')
        self.drone_id = drone_id

        self.topic = f"{self.drone_id}_Missionreceiver"
        self.pub = self.create_publisher(String, self.topic, 10)

    # Function to send de JSON vile via ROS2.
    def send_mission(self, mission: dict):
        msg = String()
        msg.data = json.dumps(mission)
        self.pub.publish(msg)
        self.get_logger().info(f"Topic sent: {self.topic}")
        self.get_logger().info(f"Mission sended to: '{self.drone_id}'!")

def main():
    rclpy.init()

    drone_id = sys.argv[1] if len(sys.argv) > 1 else "drone001"

    package_share_dir = get_package_share_directory('examplePackage')
    mission_path = os.path.join(package_share_dir,'missions','mission.json') # Mission file.
    with open(mission_path, "r") as f:
        mission = json.load(f)

    node = MissionPublisher(drone_id)
    import time
    time.sleep(1.0)

    node.send_mission(mission)

    rclpy.shutdown()

if __name__ == '__main__':
    main()