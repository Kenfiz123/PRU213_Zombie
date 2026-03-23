using UnityEngine;

public class MagicRainProjectile : MonoBehaviour
{
    public float damage = 30f;
    public GameObject hitEffect; // Hiệu ứng nổ khi chạm đất

    // Tự hủy sau 5s nếu lỡ rơi xuống vực
    void Start()
    {
        Destroy(gameObject, 5f);
    }

    void OnTriggerEnter(Collider other)
    {
        // 1. Nếu trúng Quái
        if (other.CompareTag("Enemy") || other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            var enemyHealth = other.GetComponent<ZombieHealth>();
            if (enemyHealth == null) enemyHealth = other.GetComponentInParent<ZombieHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            Explode();
        }
        // 2. Nếu trúng Đất (để nổ cho đẹp)
        // Lưu ý: Không nổ khi chạm vào Player, Ally hoặc cái Vòng tròn đỏ (MagicZone)
        else if (other.gameObject.layer == LayerMask.NameToLayer("Default") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    void Explode()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}