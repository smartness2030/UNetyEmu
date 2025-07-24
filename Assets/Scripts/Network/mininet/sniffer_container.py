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
from scapy.all import send, TCP, IP, Raw
import socket
import sys
import os
import json
import time

# -----------------------------------------------------------------------------------------------------

# Function to convert Unity coordinates to Mininet-WiFi coordinates
def ConvertUnityPositionToMininetWIFI(unity_position):
    """Convert Unity coordinates (x, y, z) to Mininet-WiFi coordinates (x, z, y)."""
    x, y, z = unity_position
    return (x, z, y)

# -----------------------------------------------------------------------------------------------------

# Add timing variables at the top level
global last_wlan0_message_time, connection_monitor_running
last_wlan0_message_time = time.time()
WLAN0_TIMEOUT = 10  # seconds
connection_monitor_running = True

# -----------------------------------------------------------------------------------------------------

# Function to send an initial ACK message
def send_initial_ack():
    """Send an initial ACK message to indicate the sniffer is starting."""
    try:
        message_dict = {
            "ack_info": "ACK - eth0 - not connected",
            "interface_ip": eth_ip
        }
        forward_to_unity_scapy(message_dict, use_wlan=False)
        print("[INIT] Sent initial ACK through eth0")
    except Exception as e:
        print("[INIT] Error sending initial ACK:", e)

# -----------------------------------------------------------------------------------------------------

# Function to send a TCP packet using Scapy
def send_tcp_packet(dst_ip, dst_port, message):
    try:
        packet = (
            IP(dst=dst_ip) /
            TCP(sport=6666, dport=int(dst_port)) /
            Raw(load=message.encode())
        )
        print("[SENDER] Sending TCP packet to {}:{} with message: {}".format(dst_ip, dst_port, message))
        send(packet, verbose=True)
        packet.show()
    except Exception as e:
        print("[SENDER] Error sending TCP packet: {}".format(e))

# -----------------------------------------------------------------------------------------------------

# Function to forward a message to Unity using Scapy
def forward_to_unity_scapy(message_dict, use_wlan=True):
    """Send a JSON-formatted message to Unity using Scapy TCP packet.
    
    Args:
        message_dict: The message dictionary to send
        use_wlan: Whether to attempt to use wlan0 (True) or force eth0 (False)
    """
    try:
        unity_ip = "192.168.56.1"
        unity_broker_port = 5005
        source_port = 6666  # Random high port; change if needed

        # Check if wlan0 has timed out
        current_time = time.time()
        wlan0_timed_out = (current_time - last_wlan0_message_time) > WLAN0_TIMEOUT

        # Add interface information to the message
        if use_wlan and not wlan0_timed_out:
            # Check if wlan0 is connected
            is_connected = os.popen("iw dev {}-wlan0 link | grep 'Connected'".format(hostname)).read().strip() != ""
            if is_connected:
                interface = "{}-wlan0".format(hostname)
                interface_ip = wlan_ip
                message_dict["ack_info"] = "ACK - wlan0 - connected"
                print("[FORWARDER] Sending ACK via wlan0 to {}:{}".format(unity_ip, unity_broker_port))
            else:
                interface = "eth0"
                interface_ip = eth_ip
                message_dict["ack_info"] = "ACK - eth0 - not connected"
                print("[FORWARDER] wlan0 not connected, falling back to eth0 for ACK to {}:{}".format(unity_ip, unity_broker_port))
        else:
            interface = "eth0"
            interface_ip = eth_ip
            message_dict["ack_info"] = "ACK - eth0 - not connected"
            print("[FORWARDER] wlan0 timed out, forcing eth0 for ACK to {}:{}".format(unity_ip, unity_broker_port))

        # Add the interface IP to the message
        message_dict["interface_ip"] = interface_ip

        # Convert dict to JSON string
        json_payload = json.dumps(message_dict)

        # Construct the packet
        packet = (
            IP(dst=unity_ip) /
            TCP(sport=source_port, dport=unity_broker_port, flags="PA") /
            Raw(load=json_payload.encode())
        )

        # Send with the selected interface
        send(packet, iface=interface, verbose=True)
        packet.show()
    except Exception as e:
        print("[FORWARDER] Error sending Scapy TCP packet to Unity:", e)

# -----------------------------------------------------------------------------------------------------

# Function to forward a message to Unity using a socket
def forward_to_unity_socket(message_dict):
    """Send a JSON-formatted message to Unity listener via TCP."""
    import json
    try:
        unity_ip = "192.168.56.1"  # Direct host IP
        unity_port = 5005
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect((unity_ip, unity_port))
            s.sendall(json.dumps(message_dict).encode("utf-8"))
            print("[FORWARDER] Sent message to Unity")
    except Exception as e:
        print("[FORWARDER] Socket error:", e)

# -----------------------------------------------------------------------------------------------------

# Function to process plain string messages (non-JSON)
def process_text_message(raw_data):
    """Process and return a formatted string from a simple text message."""
    message_str = "[MSG] Received plain message: \"{}\"".format(raw_data)
    print(message_str)
    return message_str

# -----------------------------------------------------------------------------------------------------

# Function to send a command over TCP
def send_command(ip, port, command):
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
    """Process base station and vehicle positions from the message."""
    positions = {}

    if "vehicle" in message:
        vehicle = message["vehicle"]
        vehicle_id = vehicle["id"]
        vehicle_position = vehicle["position"]
        converted_position = ConvertUnityPositionToMininetWIFI(
            (vehicle_position["x"], vehicle_position["y"], vehicle_position["z"])
        )

        # Store single vehicle position
        positions["vehicle_positions"] = [{"id": vehicle_id, "position": converted_position}]

        # Send command to set vehicle position - ALWAYS through eth0
        command = "set.{}.setPosition(\"{},{},{}\")".format(vehicle_id, converted_position[0], converted_position[1], converted_position[2])
        print("[POSITION] Sending position command through eth0 to {}".format(host_ip))
        send_command(host_ip, 12346, command)  # Always use host_ip which routes through eth0
    
    return positions

# -----------------------------------------------------------------------------------------------------

# Function to get IP addresses of the container
def get_container_ips():
    """Get both wireless and wired IP addresses of the container."""
    try:
        # Get wireless IP (wlan0)
        wlan_ip = os.popen("ip addr show {}-wlan0 | grep 'inet ' | awk '{{print $2}}' | cut -d/ -f1".format(hostname)).read().strip()
        
        # Get wired IP (eth0)
        eth_ip = os.popen("ip addr show eth0 | grep 'inet ' | awk '{{print $2}}' | cut -d/ -f1".format(hostname)).read().strip()
        
        # Get container IP (10.0.0.X)
        container_ip = os.popen("ip addr show {}-wlan0 | grep 'inet ' | awk '{{print $2}}' | cut -d/ -f1".format(hostname)).read().strip()
        
        print("[INFO] Container IPs:")
        print("  - Wireless (wlan0): {}".format(wlan_ip))
        print("  - Wired (eth0): {}".format(eth_ip))
        print("  - Container IP: {}".format(container_ip))
        
        return wlan_ip, eth_ip, container_ip
    except Exception as e:
        print("[ERROR] Failed to get container IPs: {}".format(e))
        return None, None, None

# -----------------------------------------------------------------------------------------------------

def monitor_connection():
    """Background thread to monitor connection status and send updates to Unity."""
    last_reconnect_attempt = 0
    RECONNECT_INTERVAL = 5  # seconds
    last_connection_state = None  # Track last known connection state
    
    while connection_monitor_running:
        current_time = time.time()
        
        try:
            # Check wlan0 connection status using iw command
            iw_output = os.popen("iw dev {}-wlan0 link".format(hostname)).read().strip()
            is_connected = "Connected to" in iw_output
            
            # Only log and send updates if connection state changed or disconnected
            if is_connected != last_connection_state or not is_connected:
                # Log detailed connection status
                print("[MONITOR] Current connection status:")
                print("  - Position:", os.popen("py {}.position".format(hostname)).read().strip())
                print("  - AP Position:", os.popen("py ap1.position").read().strip())
                print("  - AP Range:", os.popen("py ap1.wintfs[0].range").read().strip())
                print("  - wlan0 status:", iw_output)
                print("  - eth0 status:", os.popen("ip link show eth0").read().strip())
                
                if is_connected:
                    # If connected, update last wlan0 message time and send connected status
                    global last_wlan0_message_time
                    last_wlan0_message_time = current_time
                    message_dict = {
                        "ack_info": "ACK - wlan0 - connected",
                        "interface_ip": wlan_ip
                    }
                    print("[MONITOR] wlan0 is connected, sending connected status")
                else:
                    # If not connected, try to reconnect periodically
                    if (current_time - last_reconnect_attempt) >= RECONNECT_INTERVAL:
                        try:
                            # Try wlan0 first
                            force_assoc_cmd = "py {}.setAssociation('ap1', intf='{}-wlan0')".format(hostname, hostname)
                            print("[MONITOR] Attempting periodic reconnection with AP using wlan0: {}".format(force_assoc_cmd))
                            send_command(host_ip, 12346, force_assoc_cmd)
                            last_reconnect_attempt = current_time
                            
                            # Also try iw connect as a backup
                            iw_connect_cmd = "iw dev {}-wlan0 connect ap1-ssid".format(hostname)
                            print("[MONITOR] Also trying iw connect: {}".format(iw_connect_cmd))
                            os.system(iw_connect_cmd)
                            
                            # Verify if wlan0 association was successful
                            time.sleep(1)  # Give it a moment to associate
                            iw_output = os.popen("iw dev {}-wlan0 link".format(hostname)).read().strip()
                            is_connected = "Connected to" in iw_output
                            
                            if is_connected:
                                print("[MONITOR] Successfully re-associated with AP via wlan0")
                                message_dict = {
                                    "ack_info": "ACK - wlan0 - connected",
                                    "interface_ip": wlan_ip
                                }
                            else:
                                print("[MONITOR] wlan0 association failed, using eth0")
                                message_dict = {
                                    "ack_info": "ACK - eth0 - not connected",
                                    "interface_ip": eth_ip
                                }
                        except Exception as e:
                            print("[MONITOR] Error during reconnection attempt: {}".format(e))
                            message_dict = {
                                "ack_info": "ACK - eth0 - not connected",
                                "interface_ip": eth_ip
                            }
                    else:
                        message_dict = {
                            "ack_info": "ACK - eth0 - not connected",
                            "interface_ip": eth_ip
                        }
                
                # Send status update only if state changed or disconnected
                forward_to_unity_scapy(message_dict, use_wlan=is_connected)
                print("[MONITOR] Sent status update: {}".format(message_dict["ack_info"]))
                
                # Update last known connection state
                last_connection_state = is_connected
            
        except Exception as e:
            print("[MONITOR] Error in monitor thread: {}".format(e))
        
        time.sleep(1)  # Check every second

# -----------------------------------------------------------------------------------------------------

# Function to handle packet sniffing
def packet_callback(packet):
    """Callback function for packet sniffing."""
    # Extract essential packet information
    if IP in packet and TCP in packet:
        src_ip = packet[IP].src
        dst_ip = packet[IP].dst
        src_port = packet[TCP].sport
        dst_port = packet[TCP].dport
        payload = bytes(packet[TCP].payload).decode('utf-8', errors='ignore') if packet[TCP].payload else ""
        
        print("[PACKET] {}:{} -> {}:{}".format(src_ip, src_port, dst_ip, dst_port))
        print("[PAYLOAD] {}".format(payload))
    else:
        print(packet.show2())
    
    interface = packet.sniffed_on
    
    # Update last wlan0 message time if received through wlan0
    if interface.endswith('wlan0'):
        global last_wlan0_message_time
        last_wlan0_message_time = time.time()
        print("[INFO] Updated last wlan0 message time")
    
    message = extract_message_from_packet(packet)
    if not message:
        return
    
    # Process positions regardless of interface
    positions = process_positions(message)
    
    # Check if we're back in coverage and try to re-associate
    if interface.endswith('wlan0'):
        is_connected = os.popen("iw dev {}-wlan0 link | grep 'Connected'".format(hostname)).read().strip() != ""
        if not is_connected:
            # Try to force association with AP
            try:
                force_assoc_cmd = "py {}.setAssociation('ap1', intf='{}-wlan0')".format(hostname, hostname)
                print("[RECONNECT] Attempting to re-associate with AP using command: {}".format(force_assoc_cmd))
                send_command(host_ip, 12346, force_assoc_cmd)
                # Verify if association was successful
                time.sleep(1)  # Give it a moment to associate
                is_connected = os.popen("iw dev {}-wlan0 link | grep 'Connected'".format(hostname)).read().strip() != ""
                if is_connected:
                    print("[RECONNECT] Successfully re-associated with AP")
                else:
                    print("[RECONNECT] Failed to re-associate with AP")
            except Exception as e:
                print("[RECONNECT] Error forcing association: {}".format(e))
    
    # For ACKs, try to use wlan0 when available
    print("[ACK] Attempting to send ACK through wlan0 if available")
    forward_to_unity_scapy(message, use_wlan=True)
    
    # Only log RSSI if received through wireless interface and connected
    if interface.endswith('wlan0'):
        is_connected = os.popen("iw dev {}-wlan0 link | grep 'Connected'".format(hostname)).read().strip() != ""
        if is_connected:
            log_rssi()
            print("[INFO] Position received through {} (in range) - RSSI logged".format(interface))
        else:
            print("[INFO] Position received through {} (not connected) - No RSSI logged".format(interface))
    else:
        print("[INFO] Position received through {} - ACK will attempt to use wlan0 if available".format(interface))

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
    """Logs RSSI to a file dynamically based on hostname."""
    try:
        rssi = os.popen("iw dev {}-wlan0 link | grep 'signal' | awk '{{print $2}}'".format(hostname)).read().strip()
        if rssi:  # Only proceed if RSSI value was found
            output_file = "/root/{}_rssi.csv".format(hostname)  # File path
            
            # Append to file, creating it if necessary
            write_header = not os.path.exists(output_file)
            
            with open(output_file, "a") as file:
                if write_header:
                    file.write("PlayerName, RSSI\n")  # Write header if new file
                file.write("{}, {}\n".format(hostname, rssi))  # Log RSSI
            
            print("Logged RSSI: {} dBm in {}".format(rssi, output_file))
        else:
            print("No RSSI value found - wireless interface may not be connected")
    except Exception as e:
        print("Error logging RSSI:", e)

# -----------------------------------------------------------------------------------------------------

# Get the hostname and host IP
hostname = socket.gethostname()
print("Hostname:", hostname)
host_ip = sys.argv[1]  # Use host_ip from arguments

# Get container IPs
wlan_ip, eth_ip, container_ip = get_container_ips()

# Initialize telemetry for position tracking
try:
    # Get all nodes (stations and APs)
    nodes = os.popen("hostname").read().strip()  # Get current node
    # Send telemetry initialization command
    telemetry_cmd = "py net.telemetry(nodes=['{}'], data_type='position', min_x=-100, max_x=700, min_y=-100, max_y=700)".format(nodes)
    print("[TELEMETRY] Initializing position tracking: {}".format(telemetry_cmd))
    send_command(host_ip, 12346, telemetry_cmd)
except Exception as e:
    print("[TELEMETRY] Error initializing position tracking:", e)

# Start connection monitor thread
import threading
monitor_thread = threading.Thread(target=monitor_connection)
monitor_thread.daemon = True  # Thread will exit when main program exits
monitor_thread.start()
print("[MONITOR] Started connection monitor thread")

# Start sniffing on both interfaces
print("Starting sniffing on {}-wlan0 and eth0...".format(hostname))
try:
    sniff(iface=["{}-wlan0".format(hostname), "eth0"], filter="dst port 12345", prn=packet_callback)
except KeyboardInterrupt:
    print("\nStopping sniffer...")
    connection_monitor_running = False
    monitor_thread.join()
    print("Sniffer stopped.")
