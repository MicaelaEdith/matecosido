using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject[] hudObjects;
    [SerializeField] private GameObject quitAlert;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private bool[] hudActiveStates;
    private float previousTimeScale = 1f;
    private bool isPaused;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (quitAlert != null)
            quitAlert.SetActive(false);

        AddListener(pauseButton, Pause);
        AddListener(closeButton, Resume);
        AddListener(quitButton, ShowQuitAlert);
        AddListener(yesButton, ConfirmQuit);
        AddListener(noButton, HideQuitAlert);
    }

    private void Start()
    {
        SoundManager sound = SoundManager.Instance;
        if (sound == null)
        {
            Debug.LogError("PauseMenuController: no se encontró SoundManager.", this);
            return;
        }

        BindSlider(musicSlider, sound.MusicVolume, HandleMusicVolumeChanged);
        BindSlider(sfxSlider, sound.SfxVolume, HandleSfxVolumeChanged);
    }

    private void OnDestroy()
    {
        RemoveListener(pauseButton, Pause);
        RemoveListener(closeButton, Resume);
        RemoveListener(quitButton, ShowQuitAlert);
        RemoveListener(yesButton, ConfirmQuit);
        RemoveListener(noButton, HideQuitAlert);

        UnbindSlider(musicSlider, HandleMusicVolumeChanged);
        UnbindSlider(sfxSlider, HandleSfxVolumeChanged);

        if (isPaused)
            Time.timeScale = previousTimeScale;
    }

    public void Pause()
    {
        if (isPaused)
            return;

        GameManager game = GameManager.Instance;
        if (game != null && (game.IsGameOver || game.IsGameWon))
            return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (hudObjects != null)
        {
            hudActiveStates = new bool[hudObjects.Length];
            for (int i = 0; i < hudObjects.Length; i++)
            {
                hudActiveStates[i] = hudObjects[i] != null && hudObjects[i].activeSelf;
                if (hudObjects[i] != null)
                    hudObjects[i].SetActive(false);
            }
        }

        if (quitAlert != null)
            quitAlert.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        if (quitAlert != null)
            quitAlert.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (hudObjects != null && hudActiveStates != null)
        {
            for (int i = 0; i < hudObjects.Length && i < hudActiveStates.Length; i++)
            {
                if (hudObjects[i] != null)
                    hudObjects[i].SetActive(hudActiveStates[i]);
            }
        }

        isPaused = false;
        Time.timeScale = previousTimeScale;
    }

    private void ShowQuitAlert()
    {
        if (quitAlert != null)
            quitAlert.SetActive(true);
    }

    private void HideQuitAlert()
    {
        if (quitAlert != null)
            quitAlert.SetActive(false);
    }

    private void ConfirmQuit()
    {
        Time.timeScale = 1f;
        isPaused = false;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private static void BindSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null)
        {
            Debug.LogWarning("PauseMenuController: falta asignar un slider de volumen.");
            return;
        }

        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        slider.onValueChanged.AddListener(callback);
    }

    private static void UnbindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(callback);
    }

    private void HandleMusicVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetMusicVolume(value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSfxVolume(value);
    }
}
