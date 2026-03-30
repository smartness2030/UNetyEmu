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

# Node to control a drone using the keyboard. Publishes throttle, pitch, roll and yaw commands
class DroneKeyboardControl(Node):
    def __init__(self, droneId):
        super().__init__("droneKeyboardControl")
        self.topicName = f"{droneId}_keyboardInput"
        self.publisher = self.create_publisher(Float32MultiArray, self.topicName, 10)

        self.throttle = 0.0
        self.pitch    = 0.0
        self.roll     = 0.0
        self.yaw      = 0.0

        self.throttleRampRate   = 3.0
        self.inputRampUpRate    = 4.0
        self.inputRampDownRate  = 6.0
        self.slowReturnMult     = 2.0
        self.keys_held = set()
        self.running = True

        self.key_thread = threading.Thread(target=self.key_loop, daemon=True)
        self.key_thread.start()

        self.create_timer(0.02, self.update)
        self.last_time = time.time()

        self.get_logger().info(
            f"  T/G = Throttle ↑↓ | I/K = Pitch | J/L = Roll | F/H = Yaw | Q = Exit"
        )

    # Receive keyboard commands
    def key_loop(self):
        KEY_MAP = {
            "t": "throttle_up",   "g": "throttle_down",
            "i": "pitch_fwd",     "k": "pitch_bwd",
            "l": "roll_right",    "j": "roll_left",
            "h": "yaw_right",     "f": "yaw_left",
            "q": "quit",
        }

        while self.running and rclpy.ok():
            key = readchar.readkey().lower()
            action = KEY_MAP.get(key)
            if action == "quit":
                self.running = False
                rclpy.shutdown()
                break
            elif action:
                self.keys_held.add(action)

                threading.Thread(
                    target=self._auto_release, args=(action,), daemon=True
                ).start()
    
    #Remove the pressed key to improve control UI
    def _auto_release(self, action):
        time.sleep(0.4)
        self.keys_held.discard(action)

    def update(self):
        now = time.time()
        dt = now - self.last_time
        self.last_time = now

        self._update_throttle(dt)
        self._update_spring_axis(dt)
        self._publish()

    def _clamp(self, value, mn=-3, mx=3.0):
        return max(mn, min(mx, value))

    def _move_towards(self, current, target, delta):
        if abs(target - current) <= delta:
            return target
        return current + delta if current < target else current - delta

    def _update_throttle(self, dt):
        up   = "throttle_up"   in self.keys_held
        down = "throttle_down" in self.keys_held

        if up and not down:
            self.throttle += self.throttleRampRate * dt
        elif down and not up:
            self.throttle -= self.throttleRampRate * dt
        else:
            self.throttle = self._move_towards(
                self.throttle, 0.0,
                self.throttleRampRate * self.slowReturnMult * dt
            )

        self.throttle = self._clamp(self.throttle)

    def _update_spring_axis(self, dt):
        self.pitch = self._spring(self.pitch, "pitch_fwd",   "pitch_bwd",   dt)
        self.roll  = self._spring(self.roll,  "roll_right",  "roll_left",   dt)
        self.yaw   = self._spring(self.yaw,   "yaw_right",   "yaw_left",    dt)

    def _spring(self, value, pos_key, neg_key, dt):
        pos = pos_key in self.keys_held
        neg = neg_key in self.keys_held

        if pos and not neg:
            value = self._move_towards(value,  1.0, self.inputRampUpRate * dt)
        elif neg and not pos:
            value = self._move_towards(value, -1.0, self.inputRampUpRate * dt)
        else:
            value = self._move_towards(value,  0.0, self.inputRampDownRate * dt)

        return self._clamp(value)

    def _publish(self):
        msg = Float32MultiArray()
        msg.data = [self.throttle, self.pitch, self.roll, self.yaw]
        self.publisher.publish(msg)

def main():
    
    # Allow drone ID to be passed as an argument, with default "drone003Camera"
    drone_id = sys.argv[1] if len(sys.argv) > 1 else "drone003Camera"
    
    rclpy.init()
    node = DroneKeyboardControl(drone_id)
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()

if __name__ == "__main__":
    main()
