using System.Collections;
using UnityEngine;

/// <summary>
/// Hit points + hit feedback + death, for anything that can be hurt. Phase D uses
/// it on monsters; the player can get one later for Phase F. Call
/// <see cref="TakeDamage"/> to damage it — it flashes, and when HP hits zero it
/// raises <see cref="onDeath"/> then removes the GameObject.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Health : MonoBehaviour
{
    [Tooltip("Starting / maximum hit points.")]
    public int maxHealth = 3;

    [Header("Hit feedback")]
    public Color flashColor = Color.white;
    public float flashTime = 0.08f;

    [Tooltip("Invoked once, just before the GameObject is removed.")]
    public UnityEngine.Events.UnityEvent onDeath;

    public int Current { get; private set; }
    public bool IsDead => Current <= 0;

    SpriteRenderer sr;
    Color baseColor;

    void Awake()
    {
        Current = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        Current = Mathf.Max(0, Current - Mathf.Max(0, amount));

        if (isActiveAndEnabled) StartCoroutine(Flash());

        if (IsDead) Die();
    }

    IEnumerator Flash()
    {
        sr.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        if (sr != null) sr.color = baseColor;
    }

    void Die()
    {
        onDeath?.Invoke();
        // Minimal for Phase D: just remove it. (Loot / death FX / XP come later.)
        Destroy(gameObject);
    }
}
