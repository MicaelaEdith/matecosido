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

    [Header("Weapon")]
    [SerializeField] private bool startWithKnife = false;

    private CharacterController controller;
    private Animator animator;

    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction attackAction;
    private InputAction previousWeaponAction;
    private InputAction nextWeaponAction;

    private bool isKnifeEquipped;
    private bool inCombat;
    private float verticalVelocity;
    private float nextAttackTime;

    public bool IsKnifeEquipped => isKnifeEquipped;
    public bool InCombat => inCombat;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
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
                previousWeaponAction = playerActionMap.FindAction("Previous", false);
                nextWeaponAction = playerActionMap.FindAction("Next", false);
            }
        }

        SetKnifeEquipped(startWithKnife);
    }

    private void OnEnable()
    {
        playerActionMap?.Enable();

        if (attackAction != null)
            attackAction.performed += OnAttackPerformed;

        if (previousWeaponAction != null)
            previousWeaponAction.performed += OnSwitchWeaponPerformed;

        if (nextWeaponAction != null)
            nextWeaponAction.performed += OnSwitchWeaponPerformed;
    }

    private void OnDisable()
    {
        if (attackAction != null)
            attackAction.performed -= OnAttackPerformed;

        if (previousWeaponAction != null)
            previousWeaponAction.performed -= OnSwitchWeaponPerformed;

        if (nextWeaponAction != null)
            nextWeaponAction.performed -= OnSwitchWeaponPerformed;

        playerActionMap?.Disable();
    }

    private void Update()
    {
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        bool isMoving = moveInput.sqrMagnitude > 0.0001f;
        bool isRunning = isMoving && sprintAction != null && sprintAction.IsPressed();

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

        Vector3 velocity = moveDirection * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
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
        if (animator == null || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        animator.SetTrigger(AttackParameter);
    }

    private void OnSwitchWeaponPerformed(InputAction.CallbackContext context)
    {
        ToggleWeapon();
    }

    public void ToggleWeapon()
    {
        SetKnifeEquipped(!isKnifeEquipped);
    }

    public void SetKnifeEquipped(bool equipped)
    {
        isKnifeEquipped = equipped;

        if (animator != null)
            animator.SetBool(IsKnifeEquippedParameter, isKnifeEquipped);
    }

    public void SetInCombat(bool value)
    {
        inCombat = value;

        if (animator != null)
            animator.SetBool(InCombatParameter, inCombat);
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
}
