using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A UI slot that holds one <see cref="Item"/>. A bag slot (accepts = None) takes
/// anything; an equipment slot only takes items whose <see cref="Item.slot"/>
/// matches. Shows the item's icon, or an optional greyed placeholder when empty.
/// Dropping a <see cref="DraggableItem"/> here moves/swaps the item.
/// </summary>
[RequireComponent(typeof(Image))]
public class ItemSlot : MonoBehaviour, IDropHandler
{
    [Tooltip("None = bag slot (holds any item). Otherwise only items of this type fit.")]
    public EquipSlotType accepts = EquipSlotType.None;

    public Image iconImage;

    [Tooltip("Shown when the slot is empty (e.g. a greyed equip icon). Optional.")]
    public Sprite emptyPlaceholder;

    [SerializeField] Item item;                  // serialized so seeded gear survives into Play
    public Item Item => item;

    void Awake() => Refresh();

    public bool Accepts(Item item)
    {
        if (item == null) return true;
        if (accepts == EquipSlotType.None) return true;   // bag holds anything
        return item.slot == accepts;
    }

    public void SetItem(Item newItem)
    {
        item = newItem;
        Refresh();
    }

    void Refresh()
    {
        if (iconImage == null) return;
        if (Item != null && Item.icon != null)
        {
            iconImage.sprite = Item.icon;
            iconImage.color = Color.white;
            iconImage.enabled = true;
        }
        else if (emptyPlaceholder != null)
        {
            iconImage.sprite = emptyPlaceholder;
            iconImage.color = Color.white;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    public void OnDrop(PointerEventData e)
    {
        var dragged = e.pointerDrag != null ? e.pointerDrag.GetComponent<DraggableItem>() : null;
        if (dragged != null && dragged.slot != null) TryMove(dragged.slot, this);
    }

    /// <summary>Move (or swap) an item from one slot to another, honoring equip-type rules.</summary>
    public static bool TryMove(ItemSlot from, ItemSlot to)
    {
        if (from == null || to == null || from == to || from.Item == null) return false;
        Item a = from.Item, b = to.Item;
        if (!to.Accepts(a)) return false;                  // target won't take it
        if (b != null && !from.Accepts(b)) return false;   // swap would put b somewhere invalid
        from.SetItem(b);                                   // b may be null (plain move)
        to.SetItem(a);
        return true;
    }
}
