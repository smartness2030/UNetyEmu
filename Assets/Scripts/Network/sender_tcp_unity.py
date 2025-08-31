################################################################################
# Copyright 2025 INTRIG
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
################################################################################

# Libraries
import sys
import socket
from scapy.all import send, TCP, IP, Raw

# -----------------------------------------------------------------------------------------------------

def send_position_command(dst_ip, dst_port, vehicle_name, x, y, z):
    """Send a position command to Mininet-WiFi VM using the correct format."""
    try:
        # Construct the position command in the correct format
        position_cmd = f"set.{vehicle_name}.setPosition(\"{x},{z},{y}\")"
        
        # Create socket connection
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            print(f"[DEBUG] Connecting to {dst_ip}:{dst_port}")
            s.connect((dst_ip, dst_port))
            
            print(f"[DEBUG] Sending position command: {position_cmd}")
            s.sendall(position_cmd.encode())
            #s.flush()
                
    except Exception as e:
        print(f"[ERROR] Failed to send position command: {e}")

def send_tcp_packet(dst_ip, dst_port, message):
    """Send a regular TCP packet."""
    try:
        # Construct TCP packet with RAW payload
        packet = (
            IP(dst=dst_ip) /
            TCP(sport=55555, dport=int(dst_port)) /
            Raw(load=message.encode())
        )
        
        # Send the packet
        print(f"Sending TCP packet to {dst_ip}:{dst_port} with message: {message}")
        send(packet, verbose=True)
        packet.show()
                
    except Exception as e:
        print(f"Error sending TCP packet: {e}")

def print_usage():
    """Print usage instructions."""
    print("Usage:")
    print("  Regular message:")
    print("    python sender_tcp_unity.py <dst_ip> <dst_port> <message>")
    print("  Position command:")
    print("    python sender_tcp_unity.py <dst_ip> <dst_port> --position --vehicle <name> --x <x> --y <y> --z <z>")

# -----------------------------------------------------------------------------------------------------

# Main function
if __name__ == "__main__":
    try:
        # Check if this is a position command
        if "--position" in sys.argv:
            # For position commands, we need at least 9 arguments:
            # [0] script name
            # [1] dst_ip
            # [2] dst_port
            # [3] --position
            # [4] --vehicle
            # [5] vehicle_name
            # [6] --x
            # [7] x_value
            # [8] --y
            # [9] y_value
            # [10] --z
            # [11] z_value
            if len(sys.argv) < 12:
                print("Error: Missing required arguments for position command")
                print_usage()
                sys.exit(1)

            # Extract position command arguments
            dst_ip = sys.argv[1]
            dst_port = int(sys.argv[2])
            
            # Find indices of required arguments
            try:
                vehicle_idx = sys.argv.index("--vehicle") + 1
                x_idx = sys.argv.index("--x") + 1
                y_idx = sys.argv.index("--y") + 1
                z_idx = sys.argv.index("--z") + 1
            except ValueError as e:
                print(f"Error: Missing required flag: {e}")
                print_usage()
                sys.exit(1)

            # Extract values
            vehicle_name = sys.argv[vehicle_idx]
            x = float(sys.argv[x_idx])
            y = float(sys.argv[y_idx])
            z = float(sys.argv[z_idx])
            
            # Send position command
            send_position_command(dst_ip, dst_port, vehicle_name, x, y, z)
        else:
            # Regular message requires exactly 4 arguments
            if len(sys.argv) != 4:
                print("Error: Incorrect number of arguments for regular message")
                print_usage()
                sys.exit(1)

            # Extract regular message arguments
            dst_ip = sys.argv[1]
            dst_port = int(sys.argv[2])
            message = sys.argv[3]
            
            # Send regular TCP packet
            print(f"(sender_tcp_unity) Input message from Unity: {message}, with destination {dst_ip}:{dst_port}")
            send_tcp_packet(dst_ip, dst_port, message)

    except ValueError as e:
        print(f"Error: Invalid argument format - {e}")
        print_usage()
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        print_usage()
        sys.exit(1)