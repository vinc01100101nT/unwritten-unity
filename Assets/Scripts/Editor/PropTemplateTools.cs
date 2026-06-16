using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Authoring side of the town generator's generic prop system: turn a hand-painted object
/// (house, tree, well, stall…) into a reusable <see cref="PropTemplate"/> asset, and stamp it
/// back to prove the round-trip.
///
///   • <b>Capture Prop Template</b> — reads every painted cell from the selected Tilemaps
///     (or all Tilemaps under the open scene's Grid), normalises them to a (0,0) origin,
///     defaults the anchor to bottom-centre, and saves a PropTemplate.asset (placement Lot;
///     change it in the Inspector for trees/centrepieces/roadside props).
///   • <b>Stamp Prop Template</b> — paints a selected PropTemplate back onto the open scene's
///     Grid (re-creating layers by name) so you can confirm it matches, then Play to watch
///     DepthSortRuntime bake solid layers into foot-sorted props.
///
/// Tip: paint solid parts on a tilemap named "Buildings"/"Obstacles" (DepthSortRuntime treats
/// building/house/wall/tree/fence names as SOLID); use "Decor"/"Overhead" for non-blocking trim.
/// Leave a ≥ 1-cell gap between separate objects or the bake merges them into one prop.
/// </summary>
public static class PropTemplateTools
{
    const string DefaultFolder = "Assets/ScriptableObjects/PropTemplates";

    [MenuItem("Tools/unwritten/Capture Prop Template")]
    static void Capture()
    {
        var tilemaps = ResolveSourceTilemaps();
        if (tilemaps.Length == 0)
        {
            EditorUtility.DisplayDialog("Capture Prop Template",
                "Paint a single object onto tilemaps under a Grid, then select that Grid " +
                "(or the specific Tilemaps) and run this again.\n\n" +
                "Tip: do it in an empty scratch scene so only that object gets captured.", "OK");
            return;
        }

        // Collect every painted cell across the source tilemaps, tagged with its layer name.
        var raw = new List<(string layer, Vector3Int pos, TileBase tile)>();
        foreach (var tm in tilemaps)
        {
            tm.CompressBounds();
            foreach (var p in tm.cellBounds.allPositionsWithin)
            {
                var t = tm.GetTile(p);
                if (t != null) raw.Add((tm.name, p, t));
            }
        }
        if (raw.Count == 0)
        {
            EditorUtility.DisplayDialog("Capture Prop Template",
                "Those tilemaps have no painted tiles to capture.", "OK");
            return;
        }

        // Normalise to a (0,0) origin = bottom-left of the bounding box.
        int minX = raw.Min(c => c.pos.x), minY = raw.Min(c => c.pos.y);
        int maxX = raw.Max(c => c.pos.x), maxY = raw.Max(c => c.pos.y);

        if (!AssetDatabase.IsValidFolder(DefaultFolder)) CreateFolders(DefaultFolder);
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Prop Template", "PropTemplate", "asset",
            "Name this captured prop.", DefaultFolder);
        if (string.IsNullOrEmpty(path)) return;

        var tpl = ScriptableObject.CreateInstance<PropTemplate>();
        tpl.size = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
        tpl.anchorCell = new Vector2Int(tpl.size.x / 2, 0);   // default: bottom-centre
        tpl.cells = raw.Select(c => new PropTemplate.Cell
        {
            layer = c.layer,
            pos = new Vector2Int(c.pos.x - minX, c.pos.y - minY),
            tile = c.tile,
        }).ToList();

        AssetDatabase.CreateAsset(tpl, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = tpl;
        EditorGUIUtility.PingObject(tpl);

        string byLayer = string.Join(", ", tpl.cells.GroupBy(c => c.layer)
            .Select(g => $"{g.Key}×{g.Count()}"));
        Debug.Log($"[unwritten] Captured '{tpl.name}' — {tpl.size.x}×{tpl.size.y} footprint, " +
                  $"anchor at {tpl.anchorCell}, placement {tpl.placement} (change in Inspector). Layers: {byLayer}.");
    }

    [MenuItem("Tools/unwritten/Stamp Prop Template")]
    static void Stamp()
    {
        var tpl = Selection.activeObject as PropTemplate;
        if (tpl == null)
        {
            EditorUtility.DisplayDialog("Stamp Prop Template",
                "Select a PropTemplate asset in the Project window first " +
                "(captured via Capture Prop Template), then run this.", "OK");
            return;
        }

        var grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
        if (grid == null)
        {
            EditorUtility.DisplayDialog("Stamp Prop Template",
                "No Grid in the open scene. Open a map scene (or add a Grid), then run again.", "OK");
            return;
        }

        // Place the stamp where you're looking in the Scene view, so repeated stamps land in
        // different spots (this is a placement TEST — the generator does real spacing).
        // Falls back to cell (0,0) if there's no open Scene view.
        var origin = Vector3Int.zero;
        var view = SceneView.lastActiveSceneView;
        if (view != null) origin = grid.WorldToCell(view.pivot);
        var touched = new HashSet<Tilemap>();
        foreach (var c in tpl.cells)
        {
            var tm = FindOrCreateLayer(grid, c.layer);
            if (touched.Add(tm)) Undo.RegisterCompleteObjectUndo(tm, "Stamp Prop Template");
            tm.SetTile(origin + new Vector3Int(c.pos.x, c.pos.y, 0), c.tile);
            EditorUtility.SetDirty(tm);
        }

        // Editor preview: DepthSortRuntime only runs at Play, so band every tilemap now (by name).
        foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
        {
            var tr = tm.GetComponent<TilemapRenderer>();
            if (tr != null) Undo.RecordObject(tr, "Stamp Prop Template");
            DepthSortRuntime.BandTilemap(tm);
        }

        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        Debug.Log($"[unwritten] Stamped '{tpl.name}' ({tpl.cells.Count} cells) at {origin} across " +
                  $"{touched.Count} layer(s): {string.Join(", ", touched.Select(t => t.name))}. " +
                  "Banded all tilemaps for the Scene-view preview; press Play to see DepthSortRuntime bake it.");
    }

    // ---- helpers -------------------------------------------------------------

    // Prefer Tilemaps in the current selection; otherwise every Tilemap under the open Grid.
    static Tilemap[] ResolveSourceTilemaps()
    {
        var picked = Selection.gameObjects
            .Select(g => g.GetComponent<Tilemap>())
            .Where(t => t != null)
            .Distinct()
            .ToArray();
        if (picked.Length > 0) return picked;

        var grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
        return grid != null
            ? grid.GetComponentsInChildren<Tilemap>(true)
            : Array.Empty<Tilemap>();
    }

    static Tilemap FindOrCreateLayer(Grid grid, string name)
    {
        foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
            if (string.Equals(tm.name, name, StringComparison.OrdinalIgnoreCase))
                return tm;

        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid.transform, false);
        Undo.RegisterCreatedObjectUndo(go, "Stamp Prop Template");
        var created = go.GetComponent<Tilemap>();
        DepthSortRuntime.BandTilemap(created);          // same sorting the runtime expects
        if (IsSolidLayer(name)) go.AddComponent<TilemapCollider2D>();
        return created;
    }

    static bool IsSolidLayer(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("building") || n.Contains("house") || n.Contains("wall") ||
               n.Contains("obstacle") || n.Contains("fence") || n.Contains("solid") ||
               n.Contains("tree") || n.Contains("collision");
    }

    static void CreateFolders(string path)
    {
        var parts = path.Split('/');
        string cur = parts[0];   // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
