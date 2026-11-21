using System;
using TMPro;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    public event Action<MissionState, MissionState> StateChanged;
    public event Action GameWon;
    public event Action GameOver;

    [SerializeField] private MissionState startState = MissionState.NotStarted;

    [Header("Money / Station Flow (auto-resolved if empty)")]
    [SerializeField] private GameObject moneyObject;
    [SerializeField] private TMP_Text moneyLabel;
    [SerializeField] private GameObject stationNpc;
    [SerializeField] private string fullMoneyText = "$1.000.000";
    [SerializeField] private string halfMoneyText = "$500.000";

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

    private void Start()
    {
        ResolveMissionFlowReferences();
        SetupMoneyObject();
        RefreshMissionFlow();
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
        RefreshMissionFlow();

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
        RefreshMissionFlow();
    }

    public void CollectMoney(MissionState nextState = MissionState.ReturnToNpc1)
    {
        if (HasMoney)
            return;

        HasMoney = true;
        AdvanceTo(MissionState.CollectMoney);
        AdvanceTo(nextState);
        RefreshMissionFlow();
    }

    public void DeliverMoneyToNpc1(MissionState nextState = MissionState.GoToStation)
    {
        if (!HasMoney)
            return;

        EnemyWaveManager waveManager = FindFirstObjectByType<EnemyWaveManager>();
        if (waveManager != null)
            waveManager.DeactivateWave(0);

        AdvanceTo(nextState);
    }

    public void DeliverMoneyToNpc2(MissionState nextState = MissionState.Won)
    {
        if (!HasMoney)
            return;

        HasMoney = false;
        AdvanceTo(MissionState.DeliverToNpc2);
        AdvanceTo(nextState);
        RefreshMissionFlow();
    }

    public void TriggerGameOver()
    {
        GameOver?.Invoke();
    }

    private void ResolveMissionFlowReferences()
    {
        if (moneyLabel == null)
        {
            GameObject labelObject = FindSceneObjectByName("lbl_money");
            if (labelObject != null)
                moneyLabel = labelObject.GetComponent<TMP_Text>();
        }

        if (moneyObject == null)
            moneyObject = FindSceneObjectByName("Money");

        if (stationNpc == null)
            stationNpc = FindSceneObjectByName("NPC2");
    }

    private void SetupMoneyObject()
    {
        if (moneyObject == null)
            return;

        Collider pickupCollider = moneyObject.GetComponent<Collider>();
        if (pickupCollider == null)
            pickupCollider = moneyObject.AddComponent<BoxCollider>();

        pickupCollider.isTrigger = true;

        MoneyPickup pickup = moneyObject.GetComponent<MoneyPickup>();
        if (pickup == null)
            pickup = moneyObject.AddComponent<MoneyPickup>();
    }

    private void RefreshMissionFlow()
    {
        UpdateMoneyObjectVisibility();
        UpdateMoneyLabel();
        UpdateStationNpcVisibility();
    }

    private void UpdateMoneyObjectVisibility()
    {
        if (moneyObject == null)
            return;

        bool showMoney = CurrentState == MissionState.CollectMoney && !HasMoney;
        if (moneyObject.activeSelf != showMoney)
            moneyObject.SetActive(showMoney);
    }

    private void UpdateMoneyLabel()
    {
        if (moneyLabel == null)
            return;

        bool showLabel = HasMoney && CurrentState >= MissionState.ReturnToNpc1 && CurrentState < MissionState.DeliverToNpc2;
        if (!showLabel)
        {
            if (moneyLabel.gameObject.activeSelf)
                moneyLabel.gameObject.SetActive(false);
            return;
        }

        if (!moneyLabel.gameObject.activeSelf)
            moneyLabel.gameObject.SetActive(true);

        string value = CurrentState >= MissionState.GoToStation ? halfMoneyText : fullMoneyText;
        foreach (TMP_Text label in moneyLabel.GetComponentsInChildren<TMP_Text>(true))
            label.text = value;
    }

    private void UpdateStationNpcVisibility()
    {
        if (stationNpc == null)
            return;

        bool showStationNpc = CurrentState >= MissionState.GoToStation;
        if (stationNpc.activeSelf != showStationNpc)
            stationNpc.SetActive(showStationNpc);
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate == null || candidate.name != objectName)
                continue;

            if (!candidate.scene.IsValid() || !candidate.scene.isLoaded)
                continue;

            if ((candidate.hideFlags & HideFlags.HideInHierarchy) != 0)
                continue;

            return candidate;
        }

        return null;
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
