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
    [SerializeField] private MusicTrack startTrack = MusicTrack.None;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.6f;

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
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
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
