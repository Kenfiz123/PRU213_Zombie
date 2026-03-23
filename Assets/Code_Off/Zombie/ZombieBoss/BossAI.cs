using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class BossAI : MonoBehaviour
{
    [Header("--- CÀI ĐẶT CHUNG ---")]
    public Transform player;
    public float viewRange = 30f;    // Tầm nhìn xa hơn
    public LayerMask obstacleLayer;

    [Header("--- DI CHUYỂN & CHỈ SỐ ---")]
    public float moveSpeed = 4f;
    public float rushSpeed = 15f;    // [QUAN TRỌNG] Tốc độ lao vào cực nhanh (như tên lửa)
    public float attackRange = 5f;

    [Header("--- SÁT THƯƠNG ---")]
    public float normalDamage = 20f;
    public float heavyDamage = 50f;
    public float magicDamage = 30f;
    public float attackSpeedMultiplier = 1.5f;

    [Header("--- KỸ NĂNG BẮN PHÉP (Magic) ---")]
    public GameObject magicPrefab;
    public Transform castPoint;
    [Range(1, 20)] public int magicCount = 5;
    [Range(0, 360)] public float magicSpread = 60f;
    public float magicCooldown = 6f; // Hồi lâu hơn chút vì combo này rất mạnh
    private float nextMagicTime = 0f;

    [Header("--- KỸ NĂNG TRIỆU HỒI ---")]
    public List<GameObject> minionPrefabs;
    public int minionCount = 3;
    [Range(0f, 1f)] public float summonThreshold = 0.3f;
    private bool hasSummoned = false;

    [Header("--- PHASE 2 (Hóa Điên) ---")]
    public Material enrageMaterial;
    private bool isEnraged = false;
    private SkinnedMeshRenderer meshRenderer;

    // HỆ THỐNG
    public float comboCooldown = 0.4f;
    private float nextAttackTime = 0f;
    private int attackCount = 0;

    // BIẾN KHÓA HÀNH ĐỘNG
    private bool isBusy = false;

    private NavMeshAgent agent;
    private Animator animator;
    private ZombieHealth bossHealth;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        bossHealth = GetComponent<ZombieHealth>();
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        // Tìm Player qua TargetRegistry hoặc fallback
        if (player == null)
        {
            if (TargetRegistry.Instance != null)
                player = TargetRegistry.GetClosestTarget(transform.position);
            if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
                player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        agent.stoppingDistance = attackRange - 1f;

        // Boss luôn đuổi Player ngay lập tức
        if (player != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;
            agent.SetDestination(player.position);
        }
    }

    private float bFindTimer = 0f;

    void Update()
    {
        if (bossHealth == null) return;

        // Tìm target MỚI nếu player null hoặc mỗi 1 giây
        if (player == null || bFindTimer <= 0f)
        {
            FindNewTarget();
            bFindTimer = 1f;
        }
        bFindTimer -= Time.deltaTime;

        if (player == null) return;

        // Nếu đang bận (đang trong chuỗi Combo) thì chỉ xoay mặt
        if (isBusy)
        {
            RotateTowardsPlayer(20f);
            return;
        }

        CheckPhaseAndSummon();

        // BOSS LUÔN BIẾT VỊ TRÍ PLAYER - không cần CanSeePlayer
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            // Ở xa: Ưu tiên dùng Combo Magic -> Lao vào
            if (Time.time >= nextMagicTime)
            {
                nextMagicTime = Time.time + magicCooldown;
                StartCoroutine(MagicRushCombo());
            }
            else
            {
                ChasePlayer(true); // Luôn đuổi theo
            }
            animator.SetBool("Attack", false);
        }
        else
        {
            // Ở gần: Đánh thường
            RotateTowardsPlayer(15f);
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + comboCooldown;
                PerformMeleeAttack();
            }
        }
    }

    // --- [TÍNH NĂNG MỚI] COMBO MAGIC + RUSH + HEAVY ATTACK ---
    IEnumerator MagicRushCombo()
    {
        Debug.Log(">>> BOSS KÍCH HOẠT COMBO HỦY DIỆT! <<<");
        isBusy = true; // Khóa AI bình thường lại

        // GIAI ĐOẠN 1: BẮN PHÉP (Vừa chạy vừa bắn hoặc đứng lại bắn nhanh)
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetFloat("Speed", 0);
        animator.SetTrigger("Skill"); // Bắn phép

        // Chờ 0.5s để tung đạn ra (Tùy animation của bạn nhanh hay chậm)
        yield return new WaitForSeconds(0.5f);

        // (Lưu ý: Hàm CastMagic sẽ được gọi bởi Animation Event ở đây)

        // GIAI ĐOẠN 2: LAO TỚI (RUSH)
        Debug.Log(">>> LAO TỚI! <<<");
        agent.isStopped = false;
        agent.speed = rushSpeed; // Tăng tốc độ lên mức tối đa
        animator.SetFloat("Speed", rushSpeed); // Chạy animation chạy nhanh

        float rushTimer = 0f;
        float maxRushTime = 3f; // Chỉ lao tối đa 3 giây, nếu không bắt được thì thôi

        // Vòng lặp lao tới: Chạy cho đến khi sát Player (cách 2m)
        while (Vector3.Distance(transform.position, player.position) > 2.5f && rushTimer < maxRushTime)
        {
            agent.SetDestination(player.position);
            rushTimer += Time.deltaTime;
            yield return null; // Chờ frame tiếp theo
        }

        // GIAI ĐOẠN 3: HEAVY ATTACK (Ngay khi tiếp cận)
        Debug.Log(">>> HEAVY ATTACK! <<<");
        agent.isStopped = true; // Phanh gấp
        agent.velocity = Vector3.zero;
        animator.SetFloat("Speed", 0);

        animator.SetTrigger("HeavyAttack"); // Đấm mạnh
        yield return new WaitForSeconds(1.0f); // Chờ đấm xong

        // GIAI ĐOẠN 4: NORMAL ATTACK (Bồi thêm nhát nữa)
        Debug.Log(">>> NORMAL ATTACK! <<<");
        animator.SetTrigger("Attack"); // Đấm thường
        yield return new WaitForSeconds(0.8f); // Chờ đấm xong

        // KẾT THÚC COMBO
        isBusy = false; // Mở khóa cho AI hoạt động lại bình thường
        agent.speed = moveSpeed; // Trả lại tốc độ đi bộ
    }

    // --- CÁC HÀM CŨ (GIỮ NGUYÊN) ---

    void ChasePlayer(bool isRushing)
    {
        if (!agent.isOnNavMesh || !agent.isActiveAndEnabled) return;
        agent.isStopped = false;
        agent.speed = isRushing ? rushSpeed : moveSpeed;
        agent.SetDestination(player.position);
        animator.SetFloat("Speed", agent.velocity.magnitude);
        RotateTowardsPlayer(10f);
    }

    void PerformSummon()
    {
        hasSummoned = true;
        isBusy = true; // Khóa khi gọi đệ
        if (agent.isOnNavMesh) agent.isStopped = true;
        animator.SetTrigger("Roar");
        Invoke("ResetBusyState", 2f);
    }

    void ResetBusyState()
    {
        isBusy = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    public void SummonMinions()
    {
        if (minionPrefabs.Count == 0) return;

        for (int i = 0; i < minionCount; i++)
        {
            GameObject selectedPrefab = minionPrefabs[Random.Range(0, minionPrefabs.Count)];
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * 5f;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                // 1. Tạo ra Zombie
                GameObject minion = Instantiate(selectedPrefab, hit.position, Quaternion.identity);

                // 2. [MỚI] Bắt nó lao vào Player ngay lập tức (Aggressive Mode)
                var zombieScript = minion.GetComponent<ZombieAI>();
                if (zombieScript != null)
                {
                    zombieScript.aggressiveMode = true; // Kích hoạt chế độ "Chó điên"
                }

                // 3. Nếu đang Phase 2 thì Buff thêm damage
                if (isEnraged) BuffMinionDamage(minion);
            }
        }
    }

    public void CastMagic()
    {
        if (magicPrefab != null && castPoint != null)
        {
            float startAngle = magicCount > 1 ? -magicSpread / 2f : 0f;
            float angleStep = magicCount > 1 ? magicSpread / (magicCount - 1) : 0f;
            for (int i = 0; i < magicCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Quaternion spawnRotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);
                GameObject orb = Instantiate(magicPrefab, castPoint.position, spawnRotation);
                BossMagicOrb script = orb.GetComponent<BossMagicOrb>();
                if (script != null) script.Setup(player, magicDamage);
            }
        }
    }

    void BuffMinionDamage(GameObject minion)
    {
        bool buffed = false;
        var rangedScript = minion.GetComponent<RangedZombieAI>();
        if (rangedScript != null) { rangedScript.damage *= 2; buffed = true; }

        var meleeScript = minion.GetComponent<ZombieAI>();
        if (meleeScript != null) { meleeScript.damage *= 2; buffed = true; }

        var explodeScript = minion.GetComponent<ExplodingZombieAI>();
        if (explodeScript != null) { explodeScript.maxDamage *= 2; buffed = true; }

        if (buffed || minion.transform.localScale.x < 1.5f) minion.transform.localScale *= 1.2f;
    }

    void EnterEnrageMode()
    {
        isEnraged = true;
        animator.SetBool("IsEnraged", true);
        if (meshRenderer != null && enrageMaterial != null) meshRenderer.material = enrageMaterial;

        rushSpeed += 5f; // Phase 2 lao nhanh hơn nữa
        comboCooldown = 0.2f;

        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies) { if (enemy != this.gameObject) BuffMinionDamage(enemy); }
    }

    void OnDisable() { CancelInvoke(); StopAllCoroutines(); }

    void CheckPhaseAndSummon()
    {
        if (bossHealth.currentHealth <= bossHealth.maxHealth / 2 && !isEnraged) EnterEnrageMode();
        if (bossHealth.currentHealth <= bossHealth.maxHealth * summonThreshold && !hasSummoned) PerformSummon();
    }

    public void DealDamageArea() { CheckAndDealDamage(attackRange, normalDamage, 300f); }
    public void DealHeavyDamage() { CheckAndDealDamage(attackRange + 1f, heavyDamage, 1000f); }

    void CheckAndDealDamage(float radius, float dmgAmount, float pushForce)
    {
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position + transform.forward, radius);
        foreach (Collider hit in hitPlayers)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth pHealth = hit.GetComponent<PlayerHealth>();
                if (pHealth != null) { pHealth.TakeDamage(dmgAmount); }

                AllyHealth aHealth = hit.GetComponent<AllyHealth>();
                if (aHealth != null) { aHealth.TakeDamage(dmgAmount); }

                Rigidbody pRb = hit.GetComponent<Rigidbody>();
                if (pRb != null) pRb.AddForce((transform.forward + Vector3.up) * pushForce);
            }
        }
    }

    bool CanSeePlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > viewRange) return false;
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 direction = (player.position + Vector3.up * 1.5f) - origin;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, viewRange, obstacleLayer))
        {
            if (hit.transform != player) return false;
        }
        return true;
    }

    void PerformMeleeAttack()
    {
        animator.speed = attackSpeedMultiplier;
        if (attackCount >= 2) { animator.SetTrigger("HeavyAttack"); attackCount = 0; }
        else { animator.SetTrigger("Attack"); attackCount++; }
        StartCoroutine(ResetAnimationSpeed(0.8f));
    }

    IEnumerator ResetAnimationSpeed(float delay) { yield return new WaitForSeconds(delay); animator.speed = 1f; }

    void RotateTowardsPlayer(float speed)
    {
        Vector3 dir = (player.position - transform.position).normalized; dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
    }

    void FindNewTarget()
    {
        if (TargetRegistry.Instance != null)
        {
            Transform closest = TargetRegistry.GetClosestTarget(transform.position);
            if (closest != null) { player = closest; return; }
        }

        GameObject[] targets = GameObject.FindGameObjectsWithTag("Player");
        float bestDist = Mathf.Infinity;
        Transform best = null;
        foreach (var t in targets)
        {
            if (t == null) continue;
            float d = (transform.position - t.transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = t.transform; }
        }
        if (best != null) player = best;
    }
}