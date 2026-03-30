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
import os
import sys
from datetime import datetime

# Node to subscribe to drone camera feed and save the images to disk for dataset collection
class ImageSaver(Node):
    
    #Node startup
    def __init__(self, drone_id, output_dir):
        super().__init__("image_saver")
        
        self.output_dir = output_dir
        os.makedirs(self.output_dir, exist_ok=True)
        
        self.frame_count = 0
        topic_name = f"{drone_id}_camera"
        
        self.subscription = self.create_subscription(
            Image,
            topic_name,
            self.image_callback,
            10
        )
        
        self.get_logger().info(f"Saving images from '{topic_name}' to '{self.output_dir}'")
    
    # Function to receive drone camera image
    def image_callback(self, msg):
        try:
            img_array = np.frombuffer(msg.data, dtype=np.uint8)
            img = img_array.reshape((msg.height, msg.width, 3))
            
            img_bgr = cv2.cvtColor(img, cv2.COLOR_RGB2BGR)
            
            filename = os.path.join(
                self.output_dir,
                f"frame_{self.frame_count:06d}.jpg"
            )
            
            cv2.imwrite(filename, img_bgr)
            self.frame_count += 1
            
            if self.frame_count % 50 == 0:
                self.get_logger().info(f"{self.frame_count} images saved...")

        except Exception as e:
            self.get_logger().error(f"Error saving image: {e}")

def main():
    drone_id    = sys.argv[1] if len(sys.argv) > 1 else "drone001"
    output_dir  = sys.argv[2] if len(sys.argv) > 2 else f"dataset/{drone_id}/images"
    
    rclpy.init()
    node = ImageSaver(drone_id, output_dir)
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()

if __name__ == "__main__":
    main()
