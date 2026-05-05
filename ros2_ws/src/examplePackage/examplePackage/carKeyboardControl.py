# ----------------------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ----------------------------------------------------------------------

# Libraries
import rclpy  # ROS 2 client library
from rclpy.node import Node  # Base class for ROS 2 nodes
from std_msgs.msg import Float32MultiArray  # Same command layout Unity expects for cars
import sys  # Command-line arguments
import threading  # Non-blocking keyboard reader alongside rclpy timers
import readchar  # Raw terminal key reads (arrow keys, space, etc.)
import time  # Delta time for smoothing and auto-release delays

# ----------------------------------------------------------------------

# Node to control a car using the keyboard. Publishes throttle, brake and steering commands
class CarKeyboardControl(Node):
    def __init__(self, carId):
        super().__init__("carKeyboardControl")

        # Publisher topic matches Unity MoveCarROS (carId + "_keyboardInput")
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

        # Timer-driven control loop at 50 Hz
        self.create_timer(0.02, self.update)
        self.last_time = time.time()

        # Initial instructions
        self.get_logger().info(
            "↑↓ = Throttle | ←→ = Steering | SPACE = Brake | Q = Exit"
        )

    # Function to read keys in a background thread and update keys_held
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

    # Function to clear a key after a short pulse (tap-to-step feel)
    def _auto_release(self, action):
        time.sleep(0.6)
        self.keys_held.discard(action)

    # Function to integrate inputs each timer tick and publish
    def update(self):
        now = time.time()
        dt = now - self.last_time
        self.last_time = now

        self._update_targets()
        self._apply_smoothing(dt)
        self._publish()

    # Function to map held keys to target throttle / steering / brake
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

    # Function to linearly interpolate toward a target value
    def _lerp(self, current, target, t):
        t = min(1.0, t)
        return current + (target - current) * t

    # Function to smooth throttle and steering toward their targets
    def _apply_smoothing(self, dt):
        self.throttle = self._lerp(self.throttle, self.targetThrottle, dt * self.throttleSmooth)
        self.throttle = max(-1.0, min(1.0, self.throttle))

        if self.targetSteering != 0.0:
            self.steering += (self.targetSteering - self.steering) * dt * self.steeringSmooth
        else:
            self.steering -= self.steering * dt * self.steeringSmooth

        self.steering = max(-1.0, min(1.0, self.steering))

    # Function to publish [throttle, brake, steering] to Unity
    def _publish(self):
        msg = Float32MultiArray()
        msg.data = [self.throttle, self.brake, self.steering]
        self.publisher.publish(msg)

# ----------------------------------------------------------------------

# Main routine to run the car keyboard control node
def main():
    # Allow car ID to be passed as an argument, with default "car001"
    car_id = sys.argv[1] if len(sys.argv) > 1 else "car001"
    rclpy.init()
    node = CarKeyboardControl(car_id)
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()

# ----------------------------------------------------------------------

# Main function to run the script
if __name__ == "__main__":
    main()
