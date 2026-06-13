using UnityEngine;

/// <summary>
/// Minimal top-down enemy brain. It wanders near its spawn point, then CHASES the
/// player once they come within <see cref="aggroRadius"/>, and gives up (returns
/// to wandering) once they get past <see cref="leashRadius"/>. Movement is via
/// Rigidbody2D.MovePosition so it collides with walls/obstacles, and the existing
/// <see cref="CharacterAnimator2D"/> animates it automatically from its motion.
///
/// The player is found by its <see cref="PlayerController2D"/>, so no tags needed.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MonsterAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;

    [Header("Senses (world units)")]
    [Tooltip("Start chasing when the player is within this distance.")]
    public float aggroRadius = 6f;
    [Tooltip("Give up the chase once the player is farther than this.")]
    public float leashRadius = 10f;
    [Tooltip("Stop this close to the player so it doesn't jitter on top of them.")]
    public float stopDistance = 0.6f;

    [Header("Wander (when not chasing)")]
    public float wanderRadius = 3f;
    public float wanderPauseMin = 1f;
    public float wanderPauseMax = 3f;

    Rigidbody2D rb;
    Transform player;
    Vector2 home;
    Vector2 wanderTarget;
    float nextWanderTime;
    bool chasing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        home = rb.position;
        wanderTarget = home;
    }

    void Start()
    {
        var pc = FindFirstObjectByType<PlayerController2D>();
        if (pc != null) player = pc.transform;
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position;
        float toPlayer = player != null ? Vector2.Distance(pos, player.position) : Mathf.Infinity;

        // Aggro / leash transitions.
        if (chasing && toPlayer > leashRadius) chasing = false;
        else if (!chasing && toPlayer <= aggroRadius) chasing = true;

        Vector2 dir = chasing ? ChaseDir(pos, toPlayer) : WanderDir(pos);
        if (dir != Vector2.zero)
            rb.MovePosition(pos + dir * (moveSpeed * Time.fixedDeltaTime));
    }

    Vector2 ChaseDir(Vector2 pos, float toPlayer)
    {
        if (player == null || toPlayer <= stopDistance) return Vector2.zero;
        return ((Vector2)player.position - pos).normalized;
    }

    Vector2 WanderDir(Vector2 pos)
    {
        if (Vector2.Distance(pos, wanderTarget) > 0.15f)
            return (wanderTarget - pos).normalized;

        // Reached the target — pause, then pick a new one near home.
        if (Time.time >= nextWanderTime)
        {
            wanderTarget = home + Random.insideUnitCircle * wanderRadius;
            nextWanderTime = Time.time + Random.Range(wanderPauseMin, wanderPauseMax);
        }
        return Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, leashRadius);
    }
}
