# ----------------------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ----------------------------------------------------------------------

# Libraries
import rclpy  # ROS 2 client library
from rclpy.node import Node  # Base class for ROS 2 nodes
import threading  # Terminal input without blocking rclpy.spin
from std_msgs.msg import Float32MultiArray  # Waypoint fields as a flat float array
import sys  # Command-line arguments

# ----------------------------------------------------------------------

# Node to publish waypoints to a drone via ROS2 topic
class WaypointPublisher(Node):
    def __init__(self,droneId):
        super().__init__("waypointPublisher")
        self.droneId = droneId

        # Topic matches Unity WaypointReceiver (droneId + "_waypointReceiver")
        self.topicName = f"{droneId}_waypointReceiver"
        self.publisher = self.create_publisher(Float32MultiArray,self.topicName,10)
        self.get_logger().info("Communication started\n Insert: X | Y | Z | Orientation | Speed \n")

        self.thread = threading.Thread(target=self.inputTerminal,daemon=True)
        self.thread.start()

    # Function to read stdin lines and publish parsed waypoint tuples
    def inputTerminal(self):
        while(rclpy.ok()):
            try:
                userInput = input(">> ")
                parts = userInput.split()

                try:
                    floatValues = [float(input) for input in parts]
                    msg =Float32MultiArray()
                    msg.data = floatValues
                    self.publisher.publish(msg)
                    self.get_logger().info(f"Sending X: {floatValues[0]} | Y: {floatValues[1]} | Z: {floatValues[2]} | Orientation {floatValues[3]} | Speed: {floatValues[4]}")

                except ValueError:
                    self.get_logger().error("Parsing error: please insert just numbers separated with spaces.")
            except EOFError:
                break

# ----------------------------------------------------------------------

# Main routine to run the waypoint publisher node
def main():
    # Allow drone ID to be passed as an argument, with default "001"
    drone_id = sys.argv[1] if len(sys.argv) > 1 else "001"
    rclpy.init()
    node = WaypointPublisher(drone_id)
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()

# ----------------------------------------------------------------------

# Main function to run the script
if __name__ == "__main__":
    main()
