using UnityEngine;
using UnityEngine.UI;

public class MinimapObjectiveDot : MonoBehaviour
{
    [SerializeField] private RectTransform minimapArea;
    [SerializeField] private RectTransform dot;
    [SerializeField] private Image dotImage;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private Transform companyTarget;
    [SerializeField] private Transform moneyTarget;
    [SerializeField] private Transform stationNpcTarget;
    [SerializeField] private float blinkSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.25f;

    private Transform activeTarget;

    private void Start()
    {
        MissionManager manager = MissionManager.Instance;
        if (manager == null)
            return;

        manager.StateChanged += HandleStateChanged;
        HandleStateChanged(manager.CurrentState, manager.CurrentState);
    }

    private void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(MissionState previous, MissionState current)
    {
        switch (current)
        {
            case MissionState.GoToCompany:
                activeTarget = companyTarget;
                break;
            case MissionState.CollectMoney:
                activeTarget = moneyTarget;
                break;
            case MissionState.GoToStation:
            case MissionState.FinalBattle:
            case MissionState.DeliverToNpc2:
                activeTarget = stationNpcTarget;
                break;
            default:
                activeTarget = null;
                break;
        }
    }

    private void LateUpdate()
    {
        if (activeTarget == null || minimapArea == null || dot == null || dotImage == null || minimapCamera == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 viewport = minimapCamera.WorldToViewportPoint(activeTarget.position);
        bool visible = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
                       viewport.y >= 0f && viewport.y <= 1f;
        SetVisible(visible);

        if (!visible)
            return;

        Vector2 size = minimapArea.rect.size;
        dot.anchoredPosition = new Vector2(viewport.x * size.x, viewport.y * size.y) - dot.rect.size * 0.5f;

        Color color = dotImage.color;
        color.a = minAlpha + (1f - minAlpha) * Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
        dotImage.color = color;
    }

    private void SetVisible(bool visible)
    {
        if (dot.gameObject.activeSelf != visible)
            dot.gameObject.SetActive(visible);
    }
}
