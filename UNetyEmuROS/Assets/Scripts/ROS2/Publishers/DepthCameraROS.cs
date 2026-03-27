using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using System;

public class DepthCamera : MonoBehaviour
{   
    private ROSConnection ros;
    private string topicName;
    private Camera depthCamera;

    public int width = 640;
    public int height = 480;
    public int fps = 10;
    private float timeElapsed;

    private RenderTexture depthRenderTexture;
    private Texture2D texture2D;

    public Material depthMaterial; 

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        topicName = gameObject.name.ToLower() + "_depth_camera";
        ros.RegisterPublisher<ImageMsg>(topicName);

        depthCamera = GetComponentInChildren<Camera>();
        depthCamera.depthTextureMode = DepthTextureMode.Depth;

        depthRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.RFloat);
        depthCamera.targetTexture = depthRenderTexture;

        texture2D = new Texture2D(width, height, TextureFormat.RFloat, false);
    } 

    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= (1.0f / fps)) {
            publishDepth();
            timeElapsed = 0;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (depthMaterial != null) {
            Graphics.Blit(source, destination, depthMaterial);
        } else {
            Graphics.Blit(source, destination);
        }
    }

void publishDepth() {

    depthCamera.targetTexture = depthRenderTexture;
    depthCamera.Render();

    RenderTexture.active = depthRenderTexture;

    texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    // texture2D.Apply();

    // byte[] rawData = texture2D.GetRawTextureData();

    // Color[] pixels = texture2D.GetPixels();
    // Array.Reverse(pixels);
    // texture2D.SetPixels(pixels);
    // texture2D.Apply();

    Color[] pixels = texture2D.GetPixels();
    Color[] flipped = new Color[pixels.Length];

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            flipped[(height - 1 - y) * width + x] = pixels[y * width + x];
        }
    }

    texture2D.SetPixels(flipped);
    texture2D.Apply();





    

    byte[] rawData = texture2D.GetRawTextureData();


    if (rawData == null || rawData.Length == 0) {
        Debug.LogWarning("Matriz de profundidade vazia!");
        return;
    }
        uint sec = (uint)Time.time;
        uint nanosec = (uint)((Time.time - sec) * 1e9);

        ImageMsg depthMsg = new ImageMsg {
            header = new HeaderMsg {
                frame_id = gameObject.name.ToLower() + "_depth_camera_link",
                stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg { sec = (int)sec, nanosec = nanosec }
            },
            height = (uint)height,
            width = (uint)width,
            encoding = "32FC1",
            is_bigendian = 0,
            step = (uint)(width * 4),
            data = rawData
        };

        ros.Publish(topicName, depthMsg);
        RenderTexture.active = null;
    }
}