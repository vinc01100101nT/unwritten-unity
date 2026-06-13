using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates a small set of <see cref="Item"/> assets in Assets/Items so the bag
/// has something to drag. Uses real art where it exists — weapons + consumables
/// from Art/NinjaAdventure/Items, and the UI equipment icons for armour slots
/// (the pack has no armour item art). Idempotent: skips items that already exist.
///
/// Menu: Tools ▸ unwritten ▸ Create Starter Items. Build UI Shell also calls this
/// so the bag is seeded automatically.
/// </summary>
public static class ItemStarterSet
{
    const string ART = "Assets/Art/NinjaAdventure";
    const string Dir = "Assets/Items";

    // (asset name, sprite path, equip slot)
    static readonly (string name, string sprite, EquipSlotType slot)[] Defs =
    {
        ("Iron Sword",  ART + "/Items/Weapons/Sword/Sprite.png",  EquipSlotType.Weapon),
        ("Katana",      ART + "/Items/Weapons/Katana/Sprite.png", EquipSlotType.Weapon),
        ("Sai",         ART + "/Items/Weapons/Sai/Sprite.png",    EquipSlotType.Weapon),
        ("Iron Helm",   ART + "/UI/Skill Icon/Items & Weapon/Helmet.png", EquipSlotType.Head),
        ("Leather Vest",ART + "/UI/Skill Icon/Items & Weapon/Armor.png",  EquipSlotType.Body),
        ("Boots",       ART + "/UI/Skill Icon/Items & Weapon/Boot.png",   EquipSlotType.Boots),
        ("Wooden Guard",ART + "/UI/Skill Icon/Items & Weapon/Guard.png",  EquipSlotType.OffHand),
        ("Jade Amulet", ART + "/UI/Skill Icon/Items & Weapon/Amulet.png", EquipSlotType.Trinket),
        ("Silver Ring", ART + "/UI/Skill Icon/Items & Weapon/Ring.png",   EquipSlotType.Trinket),
        ("Life Potion", ART + "/Items/Potion/LifePot.png",       EquipSlotType.None),
        ("Gold Coin",   ART + "/Items/Treasure/GoldCoin.png",    EquipSlotType.None),
        ("Fire Scroll", ART + "/Items/Scroll/ScrollFire.png",    EquipSlotType.None),
    };

    [MenuItem("Tools/unwritten/Create Starter Items")]
    static void CreateMenu()
    {
        int n = EnsureStarterItems().Count;
        Debug.Log($"[unwritten] Starter items ready in {Dir} ({n} total). Run Build UI Shell to seed the bag.");
    }

    /// <summary>Creates any missing starter items and returns the full set (sorted).</summary>
    public static List<Item> EnsureStarterItems()
    {
        EnsureFolder(Dir);
        foreach (var d in Defs)
        {
            string path = $"{Dir}/{d.name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Item>(path) != null) continue;

            var item = ScriptableObject.CreateInstance<Item>();
            item.displayName = d.name;
            item.slot = d.slot;
            item.icon = LoadSprite(d.sprite);
            AssetDatabase.CreateAsset(item, path);
        }
        AssetDatabase.SaveAssets();

        return AssetDatabase.FindAssets("t:Item", new[] { Dir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Item>)
            .Where(x => x != null)
            .OrderBy(x => x.name)
            .ToList();
    }

    static Sprite LoadSprite(string path)
        => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        var leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
