using UnityEngine;
using System.Linq;

public class ZoneOccupancyChecker : MonoBehaviour
{
    [System.Serializable]
    public struct Point2D
    {
        public float x;
        public float y;

        public Vector2 ToVector2() => new Vector2(x, y);
    }

    [Header("Zone Corners (unordered)")]
    public Point2D[] zoneCorners = new Point2D[4];

    [Header("Incoming Points")]
    public Point2D[] incomingPoints;

    [Header("Zone Status (live)")]
    public bool isOccupied = false;

    private Vector2[] orderedPolygon;

    void Start()
    {
        if (zoneCorners.Length != 4)
        {
            Debug.LogError("Zone must have exactly 4 corners.");
            return;
        }

        orderedPolygon = OrderPolygon(zoneCorners.Select(p => p.ToVector2()).ToArray());
    }

    void Update()
    {
        if (zoneCorners.Length == 4)
        {
            orderedPolygon = OrderPolygon(zoneCorners.Select(p => p.ToVector2()).ToArray());
            isOccupied = IsZoneOccupied();
        }
    }

    public bool IsZoneOccupied()
    {
        foreach (var point in incomingPoints)
        {
            if (IsPointInPolygon(point.ToVector2(), orderedPolygon))
                return true;
        }
        return false;
    }

    private Vector2[] OrderPolygon(Vector2[] points)
    {
        Vector2 center = Vector2.zero;
        foreach (var p in points)
            center += p;
        center /= points.Length;

        return points.OrderBy(p => Mathf.Atan2(p.y - center.y, p.x - center.x)).ToArray();
    }

    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int j = polygon.Length - 1;
        bool inside = false;

        for (int i = 0; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    void OnDrawGizmos()
    {
        if (zoneCorners.Length != 4)
            return;

        Vector2[] polygon = OrderPolygon(zoneCorners.Select(p => p.ToVector2()).ToArray());

        // Draw zone borders
        Gizmos.color = Color.green;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector3 a = new Vector3(polygon[i].x, 0, polygon[i].y);
            Vector3 b = new Vector3(polygon[(i + 1) % polygon.Length].x, 0, polygon[(i + 1) % polygon.Length].y);
            Gizmos.DrawLine(a, b);
        }

        // Draw incoming points
        foreach (var p in incomingPoints)
        {
            Vector2 point = p.ToVector2();
            bool inside = IsPointInPolygon(point, polygon);
            Gizmos.color = inside ? Color.red : Color.blue;
            Gizmos.DrawSphere(new Vector3(point.x, 0, point.y), 0.1f);
        }
    }
}
