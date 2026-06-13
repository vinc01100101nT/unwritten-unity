using UnityEngine;

/// <summary>
/// Top-down 8-directional movement (the right feel for a roguelike). Reads the
/// legacy Input axes (WASD / arrow keys) and moves a Rigidbody2D so the player
/// collides with walls and crates instead of passing through them.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Tooltip("World units per second.")]
    public float moveSpeed = 6f;

    /// <summary>Last-faced cardinal direction (defaults to down). Used by combat
    /// to aim attacks even while standing still.</summary>
    public Vector2 Facing { get; private set; } = Vector2.down;

    Rigidbody2D rb;
    Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        // Don't let diagonal movement be faster than cardinal movement.
        if (input.sqrMagnitude > 1f) input = input.normalized;

        // Remember the way we're facing (cardinal) for aiming attacks.
        if (input.sqrMagnitude > 0.01f)
            Facing = Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                ? new Vector2(Mathf.Sign(input.x), 0f)
                : new Vector2(0f, Mathf.Sign(input.y));
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + input * (moveSpeed * Time.fixedDeltaTime));
    }
}
