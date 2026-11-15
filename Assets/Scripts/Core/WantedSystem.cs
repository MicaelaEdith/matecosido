using System;
using UnityEngine;

public enum WantedTier
{
    Safe,
    Suspicious,
    Wanted
}

public class WantedSystem : MonoBehaviour
{
    public static WantedSystem Instance { get; private set; }

    [Header("Suspicion")]
    [SerializeField, Range(0f, 1f)] private float suspicion;
    [SerializeField, Range(0f, 1f)] private float safeThreshold = 0.25f;
    [SerializeField, Range(0f, 1f)] private float wantedThreshold = 0.75f;

    [Header("Decay")]
    [Tooltip("Cuánta sospecha baja por segundo. 0 para que no baje sola.")]
    [SerializeField] private float decayPerSecond = 0.05f;
    [Tooltip("Segundos sin cambios antes de empezar a bajar.")]
    [SerializeField] private float decayDelay = 5f;

    public event Action<float> Changed;
    public event Action<WantedTier> TierChanged;

    [Header("State")]
    [SerializeField] private WantedTier tier;

    public float Suspicion => suspicion;
    public float SafeThreshold => safeThreshold;
    public float WantedThreshold => wantedThreshold;
    public WantedTier Tier => tier;

    public bool IsSafe => Tier == WantedTier.Safe;
    public bool IsSuspicious => Tier == WantedTier.Suspicious;
    public bool IsWanted => Tier == WantedTier.Wanted;

    public bool IsAtLeast(WantedTier minimumTier)
    {
        return Tier >= minimumTier;
    }

    private float lastChangeTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        tier = EvaluateTier(suspicion);
        lastChangeTime = Time.time;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (decayPerSecond <= 0f || suspicion <= 0f)
            return;

        if (Time.time - lastChangeTime < decayDelay)
            return;

        SetSuspicion(suspicion - decayPerSecond * Time.deltaTime, false);
    }

    public void AddSuspicion(float amount)
    {
        if (amount <= 0f)
            return;

        SetSuspicion(suspicion + amount);
    }

    public void ReportCrime(float amount)
    {
        AddSuspicion(amount);
    }

    public void SetSuspicion(float value, bool resetDecayTimer = true)
    {
        float clamped = Mathf.Clamp01(value);

        if (Mathf.Approximately(clamped, suspicion))
        {
            if (resetDecayTimer)
                lastChangeTime = Time.time;

            return;
        }

        suspicion = clamped;

        if (resetDecayTimer)
            lastChangeTime = Time.time;

        Changed?.Invoke(suspicion);

        WantedTier newTier = EvaluateTier(suspicion);

        if (newTier == tier)
            return;

        tier = newTier;
        TierChanged?.Invoke(tier);
    }

    public void ClearSuspicion()
    {
        SetSuspicion(0f);
    }

    private WantedTier EvaluateTier(float value)
    {
        if (value >= wantedThreshold)
            return WantedTier.Wanted;

        if (value > safeThreshold)
            return WantedTier.Suspicious;

        return WantedTier.Safe;
    }

    private void OnValidate()
    {
        wantedThreshold = Mathf.Max(wantedThreshold, safeThreshold);

        if (!Application.isPlaying)
            tier = EvaluateTier(suspicion);
    }

    [ContextMenu("Add Suspicion 25%")]
    private void AddSuspicion25()
    {
        AddSuspicion(0.25f);
    }

    [ContextMenu("Add Suspicion 50%")]
    private void AddSuspicion50()
    {
        AddSuspicion(0.5f);
    }

    [ContextMenu("Add Suspicion 100%")]
    private void AddSuspicion100()
    {
        SetSuspicion(1f);
    }

    [ContextMenu("Clear Suspicion")]
    private void ClearSuspicionFromContextMenu()
    {
        ClearSuspicion();
    }
}
