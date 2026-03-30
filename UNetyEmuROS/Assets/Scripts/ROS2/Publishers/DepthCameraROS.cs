// ----------------------------------------------------------------------
// Copyright 2026 INTRIG & SMARTNESS
// Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
// ----------------------------------------------------------------------

// Libraries
using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;

// Class to publish depth camera data as ROS Image messages
public class DepthCamera : MonoBehaviour
{   
    
    public int width = 640;
    public int height = 480;
    public int fps = 10;

    public Material depthMaterial; 
    
    private ROSConnection ros;
    private string topicName;
    private Camera depthCamera;
    private float timeElapsed;

    private RenderTexture depthRenderTexture;
    private Texture2D texture2D;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        topicName = gameObject.name + "_depth_camera";
        ros.RegisterPublisher<ImageMsg>(topicName);

        depthCamera = GetComponentInChildren<Camera>();
        depthCamera.depthTextureMode = DepthTextureMode.Depth;

        depthRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.RFloat);
        depthCamera.targetTexture = depthRenderTexture;

        texture2D = new Texture2D(width, height, TextureFormat.RFloat, false);
    } 

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= (1.0f / fps)) {
            publishDepth();
            timeElapsed = 0;
        }
    }

    // Apply depth shader to render texture
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (depthMaterial != null) {
            Graphics.Blit(source, destination, depthMaterial);
        } else {
            Graphics.Blit(source, destination);
        }
    }

    void publishDepth()
    {

        depthCamera.targetTexture = depthRenderTexture;
        depthCamera.Render();

        RenderTexture.active = depthRenderTexture;

        texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);

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
            Debug.LogWarning("Empty depth image!");
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
