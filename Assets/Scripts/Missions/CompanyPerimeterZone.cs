using UnityEngine;

public class CompanyPerimeterZone : MonoBehaviour
{
    [SerializeField] private float suspicionPerSecond = 0.05f;
    [SerializeField, Range(0f, 1f)] private float maxSuspicionWithoutCombat = 0.25f;

    private void OnTriggerStay(Collider other)
    {
        if (WantedSystem.Instance == null || !IsPlayer(other))
            return;

        bool inCombat = GameManager.Instance != null && GameManager.Instance.InCombat;
        float maxSuspicion = inCombat ? 1f : maxSuspicionWithoutCombat;

        WantedSystem.Instance.AddSuspicionCapped(suspicionPerSecond * Time.deltaTime, maxSuspicion);
    }

    private static bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerController>() != null;
    }
}
