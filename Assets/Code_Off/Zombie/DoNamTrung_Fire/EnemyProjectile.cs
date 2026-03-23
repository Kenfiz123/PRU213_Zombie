using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    private Transform target;
    private Vector3 direction;

    public void Seek(Transform _target)
    {
        target = _target;

        // --- SỬA ĐOẠN NÀY ---
        // Thay vì target.position (Chân), ta cộng thêm Vector3.up (Cao lên 1m - Tức là ngực/đầu)
        // Nếu muốn cao hơn nữa thì nhân thêm (Vector3.up * 1.5f)
        Vector3 aimPoint = target.position + Vector3.up * 1.2f;

        transform.LookAt(aimPoint); // Nhắm vào điểm đã nâng cao
                                    // --------------------

        Destroy(gameObject, 5f);
    }

    void Update()
    {
        // Bay thẳng về phía trước theo hướng đã tính lúc đầu
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // 1. Nếu trúng Player -> Gây damage
        if (other.CompareTag("Player"))
        {
            PlayerHealth pHealth = other.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damage);
            }
            Destroy(gameObject); // Xóa đạn ngay
        }
        // 2. Nếu trúng Zombie (Enemy) -> Kệ nó, không làm gì cả (để đạn bay xuyên qua tay lúc ném)
        else if (other.CompareTag("Enemy"))
        {
            return;
        }
        // 3. Trúng bất cứ thứ gì khác (Tường, Đất...) -> Xóa đạn
        else
        {
            Destroy(gameObject);
        }
    }
}