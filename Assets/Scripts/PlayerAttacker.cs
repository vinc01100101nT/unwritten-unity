using UnityEngine;

/// <summary>
/// A simple melee swing. Press the attack key (default J) to hit anything with a
/// <see cref="Health"/> in a small circle just in front of the player, aimed by
/// <see cref="PlayerController2D.Facing"/>. No combo/animation yet — Phase D just
/// needs "attack → monster takes damage → dies".
/// </summary>
[RequireComponent(typeof(PlayerController2D))]
public class PlayerAttacker : MonoBehaviour
{
    public KeyCode attackKey = KeyCode.J;
    public int damage = 1;

    [Tooltip("How far in front of the player the swing reaches.")]
    public float range = 0.9f;
    [Tooltip("Radius of the swing's hit circle.")]
    public float radius = 0.6f;
    [Tooltip("Seconds between swings.")]
    public float cooldown = 0.35f;

    PlayerController2D controller;
    float nextTime;

    void Awake() { controller = GetComponent<PlayerController2D>(); }

    void Update()
    {
        if (Input.GetKeyDown(attackKey) && Time.time >= nextTime)
        {
            nextTime = Time.time + cooldown;
            Swing();
        }
    }

    void Swing()
    {
        Vector2 origin = (Vector2)transform.position + controller.Facing * range;
        foreach (var hit in Physics2D.OverlapCircleAll(origin, radius))
        {
            if (hit.transform == transform) continue;          // never hit ourselves
            var hp = hit.GetComponentInParent<Health>();        // monsters carry Health; walls/NPCs don't
            if (hp != null && !hp.IsDead) hp.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        var c = GetComponent<PlayerController2D>();
        Vector2 f = c != null ? c.Facing : Vector2.down;
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere((Vector2)transform.position + f * range, radius);
    }
}
