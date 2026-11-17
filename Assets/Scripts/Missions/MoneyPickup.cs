using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MoneyPickup : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private bool requireCollectMoneyState = true;
    [SerializeField] private MissionState stateAfterPickup = MissionState.ReturnToNpc1;

    [Header("Feedback")]
    [SerializeField] private GameObject pickupVisual;

    private bool collected;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    public void SetAvailable(bool available)
    {
        collected = false;
        gameObject.SetActive(available);

        if (pickupVisual != null)
            pickupVisual.SetActive(available);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || MissionManager.Instance == null)
            return;

        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        if (requireCollectMoneyState && MissionManager.Instance.CurrentState != MissionState.CollectMoney)
            return;

        collected = true;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayMoneyPickup();

        MissionManager.Instance.CollectMoney(stateAfterPickup);

        if (pickupVisual != null)
            pickupVisual.SetActive(false);

        gameObject.SetActive(false);
    }
}
