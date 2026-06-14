using UnityEngine;

/// <summary>
/// Top-down movement SERVICE for click-to-move (Dota-style) control. It no longer
/// reads WASD — instead <see cref="PlayerCommander"/> tells it where to go:
///   • <see cref="MoveTo"/>     — walk to a fixed ground point, then stop.
///   • <see cref="MoveToward"/> — chase a moving target (the caller decides when to stop).
///   • <see cref="Halt"/>       — stop now.
/// Motion is applied through Rigidbody2D.MovePosition so the player still collides
/// with walls and crates. Pathfinding is intentionally deferred: movement is a
/// straight line toward the destination (obstacles are handled by physics, not routing).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Tooltip("World units per second.")]
    public float moveSpeed = 6f;

    [Tooltip("Stop this close (world units) to a fixed MoveTo destination.")]
    public float arriveRadius = 0.06f;

    /// <summary>Last-faced cardinal direction (defaults to down). Derived from actual
    /// motion; kept for anything that wants to aim from the player's facing.</summary>
    public Vector2 Facing { get; private set; } = Vector2.down;

    /// <summary>True when there's no active destination (idle / arrived).</summary>
    public bool Arrived => !hasDestination;

    Rigidbody2D rb;

    Vector2 destination;
    bool hasDestination;
    bool stopOnArrive;     // MoveTo = true (stop within arriveRadius); MoveToward = false

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>Walk to a fixed ground point and stop on arrival.</summary>
    public void MoveTo(Vector2 point)
    {
        destination = point;
        hasDestination = true;
        stopOnArrive = true;
    }

    /// <summary>Continuously head toward a (possibly moving) target. The caller is
    /// responsible for stopping (e.g. once in attack range or at follow distance).</summary>
    public void MoveToward(Vector2 movingTarget)
    {
        destination = movingTarget;
        hasDestination = true;
        stopOnArrive = false;
    }

    /// <summary>Cancel any movement immediately.</summary>
    public void Halt()
    {
        hasDestination = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;   // kill any drift from collisions
    }

    void FixedUpdate()
    {
        if (!hasDestination) return;

        Vector2 pos = rb.position;
        Vector2 to = destination - pos;
        float dist = to.magnitude;

        if (stopOnArrive && dist <= arriveRadius) { Halt(); return; }
        if (dist <= 1e-4f) return;

        Vector2 dir = to / dist;
        float step = moveSpeed * Time.fixedDeltaTime;
        if (stopOnArrive) step = Mathf.Min(step, dist);   // never overshoot a fixed point

        rb.MovePosition(pos + dir * step);

        // Remember the way we're facing (cardinal) for aiming.
        Facing = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));
    }
}
