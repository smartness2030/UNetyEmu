# ----------------------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ----------------------------------------------------------------------

# Libraries
import rclpy  # ROS 2 client library
from rclpy.node import Node  # Base class for ROS 2 nodes
from sensor_msgs.msg import Image  # Camera message type from Unity / bridge
import cv2  # Image display and drawing
import numpy as np  # Buffer to array conversion
import sys  # Command-line arguments
from ultralytics import YOLO  # YOLOv8 inference API
from ament_index_python.packages import get_package_share_directory  # Resolve share path for packaged weights
import os  # Filesystem paths for the model file
import threading  # Spin ROS in background while OpenCV runs on main thread
import torch  # Device placement for the YOLO model

# ----------------------------------------------------------------------

# Node to apply YOLOv8 detection on drone camera feed and display the results in a window
class YoloDetectorNode(Node):

    def __init__(self, drone_id):
        super().__init__("yolo_detector")

        # Resolve packaged model path under the examplePackage share directory
        package_share_dir = get_package_share_directory('examplePackage')
        model_path = os.path.join(package_share_dir, 'models', 'best.pt')

        # Load weights and run inference on CPU for predictable VM behavior
        self.get_logger().info(f"Loading YOLOv8 model: {model_path}")
        self.model = YOLO(model_path)
        self.model.to(torch.device('cpu'))
        self.get_logger().info("Model loaded!")

        # Fixed colors per class id for visualization
        np.random.seed(42)
        self.colors = np.random.randint(0, 255, size=(len(self.model.names), 3), dtype=np.uint8)

        # Subscribe to the same topic name Unity publishes (drone_id + "_camera")
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

    # Function to run detection on each incoming image and stash the annotated result
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

    # Function to draw YOLO boxes and class labels on the BGR image
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

# ----------------------------------------------------------------------

# Main routine to run the YOLO detector node
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
            # Read the latest annotated frame under lock (written by the callback)
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

# ----------------------------------------------------------------------

# Main function to run the script
if __name__ == "__main__":
    main()
