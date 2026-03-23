using UnityEngine;
using UnityEngine.AI;

public class ZombieHealth : MonoBehaviour
{
    [Header("Chỉ số Máu")]
    public float maxHealth = 50f;
    public float currentHealth;

    // Các thành phần cần tắt khi chết
    private Animator animator;
    private NavMeshAgent agent;
    private Collider col;

    // Khai báo các loại não Zombie
    private ZombieAI meleeAI;
    private RangedZombieAI rangedAI;
    private ExplodingZombieAI explodingAI;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        col = GetComponent<CapsuleCollider>();

        // Tự tìm xem con này đang dùng não nào
        meleeAI = GetComponent<ZombieAI>();
        rangedAI = GetComponent<RangedZombieAI>();
        explodingAI = GetComponent<ExplodingZombieAI>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        // Debug.Log(gameObject.name + " còn: " + currentHealth + " máu");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log(gameObject.name + " Đã Chết!");

        // 0. Bỏ tag Enemy để WaveManager không đếm xác chết
        gameObject.tag = "Untagged";

        // 1. Chạy Animation Chết
        if (animator != null) animator.SetTrigger("Die");

        // 2. Tắt di chuyển NavMesh
        if (agent != null) agent.enabled = false;

        // 3. Tắt não AI (Kiểm tra xem nó là loại nào thì tắt loại đó)
        if (meleeAI != null) meleeAI.enabled = false;
        if (rangedAI != null) rangedAI.enabled = false;
        if (explodingAI != null) explodingAI.enabled = false;

        // 4. Tắt va chạm (để bắn xuyên qua xác chết)
        if (col != null) col.enabled = false;

        // 5. Cộng điểm nâng cấp
        if (WeaponUpgradeManager.Instance != null)
        {
            bool isBoss = GetComponent<BossAI>() != null || GetComponent<U_BossAI>() != null;
            bool isSpecial = GetComponent<RangedZombieAI>() != null || GetComponent<ExplodingZombieAI>() != null;

            if (isBoss)
                WeaponUpgradeManager.Instance.OnBossKill();
            else if (isSpecial)
                WeaponUpgradeManager.Instance.OnSpecialKill();
            else
                WeaponUpgradeManager.Instance.OnNormalKill();
        }

        // 6. Kiểm tra Boss chết → hiện UI chiến thắng
        if (GetComponent<BossAI>() != null || GetComponent<U_BossAI>() != null)
        {
            VictoryManager.ShowVictory();
        }

        // 7. Xóa xác sau 5 giây
        Destroy(gameObject, 5f);
    }
}