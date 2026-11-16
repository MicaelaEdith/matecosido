using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MissionEnemyWave
{
    public string name;
    public List<EnemyController> enemies = new List<EnemyController>();
    public bool melee;
    public float attackRange = 2f;
    public float damage = 10f;
    public float attackCooldown = 1.2f;
}

public class MissionWaveEncounter : MonoBehaviour
{
    public event Action<int> WaveStarted;
    public event Action AllWavesCleared;

    [Header("Trigger")]
    [SerializeField] private MissionState startOnState = MissionState.FinalBattle;
    [SerializeField] private bool startOnTriggerEnter;
    [SerializeField] private bool requireMissionStateForTrigger = true;

    [Header("Waves")]
    [SerializeField] private List<MissionEnemyWave> waves = new List<MissionEnemyWave>();

    [Header("Mission")]
    [SerializeField] private bool setInCombat = true;
    [SerializeField] private bool advanceMissionOnClear = true;
    [SerializeField] private MissionState missionStateOnClear = MissionState.DeliverToNpc2;

    public int CurrentWaveIndex { get; private set; } = -1;
    public bool AllCleared { get; private set; }

    private readonly HashSet<EnemyHealth> subscribed = new HashSet<EnemyHealth>();
    private bool started;

    private void Start()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.StateChanged += HandleStateChanged;
            if (MissionManager.Instance.CurrentState == startOnState)
                StartWaves();
        }
    }

    private void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.StateChanged -= HandleStateChanged;

        UnsubscribeAll();
    }

    private void HandleStateChanged(MissionState previous, MissionState current)
    {
        if (current == startOnState)
            StartWaves();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!startOnTriggerEnter || started || AllCleared)
            return;

        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        if (requireMissionStateForTrigger && MissionManager.Instance != null && MissionManager.Instance.CurrentState != startOnState)
            return;

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
        MissionEnemyWave wave = waves[index];

        if (setInCombat && GameManager.Instance != null)
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

            enemy.ConfigureAttack(wave.attackRange, wave.damage, wave.attackCooldown, wave.melee);
            enemy.SetCombatEnabled(true);

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

    private bool IsWaveCleared(MissionEnemyWave wave)
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

        if (setInCombat && GameManager.Instance != null)
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
