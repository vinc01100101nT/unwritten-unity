using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// The single source of truth for the game's draw-order stack, applied automatically on Play.
/// Depth is split into fixed ORDER BANDS within the one Default sorting layer (sorting order is a
/// 16-bit value, so everything stays within ±32767):
///
///   Ground tilemap      −32000   behind everyone
///   Decor tilemap       −30000   under-foot decals (no collider)
///   ENTITIES         −25000..+25000   per-object by feet-Y (YDepthSorter): player/mobs/NPCs/props
///   Overhead tilemap   +30000     walk-under canopies/roofs (no collider)
///   FX               +31000..+32000   slash / click / target arrow / damage numbers
///
/// On every scene load this: points the camera transparency axis up (a tie-fallback), bands the
/// tilemaps by name, gives every in-scene unit a <see cref="YDepthSorter"/>, and **bakes obstacle
/// tilemaps into per-object props** — so you just paint trees/houses on the "Obstacles" tilemap and
/// they automatically become grouped, foot-sorted, walk-under props at Play (no editor step). The
/// bake is NON-destructive: it disables the raw tilemap's renderer + collider for the play session
/// and spawns the props; your painted tiles are untouched in the scene asset.
/// </summary>
public static class DepthSortRuntime
{
    // ---- band constants (keep within ±32767) ---------------------------------
    public const int GroundOrder   = -32000;
    public const int RoadOrder     = -31000;   // walkable paths: above ground, below decor/entities
    public const int DecorOrder    = -30000;
    public const int ObstacleStopgapOrder = -28000;  // editor-preview order for the raw (un-baked) obstacle tilemap
    public const int EntityClamp   =  25000;
    public const int OverheadOrder =  30000;
    public const int FxSlash        = 31000;
    public const int FxClickMarker  = 31100;
    public const int FxTarget       = 31200;
    public const int FxPopup        = 32000;

    const float Precision = 50f;   // sortingOrder = -feetY * Precision

    static readonly Vector3 YAxis = new Vector3(0f, 1f, 0f);
    static readonly Vector3Int[] N4 = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    /// <summary>Per-object entity order from a feet/base world-Y: lower on screen = higher = in front.</summary>
    public static int OrderForY(float y)
        => Mathf.Clamp(Mathf.RoundToInt(-y * Precision), -EntityClamp, EntityClamp);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SetupScene();
        SceneManager.sceneLoaded += (s, m) => SetupScene();
    }

    static void SetupScene()
    {
        SetupCameraAxis();
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            BandTilemap(tm);
        BakeObstacleProps();
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            if (IsUnit(sr)) EnsureUnitSorter(sr);
    }

    public static void SetupCameraAxis()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = YAxis;
        }
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = YAxis;
    }

    public static void BandTilemap(Tilemap tm)
    {
        var tr = tm != null ? tm.GetComponent<TilemapRenderer>() : null;
        if (tr == null) return;
        string n = tm.name.ToLowerInvariant();

        if (NameHas(n, "road", "street", "cobble", "path"))
            Set(tr, RoadOrder, TilemapRenderer.Mode.Chunk);
        else if (NameHas(n, "ground", "floor", "water", "grass", "base", "background", "terrain"))
            Set(tr, GroundOrder, TilemapRenderer.Mode.Chunk);
        else if (NameHas(n, "decor", "decal", "overlay", "detail", "underfoot"))
            Set(tr, DecorOrder, TilemapRenderer.Mode.Chunk);
        else if (NameHas(n, "overhead", "canopy", "roof", "treetop", "foreground", "above"))
            Set(tr, OverheadOrder, TilemapRenderer.Mode.Chunk);
        else if (IsObstacleName(n))
            Set(tr, ObstacleStopgapOrder, TilemapRenderer.Mode.Chunk);   // editor preview only; baked away at Play
    }

    public static void EnsureUnitSorter(SpriteRenderer sr)
    {
        if (sr != null && !sr.TryGetComponent<YDepthSorter>(out _))
            sr.gameObject.AddComponent<YDepthSorter>();
    }

    public static bool IsUnit(SpriteRenderer sr)
    {
        var go = sr.gameObject;
        return go.GetComponent<PlayerController2D>() != null
            || go.GetComponent<MonsterAI>() != null
            || go.GetComponent<Interactable>() != null
            || go.GetComponent<CharacterAnimator2D>() != null;
    }

    // ---- runtime "Convert Obstacles to Objects" ------------------------------

    // Bake every obstacle-ish tilemap into grouped, foot-sorted prop objects, then switch the raw
    // tilemap off (renderer + collider) so the props are the only thing drawing / blocking.
    static void BakeObstacleProps()
    {
        Transform root = null;
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            string n = tm.name.ToLowerInvariant();
            if (!IsObstacleName(n)) continue;

            var tr = tm.GetComponent<TilemapRenderer>();
            if (tr != null && !tr.enabled) continue;   // already baked (guard against re-runs)

            if (root == null)
            {
                root = new GameObject("ObstacleProps (runtime)").transform;
                // Keep baked props in the SAME scene as the tilemap, so an additively-loaded map's
                // props unload with that map (not with whatever scene happens to be active).
                SceneManager.MoveGameObjectToScene(root.gameObject, tm.gameObject.scene);
            }

            var grid = tm.GetComponentInParent<Grid>();
            Vector2 cell = grid != null ? (Vector2)grid.cellSize : Vector2.one;
            bool solid = NameHas(n, "building", "house", "wall", "solid", "fence");

            foreach (var cluster in FindClusters(tm))
                BuildProp(tm, cluster, cell, root, solid);

            if (tr != null) tr.enabled = false;                 // raw tilemap no longer draws…
            foreach (var col in tm.GetComponents<Collider2D>()) // …nor blocks; the props do both
                col.enabled = false;
        }
    }

    // 4-connected flood fill over painted cells → clusters (each = one prop).
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

    static void BuildProp(Tilemap tm, List<Vector3Int> cluster, Vector2 cell, Transform root, bool solid)
    {
        // foot/base of the cluster = bottom edge of its lowest row
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

        var parent = new GameObject(solid ? "Building" : "Tree");
        parent.transform.SetParent(root, true);
        parent.transform.position = new Vector3((bMinX + bMaxX) * 0.5f, bottomY, 0f);

        foreach (var c in cluster)
        {
            var child = new GameObject($"Cell_{c.x}_{c.y}");
            child.transform.SetParent(parent.transform, true);
            child.transform.position = tm.GetCellCenterWorld(c);
            var sr = child.AddComponent<SpriteRenderer>();
            var sprite = tm.GetSprite(c);
            if (sprite != null) sr.sprite = sprite;
            if (solid)
            {
                var b = child.AddComponent<BoxCollider2D>();   // full footprint blocks
                b.size = cell;
            }
        }

        parent.AddComponent<YDepthSorter>();   // GROUP mode: whole prop sorts by its single foot

        if (!solid)
        {
            // trunk-sized base collider so you can walk under the leafy top
            var box = parent.AddComponent<BoxCollider2D>();
            box.size = new Vector2(Mathf.Max(0.2f, (bMaxX - bMinX) * 0.6f), cell.y * 0.4f);
            box.offset = new Vector2(0f, cell.y * 0.2f);
        }
    }

    static bool IsObstacleName(string n)
        => NameHas(n, "obstacle", "obstruction", "wall", "prop", "tree", "house",
                      "building", "collision", "foliage", "bush", "fence", "solid");

    static void Set(TilemapRenderer tr, int order, TilemapRenderer.Mode mode)
    {
        tr.sortingOrder = order;
        tr.mode = mode;
    }

    static bool NameHas(string n, params string[] keys)
    {
        foreach (var k in keys) if (n.Contains(k)) return true;
        return false;
    }
}
