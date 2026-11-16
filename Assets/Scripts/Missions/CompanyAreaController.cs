using System;
using UnityEngine;

public class CompanyAreaController : MonoBehaviour
{
    public event Action<bool> PlayerInsideChanged;

    [SerializeField] private CompanyGateTrigger[] gates;

    public bool IsPlayerInside { get; private set; }

    private void OnEnable()
    {
        if (gates == null)
            return;

        foreach (CompanyGateTrigger gate in gates)
        {
            if (gate == null)
                continue;

            gate.PlayerEntered += HandleGateEntered;
            gate.PlayerExited += HandleGateExited;
        }
    }

    private void OnDisable()
    {
        if (gates == null)
            return;

        foreach (CompanyGateTrigger gate in gates)
        {
            if (gate == null)
                continue;

            gate.PlayerEntered -= HandleGateEntered;
            gate.PlayerExited -= HandleGateExited;
        }
    }

    private void HandleGateEntered()
    {
        SetPlayerInside(true);
    }

    private void HandleGateExited()
    {
        if (gates == null)
        {
            SetPlayerInside(false);
            return;
        }

        foreach (CompanyGateTrigger gate in gates)
        {
            if (gate != null && gate.IsPlayerInside)
                return;
        }

        SetPlayerInside(false);
    }

    private void SetPlayerInside(bool value)
    {
        if (IsPlayerInside == value)
            return;

        IsPlayerInside = value;
        PlayerInsideChanged?.Invoke(IsPlayerInside);
    }
}
