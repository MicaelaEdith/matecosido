using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject continueIndicator;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Names")]
    [SerializeField] private string playerName = "Matecocido";

    [Header("Timing")]
    [SerializeField] private float inputDelay = 0.15f;

    public event Action<DialogueData> DialogueStarted;
    public event Action<DialogueData> DialogueEnded;
    public event Action<DialogueSpeaker> SpeakerChanged;

    public bool IsDialogueActive { get; private set; }
    public DialogueSpeaker CurrentSpeaker { get; private set; }

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private NpcController currentNpc;
    private PlayerController currentPlayer;
    private Action onComplete;
    private InputAction interactAction;
    private float nextInputTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialoguePanel == gameObject)
        {
            Debug.LogError("DialogueSystem: el panel no puede ser el mismo GameObject que el DialogueSystem.");
            dialoguePanel = null;
        }

        if (inputActions == null)
            inputActions = InputSystem.actions;

        if (inputActions != null)
        {
            InputActionMap playerMap = inputActions.FindActionMap("Player", false);

            if (playerMap != null)
                interactAction = playerMap.FindAction("Interact", false);
        }

        if (interactAction == null)
            Debug.LogWarning("DialogueSystem: no se encontró la acción 'Interact'.");
    }

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.performed += OnAdvancePerformed;

        SetPanelVisible(false);
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnAdvancePerformed;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartDialogue(DialogueData dialogue, NpcController npc, PlayerController player, Action onComplete = null)
    {
        if (IsDialogueActive || dialogue == null || dialogue.Lines == null || dialogue.Lines.Length == 0)
            return;

        IsDialogueActive = true;
        currentDialogue = dialogue;
        currentNpc = npc;
        currentPlayer = player;
        currentLineIndex = 0;
        this.onComplete = onComplete;

        if (currentPlayer != null)
            currentPlayer.SetDialogueActive(true);

        SetPanelVisible(true);
        ShowCurrentLine();
        nextInputTime = Time.unscaledTime + inputDelay;

        DialogueStarted?.Invoke(dialogue);
    }

    private void OnAdvancePerformed(InputAction.CallbackContext context)
    {
        if (!IsDialogueActive || Time.unscaledTime < nextInputTime)
            return;

        Advance();
    }

    private void Advance()
    {
        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.Lines.Length)
        {
            EndDialogue(true);
            return;
        }

        ShowCurrentLine();
        nextInputTime = Time.unscaledTime + inputDelay;
    }

    public void CancelDialogue()
    {
        if (IsDialogueActive)
            EndDialogue(false);
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = currentDialogue.Lines[currentLineIndex];
        bool npcSpeaking = line.speaker == DialogueSpeaker.Npc;

        if (currentNpc != null)
            currentNpc.SetTalking(npcSpeaking);

        if (currentPlayer != null)
            currentPlayer.SetTalking(!npcSpeaking, line.playerTalkingVariant);

        if (speakerNameText != null)
            speakerNameText.text = npcSpeaking ? currentDialogue.NpcName : playerName;

        if (dialogueText != null)
            dialogueText.text = line.text;

        if (continueIndicator != null)
            continueIndicator.SetActive(true);

        CurrentSpeaker = line.speaker;
        SpeakerChanged?.Invoke(CurrentSpeaker);
    }

    private void EndDialogue(bool completed)
    {
        DialogueData finishedDialogue = currentDialogue;

        IsDialogueActive = false;

        if (currentNpc != null)
        {
            currentNpc.SetTalking(false);
            currentNpc.ClearLookTarget();

            if (completed && finishedDialogue != null && finishedDialogue.NpcStopsBeingSad)
                currentNpc.SetSad(false);
        }

        if (currentPlayer != null)
        {
            currentPlayer.SetTalking(false);
            currentPlayer.SetDialogueActive(false);
        }

        SetPanelVisible(false);

        currentDialogue = null;
        currentNpc = null;
        currentPlayer = null;
        currentLineIndex = 0;

        Action callback = completed ? onComplete : null;
        onComplete = null;

        DialogueEnded?.Invoke(finishedDialogue);
        callback?.Invoke();
    }

    private void SetPanelVisible(bool visible)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(visible);

        if (continueIndicator != null)
            continueIndicator.SetActive(visible);
    }
}
