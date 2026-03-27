using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using System;

public class RGBCameraROS : MonoBehaviour
{   
    private ROSConnection ros;
    private string topicName;
    private Camera rgbCamera;

    public int width = 640;
    public int height = 480;
    public int fps = 10;
    private float timeElapsed;

    private RenderTexture rgbRenderTexture;
    private Texture2D texture2D;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        topicName = gameObject.name + "_camera";
        ros.RegisterPublisher<ImageMsg>(topicName);

        rgbCamera = GetComponentInChildren<Camera>();

        // RenderTexture independiente por dron (IMPORTANTE)
        rgbRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rgbCamera.targetTexture = rgbRenderTexture;

        texture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
    } 

    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= (1.0f / fps)) {
            publishImage();
            timeElapsed = 0;
        }
    }

    void publishImage()
    {
        rgbCamera.targetTexture = rgbRenderTexture;
        rgbCamera.Render();

        RenderTexture.active = rgbRenderTexture;

        texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);

        // ---- FLIP VERTICAL (igual que depth) ----
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
            Debug.LogWarning("Imagen RGB vacía!");
            return;
        }

        uint sec = (uint)Time.time;
        uint nanosec = (uint)((Time.time - sec) * 1e9);

        ImageMsg imgMsg = new ImageMsg {
            header = new HeaderMsg {
                frame_id = gameObject.name.ToLower() + "_camera_link",
                stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg {
                    sec = (int)sec,
                    nanosec = nanosec
                }
            },
            height = (uint)height,
            width = (uint)width,
            encoding = "rgb8",
            is_bigendian = 0,
            step = (uint)(width * 3),
            data = rawData
        };

        ros.Publish(topicName, imgMsg);
        RenderTexture.active = null;
    }
}