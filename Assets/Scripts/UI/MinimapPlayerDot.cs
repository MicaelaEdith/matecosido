using UnityEngine;
using UnityEngine.UI;

public class MinimapPlayerDot : MonoBehaviour
{
    [SerializeField] private RectTransform minimapArea;
    [SerializeField] private RectTransform dot;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private Transform player;

    private void LateUpdate()
    {
        if (minimapArea == null || dot == null || minimapCamera == null || player == null)
            return;

        Vector3 viewport = minimapCamera.WorldToViewportPoint(player.position);
        Vector2 size = minimapArea.rect.size;
        Vector2 anchored = new Vector2(viewport.x * size.x, viewport.y * size.y);

        bool visible = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
                       viewport.y >= 0f && viewport.y <= 1f;

        if (dot.gameObject.activeSelf != visible)
            dot.gameObject.SetActive(visible);

        if (visible)
            dot.anchoredPosition = anchored - dot.rect.size * 0.5f;
    }
}
