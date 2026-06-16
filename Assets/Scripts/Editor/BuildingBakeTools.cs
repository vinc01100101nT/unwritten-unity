using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Converts painted building TILES into persistent, draggable GameObjects — the "hand-finish"
/// half of the tiles-plus-bake workflow. It's the editor twin of <see cref="DepthSortRuntime"/>'s
/// runtime obstacle bake, but the objects it makes are saved in the scene (not transient) and the
/// raw tiles are removed, so each house becomes a normal object you can select, move, delete, or
/// duplicate. Buildings are SOLID (full-footprint colliders) and foot-sorted via a GROUP-mode
/// <see cref="YDepthSorter"/> (which runs in edit mode, so depth is right in the Scene view too).
///
/// Each 4-connected cluster of building tiles → one "Building" object, so keep a ≥1-cell gap
/// between separate houses or they bake into a single object.
///
/// Menu: Tools ▸ unwritten ▸ Bake Buildings to Objects. The town generator will call
/// <see cref="BakeTilemap"/> directly to offer the same step after generating.
/// </summary>
public static class BuildingBakeTools
{
    static readonly Vector3Int[] N4 = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
    const string ContainerName = "BuildingObjects";

    [MenuItem("Tools/unwritten/Bake Buildings to Objects")]
    static void BakeMenu()
    {
        var maps = ResolveBuildingTilemaps();
        if (maps.Length == 0)
        {
            EditorUtility.DisplayDialog("Bake Buildings to Objects",
                "No building tilemap found. Paint houses on a tilemap named \"Buildings\" " +
                "(or select that Tilemap), then run this again.", "OK");
            return;
        }

        var container = GetOrCreateContainer(maps[0].gameObject.scene);
        int total = 0;
        foreach (var tm in maps) total += BakeTilemap(tm, container.transform);

        if (total == 0)
        {
            EditorUtility.DisplayDialog("Bake Buildings to Objects",
                "Those building tilemap(s) have no painted tiles to bake.", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(maps[0].gameObject.scene);
        Selection.activeGameObject = container;
        Debug.Log($"[unwritten] Baked {total} building(s) into draggable objects under " +
                  $"'{ContainerName}'. Select one in the Hierarchy and press W to move it.");
    }

    /// <summary>Bake every 4-connected cluster of tiles on <paramref name="tm"/> into a persistent,
    /// foot-sorted, solid "Building" GameObject under <paramref name="container"/>, then clear those
    /// cells from the tilemap. Returns the number of buildings created. Reusable by the generator.</summary>
    public static int BakeTilemap(Tilemap tm, Transform container)
    {
        var grid = tm.GetComponentInParent<Grid>();
        Vector2 cell = grid != null ? (Vector2)grid.cellSize : Vector2.one;

        var clusters = FindClusters(tm);
        foreach (var cluster in clusters)
            BuildProp(tm, cluster, cell, container);

        if (clusters.Count > 0)
        {
            Undo.RegisterCompleteObjectUndo(tm, "Bake Buildings to Objects");
            foreach (var cluster in clusters)
                foreach (var c in cluster) tm.SetTile(c, null);   // tiles become the objects
            EditorUtility.SetDirty(tm);
        }
        return clusters.Count;
    }

    // ---- internals -----------------------------------------------------------

    // 4-connected flood fill over painted cells → one cluster per building.
    static List<List<Vector3Int>> FindClusters(Tilemap tm)
    {
        tm.CompressBounds();
        var visited = new HashSet<Vector3Int>();
        var clusters = new List<List<Vector3Int>>();

        foreach (var start in tm.cellBounds.allPositionsWithin)
        {
            if (!tm.HasTile(start) || visited.Contains(start)) continue;
            var cluster = new List<Vector3Int>();
            var q = new Queue<Vector3Int>();
            q.Enqueue(start); visited.Add(start);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                cluster.Add(c);
                foreach (var d in N4)
                {
                    var nb = c + d;
                    if (tm.HasTile(nb) && visited.Add(nb)) q.Enqueue(nb);
                }
            }
            clusters.Add(cluster);
        }
        return clusters;
    }

    static void BuildProp(Tilemap tm, List<Vector3Int> cluster, Vector2 cell, Transform container)
    {
        // foot/base of the cluster = bottom edge of its lowest row (the ground-contact line)
        int minCellY = int.MaxValue;
        foreach (var c in cluster) if (c.y < minCellY) minCellY = c.y;
        float bottomY = 0f, bMinX = float.MaxValue, bMaxX = float.MinValue;
        foreach (var c in cluster)
            if (c.y == minCellY)
            {
                Vector3 w = tm.GetCellCenterWorld(c);
                bottomY = w.y - cell.y * 0.5f;
                bMinX = Mathf.Min(bMinX, w.x - cell.x * 0.5f);
                bMaxX = Mathf.Max(bMaxX, w.x + cell.x * 0.5f);
            }

        var parent = new GameObject("Building");
        parent.transform.SetParent(container, true);
        parent.transform.position = new Vector3((bMinX + bMaxX) * 0.5f, bottomY, 0f);

        foreach (var c in cluster)
        {
            var child = new GameObject($"Cell_{c.x}_{c.y}");
            child.transform.SetParent(parent.transform, true);
            child.transform.position = tm.GetCellCenterWorld(c);
            child.AddComponent<SpriteRenderer>().sprite = tm.GetSprite(c);
            child.AddComponent<BoxCollider2D>().size = cell;   // full footprint = solid + Pathfinder wall
        }

        parent.AddComponent<YDepthSorter>();   // GROUP mode: whole house sorts by its single foot line
        Undo.RegisterCreatedObjectUndo(parent, "Bake Buildings to Objects");
    }

    static Tilemap[] ResolveBuildingTilemaps()
    {
        var picked = Selection.gameObjects
            .Select(g => g.GetComponent<Tilemap>())
            .Where(t => t != null)
            .Distinct()
            .ToArray();

        var pool = picked.Length > 0
            ? picked
            : UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        return pool.Where(t => IsBuildingName(t.name)).ToArray();
    }

    static bool IsBuildingName(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("building") || n.Contains("house") || n.Contains("wall") ||
               n.Contains("fence") || n.Contains("solid");
    }

    static GameObject GetOrCreateContainer(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == ContainerName) return root;

        var go = new GameObject(ContainerName);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Bake Buildings to Objects");
        return go;
    }
}
