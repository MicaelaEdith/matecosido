using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;

    private float damage;
    private Vector3 direction;
    private GameObject owner;
    private float lifeTimer;

    public void Initialize(Vector3 shootDirection, float projectileDamage, GameObject projectileOwner)
    {
        direction = shootDirection.normalized;
        damage = projectileDamage;
        owner = projectileOwner;
        lifeTimer = 0f;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 origin = transform.position;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
        {
            HandleHit(hit.collider);
            return;
        }

        transform.position += direction * step;
        lifeTimer += Time.deltaTime;

        if (lifeTimer >= lifetime)
            Destroy(gameObject);
    }

    private void HandleHit(Collider other)
    {
        if (other == null)
            return;

        if (owner != null && other.transform.IsChildOf(owner.transform))
            return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
            player.TakeDamage(damage);

        Destroy(gameObject);
    }
}
