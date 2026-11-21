using UnityEngine;

public class MissionEnemyActivator : MonoBehaviour
{
    [SerializeField] private EnemyController[] enemies;
    [SerializeField] private MissionState triggerState = MissionState.GoToStation;
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    public bool AllEnemiesDefeated
    {
        get
        {
            if (enemies == null || enemies.Length == 0)
                return false;

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null)
                    continue;

                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health == null || !health.IsDead)
                    return false;
            }

            return true;
        }
    }

    private void Start()
    {
        MissionManager manager = MissionManager.Instance;
        if (manager == null)
            return;

        manager.StateChanged += HandleStateChanged;
        HandleStateChanged(manager.CurrentState, manager.CurrentState);
    }

    private void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(MissionState previous, MissionState current)
    {
        if (triggered && triggerOnce)
            return;

        if (current != triggerState)
            return;

        triggered = true;

        if (enemies == null)
            return;

        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null)
                continue;

            enemy.gameObject.SetActive(true);
            enemy.SetIgnoreCompanyArea(true);
            enemy.SetCombatEnabled(true);
        }
    }
}
