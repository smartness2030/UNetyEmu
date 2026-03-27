using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class SocketPythonCommunication : MonoBehaviour
{
    
    public CommandsHandler handler;
    public bool printDebug = false;


    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;

    private Coroutine communicationCoroutine;

    private bool clientReady = false;
    private TcpClient pendingClient = null;

    

    // Called when the script is enabled (e.g. when the GameObject is activated)
    void OnEnable()
    {
        if (Application.isPlaying)
        {
            StartServer();
        }
    }

    // Called when the script is disabled (e.g. when the GameObject is deactivated)
    void OnDisable()
    {
        StopServer();
    }

    // Called when the application is closing
    void OnApplicationQuit()
    {
        StopServer();
    }

    // Initializes and starts the TCP server
    public void StartServer()
    {
        Application.runInBackground = true;

        try
        {
            server = new TcpListener(IPAddress.Any, 5005);
            server.Start();
            if (printDebug) Debug.Log("Waiting for connection from Python...");
            server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), null);
        }
        catch (Exception e)
        {
            Debug.LogError("Error starting TCP server: " + e.Message);
        }
    }

    // Stops the server and closes connections
    public void StopServer()
    {
        communicationCoroutine = null;
        stream?.Close();
        client?.Close();
        server?.Stop();

        if (printDebug) Debug.Log("Server stopped");
    }

    // Callback triggered when a client connects to the server
    void OnClientConnected(IAsyncResult ar)
    {
        try
        {
            pendingClient = server.EndAcceptTcpClient(ar);
            clientReady = true; // Will be handled on the main thread
            if (printDebug) Debug.Log("Client connected, processing on main thread.");
        }
        catch (Exception e)
        {
            Debug.LogError("Error accepting client: " + e.Message);
        }
    }

    // Called every frame, handles newly connected clients
    void Update()
    {
        if (clientReady)
        {
            client = pendingClient;
            stream = client.GetStream();
            clientReady = false;

            if (printDebug) Debug.Log("Connected to Python (processed on main thread)");

            if (Application.isPlaying)
            {
                communicationCoroutine = StartCoroutine(HandleCommunication());
            }
        }
    }

    // Coroutine that continuously listens for and handles messages from the client
    IEnumerator HandleCommunication()
    {
        byte[] buffer = new byte[1024];
        string leftover = "";

        while (client.Connected)
        {
            if (stream.DataAvailable)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                leftover += Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Process complete messages separated by newline
                while (leftover.Contains("\n"))
                {
                    // Find the index of the first newline character
                    int newlineIndex = leftover.IndexOf("\n");

                    // Extract a complete message
                    string message = leftover.Substring(0, newlineIndex).Trim();

                    // Remove processed message from leftover
                    leftover = leftover.Substring(newlineIndex + 1);

                    // Handle request to get current cube positions
                    if (message == "GET_POS")
                    {
                        
                        string response = handler.GetPositions();

                        // Convert string to bytes and send to client
                        byte[] data = Encoding.UTF8.GetBytes(response);
                        stream.Write(data, 0, data.Length);

                        // Log sent message
                        if(printDebug) Debug.Log("Sent: Current positions");
                    }
                    
                    // Handle request to set cube X positions
                    if (message.StartsWith("SET_POS:")) 
                    {
                        handler.SetPositions(message.Substring(8));
                    }

                    // Exit command from Python to stop the Unity editor (Editor only)
                    if (message == "EXIT")
                    {
                        if (printDebug) Debug.Log("EXIT command received from Python.");

                        handler.StopUnityEditor();
                    }
                    
                }
            }

            yield return null;
        }
    }
}
