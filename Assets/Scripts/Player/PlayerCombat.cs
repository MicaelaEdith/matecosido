using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Transform muzzlePoint;

    [Header("Melee")]
    [SerializeField] private float meleeDamage = 25f;
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float meleeRadius = 0.8f;

    [Header("Ranged")]
    [SerializeField] private float rangedDamage = 20f;
    [SerializeField] private float rangedRange = 30f;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask hitMask = ~0;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (attackOrigin == null)
            attackOrigin = transform;
    }

    private void OnEnable()
    {
        if (playerController != null)
            playerController.AttackPerformed += HandleAttackPerformed;
    }

    private void OnDisable()
    {
        if (playerController != null)
            playerController.AttackPerformed -= HandleAttackPerformed;
    }

    private void HandleAttackPerformed(bool isKnifeAttack)
    {
        if (isKnifeAttack)
            PerformMeleeAttack();
        else
            PerformRangedAttack();
    }

    private void PerformMeleeAttack()
    {
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector3 direction = attackOrigin != null ? attackOrigin.forward : transform.forward;
        Vector3 center = origin + direction * (meleeRange * 0.5f);

        Collider[] hits = Physics.OverlapSphere(center, meleeRadius, hitMask, QueryTriggerInteraction.Ignore);

        EnemyHealth damaged = null;
        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy == damaged || enemy.IsDead)
                continue;

            enemy.TakeDamage(meleeDamage);
            damaged = enemy;
            break;
        }
    }

    private void PerformRangedAttack()
    {
        Transform originTransform = muzzlePoint != null ? muzzlePoint : (attackOrigin != null ? attackOrigin : transform);
        Vector3 origin = originTransform.position;
        Vector3 direction = originTransform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, rangedRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
                enemy.TakeDamage(rangedDamage);
        }

        Debug.DrawRay(origin, direction * rangedRange, Color.yellow, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position + origin.forward * (meleeRange * 0.5f), meleeRadius);

        Transform muzzle = muzzlePoint != null ? muzzlePoint : origin;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * rangedRange);
    }
}
