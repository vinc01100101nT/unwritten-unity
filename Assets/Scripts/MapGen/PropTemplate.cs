using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>
/// A captured multi-cell OBJECT footprint — the tiles of one hand-painted prop (house, tree,
/// well, statue, market stall, sign…), recorded per source layer and normalised so the
/// bottom-left of its bounding box is cell (0,0). The town generator stamps these as whole
/// units instead of placing loose single tiles.
///
/// This is the ONE generic object the generator places — there is deliberately no separate
/// "house" or "tree" type. WHAT a prop is comes from its captured layers (Buildings/wall/tree
/// → solid; Ground/Decor/Overhead → walkable trim); WHERE it may go comes from
/// <see cref="placement"/>. So a new kind of object never needs new code — capture it, tag it.
///
/// Author with Tools ▸ unwritten ▸ Capture Prop Template; verify with Stamp Prop Template.
/// (Formerly <c>HouseTemplate</c>; existing assets migrate automatically — same script GUID,
/// and <c>doorCell</c> carries over to <see cref="anchorCell"/>.)
/// </summary>
[CreateAssetMenu(menuName = "unwritten/Prop Template", fileName = "PropTemplate")]
public class PropTemplate : ScriptableObject
{
    [Serializable]
    public struct Cell
    {
        [Tooltip("Source tilemap name (e.g. Ground, Buildings, Decor, Overhead). Re-stamped " +
                 "to the same-named layer so DepthSortRuntime bands/bakes it correctly.")]
        public string layer;

        [Tooltip("Cell offset from the template origin (bottom-left of the bounding box).")]
        public Vector2Int pos;

        [Tooltip("The tile painted at this cell.")]
        public TileBase tile;
    }

    [Header("Placement")]
    [Tooltip("Where the generator may place this prop:\n" +
             "• Lot — building plots; gets a path carved to the road network.\n" +
             "• OpenGround — scattered in open space (trees, rocks).\n" +
             "• PlazaCenter — single town centrepiece (well/statue).\n" +
             "• Roadside — hugging an existing road (lamps/signs/stalls).")]
    public PropPlacement placement = PropPlacement.Lot;

    [Tooltip("Relative frequency vs other props sharing this placement (higher = more common).")]
    [Min(0f)] public float weight = 1f;

    [Header("Footprint")]
    [Tooltip("Footprint size in cells (width, height).")]
    public Vector2Int size;

    [Tooltip("Anchor / entrance cell (formerly 'door'): a Lot prop carves a path from the cell " +
             "just outside this to the nearest road. Defaults to bottom-centre on capture.")]
    [FormerlySerializedAs("doorCell")]
    public Vector2Int anchorCell;

    [Tooltip("Every painted cell across all captured layers, normalised to a (0,0) origin.")]
    public List<Cell> cells = new List<Cell>();

    /// <summary>The distinct layer (tilemap) names this template paints into.</summary>
    public IEnumerable<string> Layers
    {
        get
        {
            var seen = new HashSet<string>();
            foreach (var c in cells)
                if (seen.Add(c.layer)) yield return c.layer;
        }
    }
}

/// <summary>Where the generator may place a <see cref="PropTemplate"/>.</summary>
public enum PropPlacement { Lot, OpenGround, PlazaCenter, Roadside }
