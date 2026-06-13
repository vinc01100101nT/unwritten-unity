using UnityEngine;

/// <summary>
/// Lightweight, code-driven 4-direction sprite animator for top-down characters.
/// It derives movement from the transform's OWN position change each frame, so it
/// needs no reference to the controller and works for the player AND monsters,
/// however they move. Assign the sliced walk frames per direction in the
/// Inspector; idle shows the first frame of the last-faced direction.
///
/// Deliberately NOT an AnimatorController/blend tree: for plain 2D walk cycles
/// this is far less wiring and trivially reused. Swap to an Animator later if you
/// need animation events or complex transitions.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterAnimator2D : MonoBehaviour
{
    [Header("Walk frames (sliced from the character SpriteSheet)")]
    public Sprite[] down;
    public Sprite[] up;
    public Sprite[] left;
    public Sprite[] right;

    [Tooltip("Animation playback speed, in frames per second.")]
    public float framesPerSecond = 8f;

    [Tooltip("Below this speed (world units/sec) the character is treated as idle.")]
    public float moveThreshold = 0.05f;

    SpriteRenderer sr;
    Vector3 lastPos;
    Vector2 facing = Vector2.down;   // start facing the camera
    float timer;
    int frame;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        lastPos = transform.position;
    }

    void Update()
    {
        // Velocity straight from the position delta — works for any kind of mover.
        Vector2 vel = (transform.position - lastPos) / Mathf.Max(Time.deltaTime, 1e-5f);
        lastPos = transform.position;

        bool moving = vel.sqrMagnitude > moveThreshold * moveThreshold;
        if (moving)
            facing = Mathf.Abs(vel.x) > Mathf.Abs(vel.y)
                ? new Vector2(Mathf.Sign(vel.x), 0f)
                : new Vector2(0f, Mathf.Sign(vel.y));

        var clip = ClipFor(facing);
        if (clip == null || clip.Length == 0) return;

        if (moving)
        {
            timer += Time.deltaTime;
            float frameTime = 1f / Mathf.Max(framesPerSecond, 0.01f);
            while (timer >= frameTime)
            {
                timer -= frameTime;
                frame = (frame + 1) % clip.Length;
            }
        }
        else
        {
            frame = 0;     // idle = first frame of the current facing
            timer = 0f;
        }

        sr.sprite = clip[Mathf.Min(frame, clip.Length - 1)];
    }

    Sprite[] ClipFor(Vector2 dir)
    {
        if (dir.y > 0f) return up;
        if (dir.y < 0f) return down;
        if (dir.x < 0f) return left;
        return right;
    }
}
