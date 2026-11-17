using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<bool> CombatStateChanged;
    public event Action GameOver;
    public event Action GameWon;

    [Header("Combat")]
    [SerializeField] private bool startInCombat;

    [Header("Player Flow")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private bool reloadSceneOnGameOver;
    [SerializeField] private float gameOverReloadDelay = 3f;

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    public bool InCombat { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsGameWon { get; private set; }

    private Coroutine respawnRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InCombat = startInCombat;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.Died += HandlePlayerDied;
            playerHealth.OutOfLives += HandleOutOfLives;
        }

        if (MissionManager.Instance != null)
            MissionManager.Instance.GameWon += HandleMissionWon;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
            playerHealth.OutOfLives -= HandleOutOfLives;
        }

        if (MissionManager.Instance != null)
            MissionManager.Instance.GameWon -= HandleMissionWon;

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

    private void HandlePlayerDied()
    {
        if (IsGameOver || IsGameWon || playerHealth == null)
            return;

        if (!playerHealth.CanRespawn && playerHealth.CurrentLives <= 0)
            return;

        if (respawnRoutine != null)
            StopCoroutine(respawnRoutine);

        respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (playerHealth != null && playerHealth.IsDead && !IsGameOver)
            playerHealth.Respawn();

        respawnRoutine = null;
    }

    private void HandleOutOfLives()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMusic();
            SoundManager.Instance.PlayGameOver();
        }

        if (MissionManager.Instance != null)
            MissionManager.Instance.TriggerGameOver();

        GameOver?.Invoke();

        if (reloadSceneOnGameOver)
            StartCoroutine(ReloadSceneAfterDelay());
    }

    private void HandleMissionWon()
    {
        if (IsGameWon)
            return;

        IsGameWon = true;

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMusic();
            SoundManager.Instance.PlayVictory();
        }

        GameWon?.Invoke();
    }

    private IEnumerator ReloadSceneAfterDelay()
    {
        yield return new WaitForSeconds(gameOverReloadDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
