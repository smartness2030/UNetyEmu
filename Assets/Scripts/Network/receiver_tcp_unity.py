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
import argparse
from scapy.all import sniff, IP, TCP, Raw
from time import sleep
import socket

# -----------------------------------------------------------------------------------------------------

# Port 5005 is where Unity TCP Listener ('StartTcpListener') in 'BaseStationMininetWiFi.cs' will be waitinig for incoming packets
# Connect to Unity's TCP listener (with retry)
def connect_to_unity(host='localhost', port=5006, retries=3):
    
    # Create a TCP socket and connect to Unity's TCP listener
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)  # Disable Nagle's algorithm
    
    # Attempt to connect to Unity's TCP listener
    for i in range(retries):
        try:
            sock.connect((host, port))
            print(f"[BROKER] Connected to Unity on {host}:{port}")
            return sock
        except ConnectionRefusedError:
            print(f"[BROKER] Attempt {i+1}: Unity not ready, retrying...")
            sleep(1)
    raise ConnectionError("Could not connect to Unity after multiple attempts")

# -----------------------------------------------------------------------------------------------------

# Create a reusable function to send messages to Unity
def send_to_unity(sock, message):
    try:
        # Add newline to message to ensure proper message separation
        message_with_newline = message.rstrip() + '\n'
        sock.sendall(message_with_newline.encode())
        sock.flush() if hasattr(sock, 'flush') else None  # Flush if available
        print(f"[BROKER] Sent to Unity: {message}")
    except Exception as e:
        print(f"[ERROR] Failed to send to Unity: {e}")

# -----------------------------------------------------------------------------------------------------

# Callback for each sniffed packet
def packet_callback(packet, unity_sock):
    if packet.haslayer(TCP) and packet.haslayer(Raw):
        src_ip = packet[IP].src
        dst_ip = packet[IP].dst
        src_port = packet[TCP].sport
        dst_port = packet[TCP].dport
        message = packet[Raw].load.decode(errors='ignore')

        print(f"[BROKER] Packet received from {src_ip}:{src_port} to {dst_ip}:{dst_port}")
        print(f"[BROKER] Payload: {message}")

        # Send the message to Unity
        send_to_unity(unity_sock, message)

# -----------------------------------------------------------------------------------------------------

# Sniffer function to listen for packets on a specific interface and port
def sniff_packets(interface, port, unity_sock):
    print(f"[SNIFFER] Listening on interface {interface}, port {port}...")
    filter_str = f"tcp port {port}"
    
    # Wrap the callback to pass unity_sock
    sniff(iface=interface,
          filter=filter_str,
          prn=lambda pkt: packet_callback(pkt, unity_sock),
          store=0,
          promisc=True)

# -----------------------------------------------------------------------------------------------------

# Main function to set up argument parsing and start the sniffer
if __name__ == "__main__":
    
    # Argument parser for command line arguments
    parser = argparse.ArgumentParser(description="Receiver Broker - Scapy Sniffer + Unity Relay")
    parser.add_argument('--listen_port', type=int, help='Port to listen on')
    parser.add_argument('--interface', type=str, help='Network interface to sniff on', required=True)
    args = parser.parse_args()

    # Check if listen_port is provided
    if not args.listen_port:
        print("[ERROR] --listen_port is required.")
    else:
        try:
            unity_sock = connect_to_unity(port=5006)
            sniff_packets(args.interface, args.listen_port, unity_sock)
        except Exception as e:
            print(f"[FATAL] {e}")
