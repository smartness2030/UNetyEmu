using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class TrailLine : MonoBehaviour
{
    public float minDistance = 0.1f;
    public Color trailColor = Color.white; // ← Color personalizado

    private LineRenderer line;
    private List<Vector3> points = new List<Vector3>();
    private Vector3 lastPoint;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;

        // Asignar color
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(trailColor, 0.0f),
                new GradientColorKey(trailColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            }
        );
        line.colorGradient = gradient;

        AddPoint(transform.position);
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, lastPoint);
        if (distance >= minDistance)
        {
            AddPoint(transform.position);
        }
    }

    void AddPoint(Vector3 point)
    {
        points.Add(point);
        line.positionCount = points.Count;
        line.SetPosition(points.Count - 1, point);
        line.alignment = LineAlignment.View;
        lastPoint = point;
    }
}
