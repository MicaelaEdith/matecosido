using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private EnemyProjectile projectilePrefab;

    [Header("Melee")]
    [SerializeField] private float meleeDamage = 25f;
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float meleeRadius = 0.8f;
    [SerializeField, Range(0f, 180f)] private float meleeArcAngle = 110f;
    [SerializeField, Min(0f)] private float meleeKnockback = 2.5f;

    [Header("Ranged")]
    [SerializeField] private float rangedDamage = 20f;
    [SerializeField] private float rangedRange = 30f;
    [SerializeField, Min(0f)] private float rangedKnockback = 1.5f;
    [SerializeField, Min(0f)] private float shootDelay = 0.35f;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask hitMask = ~0;

    private Coroutine pendingShot;

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

        if (pendingShot != null)
        {
            StopCoroutine(pendingShot);
            pendingShot = null;
        }
    }

    private void HandleAttackPerformed(bool isKnifeAttack)
    {
        if (isKnifeAttack)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayPlayerMelee();

            PerformMeleeAttack();
        }
        else
        {
            if (pendingShot != null)
                StopCoroutine(pendingShot);

            pendingShot = StartCoroutine(ShootAfterDelay());
        }
    }

    private IEnumerator ShootAfterDelay()
    {
        if (shootDelay > 0f)
            yield return new WaitForSeconds(shootDelay);

        pendingShot = null;

        if (!isActiveAndEnabled || playerController == null || playerController.IsDead)
            yield break;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPlayerShoot();

        PerformRangedAttack();
    }

    private void PerformMeleeAttack()
    {
        Vector3 origin = transform.position;
        if (attackOrigin != null)
            origin.y = attackOrigin.position.y;

        Vector3 direction = attackOrigin != null ? attackOrigin.forward : transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        direction.Normalize();

        float searchRadius = meleeRange + meleeRadius;
        Collider[] hits = Physics.OverlapSphere(origin, searchRadius, hitMask, QueryTriggerInteraction.Ignore);

        EnemyHealth damaged = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead)
                continue;

            Vector3 toEnemy = hit.bounds.center - origin;
            toEnemy.y = 0f;

            float distance = toEnemy.magnitude;
            if (distance < 0.0001f || distance > searchRadius)
                continue;

            if (Vector3.Angle(direction, toEnemy) > meleeArcAngle)
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                damaged = enemy;
            }
        }

        if (damaged != null)
        {
            Vector3 pushDirection = damaged.transform.position - origin;
            damaged.TakeDamage(meleeDamage, pushDirection, meleeKnockback);
        }
    }

    private void PerformRangedAttack()
    {
        Transform originTransform = muzzlePoint != null ? muzzlePoint : (attackOrigin != null ? attackOrigin : transform);
        Vector3 origin = originTransform.position;
        Vector3 targetPoint = playerController != null
            ? playerController.AimPoint
            : origin + originTransform.forward * rangedRange;

        Vector3 direction = targetPoint - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = originTransform.forward;

        direction.Normalize();

        if (projectilePrefab != null)
        {
            EnemyProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            projectile.Initialize(direction, rangedDamage, gameObject);
        }
        else
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, rangedRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                    enemy.TakeDamage(rangedDamage, direction, rangedKnockback);
            }
        }

        Debug.DrawRay(origin, direction * rangedRange, Color.yellow, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        if (attackOrigin != null)
            origin.y = attackOrigin.position.y;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, meleeRange + meleeRadius);

        Transform muzzle = muzzlePoint != null ? muzzlePoint : (attackOrigin != null ? attackOrigin : transform);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * rangedRange);
    }
}
