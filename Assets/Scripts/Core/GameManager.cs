using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<bool> CombatStateChanged;

    [SerializeField] private bool startInCombat;

    public bool InCombat { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InCombat = startInCombat;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetInCombat(bool value)
    {
        if (InCombat == value)
            return;

        InCombat = value;
        CombatStateChanged?.Invoke(InCombat);
    }

    [ContextMenu("Toggle In Combat")]
    private void ToggleInCombat()
    {
        SetInCombat(!InCombat);
    }
}
