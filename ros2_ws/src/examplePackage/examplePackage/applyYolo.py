# ------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ------------------------------------------------------

# Libraries
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image
import cv2
import numpy as np
import sys
from ultralytics import YOLO
from ament_index_python.packages import get_package_share_directory
import os
import threading
import torch

# Node to apply YOLOv8 detection on drone camera feed and display the results in a window
class YoloDetectorNode(Node):

    def __init__(self, drone_id):
        super().__init__("yolo_detector")
        package_share_dir = get_package_share_directory('examplePackage')
        model_path = os.path.join(package_share_dir, 'models', 'best.pt')

        self.get_logger().info(f"Loading YOLOv8 model: {model_path}")
        self.model = YOLO(model_path)
        self.model.to(torch.device('cpu'))
        self.get_logger().info("Model loaded!")

        np.random.seed(42)
        self.colors = np.random.randint(0, 255, size=(len(self.model.names), 3), dtype=np.uint8)

        topic_name = f"{drone_id}_camera"
        self.subscription = self.create_subscription(
            Image,
            topic_name,
            self.image_callback,
            10
        )

        self.latest_frame = None            # Latest frame to display
        self.frame_lock = threading.Lock()  # Concurrent access protection
        self.frame_count = 0
        self.window_name = f"YOLO - {drone_id}"

    def image_callback(self, msg):
        try:
            img = (
                np.frombuffer(msg.data, dtype=np.uint8)
                .reshape((msg.height, msg.width, 3))
            )
            img_bgr = cv2.cvtColor(img, cv2.COLOR_RGB2BGR)
            results = self.model(img_bgr, verbose=False)[0]
            annotated = self._draw_detections(img_bgr.copy(), results)

            # Save the frame and do not display it here
            with self.frame_lock:
                self.latest_frame = annotated

            self.frame_count += 1

        except Exception as e:
            self.get_logger().error(f"Callback error: {e}")

    def _draw_detections(self, img, results):
        boxes = results.boxes
        if boxes is None or len(boxes) == 0:
            return img

        for box in boxes:
            x1, y1, x2, y2 = map(int, box.xyxy[0])
            conf   = float(box.conf[0])
            cls_id = int(box.cls[0])
            label  = self.model.names[cls_id]
            color  = tuple(int(c) for c in self.colors[cls_id])

            cv2.rectangle(img, (x1, y1), (x2, y2), color, 2)

            text = f"{label} {conf:.2f}"
            (tw, th), baseline = cv2.getTextSize(text, cv2.FONT_HERSHEY_SIMPLEX, 0.55, 1)
            cv2.rectangle(img, (x1, y1 - th - baseline - 4), (x1 + tw + 4, y1), color, -1)
            cv2.putText(
                img, text,
                (x1 + 2, y1 - baseline - 2),
                cv2.FONT_HERSHEY_SIMPLEX, 0.55,
                (255, 255, 255), 1, cv2.LINE_AA
            )

        return img


def main():
    
    # Allow drone ID to be passed as an argument, defaulting to "drone003camera"
    drone_id = sys.argv[1] if len(sys.argv) > 1 else "drone003camera"

    rclpy.init()
    node = YoloDetectorNode(drone_id)

    # ROS spin on a separate thread — frees the main thread for the GUI
    spin_thread = threading.Thread(target=rclpy.spin, args=(node,), daemon=True)
    spin_thread.start()

    # Window created in the main thread (required for Qt)
    cv2.namedWindow(node.window_name, cv2.WINDOW_NORMAL)
    cv2.resizeWindow(node.window_name, 960, 540)

    try:
        while rclpy.ok():
            with node.frame_lock:
                frame = node.latest_frame

            if frame is not None:
                cv2.imshow(node.window_name, frame)

            # waitKey MUST be in the main loop
            key = cv2.waitKey(1) & 0xFF
            if key == ord("q"):
                node.get_logger().info("Closing...")
                break

    except KeyboardInterrupt:
        pass
    finally:
        cv2.destroyAllWindows()
        node.destroy_node()
        rclpy.shutdown()

if __name__ == "__main__":
    main()
