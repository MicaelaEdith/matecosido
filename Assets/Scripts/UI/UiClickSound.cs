using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class UiClickSound : MonoBehaviour
{
    private void Awake()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
            button.onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayUiClick();
    }
}
