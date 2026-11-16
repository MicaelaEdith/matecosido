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
}

public class EnemyWaveManager : MonoBehaviour
{
    public event Action<int> WaveStarted;
    public event Action AllWavesCleared;

    [SerializeField] private List<EnemyWave> waves = new List<EnemyWave>();
    [SerializeField] private CompanyAreaController companyArea;
    [SerializeField] private bool requirePlayerInsideCompany = true;
    [SerializeField] private bool startOnCompanyEnter = true;
    [SerializeField] private bool advanceMissionOnClear = true;
    [SerializeField] private MissionState missionStateOnClear = MissionState.CollectMoney;

    public int CurrentWaveIndex { get; private set; } = -1;
    public bool AllCleared { get; private set; }

    private readonly HashSet<EnemyHealth> subscribed = new HashSet<EnemyHealth>();
    private bool started;

    private void OnEnable()
    {
        if (companyArea != null)
            companyArea.PlayerInsideChanged += HandlePlayerInsideChanged;
    }

    private void OnDisable()
    {
        if (companyArea != null)
            companyArea.PlayerInsideChanged -= HandlePlayerInsideChanged;

        UnsubscribeAll();
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
        StartWave(0);
    }

    private void StartWave(int index)
    {
        if (index < 0 || index >= waves.Count)
        {
            CompleteAllWaves();
            return;
        }

        CurrentWaveIndex = index;
        EnemyWave wave = waves[index];

        if (GameManager.Instance != null)
            GameManager.Instance.SetInCombat(true);

        int aliveCount = 0;

        foreach (EnemyController enemy in wave.enemies)
        {
            if (enemy == null)
                continue;

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();

            if (health == null || health.IsDead)
                continue;

            aliveCount++;

            enemy.SetCompanyArea(companyArea);
            enemy.ConfigureAttack(wave.attackRange, wave.damage, wave.attackCooldown, wave.melee);
            enemy.SetCombatEnabled(!requirePlayerInsideCompany || companyArea == null || companyArea.IsPlayerInside);

            if (subscribed.Add(health))
                health.Died += HandleEnemyDied;
        }

        WaveStarted?.Invoke(index);

        if (aliveCount == 0)
            StartWave(index + 1);
    }

    private void HandleEnemyDied()
    {
        if (AllCleared || CurrentWaveIndex < 0 || CurrentWaveIndex >= waves.Count)
            return;

        if (!IsWaveCleared(waves[CurrentWaveIndex]))
            return;

        StartWave(CurrentWaveIndex + 1);
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
