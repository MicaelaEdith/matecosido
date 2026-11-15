using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Prompt")]
    [SerializeField] private GameObject interactionPrompt;

    private PlayerController playerController;
    private InputAction interactAction;
    private NpcInteractable currentTarget;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (inputActions == null)
            inputActions = InputSystem.actions;

        if (inputActions != null)
        {
            InputActionMap playerMap = inputActions.FindActionMap("Player", false);

            if (playerMap != null)
                interactAction = playerMap.FindAction("Interact", false);
        }

        if (interactAction == null)
            Debug.LogWarning("PlayerInteraction: no se encontró la acción 'Interact'.");
    }

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;

        SetTarget(null);
    }

    private void Update()
    {
        if (playerController == null ||
            playerController.IsDead ||
            playerController.IsInDialogue ||
            (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive))
        {
            SetTarget(null);
            return;
        }

        SetTarget(FindNearestInteractable());
    }

    private NpcInteractable FindNearestInteractable()
    {
        NpcInteractable nearest = null;
        float nearestSqrDistance = float.MaxValue;
        Vector3 playerPosition = transform.position;

        foreach (NpcInteractable candidate in NpcInteractable.Active)
        {
            if (candidate == null || !candidate.CanInteract)
                continue;

            float sqrDistance = (candidate.transform.position - playerPosition).sqrMagnitude;
            float radius = candidate.InteractionRadius;

            if (sqrDistance > radius * radius || sqrDistance >= nearestSqrDistance)
                continue;

            nearest = candidate;
            nearestSqrDistance = sqrDistance;
        }

        return nearest;
    }

    private void SetTarget(NpcInteractable target)
    {
        if (currentTarget == target)
            return;

        currentTarget = target;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(currentTarget != null);
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (currentTarget == null || !currentTarget.CanInteract)
            return;

        currentTarget.Interact(playerController);
    }
}
