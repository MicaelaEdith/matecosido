using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameplaySceneName;
    [SerializeField, Min(0f)] private float sceneLoadDelay = 0.12f;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void Start()
    {
        SoundManager sound = SoundManager.Instance;

        if (sound == null)
        {
            Debug.LogError("MainMenuController: no se encontró SoundManager.", this);
            return;
        }

        ResolveSliders();

        BindSlider(musicSlider, sound.MusicVolume, HandleMusicVolumeChanged);
        BindSlider(sfxSlider, sound.SfxVolume, HandleSfxVolumeChanged);
    }

    private void OnDestroy()
    {
        UnbindSlider(musicSlider, HandleMusicVolumeChanged);
        UnbindSlider(sfxSlider, HandleSfxVolumeChanged);
    }

    private void ResolveSliders()
    {
        if (settingsPanel == null)
            return;

        if (musicSlider == null)
            musicSlider = FindSlider("Musica");

        if (sfxSlider == null)
            sfxSlider = FindSlider("fx");
    }

    private Slider FindSlider(string rowName)
    {
        foreach (Slider slider in settingsPanel.GetComponentsInChildren<Slider>(true))
        {
            if (slider.transform.parent != null && slider.transform.parent.name == rowName)
                return slider;
        }

        return null;
    }

    private void BindSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null)
        {
            Debug.LogWarning("MainMenuController: falta asignar un slider de volumen.", this);
            return;
        }

        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        slider.onValueChanged.AddListener(callback);
    }

    private void UnbindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
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

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("MainMenuController: no se asignó la escena de juego.", this);
            return;
        }

        StartCoroutine(LoadSceneAfterDelay(gameplaySceneName));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}
