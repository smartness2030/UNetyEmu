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
import json
import socket
import threading
import time
import os
from mininet.node import Controller
from containernet.net import Containernet
from containernet.node import DockerSta
from containernet.term import makeTerm
from mn_wifi.link import wmediumd
from mininet.log import setLogLevel, info, debug
from mn_wifi.wmediumdConnector import interference
from mn_wifi.telemetry import telemetry
from containernet.cli import CLI
import math
import subprocess
import re
import ipaddress

# -----------------------------------------------------------------------------------------------------

# Class to convert Unity coordinates to Mininet-WiFi coordinates
def ConvertUnityPositionToMininetWIFI(unity_position):
    """Convert Unity coordinates (x, y, z) to Mininet-WiFi coordinates (x, z, y)."""
    x, y, z = unity_position
    return (x, z, y)

# -----------------------------------------------------------------------------------------------------

# Calculate the maximum distance between the ap and the vehicles to set the coverage radius dinamically
def calculate_max_distance(ap_position, vehicle_positions):
    """Calculate the maximum distance from AP to any vehicle."""
    max_distance = 0
    for vehicle in vehicle_positions:
        vehicle_pos = vehicle["position"]
        distance = math.sqrt(
            (ap_position[0] - vehicle_pos[0]) ** 2 +
            (ap_position[1] - vehicle_pos[1]) ** 2 + 
            (ap_position[2] - vehicle_pos[2]) ** 2
        )
        max_distance = max(max_distance, distance)
    return max_distance

# -----------------------------------------------------------------------------------------------------

# Send the coverage range to Unity
def send_coverage_range(radius, client_socket):
    """Send the calculated coverage range to Unity and wait for acknowledgment."""
    try:
        # Format the message as a JSON object
        range_message = json.dumps({"coverageRadius": radius})
        print(f"Message to be sent to Unity: {range_message}")
        
        # Send the JSON message
        client_socket.sendall(range_message.encode("utf-8"))
        
        info(f"Coverage radius {radius} sent to Unity\n")
        
        # Wait for acknowledgment from Unity
        try:
            client_socket.settimeout(5.0)  # Set a 5-second timeout for acknowledgment
            ack = client_socket.recv(1024)
            if ack:
                info(f"Received acknowledgment from Unity: {ack.decode()}\n")
            else:
                info("No acknowledgment received from Unity\n")
        except socket.timeout:
            info("Timeout waiting for Unity acknowledgment\n")
        except Exception as e:
            info(f"Error receiving acknowledgment: {e}\n")
        
    except Exception as e:
        info(f"Error sending coverage range: {e}\n")
        # Print more debug information
        import traceback
        info(f"Traceback: {traceback.format_exc()}\n")

# -----------------------------------------------------------------------------------------------------

# Process the initial positions received from Unity
def process_positions_once(host, port):
    """Receive initial positions from Unity and maintain connection."""
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((host, port))
    server_socket.listen(5)
    info(f"Waiting for Unity connection...\n")

    positions = {}
    
    try:
        # Accept connection from Unity
        client_socket, addr = server_socket.accept()
        info(f"Connection from {addr}\n")
        
        # Set socket options for better reliability
        client_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        client_socket.settimeout(60.0)  # Set a timeout to prevent hanging

        # Receive initial data
        data = client_socket.recv(4096)
        #info(f"Data length received from Unity: {len(data)}\n")
        
        print("[Unity -> Mininet-WiFi] First message received: ", data.decode())

        json_data = json.loads(data.decode())

        # Extract the base station position
        if "baseStation" in json_data:
            base_station = json_data["baseStation"]
            base_station_id = base_station["id"]
            base_station_position = base_station["position"]
            positions["ap_position"] = ConvertUnityPositionToMininetWIFI(
                (
                    base_station_position["x"],
                    base_station_position["y"],
                    base_station_position["z"],
                )
            )
            positions["ap_id"] = base_station_id
            info(f"[Unity -> Mininet-WiFi] BaseStation ID: {base_station_id}, Position: {positions['ap_position']}\n")

        # Extract the vehicle positions
        if "vehicles" in json_data:
            positions["vehicle_positions"] = []
            for vehicle in json_data["vehicles"]:
                vehicle_id = vehicle["id"]
                vehicle_position = vehicle["position"]
                positions["vehicle_positions"].append(
                    {
                        "id": vehicle_id,
                        "position": ConvertUnityPositionToMininetWIFI(
                            (
                                vehicle_position["x"],
                                vehicle_position["y"],
                                vehicle_position["z"],
                            )
                        ),
                    }
                )
            info(f"[Unity -> Mininet-WiFi] Vehicle positions parsed: {positions['vehicle_positions']}\n")

        info("Positions received and converted\n")

        # Keep the connection alive
        return client_socket, positions

    except json.JSONDecodeError as json_error:
        info(f"Error decoding JSON data: {json_error}\n")
    except Exception as e:
        info(f"Error receiving position: {e}\n")
        if 'client_socket' in locals():
            client_socket.close()
        server_socket.close()
        raise  # Re-raise the exception to handle it in the calling function

    return None, positions

# -----------------------------------------------------------------------------------------------------

# Function to retrieve all IPv4 addresses on the system
def get_all_ipv4_addresses():
    try:
        ip_output = subprocess.check_output("ip -4 addr", shell=True, text=True)
        ip_addresses = re.findall(r'inet (\d+\.\d+\.\d+\.\d+)/', ip_output)
        return ip_addresses
    except Exception as e:
        print(f"Error retrieving IP addresses: {e}")
        return []

# -----------------------------------------------------------------------------------------------------

# Classify the IP address to determine its type
def classify_ip(ip_str):
    ip = ipaddress.IPv4Address(ip_str)

    if ip.is_loopback:
        return None  # Ignore loopback

    if ip in ipaddress.IPv4Network("10.0.2.0/24"):
        return "nat"

    if ip in ipaddress.IPv4Network("192.168.56.0/24"):
        return "host-only"

    if ip in ipaddress.IPv4Network("172.17.0.0/16"):
        return "docker"

    return "bridge"  # Default fallback for valid non-loopback IPs

# -----------------------------------------------------------------------------------------------------

# Function to get the IP address of a specific interface type
def get_ip_address(mode):
    ips = get_all_ipv4_addresses()
    for ip in ips:
        iface_type = classify_ip(ip)
        if iface_type == mode:
            return ip

    print(f"No interface found for mode '{mode}'")
    return None

# -----------------------------------------------------------------------------------------------------

# Main function to create the topology
def minimal_topology():
    
    # Get the path of the current file
    os.system('iptables -A FORWARD -d 172.17.0.0/16 -j ACCEPT')
    path = os.path.dirname(os.path.abspath(__file__))

    # Set the host_ip variable dynamically
    vm_host_only_ip = get_ip_address("host-only")
    print(f"Mininet-WiFi VM IP: {vm_host_only_ip}")

    # Create mininet object that supports containernet
    net = Containernet(link=wmediumd, wmediumd_mode=interference,
                       noise_th=-91, fading_cof=3)

    # info("* Receiving initial positions for AP and vehicles\n")
    client_socket, positions = process_positions_once(host="0.0.0.0", port=12345)
    
    # Verify socket is still valid
    if client_socket is None:
        info("Error: Failed to establish socket connection with Unity\n")
        return

    ap_position = positions.get("ap_position")
    ap_id = positions.get("ap_id")
    vehicle_positions = positions.get("vehicle_positions", [])

    #info(f"Base station ID: {ap_id}, Position: {ap_position}\n")

    # Calculate max distance first to set appropriate parameters
    #max_distance = calculate_max_distance(ap_position, vehicle_positions)
    max_distance = 350
    
    # For 2.4GHz WiFi (λ ≈ 0.125m)
    frequency = 2.4  # GHz
    wavelength = 0.3 / frequency  # meters
    
    # Target received power (dBm) - typical WiFi sensitivity is around -70 to -90 dBm
    target_rx_power = -70  # dBm
    
    # Antenna gains (dBi)
    tx_antenna_gain = 2.0  # Typical WiFi antenna gain
    rx_antenna_gain = 2.0
    
    # Calculate required txpower
    path_loss = 20 * math.log10(4 * math.pi * max_distance / wavelength)
    required_txpower = target_rx_power - tx_antenna_gain - rx_antenna_gain + path_loss
    
    # Cap txpower between 1 and 30 dBm (typical WiFi range)
    txpower = max(1, min(30, required_txpower))

    # Create the access point with the received position and ID
    ap1 = net.addAccessPoint(
        f"{ap_id}",
        mac="00:00:00:00:00:02",
        ssid="handover",
        failMode="standalone",
        mode="g",
        channel="1",
        position=ap_position,
        txpower=txpower,
        antennaGain=tx_antenna_gain,
        range=max_distance,  # Set explicit range for visualization
        mesh=True  # Enable mesh mode
    )

    vehicle_last_ip_octect_fixed = 101
    
    # Create the vehicles with the received positions
    for i, vehicle_data in enumerate(vehicle_positions):
        vehicle_id = vehicle_data["id"]
        pos = vehicle_data["position"]
        info(f"Initial position for vehicle {vehicle_id}: {pos}\n")
        vehicle_name = f"{vehicle_id}"

        net.addStation(vehicle_name, 
                      position=",".join(map(str,pos)), 
                      ip=f"10.0.0.{vehicle_last_ip_octect_fixed + i}",
                      cls=DockerSta, 
                      volumes=[f"{path}:/root"], 
                      dimage="ramonfontes/socket_position:python35",
                      cpu_shares=20, 
                      mac=f"00:02:00:00:00:{int(i)+10:02}",
                      antennaGain=rx_antenna_gain,
                      range=max_distance,  # Set same range for visualization
                      mode="g",  # Set WiFi mode
                      mesh=True, # Enable mesh mode for direct vehicle-to-vehicle communication
                      wlans=2)  

        print("IP FIX:", vehicle_last_ip_octect_fixed + i)
        info(f"Created vehicle {vehicle_name} at position {pos}\n")

    info('*** Configuring WiFi nodes\n')
    net.configureWifiNodes()

    # Add links between vehicles to ensure they can communicate directly
    for i in range(len(vehicle_positions)):
        for j in range(i + 1, len(vehicle_positions)):
            vehicle1 = net.stations[i]
            vehicle2 = net.stations[j]
            net.addLink(vehicle1, vehicle2)
            info(f"Added link between {vehicle1.name} and {vehicle2.name}\n")

    info('*** Starting network\n')
    net.build()
    net.addNAT().configDefault()
    ap1.start([])

    info(f"Updated AP power to {txpower} dBm for range {max_distance}m\n")
    info(f"Antenna gains: TX={tx_antenna_gain}dBi, RX={rx_antenna_gain}dBi\n")

    # Send coverage radius back to Unity
    try:
        send_coverage_range(max_distance, client_socket)
    except Exception as e:
        info(f"Error sending coverage range: {e}\n")
    finally:
        # Always close the socket when done
        try:
            client_socket.close()
        except:
            pass

    info("*** Vehicle names ***\n")
    for vehicle in vehicle_positions:
        info(f"Vehicle {vehicle['id']} created\n")

    net.socketServer(ip=vm_host_only_ip, port=12346) # receive commands from the reconnect attemps: Unity -> Mininet-WiFi

    nodes = net.stations + net.aps
    net.telemetry(nodes=nodes, data_type="position", min_x=-100, max_x=700, min_y=-100, max_y=700)

    # Receiver call
    for vehicle in net.stations:
        makeTerm(vehicle, title=f'dr+{vehicle} sniffer', cmd=f"bash -c 'python3.5 /root/sniffer_container.py {vm_host_only_ip};'")

    info('*** Running CLI\n')
    CLI(net)

    # If the experiment does not finalizes as it should, containers might keep running. So, we manually ensure they stop running
    info('*** Executing post-CLI process\n')
    subprocess.run(["sudo", "./remove_running_containers.sh"])  # Replace with your script

    info('*** Stopping network\n')
    net.stop()

# -----------------------------------------------------------------------------------------------------

# Main function
if __name__ == "__main__":
    setLogLevel('info')
    minimal_topology()
