using UnityEngine;

/// <summary>
/// Smoothly keeps the camera centred on a target (the player), preserving the
/// camera's own Z so the orthographic view stays correct.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    [Tooltip("Higher = snappier follow.")]
    public float smooth = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        var desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        // Framerate-independent smoothing.
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
