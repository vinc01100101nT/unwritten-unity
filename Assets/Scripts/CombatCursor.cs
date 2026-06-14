using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Swaps the pointer to a sword icon while it's hovering an attackable monster, so
/// "click = attack" reads differently from normal UI/movement clicks. Lives on the
/// persistent UI canvas; the sword Image is assigned by UIShellBuilder. Uses the
/// same hover test as <see cref="PlayerAttacker"/> so the two always agree.
/// </summary>
public class CombatCursor : MonoBehaviour
{
    [Tooltip("The sword sprite shown at the cursor (assigned by the builder).")]
    public Image icon;
    [Tooltip("Screen-pixel offset of the sword from the actual pointer.")]
    public Vector2 offset = new Vector2(10, -10);

    Camera cam;
    bool systemCursorHidden;

    void Awake() { cam = Camera.main; ShowSystemCursor(); }
    void OnDisable() => ShowSystemCursor();

    void Update()
    {
        if (icon == null) return;
        if (cam == null) cam = Camera.main;

        bool overEnemy = !PlayerAttacker.PointerOverUI() &&
                         PlayerAttacker.MonsterUnderCursor(cam) != null;

        if (overEnemy)
        {
            if (!systemCursorHidden) { Cursor.visible = false; systemCursorHidden = true; }
            icon.enabled = true;
            ((RectTransform)icon.transform).position = (Vector2)Input.mousePosition + offset;
        }
        else ShowSystemCursor();
    }

    void ShowSystemCursor()
    {
        if (systemCursorHidden) { Cursor.visible = true; systemCursorHidden = false; }
        if (icon != null) icon.enabled = false;
    }
}
