using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Smoothly keeps the camera centred on a target (the player), preserving the
/// camera's own Z. Optionally clamps the view so it never shows past the edge of
/// a Tilemap (e.g. the Ground layer) — "camera bounds".
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    [Tooltip("Higher = snappier follow.")]
    public float smooth = 8f;

    [Tooltip("Optional: clamp the camera so its view stays inside this Tilemap's " +
             "bounds (drag in your Ground layer). Leave empty for no clamping.")]
    public Tilemap boundsTilemap;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        var desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        if (boundsTilemap != null)
            desired = ClampToBounds(desired);

        // Framerate-independent smoothing toward the (already clamped) target.
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }

    /// <summary>Keep the camera's view rectangle inside the tilemap's world rectangle.</summary>
    Vector3 ClampToBounds(Vector3 pos)
    {
        // The world-space rectangle the painted tiles cover.
        Bounds b = boundsTilemap.localBounds;
        Vector3 worldMin = boundsTilemap.transform.TransformPoint(b.min);
        Vector3 worldMax = boundsTilemap.transform.TransformPoint(b.max);

        // Half the camera's visible area, in world units.
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = worldMin.x + halfW, maxX = worldMax.x - halfW;
        float minY = worldMin.y + halfH, maxY = worldMax.y - halfH;

        // Clamp; but if the map is smaller than the view, just centre on it.
        pos.x = minX <= maxX ? Mathf.Clamp(pos.x, minX, maxX) : (worldMin.x + worldMax.x) * 0.5f;
        pos.y = minY <= maxY ? Mathf.Clamp(pos.y, minY, maxY) : (worldMin.y + worldMax.y) * 0.5f;
        return pos;
    }
}
