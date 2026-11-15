using UnityEngine;
using UnityEngine.UI;

public class WantedBarUI : MonoBehaviour
{
    private static Sprite defaultFillSprite;

    [Header("References")]
    [SerializeField] private WantedSystem wantedSystem;
    [Tooltip("Image de fondo. Se pinta con SafeColor y queda visible cuando la sospecha es 0.")]
    [SerializeField] private Image backgroundImage;
    [Tooltip("Image que crece de izquierda a derecha según la sospecha.")]
    [SerializeField] private Image fillImage;

    [Header("Visuals")]
    [SerializeField] private Color safeColor = new Color(0.15f, 0.45f, 1f, 1f);
    [SerializeField] private Gradient fillGradient = CreateDefaultGradient();
    [SerializeField] private float smoothSpeed = 8f;

    private float targetValue;
    private float displayedValue;
    private bool subscribed;

    private void Awake()
    {
        if (fillGradient == null)
            fillGradient = CreateDefaultGradient();

        ConfigureFillImage();

        if (backgroundImage != null)
        {
            backgroundImage.color = safeColor;
            backgroundImage.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        ResolveSystem();
        Subscribe();
    }

    private void Start()
    {
        ResolveSystem();
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

    private void HandleChanged(float value)
    {
        targetValue = value;
    }

    private void ResolveSystem()
    {
        if (wantedSystem != null)
            return;

        wantedSystem = WantedSystem.Instance;

        if (wantedSystem == null)
            wantedSystem = FindFirstObjectByType<WantedSystem>();
    }

    private void Subscribe()
    {
        if (subscribed || wantedSystem == null)
            return;

        wantedSystem.Changed += HandleChanged;
        subscribed = true;

        targetValue = wantedSystem.Suspicion;
        displayedValue = targetValue;
        ApplyValue(displayedValue);
    }

    private void Unsubscribe()
    {
        if (!subscribed || wantedSystem == null)
            return;

        wantedSystem.Changed -= HandleChanged;
        subscribed = false;
    }

    private void ApplyValue(float value)
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = value;
        fillImage.color = fillGradient.Evaluate(value);
    }

    private void ConfigureFillImage()
    {
        if (fillImage == null)
            return;

        if (fillImage.sprite == null)
            fillImage.sprite = GetDefaultFillSprite();

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Right;
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
        defaultFillSprite.name = "WantedBarFillSprite";

        return defaultFillSprite;
    }

    private void OnValidate()
    {
        if (fillGradient == null)
            fillGradient = CreateDefaultGradient();

        if (backgroundImage != null)
            backgroundImage.color = safeColor;
    }

    private static Gradient CreateDefaultGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.05f), 0.5f),
                new GradientColorKey(new Color(0.9f, 0.05f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });

        return gradient;
    }
}
