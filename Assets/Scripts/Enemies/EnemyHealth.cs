using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public event Action Died;

    [SerializeField] private float maxHealth = 50f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 knockbackDirection = default, float knockbackForce = 0f)
    {
        if (IsDead || amount <= 0f)
            return;

        if (knockbackForce > 0f)
        {
            EnemyController enemy = GetComponent<EnemyController>();
            if (enemy != null)
                enemy.ApplyKnockback(knockbackDirection, knockbackForce);
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
        {
            Die();
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPoliceTakeDamage();
    }

    public void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPoliceDeath();

        Died?.Invoke();
    }

    [ContextMenu("Kill")]
    private void KillFromContextMenu()
    {
        Die();
    }
}
