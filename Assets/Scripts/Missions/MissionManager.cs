using System;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    public event Action<MissionState, MissionState> StateChanged;
    public event Action GameWon;
    public event Action GameOver;

    [SerializeField] private MissionState startState = MissionState.NotStarted;

    public MissionState CurrentState { get; private set; }
    public bool HasMoney { get; private set; }
    public bool IsGameWon { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentState = startState;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool SetState(MissionState newState)
    {
        if (CurrentState == newState)
            return false;

        MissionState previousState = CurrentState;
        CurrentState = newState;
        StateChanged?.Invoke(previousState, CurrentState);

        if (CurrentState == MissionState.Won && !IsGameWon)
        {
            IsGameWon = true;
            GameWon?.Invoke();
        }

        return true;
    }

    public bool AdvanceTo(MissionState newState)
    {
        if ((int)newState <= (int)CurrentState)
            return false;

        return SetState(newState);
    }

    public void SetHasMoney(bool value)
    {
        HasMoney = value;
    }

    public void CollectMoney(MissionState nextState = MissionState.ReturnToNpc1)
    {
        if (HasMoney)
            return;

        HasMoney = true;
        AdvanceTo(MissionState.CollectMoney);
        AdvanceTo(nextState);
    }

    public void DeliverMoneyToNpc1(MissionState nextState = MissionState.GoToStation)
    {
        if (!HasMoney)
            return;

        AdvanceTo(nextState);
    }

    public void DeliverMoneyToNpc2(MissionState nextState = MissionState.Won)
    {
        if (!HasMoney)
            return;

        HasMoney = false;
        AdvanceTo(MissionState.DeliverToNpc2);
        AdvanceTo(nextState);
    }

    public void TriggerGameOver()
    {
        GameOver?.Invoke();
    }

    [ContextMenu("Debug/Advance To Next State")]
    private void DebugAdvanceToNextState()
    {
        int next = (int)CurrentState + 1;
        if (next <= (int)MissionState.Won)
            AdvanceTo((MissionState)next);
    }

    [ContextMenu("Debug/Set Has Money")]
    private void DebugSetHasMoney()
    {
        SetHasMoney(true);
    }
}
