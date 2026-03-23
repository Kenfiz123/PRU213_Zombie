using UnityEngine;
using UnityEngine.AI;

public class RangedZombieAI : MonoBehaviour
{
    [Header("Cài đặt chung")]
    public Transform player;            // Mục tiêu (Player)
    public float moveSpeed = 3.5f;      // Tốc độ chạy
    public float attackRange = 15f;     // Tầm bắn (Nên để xa, VD: 10-15m)

    [Header("Cài đặt Tấn công")]
    public GameObject projectilePrefab; // Kéo Prefab viên đạn (ZombieRock) vào đây
    public Transform firePoint;         // Kéo điểm bắn (trên tay phải) vào đây
    public float timeBetweenAttacks = 2f; // Tốc độ ném (giây/lần)
    public float damage = 10f;          // Sát thương của đạn

    // Biến nội bộ
    private NavMeshAgent agent;
    private Animator animator;
    private float nextAttackTime = 0f;
    private float findTargetTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        FindClosestTarget();

        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange;
    }

    void Update()
    {
        // Tìm target gần nhất mỗi 2 giây
        findTargetTimer += Time.deltaTime;
        if (findTargetTimer >= 2f)
        {
            findTargetTimer = 0f;
            FindClosestTarget();
        }

        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Cập nhật Animation chạy
        // agent.velocity.magnitude là tốc độ hiện tại (0 là đứng, >0 là chạy)
        animator.SetFloat("Speed", agent.velocity.magnitude);

        // LOGIC DI CHUYỂN & TẤN CÔNG
        if (distance <= attackRange)
        {
            // 1. TRONG TẦM BẮN -> ĐỨNG LẠI & TẤN CÔNG
            RotateTowardsPlayer(); // Luôn quay mặt về phía Player để ném cho chuẩn

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + timeBetweenAttacks;
                ThrowAttack();
            }
        }
        else
        {
            // 2. NGOÀI TẦM BẮN -> CHẠY THEO
            agent.SetDestination(player.position);
            animator.SetBool("Attack", false); // Tắt animation đánh
        }
    }

    // Hàm quay mặt từ từ về phía Player (để không bị giật cục)
    void RotateTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    // Hàm thực hiện hành động ném
    void ThrowAttack()
    {
        // Chạy Animation Ném
        animator.SetTrigger("Attack");

        // Gọi hàm sinh đạn (Delay nhẹ 0.5s để khớp với tay vung lên)
        
    }

    // Hàm sinh ra viên đạn thực sự
    public void SpawnProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject rock = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            EnemyProjectile projectileScript = rock.GetComponent<EnemyProjectile>();
            if (projectileScript != null)
            {
                projectileScript.damage = damage;
                projectileScript.Seek(player);
            }
        }
    }

    void FindClosestTarget()
    {
        if (TargetRegistry.Instance != null)
        {
            Transform best = TargetRegistry.GetClosestTarget(transform.position);
            if (best != null) player = best;
            return;
        }

        // Fallback
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Player");
        float closest = Mathf.Infinity;
        Transform best2 = null;
        for (int i = 0; i < targets.Length; i++)
        {
            float d = (transform.position - targets[i].transform.position).sqrMagnitude;
            if (d < closest) { closest = d; best2 = targets[i].transform; }
        }
        if (best2 != null) player = best2;
    }
}