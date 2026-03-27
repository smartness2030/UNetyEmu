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
from scapy.all import sniff
import socket
import sys
import os
from datetime import datetime

# Get unique timestamp at program start
timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

# -----------------------------------------------------------------------------------------------------

# Function to convert Unity coordinates to Mininet-WiFi coordinates
def ConvertUnityPositionToMininetWIFI(unity_position):
    """Convert Unity coordinates (x, y, z) to Mininet-WiFi coordinates (x, z, y)."""
    x, y, z = unity_position
    return (x, z, y)

# -----------------------------------------------------------------------------------------------------

# Function to send a command over TCP
def send_command(ip, port, command):
    """Send command over TCP."""
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect((ip, port))
            s.sendall(command.encode())  # Send the command as bytes
            print("Command sent: {}".format(command))
    except Exception as e:
        print("Failed to send command: {}".format(e))

# -----------------------------------------------------------------------------------------------------

# Function to process positions from the message
def process_positions(message):
    """Process base station and drone positions from the message."""
    positions = {}

    if "drone" in message:
        drone = message["drone"]
        drone_id = drone["id"]
        drone_position = drone["position"]
        converted_position = ConvertUnityPositionToMininetWIFI(
            (drone_position["x"], drone_position["y"], drone_position["z"])
        )

        # Store single drone position
        positions["drone_positions"] = [{"id": drone_id, "position": converted_position}]

        # Send command to set drone position
        command = "set.{}.setPosition(\"{},{},{}\")".format(drone_id, converted_position[0], converted_position[1], converted_position[2])
        send_command("{}".format(host_ip), 12346, command)
    
# -----------------------------------------------------------------------------------------------------

# Function to process the packet and extract the message
def packet_callback(packet):
    """Callback function for packet sniffing."""
    print(packet.show2())  # Print detailed packet info
    message = extract_message_from_packet(packet)  # Extract formatted message from packet
    positions = process_positions(message)  # Process and set positions
    log_rssi()  # Log RSSI for this drone

# -----------------------------------------------------------------------------------------------------

# Function to extract the message from the packet
def extract_message_from_packet(packet):
    """Extract JSON message from packet payload."""
    try:
        raw_data = bytes(packet["Raw"].load).decode("utf-8")
        import json
        return json.loads(raw_data)  # Convert to dictionary
    except Exception as e:
        print("Error extracting message:", e)
        return {}

# -----------------------------------------------------------------------------------------------------

# Function to log RSSI
def log_rssi():
    """Logs RSSI to a file dynamically based on hostname and timestamp."""
    rssi = os.popen("iw dev {}-wlan0 link | grep 'signal' | awk '{{print $2}}'".format(hostname)).read().strip()

    # File name with fixed timestamp
    output_file = "/root/{}_rssi_{}.csv".format(hostname, timestamp)

    # Append to file, creating it if necessary
    write_header = not os.path.exists(output_file)

    with open(output_file, "a") as file:
        if write_header:
            file.write("PlayerName, RSSI\n")  # Write header if new file
        file.write("{}, {}\n".format(hostname, rssi))  # Log RSSI

    print("Logged RSSI: {} dBm in {}".format(rssi, output_file))

# -----------------------------------------------------------------------------------------------------

# Get the hostname and host IP
hostname = socket.gethostname()
print("Hostname:", hostname)
host_ip = sys.argv[1]

# Start sniffing on the correct interface
sniff(iface="{}-wlan0".format(hostname), filter="dst port 12345", prn=packet_callback)