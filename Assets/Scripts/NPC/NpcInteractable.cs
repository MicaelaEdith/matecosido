using System.Collections.Generic;
using UnityEngine;

public class NpcInteractable : MonoBehaviour
{
    public static readonly List<NpcInteractable> Active = new List<NpcInteractable>();

    [Header("References")]
    [SerializeField] private NpcController npc;
    [SerializeField] private MissionMarker missionMarker;
    [SerializeField] private DialogueData firstDialogue;
    [SerializeField] private DialogueData repeatDialogue;

    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 2.5f;
    [SerializeField] private bool turnNpcToPlayer = true;
    [SerializeField] private float npcYawOffset;
    [SerializeField] private bool hideMarkerAfterFirstDialogue = true;

    [Header("Mission - Generic")]
    [SerializeField] private bool startsMissionOnFirstDialogue;

    [Header("Mission - NPC 1 (entrega en pueblo)")]
    [SerializeField] private bool isVillageNpc;
    [SerializeField] private DialogueData deliveryDialogue;
    [SerializeField] private MissionState requiredStateForDelivery = MissionState.ReturnToNpc1;

    [Header("Mission - NPC 2 (entrega en estación)")]
    [SerializeField] private bool isStationNpc;
    [SerializeField] private DialogueData finalDeliveryDialogue;
    [SerializeField] private MissionState requiredStateForFinalDelivery = MissionState.DeliverToNpc2;
    [SerializeField] private bool requireFinalBattleCleared = true;
    [SerializeField] private bool playThankfulOnFinalDelivery;

    [Header("Mission - Final Battle")]
    [SerializeField] private bool startsFinalBattleOnDialogueEnd;

    private bool hasTalked;
    private bool hasDeliveredToVillage;
    private bool hasDeliveredToStation;

    public float InteractionRadius => interactionRadius;
    public bool HasTalked => hasTalked;

    public bool CanInteract
    {
        get
        {
            if (!isActiveAndEnabled || DialogueSystem.Instance == null)
                return false;

            if (DialogueSystem.Instance.IsDialogueActive)
                return false;

            DialogueData dialogue = GetCurrentDialogue();
            return dialogue != null && dialogue.Lines != null && dialogue.Lines.Length > 0;
        }
    }

    private void Awake()
    {
        if (npc == null)
            npc = GetComponentInParent<NpcController>();

        if (missionMarker == null)
        {
            missionMarker = GetComponentInParent<MissionMarker>();

            if (missionMarker == null)
                missionMarker = GetComponentInChildren<MissionMarker>(true);
        }
    }

    private void Start()
    {
        if (DialogueSystem.Instance == null)
            Debug.LogWarning("NpcInteractable: no hay un DialogueSystem en la escena.");
    }

    private void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public void Interact(PlayerController player)
    {
        if (player == null || !CanInteract)
            return;

        DialogueData dialogue = GetCurrentDialogue();

        if (turnNpcToPlayer && npc != null)
            npc.FaceTarget(player.transform, npcYawOffset);

        player.FaceTowards(transform.position);

        DialogueSystem.Instance.StartDialogue(dialogue, npc, player, HandleDialogueFinished);
    }

    private DialogueData GetCurrentDialogue()
    {
        MissionManager mission = MissionManager.Instance;

        if (isVillageNpc && mission != null && !hasDeliveredToVillage &&
            mission.CurrentState == requiredStateForDelivery && mission.HasMoney && deliveryDialogue != null)
        {
            return deliveryDialogue;
        }

        if (isStationNpc && mission != null && !hasDeliveredToStation && mission.HasMoney && finalDeliveryDialogue != null)
        {
            bool finalBattleReady = !requireFinalBattleCleared || mission.CurrentState >= requiredStateForFinalDelivery;
            if (finalBattleReady)
                return finalDeliveryDialogue;
        }

        return hasTalked ? repeatDialogue : firstDialogue;
    }

    private void HandleDialogueFinished()
    {
        if (npc != null)
            npc.ClearLookTarget();

        MissionManager mission = MissionManager.Instance;

        if (isVillageNpc && mission != null && !hasDeliveredToVillage &&
            mission.CurrentState == requiredStateForDelivery && mission.HasMoney)
        {
            hasDeliveredToVillage = true;
            mission.DeliverMoneyToNpc1(MissionState.GoToStation);

            if (hideMarkerAfterFirstDialogue && missionMarker != null)
                missionMarker.SetVisible(false);

            return;
        }

        if (isStationNpc && mission != null && !hasDeliveredToStation && mission.HasMoney &&
            (!requireFinalBattleCleared || mission.CurrentState >= requiredStateForFinalDelivery))
        {
            hasDeliveredToStation = true;
            mission.DeliverMoneyToNpc2(MissionState.Won);

            if (playThankfulOnFinalDelivery && npc != null)
                npc.SetThankful(true);

            if (hideMarkerAfterFirstDialogue && missionMarker != null)
                missionMarker.SetVisible(false);

            return;
        }

        if (!hasTalked)
        {
            hasTalked = true;

            if (hideMarkerAfterFirstDialogue && missionMarker != null)
                missionMarker.SetVisible(false);

            if (startsMissionOnFirstDialogue && mission != null)
                mission.AdvanceTo(MissionState.GoToCompany);

            if (startsFinalBattleOnDialogueEnd && mission != null)
                mission.AdvanceTo(MissionState.FinalBattle);
        }
    }
}
