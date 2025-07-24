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
from scapy.all import send, TCP, IP, Raw

# -----------------------------------------------------------------------------------------------------

# Function to send a TCP packet
def send_tcp_packet(dst_ip, dst_port, message):
    
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

# -----------------------------------------------------------------------------------------------------

# Main function
if __name__ == "__main__":
    
    # Check if the number of arguments is correct
    if len(sys.argv) < 4:
        print("Usage: python broker.py <dst_ip> <dst_port> <message>")
        sys.exit(1)
    
    # Extract arguments
    dst_ip = sys.argv[1]
    dst_port = int(sys.argv[2])
    message = sys.argv[3]

    # Print the received message
    print(f"(sender_tcp_unity) Destination {dst_ip}:{dst_port} Input message from Unity: {message}")

    # Send the TCP packet
    send_tcp_packet(dst_ip, dst_port, message)
