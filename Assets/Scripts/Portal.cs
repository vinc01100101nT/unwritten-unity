using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A doorway between maps. When the player walks into its trigger collider, it
/// loads <c>targetScene</c>. Both scenes must be listed in File ▸ Build Settings.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Tooltip("Name of the scene to load when the player enters (must be in Build Settings).")]
    public string targetScene;

    [Tooltip("Id of the SpawnPoint in the target scene to arrive at (leave empty to keep the scene's default position).")]
    public string spawnId;

    // When the component is first added, make its collider a trigger.
    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() == null) return;   // only the player triggers it
        if (string.IsNullOrEmpty(targetScene)) return;
        SceneTravel.Target = spawnId;            // tell the next scene where to drop us
        SceneManager.LoadScene(targetScene);
    }

    // Draw a translucent box in the Scene view so you can place it (invisible in-game).
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
        var col = GetComponent<Collider2D>();
        Vector3 size = col != null ? (Vector3)col.bounds.size : Vector3.one;
        Gizmos.DrawCube(transform.position, size);
    }
}
