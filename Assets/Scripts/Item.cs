using UnityEngine;

/// <summary>Which equipment slot an item fits. None = a bag-only item (consumable / material).</summary>
public enum EquipSlotType { None, Head, Body, Weapon, OffHand, Boots, Trinket }

/// <summary>
/// A piece of gear or a bag item. For Phase E it's just an icon + which slot it
/// fits; stats (attack/defense/…) get added in Phase F. Create more via
/// Assets ▸ Create ▸ unwritten ▸ Item, or Tools ▸ unwritten ▸ Create Starter Items.
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "unwritten/Item")]
public class Item : ScriptableObject
{
    public string displayName = "Item";
    public Sprite icon;

    [Tooltip("Which equipment slot this fits. None = bag-only (consumable/material).")]
    public EquipSlotType slot = EquipSlotType.None;

    [TextArea] public string description;
}
