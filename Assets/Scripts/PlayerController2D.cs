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
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + input * (moveSpeed * Time.fixedDeltaTime));
    }
}
