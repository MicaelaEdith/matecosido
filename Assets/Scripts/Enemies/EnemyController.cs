using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int IsRunningParameter = Animator.StringToHash("IsRunning");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int IsDeadParameter = Animator.StringToHash("IsDead");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private PoliceWeaponHandler weaponHandler;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private EnemyProjectile projectilePrefab;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private bool requirePlayerInsideCompany;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool combatEnabled;

    [Header("Area")]
    [SerializeField] private CompanyAreaController companyArea;

    private Transform target;
    private PlayerHealth targetHealth;
    private PlayerController targetController;
    private float nextAttackTime;

    public bool CombatEnabled => combatEnabled;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<EnemyHealth>();

        if (weaponHandler == null)
            weaponHandler = GetComponent<PoliceWeaponHandler>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);
            agent.updateRotation = false;
        }
    }

    private void Start()
    {
        FindTarget();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= HandleDied;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        if (target == null)
            FindTarget();

        if (!CanEngageTarget())
        {
            StopMoving();
            UpdateAnimator(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > detectionRange)
        {
            StopMoving();
            UpdateAnimator(false);
            return;
        }

        if (distance > attackRange)
        {
            ChaseTarget();
            UpdateAnimator(true);
            return;
        }

        StopMoving();
        FaceTarget();
        UpdateAnimator(false);
        TryAttack();
    }

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;

        if (!combatEnabled)
        {
            StopMoving();
            UpdateAnimator(false);
        }
    }

    public void SetCompanyArea(CompanyAreaController area)
    {
        companyArea = area;
    }

    public void ConfigureAttack(float newAttackRange, float newDamage, float newCooldown, bool melee)
    {
        attackRange = newAttackRange;
        damage = newDamage;
        attackCooldown = newCooldown;

        if (agent != null)
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.8f);

        if (weaponHandler != null)
            weaponHandler.SetCachiporraEquipped(melee);
    }

    private void FindTarget()
    {
        if (target != null)
            return;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
            return;

        target = player.transform;
        targetController = player;
        targetHealth = player.GetComponent<PlayerHealth>();
    }

    private bool CanEngageTarget()
    {
        if (!combatEnabled || target == null || targetHealth == null || targetHealth.IsDead)
            return false;

        if (targetController != null && (targetController.IsDead || targetController.IsInDialogue))
            return false;

        if (requirePlayerInsideCompany && companyArea != null && !companyArea.IsPlayerInside)
            return false;

        return true;
    }

    private void ChaseTarget()
    {
        FaceTarget();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;
            agent.SetDestination(target.position);
        }
        else
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
                transform.position += direction.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = true;
    }

    private void FaceTarget()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
            animator.SetTrigger(AttackParameter);

        bool useRanged = weaponHandler != null && !weaponHandler.CachiporraEquipped;

        if (useRanged && projectilePrefab != null && muzzlePoint != null)
        {
            Vector3 direction = GetAimDirection();
            EnemyProjectile projectile = Instantiate(projectilePrefab, muzzlePoint.position, Quaternion.LookRotation(direction));
            projectile.Initialize(direction, damage, gameObject);
            return;
        }

        if (targetHealth != null)
            targetHealth.TakeDamage(damage);
    }

    private Vector3 GetAimDirection()
    {
        if (target == null)
            return transform.forward;

        Vector3 targetPoint = target.position + Vector3.up * 1.2f;
        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : (transform.position + Vector3.up * 1.2f);
        Vector3 direction = targetPoint - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        return direction.normalized;
    }

    private void UpdateAnimator(bool moving)
    {
        if (animator == null)
            return;

        animator.SetBool(IsMovingParameter, moving);
        animator.SetBool(IsRunningParameter, false);
    }

    private void HandleDied()
    {
        StopMoving();

        if (agent != null)
            agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (animator != null)
            animator.SetBool(IsDeadParameter, true);

        enabled = false;
    }
}
