using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int maxLives = 5;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;

    public event Action<float, float> HealthChanged;
    public event Action<int, int> LivesChanged;
    public event Action Died;
    public event Action Respawned;
    public event Action OutOfLives;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public int CurrentLives { get; private set; }
    public int MaxLives => maxLives;
    public bool IsDead { get; private set; }
    public bool CanRespawn => CurrentLives > 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentLives = maxLives;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPlayerTakeDamage();

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetRespawnPoint(Transform point)
    {
        respawnPoint = point;
    }

    public void Respawn()
    {
        if (!IsDead)
            return;

        if (CurrentLives <= 0)
        {
            OutOfLives?.Invoke();
            return;
        }

        CurrentLives--;
        CurrentHealth = maxHealth;
        IsDead = false;

        TeleportToRespawnPoint();

        LivesChanged?.Invoke(CurrentLives, maxLives);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Respawned?.Invoke();
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        Died?.Invoke();

        if (CurrentLives <= 0)
            OutOfLives?.Invoke();
    }

    private void TeleportToRespawnPoint()
    {
        if (respawnPoint == null)
            return;

        CharacterController controller = GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;

        if (controllerWasEnabled)
            controller.enabled = false;

        transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

        if (controllerWasEnabled)
            controller.enabled = true;
    }

    [ContextMenu("Take 25 Damage")]
    private void TakeTestDamage()
    {
        TakeDamage(25f);
    }

    [ContextMenu("Respawn")]
    private void RespawnFromContextMenu()
    {
        Respawn();
    }
}
