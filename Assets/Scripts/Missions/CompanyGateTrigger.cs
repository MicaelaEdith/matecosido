using System;
using UnityEngine;

public class CompanyGateTrigger : MonoBehaviour
{
    public event Action PlayerEntered;
    public event Action PlayerExited;

    [SerializeField] private bool advanceMissionOnEnter = true;

    public bool IsPlayerInside { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other) || IsPlayerInside)
            return;

        IsPlayerInside = true;

        if (advanceMissionOnEnter && MissionManager.Instance != null)
            MissionManager.Instance.AdvanceTo(MissionState.CompanyAssault);

        PlayerEntered?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other) || !IsPlayerInside)
            return;

        IsPlayerInside = false;
        PlayerExited?.Invoke();
    }

    private static bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerController>() != null;
    }
}
