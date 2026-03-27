# ------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ------------------------------------------------------

#Libraries
import rclpy
from rclpy.node import Node
import threading
from std_msgs.msg import Float32MultiArray
import sys

class WaypointPublisher(Node):
    def __init__(self,droneId):
        super().__init__("waypointPublisher")
        self.droneId = droneId
        self.topicName = f"{droneId}_waypointReceiver"
        self.publisher = self.create_publisher(Float32MultiArray,self.topicName,10)
        self.get_logger().info("Communication started\n Insert: X | Y | Z | Orientation | Speed \n")

        self.thread = threading.Thread(target=self.inputTerminal,daemon=True)
        self.thread.start()

    # Another thread to read the terminal while running ROS2 communication.    
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


def main():
    drone_id = sys.argv[1] if len(sys.argv) > 1 else "001"
    rclpy.init()
    node = WaypointPublisher(drone_id)
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()



if __name__ == "__main__":
    main()

