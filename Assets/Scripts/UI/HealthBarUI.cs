using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    private static Sprite defaultFillSprite;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text livesText;

    [Header("Visuals")]
    [SerializeField] private Color emptyColor = new Color(0.35f, 0.05f, 0.05f, 0.85f);
    [SerializeField] private Gradient healthGradient = CreateDefaultGradient();
    [SerializeField] private float smoothSpeed = 8f;

    private float targetValue;
    private float displayedValue;
    private bool subscribed;

    private void Awake()
    {
        if (healthGradient == null)
            healthGradient = CreateDefaultGradient();

        ConfigureImages();
        UpdateLivesText();
    }

    private void OnEnable()
    {
        ResolveHealth();
        Subscribe();
    }

    private void Start()
    {
        ResolveHealth();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (fillImage == null)
            return;

        if (smoothSpeed <= 0f)
            displayedValue = targetValue;
        else
            displayedValue = Mathf.MoveTowards(displayedValue, targetValue, smoothSpeed * Time.deltaTime);

        ApplyValue(displayedValue);
    }

    private void HandleHealthChanged(float current, float max)
    {
        targetValue = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void HandleLivesChanged(int current, int max)
    {
        UpdateLivesText();
    }

    private void ResolveHealth()
    {
        if (playerHealth != null)
            return;

        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void Subscribe()
    {
        if (subscribed || playerHealth == null)
            return;

        playerHealth.HealthChanged += HandleHealthChanged;
        playerHealth.LivesChanged += HandleLivesChanged;
        subscribed = true;

        targetValue = playerHealth.MaxHealth > 0f
            ? Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth)
            : 0f;
        displayedValue = targetValue;
        ApplyValue(displayedValue);
        UpdateLivesText();
    }

    private void Unsubscribe()
    {
        if (!subscribed || playerHealth == null)
            return;

        playerHealth.HealthChanged -= HandleHealthChanged;
        playerHealth.LivesChanged -= HandleLivesChanged;
        subscribed = false;
    }

    private void ApplyValue(float value)
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = value;
        fillImage.color = healthGradient.Evaluate(value);
    }

    private void UpdateLivesText()
    {
        if (livesText == null)
            return;

        int lives = playerHealth != null ? playerHealth.CurrentLives : 0;
        livesText.text = "x " + lives;
    }

    private void ConfigureImages()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = emptyColor;
            backgroundImage.raycastTarget = false;
        }

        if (fillImage == null)
            return;

        if (fillImage.sprite == null)
            fillImage.sprite = GetDefaultFillSprite();

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;
    }

    private static Sprite GetDefaultFillSprite()
    {
        if (defaultFillSprite != null)
            return defaultFillSprite;

        Texture2D texture = Texture2D.whiteTexture;
        defaultFillSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
        defaultFillSprite.name = "HealthBarFillSprite";

        return defaultFillSprite;
    }

    private void OnValidate()
    {
        if (healthGradient == null)
            healthGradient = CreateDefaultGradient();

        if (backgroundImage != null)
            backgroundImage.color = emptyColor;
    }

    private static Gradient CreateDefaultGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.15f, 0.1f), 0f),
                new GradientColorKey(new Color(1f, 0.75f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.85f, 0.25f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });

        return gradient;
    }
}
