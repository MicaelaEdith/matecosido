using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public enum MusicTrack
    {
        None,
        Menu,
        Game
    }

    private const string MasterVolumeKey = "Matecosido.MasterVolume";

    public static SoundManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip combatMusic;
    [SerializeField] private MusicTrack startTrack = MusicTrack.None;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.6f;
    [Min(0f)]
    [SerializeField] private float musicFadeOutDuration = 0.4f;
    [Min(0f)]
    [SerializeField] private float musicFadeInDuration = 0.8f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Player")]
    [SerializeField] private AudioClip playerShoot;
    [SerializeField] private AudioClip playerMelee;
    [SerializeField] private AudioClip playerTakeDamage;
    [SerializeField] private AudioClip playerTalk;

    [Header("Police")]
    [SerializeField] private AudioClip policeShoot;
    [SerializeField] private AudioClip policeMelee;
    [SerializeField] private AudioClip policeTakeDamage;
    [SerializeField] private AudioClip policeDeath;

    [Header("NPC")]
    [SerializeField] private AudioClip npcTalk;

    [Header("Items")]
    [SerializeField] private AudioClip moneyPickup;

    [Header("Game Flow")]
    [SerializeField] private AudioClip gameOver;
    [SerializeField] private AudioClip victory;

    [Header("UI")]
    [SerializeField] private AudioClip uiClick;

    public float MasterVolume => AudioListener.volume;
    public MusicTrack StartTrack => startTrack;

    private Coroutine musicFadeRoutine;
    private GameManager boundGameManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 1f;

        ApplyMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
    }

    private void Start()
    {
        switch (startTrack)
        {
            case MusicTrack.Menu:
                PlayMenuMusic();
                break;
            case MusicTrack.Game:
                PlayGameMusic();
                break;
        }

        if (GameManager.Instance == null)
            return;

        boundGameManager = GameManager.Instance;
        boundGameManager.CombatStateChanged += HandleCombatStateChanged;

        if (startTrack == MusicTrack.Game && boundGameManager.InCombat)
            PlayMusic(combatMusic);
    }

    private void OnDestroy()
    {
        if (boundGameManager != null)
            boundGameManager.CombatStateChanged -= HandleCombatStateChanged;

        if (Instance == this)
            Instance = null;
    }

    private void HandleCombatStateChanged(bool inCombat)
    {
        if (startTrack != MusicTrack.Game)
            return;

        if (boundGameManager != null && (boundGameManager.IsGameOver || boundGameManager.IsGameWon))
            return;

        SwitchMusicWithFade(inCombat ? combatMusic : gameMusic);
    }

    private void SwitchMusicWithFade(AudioClip nextClip)
    {
        if (musicSource == null || nextClip == null)
            return;

        if (musicSource.clip == nextClip && musicSource.isPlaying)
            return;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeMusicTo(nextClip));
    }

    private IEnumerator FadeMusicTo(AudioClip nextClip)
    {
        if (musicSource.isPlaying && musicFadeOutDuration > 0f)
        {
            float from = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(from, 0f, Mathf.Clamp01(elapsed / musicFadeOutDuration));
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.clip = nextClip;
        musicSource.volume = 0f;
        musicSource.Play();

        if (musicFadeInDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < musicFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, Mathf.Clamp01(elapsed / musicFadeInDuration));
                yield return null;
            }
        }

        musicSource.volume = musicVolume;
        musicFadeRoutine = null;
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
            return;

        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
    }

    public void PlayPlayerShoot()
    {
        PlaySfx(playerShoot);
    }

    public void PlayPlayerMelee()
    {
        PlaySfx(playerMelee);
    }

    public void PlayPlayerTakeDamage()
    {
        PlaySfx(playerTakeDamage);
    }

    public void PlayPlayerTalk()
    {
        PlaySfx(playerTalk);
    }

    public void PlayPoliceShoot()
    {
        PlaySfx(policeShoot);
    }

    public void PlayPoliceMelee()
    {
        PlaySfx(policeMelee);
    }

    public void PlayPoliceTakeDamage()
    {
        PlaySfx(policeTakeDamage);
    }

    public void PlayPoliceDeath()
    {
        PlaySfx(policeDeath);
    }

    public void PlayNpcTalk()
    {
        PlaySfx(npcTalk);
    }

    public void PlayMoneyPickup()
    {
        PlaySfx(moneyPickup);
    }

    public void PlayGameOver()
    {
        PlaySfx(gameOver);
    }

    public void PlayVictory()
    {
        PlaySfx(victory);
    }

    public void PlayUiClick()
    {
        PlaySfx(uiClick);
    }

    public void SetMasterVolume(float volume)
    {
        ApplyMasterVolume(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
    }

    private void ApplyMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }
}
