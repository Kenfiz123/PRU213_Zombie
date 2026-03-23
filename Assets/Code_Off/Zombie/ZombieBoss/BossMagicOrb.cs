using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BossMagicOrb : MonoBehaviour
{
    public float speed = 15f;
    public float turnSpeed = 4f;
    public float lifeTime = 5f;
    [HideInInspector] public float damage;

    private Transform target;
    private Rigidbody rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
    }

    public void Setup(Transform _target, float _damage)
    {
        target = _target;
        damage = _damage;
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (hasHit) return;
        rb.linearVelocity = transform.forward * speed;

        if (target != null)
        {
            Vector3 direction = (target.position + Vector3.up - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * turnSpeed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (other.CompareTag("Enemy") || other.CompareTag("Boss")) return;
        if (other.isTrigger && !other.CompareTag("Player")) return;

        hasHit = true;

        if (other.CompareTag("Player"))
        {
            PlayerHealth pHealth = other.GetComponent<PlayerHealth>();
            if (pHealth != null) pHealth.TakeDamage(damage);

            AllyHealth aHealth = other.GetComponent<AllyHealth>();
            if (aHealth != null) aHealth.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}