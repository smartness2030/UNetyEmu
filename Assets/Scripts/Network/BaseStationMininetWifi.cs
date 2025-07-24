/*******************************************************************************
* Copyright 2025 INTRIG
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
******************************************************************************/

// Libraries
using System; // Library to use the Environment class
using System.Net.Sockets; // Library to use the TcpClient and NetworkStream classes
using System.Net; // Library to use the IPAddress class
using System.Collections; // Library to use coroutines
using System.Text; // Library to use the Encoding class
using UnityEngine; // Library to use the MonoBehaviour class
using System.Collections.Generic; // Library to use lists
using System.Threading.Tasks; // Library to use tasks
using System.Diagnostics;  // Library to use the ProcessStartInfo class
using System.Threading; // Library to use the Thread class
using Newtonsoft.Json; // Library to use JSON serialization/deserialization
using System.Linq; // Library to use LINQ methods like Concat

// Class to control the communication of the base station (Unity to Mininet-WiFi)
public class BaseStationMininetWifi : MonoBehaviour
{

    // -----------------------------------------------------------------------------------------------------
    // Public variables that appear in the Inspector:

    // Flag to check if the connection with Mininet is established
    [ReadOnly] public bool flagMininetWifiReady = false;

    // Variables to show the coverage radius of the base station
    public Color gizmoColor = Color.green;
    public float radius = 1.0f;

    // Time interval between messages in seconds
    public float messageInterval = 1f;

    // Get the current user's username dynamically
    string userName = Environment.UserName;

    // List to store vehicle IPs (10.0.0.X), where X is 101, 102, 103, and so on
    public List<string> vehicleIPs = new List<string>(); 

    // List to store backup vehicle IPs (172.17.0.X), where X is 2, 3, 4, and so on
    public List<string> vehicleBackupIPsList = new List<string>(); 

    // List to store vehicle names
    public List<string> vehicleNames = new List<string>(); 

    // Dictionary to track vehicles with communication
    public Dictionary<string, bool> vehiclesWithCommunication = new Dictionary<string, bool>();

    // Enum to define message labels
    public enum Label
    {
        FirstSpecialMessage, // this label is for the first message only
        Position // used to send positions timely
    }

    // -----------------------------------------------------------------------------------------------------
    // Private variables of this class:

    // Variables to store the IP address of the Mininet-WiFi VM
    private TcpClient tcpClient;
    private NetworkStream stream;
    private string response;
    private float lastMessageTime = 0f; // Timestamp of the last message sent
    private string mn_wifi_vm_ip = ""; // IP address of the Mininet-WiFi VM
    private int dstPort = 12345; // send through this port

    // Variables for the TCP listener
    private int baseIPLastOctet = 101; // Start IP counter from 101
    private TcpListener tcpListener;
    private Thread listenerThread;
    private TcpClient client;
    private bool clientReady = false;
    private TcpClient pendingClient;
    private Coroutine communicationCoroutine;
    private Process receiverProcess = null;

    // Track if each vehicle is connected via wlan0
    private Dictionary<string, bool> vehicleConnectionStatus = new Dictionary<string, bool>(); 

    // Store backup IPs for each vehicle
    private Dictionary<string, string> vehicleBackupIPs = new Dictionary<string, string>(); 

    // Start backup IPs from 172.17.0.2
    private const int BACKUP_IP_START = 2; 
    private Dictionary<string, float> lastReconnectionAttempt = new Dictionary<string, float>();

    // Try to reconnect every x seconds
    private const float RECONNECTION_INTERVAL = 5.0f;

    // -----------------------------------------------------------------------------------------------------
    // Start is called before the first frame update:

    void Start()
    {

        // Create an instance of GetVmIp to get the Mininet-WiFi VM IP
        GetVmIp executor = new GetVmIp();

        // Set the VM name and adapter index for the Mininet-WiFi connection
        string vmName = "mn-wifi";
        int adapterIndex = 1;

        UnityEngine.Debug.Log("Unity Simulation started");
        UnityEngine.Debug.Log("Trying to connect to Mininet-WiFi");

        // Execute the script to get the Mininet-WiFi VM IP address
        executor.ExecuteScript(vmName, adapterIndex, userName, this);

        // Wait for the IP address to be set
        CallReceiverPythonScript();

        // Start the TCP listener in a separate thread
        StartTcpListener();

        // Start the coroutine to wait for vehicles to be created
        StartCoroutine(WaitForVehicles());
        
    }

    // -----------------------------------------------------------------------------------------------------
    // FixedUpdate is called at a fixed interval:

    void FixedUpdate()
    {

        // If the TCP client is null or not connected, try to connect to Mininet
        if (stream == null || !stream.CanWrite)
        {
            ConnectToMininet(mn_wifi_vm_ip, dstPort); // Explicit connection to Mininet-WiFi VM
            return;
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to start the TCP listener in a separate thread:

    void StartTcpListener()
    {

        // Initialize the TCP listener and start it in a separate thread
        listenerThread = new Thread(() =>
        {
            try
            {

                // Create a new TCP listener on port 5006
                int port_tcp_listener = 5006;
                tcpListener = new TcpListener(IPAddress.Any, port_tcp_listener);
                tcpListener.Start();
                UnityEngine.Debug.Log("TCP Listener started on port " + port_tcp_listener);

                // Continuously listen for incoming connections
                while (true)
                {
                    if (!tcpListener.Pending())
                    {
                        Thread.Sleep(10); // Sleep for a short time to avoid busy waiting
                        continue;
                    }

                    // Accept incoming TCP client connections
                    pendingClient = tcpListener.AcceptTcpClient();
                    clientReady = true;

                }

            }
            catch (ThreadAbortException)
            {
                UnityEngine.Debug.LogWarning("TCP Listener thread aborted.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("TCP Listener error: " + e.Message);
            }
            
        });

        // Set the thread as a background thread and start it
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    // -----------------------------------------------------------------------------------------------------
    // Update is called once per frame:

    void Update()
    {

        // If the client is ready, set it to the pending client
        if (clientReady)
        {

            // Set the client to the pending client
            client = pendingClient;
            NetworkStream vehicleStream = client.GetStream(); // NOT stream used for Mininet
            clientReady = false;

            UnityEngine.Debug.Log("Connected to vehicle container in Mininet-WiFi (listener)");

            // If the communication coroutine is not running, start it
            if (Application.isPlaying)
            {
                communicationCoroutine = StartCoroutine(HandleCommunication(vehicleStream));
            }

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Coroutine to handle communication with the vehicle container:

    IEnumerator HandleCommunication(NetworkStream vehicleStream)
    {

        // Initialize buffer and leftover string
        byte[] buffer = new byte[1024];
        string leftover = "";

        // While the client is connected, read data from the stream
        while (client.Connected)
        {

            // Check if the stream is available and can read
            if (vehicleStream.DataAvailable)
            {

                // Read data from the stream
                int bytesRead = vehicleStream.Read(buffer, 0, buffer.Length);
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] messages = (leftover + receivedData).Split('\n');
                leftover = messages[messages.Length - 1];

                // Process each message except the last one (which may be incomplete)
                for (int i = 0; i < messages.Length - 1; i++)
                {

                    // Trim whitespace and check if the message is not empty
                    string message = messages[i].Trim();
                    if (!string.IsNullOrEmpty(message))
                    {
                        //UnityEngine.Debug.Log("[Python -> Unity] Received: " + message);
                        ProcessAckMessage(message);
                    }

                }

            }

            // Return control to Unity
            yield return new WaitForSeconds(0.01f);

        }

        UnityEngine.Debug.Log("[Mininet-WiFi] Vehicle client disconnected");
    }

    // -----------------------------------------------------------------------------------------------------
    // Coroutine to wait for the vehicles to be created:

    IEnumerator WaitForVehicles()
    {

        // Wait until the vehicles are created
        while (GameObject.FindGameObjectsWithTag("Drone").Length == 0)
        {
            yield return new WaitForSeconds(1);  // Wait for 1 second before checking again
        }

        // Find all vehicles in the scene
        GameObject[] droneObjects = GameObject.FindGameObjectsWithTag("Drone");
        GameObject[] carObjects = GameObject.FindGameObjectsWithTag("Car");

        // Combine the drone and car objects into a single array
        GameObject[] vehicleObjects = droneObjects.Concat(carObjects).ToArray();

        // Ensure the lists are empty before adding new ones
        vehicleIPs.Clear();
        vehicleConnectionStatus.Clear();
        vehicleBackupIPs.Clear();

        // Assign IPs to each vehicle
        for (int i = 0; i < vehicleObjects.Length; i++)
        {
            string primaryIP = $"10.0.0.{baseIPLastOctet + i}";
            string backupIP = $"172.17.0.{BACKUP_IP_START + i}";
            vehicleIPs.Add(primaryIP);
            vehicleConnectionStatus[primaryIP] = true; // Assume connected initially
            vehicleBackupIPs[primaryIP] = backupIP;

            UnityEngine.Debug.Log($"[Unity BaseStation] Assigned IPs for vehicle {vehicleObjects[i].name}: Primary={primaryIP}, Backup={backupIP}");

            vehicleNames.Add(vehicleObjects[i].name); // Store the vehicle name
            vehicleBackupIPsList.Add(backupIP);
            vehiclesWithCommunication[backupIP] = true; // Assume communication is established initially
        }

        // Log the assigned vehicle IPs
        UnityEngine.Debug.Log($"[Unity BaseStation] Assigned {vehicleIPs.Count} vehicle IPs: {string.Join(", ", vehicleIPs)}");
        UnityEngine.Debug.Log("[Unity BaseStation] Vehicles detected. Setting up connection...");

        // Set the online communication flag for each vehicle
        SendFirstPositionMessage(mn_wifi_vm_ip, dstPort);

        // Start the coroutine to send vehicle messages
        StartCoroutine(SendVehicleMessagesCoroutine());

    }

    // -----------------------------------------------------------------------------------------------------
    // Coroutine to send vehicle messages:

    IEnumerator SendVehicleMessagesCoroutine()
    {

        // Infinite loop to send messages
        while (true)
        {

            // Check if the time interval has passed
            if (Time.time - lastMessageTime >= messageInterval)
            {

                // Get all vehicles in the scene
                GameObject[] droneObjects = GameObject.FindGameObjectsWithTag("Drone");
                GameObject[] carObjects = GameObject.FindGameObjectsWithTag("Car");

                // Combine the drone and car objects into a single array
                GameObject[] vehicleObjects = droneObjects.Concat(carObjects).ToArray();

                // Send the position of each vehicle
                foreach (GameObject vehicle in vehicleObjects)
                {

                    // Get the primary IP of the vehicle and encode its position
                    string primaryIP = vehicleIPs[Array.IndexOf(vehicleObjects, vehicle)];
                    string message = EncodeDecode.EncodeSingleVehiclePositionWithLabelAndID(vehicle, Label.Position.ToString());

                    // Reconnection logic
                    bool shouldTryReconnect = false;
                    if (!vehicleConnectionStatus[primaryIP] &&
                        (!lastReconnectionAttempt.ContainsKey(primaryIP) ||
                         Time.time - lastReconnectionAttempt[primaryIP] >= RECONNECTION_INTERVAL))
                    {

                        // If the vehicle is not connected and the last reconnection attempt was more than RECONNECTION_INTERVAL seconds ago
                        shouldTryReconnect = true;
                        lastReconnectionAttempt[primaryIP] = Time.time;

                        // Send position command to Mininet-WiFi
                        Vector3 vehiclePos = vehicle.transform.position;
                        vehiclePos.y = vehiclePos.y + 0.1f; // Adjust because of the center of mass of the cars

                        // Convert float values to use dots instead of commas for Python compatibility
                        string positionCmd = $"{vehicle.name} --x {vehiclePos.x.ToString(System.Globalization.CultureInfo.InvariantCulture)} --y {vehiclePos.y.ToString(System.Globalization.CultureInfo.InvariantCulture)} --z {vehiclePos.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                        UnityEngine.Debug.Log($"[RECONNECT] Attempting to reconnect vehicle {vehicle.name} by setting position through Mininet-WiFi VM");

                        // Call the Python script to send the position command
                        Task.Run(() => CallSenderPythonScriptTask(mn_wifi_vm_ip, 12346, positionCmd, true));

                    }

                    // Only use wlan0 if we're connected or trying to reconnect
                    string targetIP = vehicleConnectionStatus[primaryIP] ? primaryIP : vehicleBackupIPs[primaryIP];
                    bool usingWlan = vehicleConnectionStatus[primaryIP] || shouldTryReconnect;

                    // Call the Python script to send the message
                    Task.Run(() => CallSenderPythonScriptTask(targetIP, dstPort, message, false));

                }

                // Update the last message time
                lastMessageTime = Time.time;

            }

            // Wait for the next frame
            yield return null;

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the Mininet-WiFi VM IP address:

    public void SetMnWifiVmIp(string ip)
    {
        mn_wifi_vm_ip = ip; // Set the IP address of the Mininet-WiFi VM
        UnityEngine.Debug.Log($"Mininet-WiFi VM IP set to: {mn_wifi_vm_ip}"); // Log the IP address of the Mininet-WiFi VM
    }

    // -----------------------------------------------------------------------------------------------------
    // Class to call the Python script to send messages to the vehicles:

    void CallSenderPythonScriptTask(string dstIp, int dstPort, string message, bool isPositionCommand = false)
    {

        // Try to call the Python script
        try
        {

            // Create a new instance of ProcessStartInfo
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "python";

            // Get the path of the Python script
            string folder = Application.dataPath + "/Scripts/Network/sender_tcp_unity.py";

            // Set the arguments based on the mode
            if (isPositionCommand)
            {
                // For position commands, pass each argument separately
                psi.Arguments = $"\"{folder}\" \"{dstIp}\" \"{dstPort}\" --position --vehicle {message}";
            }
            else
            {
                // For regular messages, escape double quotes
                string escapedMessage = message.Replace("\"", "\\\"");
                psi.Arguments = $"\"{folder}\" \"{dstIp}\" \"{dstPort}\" \"{escapedMessage}\"";
            }

            // Set execution settings
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            // Start the Python script
            using (Process process = Process.Start(psi))
            {

                // Log the start of the Python script
                UnityEngine.Debug.Log($"[Unity -> Mininet-WiFi] (Sender Task) Starting TCP Unity to {dstIp}:{dstPort} with message: {message}");

                // Read the output and error of the Python script
                string result = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // Wait for the Python script to exit
                process.WaitForExit();

                // Log the result of the Python script
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError($"(Sender Task) Broker script error: {error}");
                }
                else
                {
                    UnityEngine.Debug.Log("[Unity -> Mininet-WiFi] (Sender Task) Broker response: " + result);
                }

            }

        }
        catch (Exception e)
        {
            // Log the error if the Python script fails
            UnityEngine.Debug.LogError($"(Sender Task) Error calling broker_tcp_sender.py: {e.Message}");
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to call the Python receiver script:

    void CallReceiverPythonScript()
    {
        try
        {

            // Kill any existing process
            if (receiverProcess != null && !receiverProcess.HasExited)
            {
                receiverProcess.Kill();
                receiverProcess.WaitForExit();
            }

            // Create a new instance of ProcessStartInfo
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "python";
            psi.WorkingDirectory = Application.dataPath + "/Scripts/Network";

            // Use the exact same command line arguments that work manually
            string scriptName = "receiver_tcp_unity.py";
            string interfaceName = "Ethernet 2";
            int listenPort = 5005;

            // Build arguments exactly as they appear in the working manual command
            psi.Arguments = $"{scriptName} --listen_port {listenPort} --interface \"{interfaceName}\"";

            // Configure process settings
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;

            // Create and start the process
            receiverProcess = new Process();
            receiverProcess.StartInfo = psi;
            receiverProcess.EnableRaisingEvents = true;

            // Handle output (this will show the same output you see when running manually)
            receiverProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    UnityEngine.Debug.Log($"[Receiver] {args.Data}");
                }
            };

            receiverProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    UnityEngine.Debug.LogError($"[Receiver Error] {args.Data}");
                }
            };

            // Start the process and begin reading output
            receiverProcess.Start();
            receiverProcess.BeginOutputReadLine();
            receiverProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log($"Started Python receiver script with command: python {psi.Arguments}");

        }
        catch (Exception e) // Log the error if the receiver script fails
        {
            UnityEngine.Debug.LogError($"Failed to start receiver script: {e.Message}"); 
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Class to connect to Mininet-WiFi:

    void ConnectToMininet(string ip, int port)
    {

        // Try to connect to Mininet-WiFi
        try
        {

            // If the TCP client is not null
            if (tcpClient != null)
            {

                // If the TCP client is connected
                if (tcpClient.Connected)
                {
                    UnityEngine.Debug.Log($"Already connected to {ip}:{port}"); // Log the connection
                    return;
                }

                // Close the TCP client
                tcpClient.Close();

            }

            // Log the connection attempt
            UnityEngine.Debug.Log($"Attempting to connect to {ip}:{port}...");

            // Create a new TCP client
            tcpClient = new TcpClient();

            // Connect to the IP address and port
            tcpClient.Connect(ip, port);  // Explicit connection
            stream = tcpClient.GetStream();

            // Log the successful connection
            UnityEngine.Debug.Log($"Connected to {ip}:{port} successfully!");

        }
        catch (SocketException e)
        {

            // Log the socket exception
            UnityEngine.Debug.LogError($"SocketException: {e.Message} (Error Code: {e.SocketErrorCode})");

            // Handle the connection failure. End simulation.
            HandleConnectionFailure();

        }
        catch (Exception e)
        {

            // Log the general exception
            UnityEngine.Debug.LogError($"General Exception: {e.Message}");

            // Handle the connection failure. End simulation.
            HandleConnectionFailure();

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Class to send the first position message to Mininet-WiFi:

    void SendFirstPositionMessage(string ip, int port)
    {

        // Connect to Mininet
        ConnectToMininet(ip, port);

        // If the stream is not null and can write
        if (stream != null && stream.CanWrite)
        {

            // Get the position of the base station
            Vector3 position = transform.position;

            // Encode the position and coverage radius message
            string message = EncodeDecode.EncodePositionAndCoverageRadiusMessageOnce(position, Label.FirstSpecialMessage.ToString());

            // Send the message to Mininet
            //UnityEngine.Debug.Log("Encoded JSON message: " + message);
            SendMessageToMininet(message, ip, port);

            // Wait for Mininet's response asynchronously
            _ = WaitForResponseAsync();  // Fire and forget if SendBaseStationPosition isn't async

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Class to send messages to Mininet-WiFi:

    void SendMessageToMininet(string message, string ip, int port)
    {

        // If the TCP client is null, not connected, or the stream is null or cannot write
        if (tcpClient == null || !tcpClient.Connected || stream == null || !stream.CanWrite)
        {
            UnityEngine.Debug.LogError($"Not connected. Attempting to reconnect to {ip}:{port}..."); // Log the reconnection
            ConnectToMininet(ip, port);  // Reconnect to the current target IP
        }

        // If the TCP client is not null and connected, and the stream is not null and can write
        if (tcpClient != null && tcpClient.Connected && stream != null && stream.CanWrite)
        {

            // Encode the message to bytes
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // Send the message bytes to the stream
            stream.Write(messageBytes, 0, messageBytes.Length);
            //UnityEngine.Debug.Log($"Sent message bytes to {ip}:{port}: {message}");
            UnityEngine.Debug.Log($"[Unity -> Mininet-WiFi] Sent message to {ip}:{port}: {message}");

        }
        else
        {

            // Log the error if the message fails to send
            UnityEngine.Debug.LogError($"Failed to send message to {ip}:{port}");

        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Class to wait for Mininet-WiFi's response asynchronously:

    async Task WaitForResponseAsync()
    {

        // Try to wait for the response
        try
        {

            // Create a buffer to read the response
            byte[] buffer = new byte[1024];

            // Read the stream asynchronously
            var readTask = stream.ReadAsync(buffer, 0, buffer.Length);

            // Wait for the response or timeout
            var timeoutTask = Task.Delay(60000);

            // If the read task finishes first
            if (await Task.WhenAny(readTask, timeoutTask) == readTask)
            {

                // Get the number of bytes read
                int bytesRead = readTask.Result;

                // If there are bytes read
                if (bytesRead > 0)
                {

                    // Get the response from the buffer
                    response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Decode the response to a CoverageData object
                    var coverageData = JsonUtility.FromJson<EncodeDecode.CoverageData>(response);

                    // If the coverage data is not null
                    if (coverageData != null)
                    {

                        // Set the coverage radius using the Base Station GameObject
                        UnityEngine.Debug.Log($"[Mininet-WiFi -> Unity] Coverage radius received: {coverageData.coverageRadius} m");
                        radius = coverageData.coverageRadius;

                        // Activate the flag to indicate that Mininet is ready
                        flagMininetWifiReady = true;

                        // Send acknowledgment back to Mininet
                        try
                        {
                            string ack = "ACK";
                            byte[] ackBytes = Encoding.UTF8.GetBytes(ack);
                            await stream.WriteAsync(ackBytes, 0, ackBytes.Length);
                            UnityEngine.Debug.Log("[Unity -> Mininet-WiFi] Send ACK message that coverage was received");
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogError($"Error sending acknowledgment: {e.Message}");
                        }

                    }

                }
                else
                {
                    UnityEngine.Debug.LogWarning("Empty response from Mininet."); // Log the empty response
                }

            }
            else
            {
                UnityEngine.Debug.LogWarning("Response timed out."); // Log the timeout
            }

        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error while waiting for response: {e.Message}"); // Log the error to wait for the response
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Special Unity method to clean the stream once it terminates:

    void OnApplicationQuit()
    {

        // Kill the receiver process if it's still running
        if (receiverProcess != null && !receiverProcess.HasExited)
        {
            try
            {
                receiverProcess.Kill();
                receiverProcess.WaitForExit();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error killing receiver process: {e.Message}");
            }
        }

        // Clean up the connection and TCP listener
        CleanupConnection();
        CleanTcpListener();
        
    }

    // -----------------------------------------------------------------------------------------------------
    // Class to clean up the connection:

    void CleanupConnection()
    {

        // Close the stream and TCP client if they are not null
        if (stream != null) stream.Close();
        if (tcpClient != null) tcpClient.Close();
        if (communicationCoroutine != null)
        {
            StopCoroutine(communicationCoroutine);
            communicationCoroutine = null;
        }
        
    }

    // -----------------------------------------------------------------------------------------------------
    // Method to clean the TCP listener and its thread:

    void CleanTcpListener()
    {

        // If the TCP listener is not null, stop it
        if (tcpListener != null)
        {
            tcpListener.Stop();
        }

        // If the listener thread is not null and alive, abort it
        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Abort();
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Class to draw the coverage radius of the base station:

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor; // Set the color of the gizmo
        Gizmos.DrawWireSphere(transform.position, radius); // Draw the wire sphere
    }

    // -----------------------------------------------------------------------------------------------------
    // Class to handle connection failure:

    void HandleConnectionFailure()
    {

        CleanupConnection(); // Clean up the connection

        // Log the connection failure
        UnityEngine.Debug.LogError("Connection failed. Stopping the simulation...");

        // Log the warning to run the Mininet-WiFi topology script
        UnityEngine.Debug.LogWarning("Make sure to run first the mininet_topo.py file in the Mininet-Wifi virtual machine.");

        // Stop the simulation
        UnityEditor.EditorApplication.isPlaying = false;

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to process ACKs from sniffer_container.py

    private void ProcessAckMessage(string message)
    {
        try
        {

            // Deserialize the JSON message into a dictionary
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

            // Check if the message contains the expected keys
            if (jsonData.ContainsKey("ack_info") && jsonData.ContainsKey("interface_ip"))
            {

                // Extract the ack_info and interface_ip from the JSON data
                string ackInfo = jsonData["ack_info"].ToString();
                string interfaceIP = jsonData["interface_ip"].ToString();

                UnityEngine.Debug.Log($"[Mininet-WiFi -> Unity] Received ACK message: {ackInfo} from interface IP: {interfaceIP}");

                // Check if the interface IP is in the list of vehicle backup IPs
                if (vehiclesWithCommunication.ContainsKey(interfaceIP))
                {
                    vehiclesWithCommunication[interfaceIP] = false; // Mark the vehicle loss connection
                    setVehicleOnlineCommunication();
                }

                // Check if this is an eth0 not connected message
                bool isEth0NotConnected = ackInfo.Contains("eth0") && ackInfo.Contains("not connected");
                bool isConnected = ackInfo.Contains("connected") && !isEth0NotConnected;

                // Set the connection status for the vehicle based on the interface IP
                string vehicleIP = null;
                foreach (var ip in vehicleIPs)
                {
                    if (vehicleBackupIPs.ContainsKey(ip) &&
                        (ip == interfaceIP || vehicleBackupIPs[ip] == interfaceIP))
                    {
                        vehicleIP = ip;
                        break;
                    }
                }

                // If a matching vehicle IP is found, update its connection status
                if (vehicleIP != null)
                {

                    // Update the connection status for the vehicle
                    bool wasConnected = vehicleConnectionStatus[vehicleIP];
                    vehicleConnectionStatus[vehicleIP] = isConnected;

                    if (!wasConnected && isConnected)
                    {
                        lastReconnectionAttempt[vehicleIP] = Time.time;
                        UnityEngine.Debug.Log($"[Unity BaseStation] Vehicle {vehicleIP} reconnected! Resetting reconnection timer.");
                    }

                    // Log the connection status update
                    string currentIP = isConnected ? vehicleIP : vehicleBackupIPs[vehicleIP];
                    UnityEngine.Debug.Log($"[Unity BaseStation] Updated connection status for {vehicleIP}: Connected={isConnected} (Using IP: {currentIP})");

                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Could not find matching vehicle IP for interface IP: {interfaceIP}");
                }

            }

        }
        catch (Exception e) // Log the error if the message processing fails
        {
            UnityEngine.Debug.LogError($"Error processing ACK message: {e.Message}");
        }

    }

    // -----------------------------------------------------------------------------------------------------
    // Method to set the online communication flag for each vehicle:

    void setVehicleOnlineCommunication()
    {

        // Iterate through the vehicle names and their corresponding backup IPs
        for (int i = 0; i < vehicleNames.Count; i++)
        {

            // Get the vehicle name and backup IP
            string vehicleName = vehicleNames[i];
            string ip = vehicleBackupIPsList[i];

            // Find the vehicle GameObject by name
            GameObject vehicleObject = GameObject.Find(vehicleName);

            // Find the VehicleCommunication component
            VehicleCommunication comm = vehicleObject.GetComponent<VehicleCommunication>();
            comm.flagOnlineCommunication = vehiclesWithCommunication[ip];

        }

    }

}
