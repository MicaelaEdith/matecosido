using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyWave
{
    public string name;
    public List<EnemyController> enemies = new List<EnemyController>();
    public bool melee;
    public float attackRange = 2f;
    public float damage = 10f;
    public float attackCooldown = 1.2f;

    [Range(0f, 1f)]
    [Tooltip("Fracción de la ola anterior que debe caer para que esta aparezca.")]
    public float spawnAfterPreviousDefeatedFraction = 1f;

    [Tooltip("Alterna cachiporra/disparo según el índice del enemigo en la ola.")]
    public bool alternateMelee;
}

public class EnemyWaveManager : MonoBehaviour
{
    public event Action<int> WaveStarted;
    public event Action AllWavesCleared;

    [SerializeField] private List<EnemyWave> waves = new List<EnemyWave>();
    [SerializeField] private CompanyAreaController companyArea;
    [SerializeField] private bool requirePlayerInsideCompany = true;
    [SerializeField] private bool startOnCompanyEnter = true;
    [SerializeField] private float autoStartDistance = 18f;
    [SerializeField] private bool advanceMissionOnClear = true;
    [SerializeField] private MissionState missionStateOnClear = MissionState.CollectMoney;

    [Header("First Assault Completion")]
    [SerializeField] private bool completeOnPolice01To12Cleared = true;
    [SerializeField] private string firstAssaultNamePrefix = "Police_";
    [SerializeField] private int firstAssaultMinIndex = 1;
    [SerializeField] private int firstAssaultMaxIndex = 12;

    public int CurrentWaveIndex { get; private set; } = -1;
    public bool AllCleared { get; private set; }

    public void ConfigureWave(int index, string waveName, IEnumerable<EnemyController> enemies, bool melee,
        float attackRange, float damage, float attackCooldown, float spawnAfterPreviousDefeatedFraction = 1f)
    {
        while (waves.Count <= index)
            waves.Add(new EnemyWave());

        EnemyWave wave = waves[index];
        wave.name = waveName;
        wave.enemies.Clear();
        wave.enemies.AddRange(enemies);
        wave.melee = melee;
        wave.attackRange = attackRange;
        wave.damage = damage;
        wave.attackCooldown = attackCooldown;
        wave.spawnAfterPreviousDefeatedFraction = Mathf.Clamp01(spawnAfterPreviousDefeatedFraction);
    }

    public void SetAlternateMelee(int index, bool alternate)
    {
        if (index >= 0 && index < waves.Count)
            waves[index].alternateMelee = alternate;
    }

    public void DeactivateWave(int index)
    {
        if (index < 0 || index >= waves.Count)
            return;

        foreach (EnemyController enemy in waves[index].enemies)
        {
            if (enemy != null)
                enemy.gameObject.SetActive(false);
        }
    }

    private readonly HashSet<EnemyHealth> subscribed = new HashSet<EnemyHealth>();
    private readonly HashSet<int> startedWaves = new HashSet<int>();
    private readonly List<EnemyHealth> firstAssaultGroup = new List<EnemyHealth>();
    private bool started;
    private PlayerController cachedPlayer;

    private void Awake()
    {
        CacheFirstAssaultGroup();
    }

    private void OnEnable()
    {
        CacheFirstAssaultGroup();

        if (companyArea != null)
            companyArea.PlayerInsideChanged += HandlePlayerInsideChanged;

        if (startOnCompanyEnter && companyArea != null && companyArea.IsPlayerInside && !started)
            StartWaves();
    }

    private void OnDisable()
    {
        if (companyArea != null)
            companyArea.PlayerInsideChanged -= HandlePlayerInsideChanged;

        UnsubscribeAll();
    }

    private void Update()
    {
        if (started || AllCleared || !startOnCompanyEnter)
            return;

        if (IsPlayerNearEnemy())
            StartWaves();
    }

    private bool IsPlayerNearEnemy()
    {
        if (cachedPlayer == null)
            cachedPlayer = FindObjectOfType<PlayerController>();

        if (cachedPlayer == null)
            return false;

        foreach (EnemyWave wave in waves)
        {
            foreach (EnemyController enemy in wave.enemies)
            {
                if (enemy == null)
                    continue;

                if (Vector3.Distance(cachedPlayer.transform.position, enemy.transform.position) <= autoStartDistance)
                    return true;
            }
        }

        return false;
    }

    private void HandlePlayerInsideChanged(bool inside)
    {
        if (inside && startOnCompanyEnter && !started)
            StartWaves();
    }

    [ContextMenu("Start Waves")]
    public void StartWaves()
    {
        if (started || AllCleared)
            return;

        started = true;
        startedWaves.Clear();
        SubscribeFirstAssaultGroup();
        StartWave(0);

        if (ArePolice01To12Cleared())
            CompleteAllWaves();
    }

    private void StartWave(int index)
    {
        if (index < 0 || index >= waves.Count)
        {
            CompleteAllWaves();
            return;
        }

        CurrentWaveIndex = index;
        startedWaves.Add(index);
        EnemyWave wave = waves[index];

        if (GameManager.Instance != null)
            GameManager.Instance.SetInCombat(true);

        int aliveCount = 0;

        for (int enemyIndex = 0; enemyIndex < wave.enemies.Count; enemyIndex++)
        {
            EnemyController enemy = wave.enemies[enemyIndex];

            if (enemy == null)
                continue;

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();

            if (health == null || health.IsDead)
                continue;

            aliveCount++;

            bool useMelee = wave.alternateMelee ? enemyIndex % 2 == 0 : wave.melee;

            enemy.SetCompanyArea(companyArea);
            enemy.ConfigureAttack(wave.attackRange, wave.damage, wave.attackCooldown, useMelee);
            enemy.SetCombatEnabled(!requirePlayerInsideCompany || companyArea == null || companyArea.IsPlayerInside);

            if (subscribed.Add(health))
                health.Died += HandleEnemyDied;
        }

        if (aliveCount == 0)
        {
            Debug.LogError($"EnemyWaveManager: la ola '{wave.name}' no tiene enemigos válidos. No se avanzará la misión.", this);
            return;
        }

        WaveStarted?.Invoke(index);
    }

    private void HandleEnemyDied()
    {
        if (AllCleared || !started)
            return;

        if (ArePolice01To12Cleared())
        {
            CompleteAllWaves();
            return;
        }

        if (startedWaves.Contains(0) && IsWaveCleared(waves[0]))
        {
            CompleteAllWaves();
            return;
        }

        if (CurrentWaveIndex < 0 || CurrentWaveIndex >= waves.Count)
            return;

        for (int i = 1; i < waves.Count; i++)
        {
            if (startedWaves.Contains(i))
                continue;

            if (i > 0 && GetDefeatedFraction(waves[i - 1]) < waves[i].spawnAfterPreviousDefeatedFraction)
                break;

            StartWave(i);
        }
    }

    private float GetDefeatedFraction(EnemyWave wave)
    {
        int total = 0;
        int defeated = 0;

        foreach (EnemyController enemy in wave.enemies)
        {
            EnemyHealth health = enemy != null ? enemy.GetComponent<EnemyHealth>() : null;
            if (health == null)
                continue;

            total++;
            if (health.IsDead)
                defeated++;
        }

        return total == 0 ? 0f : (float)defeated / total;
    }

    private bool AllStartedWavesCleared()
    {
        foreach (int index in startedWaves)
        {
            if (!IsWaveCleared(waves[index]))
                return false;
        }

        return true;
    }

    private bool IsWaveCleared(EnemyWave wave)
    {
        foreach (EnemyController enemy in wave.enemies)
        {
            if (enemy == null)
                continue;

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null && !health.IsDead)
                return false;
        }

        return true;
    }

    private void CompleteAllWaves()
    {
        if (AllCleared)
            return;

        AllCleared = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetInCombat(false);

        UnsubscribeAll();

        if (advanceMissionOnClear && MissionManager.Instance != null)
            MissionManager.Instance.AdvanceTo(missionStateOnClear);

        AllWavesCleared?.Invoke();
    }

    private void CacheFirstAssaultGroup()
    {
        firstAssaultGroup.Clear();

        if (!completeOnPolice01To12Cleared)
            return;

        EnemyController[] controllers = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            EnemyController controller = controllers[i];
            if (controller == null || !IsInFirstAssaultGroup(controller.gameObject.name))
                continue;

            EnemyHealth health = controller.GetComponent<EnemyHealth>();
            if (health != null && !firstAssaultGroup.Contains(health))
                firstAssaultGroup.Add(health);
        }
    }

    private bool IsInFirstAssaultGroup(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(firstAssaultNamePrefix))
            return false;

        if (!objectName.StartsWith(firstAssaultNamePrefix, StringComparison.Ordinal))
            return false;

        string suffix = objectName.Substring(firstAssaultNamePrefix.Length);
        if (!int.TryParse(suffix, out int index))
            return false;

        return index >= firstAssaultMinIndex && index <= firstAssaultMaxIndex;
    }

    private void SubscribeFirstAssaultGroup()
    {
        if (!completeOnPolice01To12Cleared)
            return;

        if (firstAssaultGroup.Count == 0)
            CacheFirstAssaultGroup();

        for (int i = 0; i < firstAssaultGroup.Count; i++)
        {
            EnemyHealth health = firstAssaultGroup[i];
            if (health != null && subscribed.Add(health))
                health.Died += HandleEnemyDied;
        }
    }

    private bool ArePolice01To12Cleared()
    {
        if (!completeOnPolice01To12Cleared)
            return false;

        if (firstAssaultGroup.Count == 0)
            CacheFirstAssaultGroup();

        if (firstAssaultGroup.Count == 0)
            return false;

        for (int i = 0; i < firstAssaultGroup.Count; i++)
        {
            EnemyHealth health = firstAssaultGroup[i];
            if (health == null)
                continue;

            if (!health.IsDead)
                return false;
        }

        return true;
    }

    private void UnsubscribeAll()
    {
        foreach (EnemyHealth health in subscribed)
        {
            if (health != null)
                health.Died -= HandleEnemyDied;
        }

        subscribed.Clear();
    }
}
