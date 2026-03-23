using UnityEngine;

public class PlayerKnifeController : MonoBehaviour
{
    [Header("--- CÀI ĐẶT SÁT THƯƠNG ---")]
    public float fastDamage = 20f;    // Sát thương chuột trái
    public float heavyDamage = 50f;   // Sát thương chuột phải
    public float attackRange = 2.5f;  // Tầm xa của dao (nên để 2.5 - 3)
    public float attackRadius = 0.5f; // Độ to của vết chém (để 0.5 là vừa)

    [Header("--- TỐC ĐỘ ĐÁNH ---")]
    public float fastRate = 0.25f;    // Tốc độ chuột trái (thấp = nhanh)
    public float heavyRate = 1.2f;    // Tốc độ chuột phải (cao = chậm)

    [Header("--- CÀI ĐẶT BẮT BUỘC ---")]
    public Transform attackPoint;     // Điểm xuất phát (Mũi dao hoặc Camera)
    public LayerMask enemyLayer;      // Layer của Zombie (Quan trọng!)
    public Animator animator;         // Animator của tay

    // Biến nội bộ để tính hồi chiêu
    private float nextAttackTime = 0f;

    void Update()
    {
        // Kiểm tra thời gian hồi chiêu
        if (Time.time >= nextAttackTime)
        {
            // 1. CHUỘT TRÁI (Giữ để chém liên tục)
            if (Input.GetMouseButton(0))
            {
                PerformAttack(fastDamage, "AttackFast");
                nextAttackTime = Time.time + fastRate;
            }
            // 2. CHUỘT PHẢI (Ấn 1 lần chém 1 cái mạnh)
            else if (Input.GetMouseButtonDown(1))
            {
                PerformAttack(heavyDamage, "AttackHeavy");
                nextAttackTime = Time.time + heavyRate;
            }
        }
    }

    void PerformAttack(float damageAmount, string animTrigger)
    {
        // 1. Chạy Animation (Nếu có)
        if (animator != null)
        {
            animator.SetTrigger(animTrigger);
        }

        // 2. Xử lý va chạm bằng SphereCast (Quả cầu)
        RaycastHit hit;

        // Bắn 1 quả cầu từ attackPoint ra phía trước
        if (Physics.SphereCast(attackPoint.position, attackRadius, attackPoint.forward, out hit, attackRange, enemyLayer))
        {
            Debug.Log("⚔️ CHÉM TRÚNG: " + hit.collider.name);

            // 3. Tìm script máu (Tìm cả ở object bị chém lẫn object cha nó)
            ZombieHealth targetHealth = hit.collider.GetComponentInParent<ZombieHealth>();

            if (targetHealth != null)
            {
                float finalDmg = damageAmount * DifficultyManager.PlayerDamageMul;
                targetHealth.TakeDamage(finalDmg);
                Debug.Log($"Gây {finalDmg} sát thương lên {targetHealth.name}");

                // Damage Number
                if (DamageNumberManager.Instance != null)
                    DamageNumberManager.Instance.Spawn(hit.point, finalDmg, false);
            }
            else
            {
                Debug.LogWarning("⚠️ Trúng Collider nhưng không tìm thấy script 'ZombieHealth'! Kiểm tra lại Zombie.");
            }
        }
        else
        {
            // Nếu muốn debug xem chém hụt thì bỏ comment dòng dưới
            // Debug.Log("❌ Chém vào không khí (hoặc sai Layer)");
        }
    }

    // Vẽ hình cầu trong Editor để bạn dễ căn chỉnh tầm đánh
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        // Vẽ tia dài bằng tầm đánh
        Vector3 endPosition = attackPoint.position + attackPoint.forward * attackRange;
        Gizmos.DrawLine(attackPoint.position, endPosition);

        // Vẽ quả cầu tại điểm cuối cùng
        Gizmos.DrawWireSphere(endPosition, attackRadius);
    }
}