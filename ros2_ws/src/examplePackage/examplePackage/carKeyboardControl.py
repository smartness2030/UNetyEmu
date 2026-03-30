# ------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ------------------------------------------------------

# Libraries
import rclpy
from rclpy.node import Node
from std_msgs.msg import Float32MultiArray
import sys
import threading
import readchar
import time

# Node to control a car using the keyboard. Publishes throttle, brake and steering commands
class CarKeyboardControl(Node):
    def __init__(self, carId):
        super().__init__("carKeyboardControl")

        self.topicName = f"{carId}_keyboardInput"
        self.publisher = self.create_publisher(Float32MultiArray, self.topicName, 10)

        # Current state
        self.throttle = 0.0
        self.steering = 0.0
        self.brake = 0.0

        # Targets
        self.targetThrottle = 0.0
        self.targetSteering = 0.0

        # Parameters
        self.throttleSmooth = 5.0
        self.steeringSmooth = 5.0
        self.step = 0.1  # Incremental throttle step

        self.keys_held = set()
        self.running = True

        # Thread keyboard input
        self.key_thread = threading.Thread(target=self.key_loop, daemon=True)
        self.key_thread.start()

        self.create_timer(0.02, self.update)
        self.last_time = time.time()

        # Initial instructions
        self.get_logger().info(
            "↑↓ = Throttle | ←→ = Steering | SPACE = Brake | Q = Exit"
        )

    def key_loop(self):
        KEY_MAP = {
            readchar.key.UP: "forward",
            readchar.key.DOWN: "backward",
            readchar.key.RIGHT: "right",
            readchar.key.LEFT: "left",
            readchar.key.SPACE: "brake",
            "q": "quit",
        }

        while self.running and rclpy.ok():
            key = readchar.readkey()
            action = KEY_MAP.get(key)

            if action == "quit":
                self.running = False
                rclpy.shutdown()
                break

            elif action:
                self.keys_held.add(action)
                threading.Thread(target=self._auto_release, args=(action,), daemon=True).start()

    def _auto_release(self, action):
        time.sleep(0.6)
        self.keys_held.discard(action)

    def update(self):
        now = time.time()
        dt = now - self.last_time
        self.last_time = now

        self._update_targets()
        self._apply_smoothing(dt)
        self._publish()

    def _update_targets(self):
        
        if "forward" in self.keys_held:
            if self.targetThrottle < 0:
                self.targetThrottle = 0.0
            self.targetThrottle += self.step
        elif "backward" in self.keys_held:
            if self.targetThrottle > 0:
                self.targetThrottle = 0.0
            self.targetThrottle -= self.step

        self.targetThrottle = max(-1.0, min(1.0, self.targetThrottle))

        if "right" in self.keys_held:
            self.targetSteering = 0.5
        elif "left" in self.keys_held:
            self.targetSteering = -0.5
        else:
            self.targetSteering = 0.0

        if "brake" in self.keys_held:
            self.brake = 1.0
            self.throttle = 0.0
            self.steering = 0.0
            self.targetThrottle = 0.0
            self.targetSteering = 0.0
        else:
            self.brake = 0.0

    def _lerp(self, current, target, t):
        t = min(1.0, t)
        return current + (target - current) * t

    def _apply_smoothing(self, dt):
        self.throttle = self._lerp(self.throttle, self.targetThrottle, dt * self.throttleSmooth)
        self.throttle = max(-1.0, min(1.0, self.throttle))

        if self.targetSteering != 0.0:
            self.steering += (self.targetSteering - self.steering) * dt * self.steeringSmooth
        else:
            self.steering -= self.steering * dt * self.steeringSmooth

        self.steering = max(-1.0, min(1.0, self.steering))

    def _publish(self):
        msg = Float32MultiArray()
        msg.data = [self.throttle, self.brake, self.steering]
        self.publisher.publish(msg)

def main():
    car_id = sys.argv[1] if len(sys.argv) > 1 else "car001"
    rclpy.init()
    node = CarKeyboardControl(car_id)
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()

if __name__ == "__main__":
    main()
