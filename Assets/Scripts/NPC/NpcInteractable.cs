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

    private bool hasTalked;

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
        return hasTalked ? repeatDialogue : firstDialogue;
    }

    private void HandleDialogueFinished()
    {
        if (npc != null)
            npc.ClearLookTarget();

        if (hasTalked)
            return;

        hasTalked = true;

        if (hideMarkerAfterFirstDialogue && missionMarker != null)
            missionMarker.SetVisible(false);
    }
}
