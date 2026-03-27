# ------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ------------------------------------------------------



#Libraries
import rclpy
from rclpy.node import Node
from std_msgs.msg import Float32MultiArray
import sys
import threading
import readchar

class CarKeyboardControl(Node):
    def __init__(self, carId):
        super().__init__("carKeyboardControl")
        self.topicName = f"{carId}_keyboardInput"
        self.publisher = self.create_publisher(Float32MultiArray, self.topicName, 10)
        self.outputCommand = [0.0, 0.0, 0.0]  
        self.step = 0.5

        self.keyboard_thread = threading.Thread(target=self.keyboard_loop, daemon=True)
        self.keyboard_thread.start()
        self.get_logger().info(f"Publicando em '{self.topicName}' | Setas = direção, Espaço = freio, Q = sair")
    
    # Function to receive keyboard commmands.
    def keyboard_loop(self):
        KEY_MAP = {
            readchar.key.UP:    ("throttle", +1),
            readchar.key.DOWN:  ("throttle", -1),
            readchar.key.RIGHT: ("steering", +1),
            readchar.key.LEFT:  ("steering", -1),
            readchar.key.SPACE: ("brake",     0),
            "q":                ("quit",       0),
        }

        while rclpy.ok():
            key = readchar.readkey()

            action = KEY_MAP.get(key)
            if action is None:
                continue

            field, direction = action

            if field == "quit":
                self.get_logger().info("Encerrando...")
                rclpy.shutdown()
                break
            elif field == "throttle":
                self.outputCommand[0] = max(-1.0, min(1.0, self.outputCommand[0] + self.step * direction))
            elif field == "brake":
                self.outputCommand[1] = 0.0 if self.outputCommand[1] == 1.0 else 1.0
            elif field == "steering":
                self.outputCommand[2] = max(-1.0, min(1.0, self.outputCommand[2] + self.step * direction))

            self.publishCommand()
    
    # Publish command to Unity.
    def publishCommand(self):
        msg = Float32MultiArray()
        msg.data = list(self.outputCommand)
        self.publisher.publish(msg)

        direction_str = "FRENTE" if self.outputCommand[0] > 0 else ("RÉ" if self.outputCommand[0] < 0 else "PARADO")
        self.get_logger().info(
            f"{direction_str} | Throttle={self.outputCommand[0]:.1f} | Brake={self.outputCommand[1]:.0f} | Steer={self.outputCommand[2]:.1f}"
        )


def main():
    car_id = sys.argv[1] if len(sys.argv) > 1 else "car001"
    rclpy.init()
    node = CarKeyboardControl(car_id)
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()