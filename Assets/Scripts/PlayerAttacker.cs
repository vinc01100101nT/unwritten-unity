using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mouse-target melee combat. Left-click a monster to make it your active target
/// (a down-arrow marks it); while it's your target and within <see cref="range"/>
/// you auto-swing at it every <see cref="cooldown"/> seconds for Character.Attack
/// damage. SINGLE target only — clicking another monster switches target, clicking
/// empty ground or pressing Esc clears it. (Hitting several enemies at once is
/// being saved for a future Cleave skill in Phase G.)
/// </summary>
[RequireComponent(typeof(PlayerController2D))]
public class PlayerAttacker : MonoBehaviour
{
    [Tooltip("How close (world units) you must be to land a hit on your target.")]
    public float range = 1.4f;
    [Tooltip("Seconds between auto-attacks on your target.")]
    public float cooldown = 0.6f;
    [Tooltip("Fallback damage if there's no Character yet; normally uses Character.Attack.")]
    public int damage = 1;

    /// <summary>The monster currently being attacked (null = none).</summary>
    public Health Target { get; private set; }

    Camera cam;
    float nextSwing;
    TargetIndicator indicator;

    void Awake() => cam = Camera.main;

    void Update()
    {
        if (cam == null) cam = Camera.main;

        // Pick / switch / clear target on left-click (clicks on the UI are ignored).
        if (Input.GetMouseButtonDown(0) && !PointerOverUI())
            SetTarget(MonsterUnderCursor(cam));      // null when you click empty ground

        if (Input.GetKeyDown(KeyCode.Escape) && Target != null)
            SetTarget(null);

        // Forget a target that died or despawned.
        if (Target == null || Target.IsDead) { if (Target != null) SetTarget(null); return; }

        // Auto-attack while the target is in range.
        if (Time.time >= nextSwing &&
            Vector2.Distance(transform.position, Target.transform.position) <= range)
        {
            int power = Character.Instance != null ? Character.Instance.Attack : damage;
            Target.TakeDamage(power);
            nextSwing = Time.time + cooldown;
            if (Target == null || Target.IsDead) SetTarget(null);   // it just died
        }
    }

    void SetTarget(Health h)
    {
        Target = h;
        if (h != null && indicator == null) indicator = TargetIndicator.Create();
        if (indicator != null) indicator.Follow(h != null ? h.transform : null);
    }

    // ---- shared mouse helpers (also used by CombatCursor) ---------------------

    public static bool PointerOverUI()
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    /// <summary>The closest living monster under the mouse cursor, or null.</summary>
    public static Health MonsterUnderCursor(Camera cam)
    {
        if (cam == null) return null;
        Vector2 w = cam.ScreenToWorldPoint(Input.mousePosition);
        Health best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Physics2D.OverlapCircleAll(w, 0.35f))
        {
            var h = c.GetComponentInParent<Health>();
            if (h == null || h.IsDead || h.GetComponent<MonsterAI>() == null) continue;
            float d = Vector2.SqrMagnitude((Vector2)h.transform.position - w);
            if (d < bestDist) { bestDist = d; best = h; }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
