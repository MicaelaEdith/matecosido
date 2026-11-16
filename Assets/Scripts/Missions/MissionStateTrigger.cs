using UnityEngine;

public class MissionStateTrigger : MonoBehaviour
{
    [SerializeField] private MissionState requiredState = MissionState.GoToStation;
    [SerializeField] private MissionState stateToAdvanceTo = MissionState.FinalBattle;
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && triggerOnce)
            return;

        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        if (MissionManager.Instance == null)
            return;

        if (MissionManager.Instance.CurrentState != requiredState)
            return;

        triggered = true;
        MissionManager.Instance.AdvanceTo(stateToAdvanceTo);
    }
}
