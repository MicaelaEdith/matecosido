using UnityEngine;

public class MissionObjectActivator : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject[] activateOnState;
    [SerializeField] private GameObject[] deactivateOnState;

    [Header("State")]
    [SerializeField] private MissionState triggerState = MissionState.CollectMoney;
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    private void Start()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.StateChanged += HandleStateChanged;
            HandleStateChanged(MissionManager.Instance.CurrentState, MissionManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(MissionState previous, MissionState current)
    {
        if (triggered && triggerOnce)
            return;

        if (current != triggerState)
            return;

        triggered = true;
        SetObjects(activateOnState, true);
        SetObjects(deactivateOnState, false);
    }

    private static void SetObjects(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}
