# ----------------------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ----------------------------------------------------------------------

# Libraries
import rclpy  # ROS 2 client library
from rclpy.node import Node  # Base class for ROS 2 nodes
from std_msgs.msg import String  # JSON payload as text for Unity MissionReceiver
import json  # Serialize mission dict to JsonUtility-compatible text
import os  # Mission file path construction
import sys  # Command-line arguments
from ament_index_python.packages import get_package_share_directory  # Resolve share path for missions

# ----------------------------------------------------------------------

# Node to publish a JSON mission file to a drone via ROS2 topic
class MissionPublisher(Node):
    def __init__(self, drone_id):
        super().__init__('mission_publisher')
        self.drone_id = drone_id

        # Topic matches Unity MissionReceiver (drone_id + "_Missionreceiver")
        self.topic = f"{self.drone_id}_Missionreceiver"
        self.pub = self.create_publisher(String, self.topic, 10)

    # Function to publish one String message containing the full mission JSON
    def send_mission(self, mission: dict):
        msg = String()
        msg.data = json.dumps(mission)
        self.pub.publish(msg)
        self.get_logger().info(f"Topic sent: {self.topic}")
        self.get_logger().info(f"Mission sended to: '{self.drone_id}'!")

# ----------------------------------------------------------------------

# Main routine to run the mission publisher node
def main():
    rclpy.init()

    # Allow drone ID to be passed as an argument, with default "drone001"
    drone_id = sys.argv[1] if len(sys.argv) > 1 else "drone001"

    # Load the default mission definition shipped with the package
    package_share_dir = get_package_share_directory('examplePackage')
    mission_path = os.path.join(package_share_dir,'missions','mission.json') # Mission file
    with open(mission_path, "r") as f:
        mission = json.load(f)

    node = MissionPublisher(drone_id)
    import time
    # Brief delay so ROS-TCP subscriber registration can complete before a single-shot publish
    time.sleep(1.0)

    node.send_mission(mission)

    rclpy.shutdown()

# ----------------------------------------------------------------------

# Main function to run the script
if __name__ == '__main__':
    main()
