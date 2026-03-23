using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    [Header("=== THÙNG NỔ ===")]
    public float health = 30f;
    public float explosionRadius = 6f;
    public float explosionDamage = 80f;
    public float chainDelay = 0.15f;

    [Header("=== MẢNH VỠ (PBS Barrel Asset) ===")]
    [Tooltip("Kéo HazmatBarrel_broken prefab vào đây")]
    public GameObject explodedPrefab;
    public float debrisExplosionForce = 5f;
    public float debrisUpForceMin = 0f;
    public float debrisUpForceMax = 0.5f;
    public float debrisLifeTime = 5f;

    [Header("=== HIỆU ỨNG ===")]
    public GameObject explosionVFX;
    public AudioClip explosionSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    private bool hasExploded = false;
    private float currentHealth;

    void Start()
    {
        currentHealth = health;
    }

    /// <summary>
    /// Gọi khi bị bắn trúng (từ PlayerShooting raycast)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (hasExploded) return;

        currentHealth -= damage;
        if (currentHealth <= 0f)
            Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // 1. Spawn mảnh vỡ barrel (PBS asset)
        if (explodedPrefab != null)
        {
            GameObject debris = Instantiate(explodedPrefab, pos, rot);
            // Đẩy mảnh vỡ ra ngoài
            foreach (Transform child in debris.transform)
            {
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddExplosionForce(debrisExplosionForce, pos, explosionRadius,
                        Random.Range(debrisUpForceMin, debrisUpForceMax), ForceMode.Impulse);
            }
            Destroy(debris, debrisLifeTime);
        }

        // 2. VFX nổ
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, pos, Quaternion.identity);
            Destroy(vfx, 4f);
        }

        // 3. Sound
        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, pos, soundVolume);

        // 4. AoE Damage
        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            float dist = Vector3.Distance(pos, hit.transform.position);
            float dmgPercent = 1f - (dist / explosionRadius);
            float finalDamage = explosionDamage * Mathf.Clamp01(dmgPercent);

            if (finalDamage <= 0f) continue;

            // Zombie
            ZombieHealth zh = hit.GetComponent<ZombieHealth>();
            if (zh == null) zh = hit.GetComponentInParent<ZombieHealth>();
            if (zh != null)
            {
                zh.TakeDamage(finalDamage);
                continue;
            }

            // Player
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph == null) ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(finalDamage);
                continue;
            }

            // Ally
            AllyHealth ah = hit.GetComponent<AllyHealth>();
            if (ah == null) ah = hit.GetComponentInParent<AllyHealth>();
            if (ah != null)
            {
                ah.TakeDamage(finalDamage);
                continue;
            }

            // Chain reaction — nổ thùng khác
            ExplosiveBarrel otherBarrel = hit.GetComponent<ExplosiveBarrel>();
            if (otherBarrel != null && !otherBarrel.hasExploded)
            {
                otherBarrel.Invoke("Explode", chainDelay);
            }
        }

        // 5. Camera shake
        CameraShake.ShakeExplosion();

        // 6. Hủy thùng gốc
        Destroy(gameObject);
    }

    // Hiện bán kính nổ trong Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
