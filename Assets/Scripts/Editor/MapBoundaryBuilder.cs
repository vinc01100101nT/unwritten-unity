using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One-click invisible wall around the map so the player can't walk off the edge.
/// It reads the world bounds of the "Ground" tilemap (or the selected tilemap) and
/// builds a closed rectangle of edge colliders on a "MapBoundary" object.
///
/// Re-running rebuilds it, so it's safe to run again after you paint a bigger field.
/// </summary>
public static class MapBoundaryBuilder
{
    [MenuItem("Tools/unwritten/Build Map Boundary from Ground")]
    static void BuildBoundary()
    {
        // Prefer a tilemap named "Ground"; otherwise use the current selection.
        Tilemap ground = null;
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (tm.name == "Ground") { ground = tm; break; }
        if (ground == null && Selection.activeGameObject != null)
            ground = Selection.activeGameObject.GetComponent<Tilemap>();

        if (ground == null)
        {
            EditorUtility.DisplayDialog("Build Map Boundary",
                "Couldn't find a tilemap named \"Ground\". Select your ground tilemap in the Hierarchy and run this again.",
                "OK");
            return;
        }

        // World-space rectangle the painted ground covers.
        Bounds b = ground.localBounds;
        Vector3 min = ground.transform.TransformPoint(b.min);
        Vector3 max = ground.transform.TransformPoint(b.max);

        var old = GameObject.Find("MapBoundary");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var go = new GameObject("MapBoundary");
        var edge = go.AddComponent<EdgeCollider2D>();
        // A closed loop of 5 points = an invisible wall on all four sides.
        edge.points = new[]
        {
            new Vector2(min.x, min.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y),
            new Vector2(min.x, max.y),
            new Vector2(min.x, min.y),
        };

        Undo.RegisterCreatedObjectUndo(go, "Build Map Boundary");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log($"[unwritten] Built MapBoundary around '{ground.name}' " +
                  $"({min.x:0.#},{min.y:0.#}) → ({max.x:0.#},{max.y:0.#}).");
    }
}
