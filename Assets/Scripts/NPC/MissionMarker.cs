using UnityEngine;

public class MissionMarker : MonoBehaviour
{
    [Header("Marker")]
    [SerializeField] private Color markerColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private float orbSize = 0.22f;
    [SerializeField] private float headOffset = 0.45f;
    [SerializeField] private float fallbackHeight = 2.1f;

    [Header("Animation")]
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.15f;

    [Header("Light")]
    [SerializeField] private bool createLight = true;
    [SerializeField] private float lightRange = 2.5f;
    [SerializeField] private float lightIntensity = 2.5f;

    private GameObject orb;
    private Light glowLight;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;

    private void Awake()
    {
        CreateMarker();
    }

    private void Update()
    {
        if (orb == null)
            return;

        float wave = Mathf.Sin(Time.time * bobSpeed);
        orb.transform.localPosition = baseLocalPosition + Vector3.up * (wave * bobAmplitude);

        float pulse = 1f + Mathf.Sin(Time.time * bobSpeed * 1.5f) * pulseAmount;
        orb.transform.localScale = baseLocalScale * pulse;

        if (glowLight != null)
            glowLight.intensity = lightIntensity + Mathf.Sin(Time.time * bobSpeed * 1.5f) * 0.5f;
    }

    public void SetVisible(bool visible)
    {
        if (orb != null)
            orb.SetActive(visible);
    }

    private void CreateMarker()
    {
        float height = GetMarkerHeight();

        orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "MissionMarker";
        orb.transform.SetParent(transform, false);
        orb.transform.localPosition = new Vector3(0f, height, 0f);
        orb.transform.localScale = Vector3.one * orbSize;

        Collider orbCollider = orb.GetComponent<Collider>();
        if (orbCollider != null)
        {
            orbCollider.enabled = false;
            Destroy(orbCollider);
        }

        Renderer orbRenderer = orb.GetComponent<Renderer>();
        if (orbRenderer != null)
        {
            Material material = orbRenderer.material;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", markerColor);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", markerColor);

            material.EnableKeyword("_EMISSION");

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", markerColor * 3f);
        }

        if (createLight)
        {
            GameObject lightObject = new GameObject("GlowLight");
            lightObject.transform.SetParent(orb.transform, false);

            glowLight = lightObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = markerColor;
            glowLight.range = lightRange;
            glowLight.intensity = lightIntensity;
            glowLight.shadows = LightShadows.None;
        }

        baseLocalPosition = orb.transform.localPosition;
        baseLocalScale = orb.transform.localScale;
    }

    private float GetMarkerHeight()
    {
        Transform head = FindHead(transform);

        if (head != null)
        {
            Vector3 localHead = transform.InverseTransformPoint(head.position);
            return localHead.y + headOffset;
        }

        return fallbackHeight;
    }

    private static Transform FindHead(Transform root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform candidate in transforms)
        {
            if (candidate.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase) ||
                candidate.name.Equals("mixamorig:Head", System.StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }
}
