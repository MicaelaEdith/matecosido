using TMPro;
using UnityEngine;

public class MissionObjectiveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text objectiveText;

    [Header("Labels")]
    [SerializeField] private string notStartedText = "Habla con el trabajador del pueblo";
    [SerializeField] private string goToCompanyText = "Ve a la empresa";
    [SerializeField] private string companyAssaultText = "Neutraliza a los policías";
    [SerializeField] private string collectMoneyText = "Recolecta el dinero";
    [SerializeField] private string returnToNpc1Text = "Vuelve con el trabajador del pueblo";
    [SerializeField] private string goToStationText = "Ve a la estación de tren";
    [SerializeField] private string finalBattleText = "Gana el enfrentamiento final";
    [SerializeField] private string deliverToNpc2Text = "Entrega el dinero en la estación";
    [SerializeField] private string wonText = "Misión completada";

    private void Start()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.StateChanged += HandleStateChanged;
            UpdateObjective(MissionManager.Instance.CurrentState);
        }
        else
        {
            UpdateObjective(MissionState.NotStarted);
        }
    }

    private void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(MissionState previous, MissionState current)
    {
        UpdateObjective(current);
    }

    private void UpdateObjective(MissionState state)
    {
        if (objectiveText == null)
            return;

        switch (state)
        {
            case MissionState.NotStarted: objectiveText.text = notStartedText; break;
            case MissionState.GoToCompany: objectiveText.text = goToCompanyText; break;
            case MissionState.CompanyAssault: objectiveText.text = companyAssaultText; break;
            case MissionState.CollectMoney: objectiveText.text = collectMoneyText; break;
            case MissionState.ReturnToNpc1: objectiveText.text = returnToNpc1Text; break;
            case MissionState.GoToStation: objectiveText.text = goToStationText; break;
            case MissionState.FinalBattle: objectiveText.text = finalBattleText; break;
            case MissionState.DeliverToNpc2: objectiveText.text = deliverToNpc2Text; break;
            case MissionState.Won: objectiveText.text = wonText; break;
            default: objectiveText.text = string.Empty; break;
        }
    }
}
