using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int IsRunningParameter = Animator.StringToHash("IsRunning");
    private static readonly int IsKnifeEquippedParameter = Animator.StringToHash("IsKnifeEquipped");
    private static readonly int InCombatParameter = Animator.StringToHash("InCombat");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int IsDeadParameter = Animator.StringToHash("IsDead");
    private static readonly int IsTalkingParameter = Animator.StringToHash("IsTalking");
    private static readonly int TalkingVariantParameter = Animator.StringToHash("TalkingVariant");

    private const float MoveInputThreshold = 0.0001f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float modelYawOffset = 0f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.6f;

    [Header("Feedback")]
    [SerializeField, Min(0f)] private float knockbackDecay = 12f;

    [Header("Weapon")]
    [SerializeField] private bool startWithKnife = false;

    public event Action<bool> WeaponEquippedChanged;
    public event Action<bool> AttackPerformed;

    private CharacterController controller;
    private Animator animator;
    private PlayerHealth playerHealth;
    private GameManager gameManager;

    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction attackAction;
    private InputAction switchWeaponAction;

    private bool isKnifeEquipped;
    private bool inCombat;
    private bool isDead;
    private bool isTalking;
    private bool isInDialogue;
    private float verticalVelocity;
    private Vector3 knockbackVelocity;
    private float nextAttackTime;

    public bool IsKnifeEquipped => isKnifeEquipped;
    public bool InCombat => inCombat;
    public bool IsDead => isDead;
    public bool IsTalking => isTalking;
    public bool IsInDialogue => isInDialogue;
    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }
    public Vector3 AimPoint { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (inputActions == null)
            inputActions = InputSystem.actions;

        if (inputActions != null)
        {
            playerActionMap = inputActions.FindActionMap("Player", false);

            if (playerActionMap != null)
            {
                moveAction = playerActionMap.FindAction("Move", false);
                sprintAction = playerActionMap.FindAction("Sprint", false);
                attackAction = playerActionMap.FindAction("Attack", false);
                switchWeaponAction = playerActionMap.FindAction("SwitchWeapon", false);
            }
        }

        SetKnifeEquipped(startWithKnife);
        AimPoint = transform.position + transform.forward * 10f;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager != null)
        {
            gameManager.CombatStateChanged += SetInCombat;
            SetInCombat(gameManager.InCombat);
        }

        if (playerHealth != null)
        {
            playerHealth.Died += HandlePlayerDied;
            playerHealth.Respawned += HandlePlayerRespawned;
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.CombatStateChanged -= SetInCombat;

        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
            playerHealth.Respawned -= HandlePlayerRespawned;
        }
    }

    private void OnEnable()
    {
        playerActionMap?.Enable();

        if (attackAction != null)
            attackAction.performed += OnAttackPerformed;

        if (switchWeaponAction != null)
            switchWeaponAction.performed += OnSwitchWeaponPerformed;
    }

    private void OnDisable()
    {
        if (attackAction != null)
            attackAction.performed -= OnAttackPerformed;

        if (switchWeaponAction != null)
            switchWeaponAction.performed -= OnSwitchWeaponPerformed;

        playerActionMap?.Disable();
    }

    private void Update()
    {
        if (isDead)
        {
            IsMoving = false;
            IsRunning = false;
            return;
        }

        if (isInDialogue)
        {
            IsMoving = false;
            IsRunning = false;
            Move(Vector2.zero, false, false);
            UpdateAnimator(false, false);
            return;
        }

        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        bool isMoving = moveInput.sqrMagnitude > MoveInputThreshold;
        bool isRunning = isMoving && sprintAction != null && sprintAction.IsPressed();

        IsMoving = isMoving;
        IsRunning = isRunning;

        Move(moveInput, isMoving, isRunning);
        UpdateAnimator(isMoving, isRunning);
    }

    private void Move(Vector2 moveInput, bool isMoving, bool isRunning)
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (isMoving)
        {
            moveDirection.Normalize();

            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection) * Quaternion.Euler(0f, modelYawOffset, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        float speed = isMoving ? (isRunning ? runSpeed : walkSpeed) : 0f;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * speed + knockbackVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if (knockbackVelocity.sqrMagnitude > 0.0001f)
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (isDead || force <= 0f)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        knockbackVelocity += direction.normalized * force;
        verticalVelocity = Mathf.Max(verticalVelocity, 1f);
    }

    private void UpdateAnimator(bool isMoving, bool isRunning)
    {
        if (animator == null)
            return;

        animator.SetBool(IsMovingParameter, isMoving);
        animator.SetBool(IsRunningParameter, isRunning);
        animator.SetBool(IsKnifeEquippedParameter, isKnifeEquipped);
        animator.SetBool(InCombatParameter, inCombat);
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (animator == null || isDead || isTalking || isInDialogue || Time.time < nextAttackTime || HasMovementInput())
            return;

        AimAtPointer();

        nextAttackTime = Time.time + attackCooldown;
        animator.SetTrigger(AttackParameter);
        AttackPerformed?.Invoke(isKnifeEquipped);
    }

    private void AimAtPointer()
    {
        AimPoint = GetMouseAimPoint();

        Vector3 aimDirection = AimPoint - transform.position;
        aimDirection.y = 0f;

        if (aimDirection.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(aimDirection.normalized)
            * Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    private Vector3 GetMouseAimPoint()
    {
        Vector3 fallback = transform.position + transform.forward * 10f + Vector3.up;

        if (Camera.main == null || Mouse.current == null)
            return fallback;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            return enemy != null ? enemy.transform.position + Vector3.up : hit.point;
        }

        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y + 1f, 0f));

        if (groundPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return fallback;
    }

    private bool HasMovementInput()
    {
        return moveAction != null && moveAction.ReadValue<Vector2>().sqrMagnitude > MoveInputThreshold;
    }

    private void OnSwitchWeaponPerformed(InputAction.CallbackContext context)
    {
        if (isDead || isInDialogue)
            return;

        ToggleWeapon();
    }

    public void ToggleWeapon()
    {
        SetKnifeEquipped(!isKnifeEquipped);
    }

    public void SetKnifeEquipped(bool equipped)
    {
        if (isKnifeEquipped == equipped)
            return;

        isKnifeEquipped = equipped;

        if (animator != null)
            animator.SetBool(IsKnifeEquippedParameter, isKnifeEquipped);

        WeaponEquippedChanged?.Invoke(isKnifeEquipped);
    }

    public void SetInCombat(bool value)
    {
        inCombat = value;

        if (animator != null)
            animator.SetBool(InCombatParameter, inCombat);
    }

    public void SetTalking(bool talking, int variant = 0)
    {
        isTalking = talking;

        if (animator == null)
            return;

        animator.SetInteger(TalkingVariantParameter, variant);
        animator.SetBool(IsTalkingParameter, talking);
    }

    public void SetDialogueActive(bool active)
    {
        isInDialogue = active;
    }

    public void FaceTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    private void HandlePlayerDied()
    {
        isDead = true;
        IsMoving = false;
        IsRunning = false;

        if (isInDialogue && DialogueSystem.Instance != null)
            DialogueSystem.Instance.CancelDialogue();

        if (animator != null)
            animator.SetBool(IsDeadParameter, true);
    }

    private void HandlePlayerRespawned()
    {
        isDead = false;
        verticalVelocity = 0f;

        if (animator != null)
            animator.SetBool(IsDeadParameter, false);
    }

    [ContextMenu("Toggle Weapon")]
    private void ToggleWeaponFromContextMenu()
    {
        ToggleWeapon();
    }

    [ContextMenu("Toggle In Combat")]
    private void ToggleInCombatFromContextMenu()
    {
        SetInCombat(!inCombat);
    }

    [ContextMenu("Toggle Talking 00")]
    private void ToggleTalking00FromContextMenu()
    {
        SetTalking(!isTalking, 0);
    }

    [ContextMenu("Toggle Talking 01")]
    private void ToggleTalking01FromContextMenu()
    {
        SetTalking(!isTalking, 1);
    }
}
