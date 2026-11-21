using UnityEngine;

public class CombatMusicZone : MonoBehaviour
{
    [Header("Start")]
    [SerializeField] private float startX = 100f;
    [SerializeField] private MissionState requiredState = MissionState.GoToStation;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform npc2;
    [SerializeField] private MissionEnemyActivator stationActivator;

    [Header("Stop")]
    [SerializeField] private float npc2Distance = 4f;

    private GameManager gameManager;
    private MissionManager missionManager;
    private bool started;
    private bool stopped;

    private void Start()
    {
        gameManager = GameManager.Instance;
        missionManager = MissionManager.Instance;
    }

    private void Update()
    {
        if (started && stopped)
            return;
        if (gameManager == null || missionManager == null || player == null || npc2 == null)
            return;
        if (gameManager.IsGameOver || gameManager.IsGameWon)
            return;
        if (missionManager.CurrentState < requiredState)
            return;

        if (!started)
        {
            if (player.position.x >= startX)
            {
                started = true;
                gameManager.SetInCombat(true);
            }
            return;
        }

        bool defeated = stationActivator != null && stationActivator.AllEnemiesDefeated;
        bool delivered = missionManager.CurrentState >= MissionState.DeliverToNpc2;
        if ((defeated || delivered) && IsNearNpc2())
        {
            stopped = true;
            gameManager.SetInCombat(false);
        }
    }

    private bool IsNearNpc2()
    {
        Vector3 a = player.position;
        Vector3 b = npc2.position;
        a.y = 0f;
        b.y = 0f;
        return (a - b).sqrMagnitude <= npc2Distance * npc2Distance;
    }
}
