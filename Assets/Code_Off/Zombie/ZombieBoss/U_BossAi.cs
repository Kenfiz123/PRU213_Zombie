using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class U_BossAI : MonoBehaviour
{
    [Header("--- CÀI ĐẶT CHUNG ---")]
    public Transform player;
    public float viewRange = 30f;
    public LayerMask obstacleLayer;

    [Header("--- DI CHUYỂN & CHỈ SỐ ---")]
    public float moveSpeed = 4f;
    public float rushSpeed = 15f;
    public float attackRange = 5f;
    public float optimalRange = 8f; // Khoảng cách tối ưu để sử dụng skill
    public float retreatRange = 3f; // Quá gần sẽ lùi lại

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
    public float magicCooldown = 6f;
    private float nextMagicTime = 0f;

    [Header("--- KỸ NĂNG TRIỆU HỒI ---")]
    public List<GameObject> minionPrefabs;
    public int minionCount = 3;
    [Range(0f, 1f)] public float summonThreshold = 0.3f;
    private bool hasSummoned = false;

    [Header("--- PHASE 3: AUTO SUMMON (mỗi X giây) ---")]
    [Tooltip("Bật: Khi Boss vào Phase 3 sẽ tự triệu hồi zombie con theo chu kỳ.")]
    [SerializeField] private bool phase3AutoSummon = true;
    [Tooltip("Chu kỳ triệu hồi ở Phase 3 (giây/lần). Mặc định 5s.")]
    [SerializeField] private float phase3SummonInterval = 5f;
    [Tooltip("Bán kính random điểm spawn quanh Boss (đơn vị).")]
    [SerializeField] private float phase3SummonRadius = 5f;
    [Tooltip("Số lượng zombie con mỗi lần summon ở Phase 3.")]
    [SerializeField] private int phase3MinionCount = 3;
    [Tooltip("Danh sách prefab zombie con cho Phase 3. Nếu để trống sẽ dùng minionPrefabs.")]
    [SerializeField] private List<GameObject> phase3MinionPrefabs = new List<GameObject>();
    [Tooltip("Nếu bật: vừa vào Phase 3 sẽ summon ngay 1 lần, sau đó lặp theo interval.")]
    [SerializeField] private bool phase3SummonOnEnter = true;
    [Tooltip("Nếu bật: zombie con được buff damage giống Phase 2 (nhân 2) khi spawn ở Phase 3.")]
    [SerializeField] private bool phase3BuffMinions = false;

    private float nextPhase3SummonTime = float.PositiveInfinity;

    [Header("--- PHASE 2 (Hóa Điên) ---")]
    public Material enrageMaterial;
    private bool isEnraged = false;
    private SkinnedMeshRenderer meshRenderer;

    [Header("--- PHASE 3 (Tuyệt Vọng) ---")]
    public Material desperationMaterial;
    private bool isDesperate = false;
    [Range(0f, 1f)] public float desperationThreshold = 0.15f;

    [Header("--- HỆ THỐNG STAMINA ---")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f;
    public float comboStaminaCost = 40f;
    public float magicStaminaCost = 30f;
    private float currentStamina;

    [Header("--- DỰ ĐOÁN VỊ TRÍ ---")]
    public bool usePrediction = true;
    public float predictionTime = 0.5f; // Dự đoán player sẽ ở đâu sau 0.5s

    [Header("--- HIT DETECTION (Đánh trúng) ---")]
    [Tooltip("Tâm vùng gây damage sẽ = transform.position + forward*offset + up*offset. Tăng upOffset nếu Player đứng sát chân mà vẫn hụt.")]
    [SerializeField] private float meleeHitForwardOffset = 0.75f;
    [SerializeField] private float meleeHitUpOffset = 1.0f;

    [Header("--- COUNTER ATTACK ---")]
    public float counterWindow = 1.5f; // Thời gian cửa sổ phản công sau khi bị đánh
    public float counterDamage = 80f;
    public float counterCooldown = 10f; // Cooldown giữa các lần counter (Phase 1)
    private float lastHitTime = -999f;
    private float lastCounterTime = -999f;
    private bool canCounter = false;
    
    [Header("--- PHASE 2: COUNTER BOOST ---")]
    [Tooltip("Giảm counterWindow ở Phase 2 để counter ít hơn (nhân với giá trị này).")]
    [SerializeField] private float phase2CounterWindowMultiplier = 0.6f;
    [Tooltip("Số lượng zone độc ở Phase 2 (nhân với counterAreaCount).")]
    [SerializeField] private float phase2CounterAreaCountMultiplier = 1.5f;
    [Tooltip("Damage zone độc ở Phase 2 (nhân với counterHazardDamageAmount).")]
    [SerializeField] private float phase2CounterDamageMultiplier = 1.5f;
    [Tooltip("Thời gian tồn tại zone độc ở Phase 2 (nhân với counterHazardDuration).")]
    [SerializeField] private float phase2CounterDurationMultiplier = 1.5f;
    [Tooltip("Tầm gây damage của HeavyAttack sau counter ở Phase 2 (nhân với attackRange).")]
    [SerializeField] private float phase2HeavyAttackRangeMultiplier = 1.8f;

    [Header("--- AREA DENIAL ---")]
    public GameObject areaDenialPrefab; // Vùng nguy hiểm (lava, poison, etc.)
    public float areaDenialCooldown = 12f;
    private float nextAreaDenialTime = 0f;

    [Header("--- COUNTER: SPAWN AREA DENIAL ---")]
    [Tooltip("Nếu bật: khi Boss Counter (animation xoay) sẽ bắn areaDenialPrefab ngẫu nhiên xung quanh.")]
    [SerializeField] private bool counterSpawnAreaDenial = true;
    [Tooltip("Delay sau khi Trigger Counter để bắt nhịp animation (giây).")]
    [SerializeField] private float counterAreaSpawnDelay = 0.1f;
    [Tooltip("Số lượng vùng nguy hiểm spawn mỗi lần Counter.")]
    [SerializeField] private int counterAreaCount = 6;
    [Tooltip("Bán kính spawn ngẫu nhiên xung quanh Boss.")]
    [SerializeField] private float counterAreaRadius = 6f;
    [Tooltip("Sát thương mỗi nhịp (tick). Áp dụng cho HazardZone nếu prefab có script HazardZone.")]
    [SerializeField] private float counterHazardDamageAmount = 15f;
    [Tooltip("Tần suất gây sát thương (giây/lần). Áp dụng cho HazardZone nếu prefab có script HazardZone.")]
    [SerializeField] private float counterHazardTickRate = 0.5f;
    [Tooltip("Thời gian tồn tại của zone (giây). Áp dụng cho HazardZone nếu prefab có script HazardZone.")]
    [SerializeField] private float counterHazardDuration = 6f;

    [Tooltip("Damage/giây của zone (fallback nếu prefab dùng AreaDenialZone).")]
    [SerializeField] private float counterAreaDamagePerSecond = 12f;
    [Tooltip("Lifetime của zone (fallback nếu prefab dùng AreaDenialZone).")]
    [SerializeField] private float counterAreaLifeTime = 6f;

    [Header("--- COMBO VARIATIONS ---")]
    public bool useVariedCombos = true;
    private int comboPattern = 0; // 0: Magic+Rush, 1: Rush+Magic, 2: Area+Magic, etc.

    // HỆ THỐNG
    public float comboCooldown = 0.4f;
    private float nextAttackTime = 0f;
    private int attackCount = 0;

    // BIẾN KHÓA HÀNH ĐỘNG
    private bool isBusy = false;

    // THEO DÕI PLAYER
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private float playerTrackingUpdateRate = 0.1f;
    private float lastTrackingUpdate = 0f;
    private float findTargetTimer = 0f;

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
        currentStamina = maxStamina;
        lastPlayerPosition = player != null ? player.position : transform.position;

        // Boss luôn đuổi Player ngay lập tức
        if (player != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;
            agent.SetDestination(player.position);
        }

        // Đăng ký event khi bị tấn công
        if (bossHealth != null)
        {
            // Giả sử ZombieHealth có event OnTakeDamage, nếu không thì dùng cách khác
            StartCoroutine(CheckForDamage());
        }
    }

    void Update()
    {
        if (bossHealth == null) return;

        // Tìm target gần nhất (Player hoặc Ally) — PHẢI chạy TRƯỚC null check
        if (player == null || findTargetTimer <= 0f)
        {
            FindNewTarget();
            findTargetTimer = 1f;
        }
        findTargetTimer -= Time.deltaTime;

        if (player == null) return;

        // Cập nhật stamina
        UpdateStamina();

        // Theo dõi vận tốc player để dự đoán
        if (Time.time - lastTrackingUpdate >= playerTrackingUpdateRate)
        {
            UpdatePlayerTracking();
            lastTrackingUpdate = Time.time;
        }

        // Nếu đang bận (đang trong chuỗi Combo) thì chỉ xoay mặt
        if (isBusy)
        {
            RotateTowardsPlayer(20f);
            return;
        }

        // Kiểm tra counter attack
        if (canCounter && Time.time - lastHitTime <= counterWindow && Time.time - lastCounterTime >= counterCooldown)
        {
            if (Vector3.Distance(transform.position, player.position) <= attackRange * 1.5f)
            {
                StartCoroutine(CounterAttack());
                return;
            }
        }

        CheckPhaseAndSummon();

        // BOSS LUÔN BIẾT VỊ TRÍ PLAYER - không cần CanSeePlayer
        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 predictedPosition = GetPredictedPlayerPosition();

        // AI QUYẾT ĐỊNH CHIẾN THUẬT
        if (distance < retreatRange)
        {
            // Quá gần: Lùi lại và tấn công
            TacticalRetreat();
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + comboCooldown;
                PerformMeleeAttack();
            }
        }
        else if (distance <= attackRange)
        {
            // Trong tầm đánh cận chiến
            RotateTowardsPlayer(15f);
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + comboCooldown;
                PerformMeleeAttack();
            }
        }
        else if (distance <= optimalRange)
        {
            // Khoảng cách tối ưu: Sử dụng skill
            RotateTowardsPlayer(12f);

            if (currentStamina >= comboStaminaCost && Time.time >= nextMagicTime)
            {
                if (useVariedCombos)
                {
                    comboPattern = Random.Range(0, 4);
                    StartCoroutine(ExecuteComboPattern(comboPattern));
                }
                else
                {
                    StartCoroutine(MagicRushCombo());
                }
                nextMagicTime = Time.time + magicCooldown;
            }
            else if (currentStamina >= magicStaminaCost && Time.time >= nextAreaDenialTime && areaDenialPrefab != null)
            {
                StartCoroutine(AreaDenialAttack());
                nextAreaDenialTime = Time.time + areaDenialCooldown;
            }
            else
            {
                ChasePlayer(false);
            }
        }
        else
        {
            // Ở xa: Luôn đuổi theo hoặc dùng combo
            if (currentStamina >= comboStaminaCost && Time.time >= nextMagicTime)
            {
                nextMagicTime = Time.time + magicCooldown;
                StartCoroutine(MagicRushCombo());
            }
            else
            {
                ChasePlayer(true); // Luôn đuổi theo, không bao giờ đứng yên
            }
            animator.SetBool("Attack", false);
        }
    }

    // ========== HỆ THỐNG MỚI ==========

    void UpdateStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }
    }

    void UpdatePlayerTracking()
    {
        if (player != null)
        {
            playerVelocity = (player.position - lastPlayerPosition) / playerTrackingUpdateRate;
            lastPlayerPosition = player.position;
        }
    }

    Vector3 GetPredictedPlayerPosition()
    {
        if (!usePrediction || player == null)
            return player != null ? player.position : transform.position;
        return player.position + (playerVelocity * predictionTime);
    }

    IEnumerator CheckForDamage()
    {
        float lastHealth = bossHealth.currentHealth;
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            if (bossHealth != null && bossHealth.currentHealth < lastHealth)
            {
                lastHitTime = Time.time;
                canCounter = true;
                lastHealth = bossHealth.currentHealth;
            }
        }
    }

    IEnumerator CounterAttack()
    {
        Debug.Log(">>> COUNTER ATTACK! <<<");
        isBusy = true;
        canCounter = false;
        lastCounterTime = Time.time;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetTrigger("Counter");

        // Tính toán giá trị cho Phase 2 (nếu đang ở Phase 2)
        int areaCount = counterAreaCount;
        float hazardDamage = counterHazardDamageAmount;
        float hazardDuration = counterHazardDuration;
        float heavyAttackRange = attackRange * 1.5f;
        
        if (isEnraged)
        {
            areaCount = Mathf.RoundToInt(counterAreaCount * phase2CounterAreaCountMultiplier);
            hazardDamage = counterHazardDamageAmount * phase2CounterDamageMultiplier;
            hazardDuration = counterHazardDuration * phase2CounterDurationMultiplier;
            heavyAttackRange = attackRange * phase2HeavyAttackRangeMultiplier;
            Debug.Log($"Phase 2 Counter: {areaCount} zones, {hazardDamage} dmg, {hazardDuration}s duration");
        }

        // Trong lúc Counter (boss xoay), bắn areaDenialPrefab ngẫu nhiên xung quanh
        if (counterSpawnAreaDenial && areaDenialPrefab != null)
        {
            yield return new WaitForSeconds(counterAreaSpawnDelay);
            SpawnAreaDenialAroundBoss(
                areaCount,
                counterAreaRadius,
                hazardDamage,
                counterHazardTickRate,
                hazardDuration,
                counterAreaDamagePerSecond * (isEnraged ? phase2CounterDamageMultiplier : 1f),
                counterAreaLifeTime * (isEnraged ? phase2CounterDurationMultiplier : 1f)
            );
        }

        yield return new WaitForSeconds(0.3f);

        // Xoay về player và tấn công
        RotateTowardsPlayer(30f);
        yield return new WaitForSeconds(0.2f);

        // Đòn phản công mạnh
        CheckAndDealDamage(heavyAttackRange, counterDamage, 1500f);

        // Phase 2: Sau counter thì thực hiện HeavyAttack 2 lần liên tiếp
        if (isEnraged)
        {
            yield return new WaitForSeconds(0.3f);
            
            // HeavyAttack lần 1
            Debug.Log(">>> PHASE 2: HEAVY ATTACK #1 <<<");
            animator.SetTrigger("HeavyAttack");
            yield return new WaitForSeconds(0.5f);
            CheckAndDealDamage(heavyAttackRange, heavyDamage, 1200f);
            
            yield return new WaitForSeconds(0.2f);
            
            // HeavyAttack lần 2 (để bắt Player đang chạy né)
            Debug.Log(">>> PHASE 2: HEAVY ATTACK #2 (Bắt Player đang chạy) <<<");
            // Cập nhật hướng về Player trước khi đánh lần 2
            if (player != null)
            {
                RotateTowardsPlayer(40f);
                yield return new WaitForSeconds(0.1f);
            }
            animator.SetTrigger("HeavyAttack");
            yield return new WaitForSeconds(0.5f);
            CheckAndDealDamage(heavyAttackRange, heavyDamage, 1200f);
        }

        yield return new WaitForSeconds(0.5f);
        isBusy = false;
    }

    void SpawnAreaDenialAroundBoss(
        int count,
        float radius,
        float hazardDamageAmount,
        float hazardTickRate,
        float hazardDuration,
        float fallbackDamagePerSecond,
        float fallbackLifeTime
    )
    {
        if (areaDenialPrefab == null) return;
        if (count <= 0) return;
        radius = Mathf.Max(0.1f, radius);

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * radius;
            randomPoint.y = transform.position.y;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                GameObject zone = Instantiate(areaDenialPrefab, hit.position, Quaternion.identity);

                // Ưu tiên HazardZone (đúng với prefab hiện tại của bạn)
                HazardZone hazard = zone.GetComponent<HazardZone>();
                if (hazard != null)
                {
                    hazard.damageAmount = hazardDamageAmount;
                    hazard.tickRate = Mathf.Max(0.01f, hazardTickRate);
                    hazard.duration = Mathf.Max(0.1f, hazardDuration);
                    continue;
                }

                // Fallback: nếu prefab dùng AreaDenialZone thì set theo cấu hình fallback
                AreaDenialZone zoneScript = zone.GetComponent<AreaDenialZone>();
                if (zoneScript != null)
                {
                    zoneScript.damagePerSecond = fallbackDamagePerSecond;
                    zoneScript.lifeTime = fallbackLifeTime;
                }
            }
        }
    }

    void TacticalRetreat()
    {
        if (!agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        Vector3 retreatDirection = (transform.position - player.position).normalized;
        Vector3 retreatPosition = transform.position + retreatDirection * 5f;

        if (NavMesh.SamplePosition(retreatPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            agent.isStopped = false;
            agent.speed = moveSpeed * 1.2f;
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    IEnumerator ExecuteComboPattern(int pattern)
    {
        isBusy = true;
        currentStamina -= comboStaminaCost;

        switch (pattern)
        {
            case 0: // Magic -> Rush -> Heavy (Combo gốc)
                yield return StartCoroutine(MagicRushCombo());
                break;
            case 1: // Rush -> Magic -> Heavy (Lao trước, bắn sau)
                yield return StartCoroutine(RushMagicCombo());
                break;
            case 2: // Area Denial -> Magic -> Rush
                yield return StartCoroutine(AreaMagicRushCombo());
                break;
            case 3: // Triple Magic Burst
                yield return StartCoroutine(TripleMagicBurst());
                break;
        }

        isBusy = false;
    }

    IEnumerator RushMagicCombo()
    {
        Debug.Log(">>> COMBO: RUSH -> MAGIC <<<");

        // Lao trước
        agent.isStopped = false;
        agent.speed = rushSpeed;
        animator.SetFloat("Speed", rushSpeed);

        float rushTimer = 0f;
        while (Vector3.Distance(transform.position, GetPredictedPlayerPosition()) > 3f && rushTimer < 2.5f)
        {
            agent.SetDestination(GetPredictedPlayerPosition());
            rushTimer += Time.deltaTime;
            yield return null;
        }

        // Dừng và bắn phép
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetFloat("Speed", 0);
        animator.SetTrigger("Skill");
        yield return new WaitForSeconds(0.5f);

        // Heavy attack
        animator.SetTrigger("HeavyAttack");
        yield return new WaitForSeconds(1f);

        agent.speed = moveSpeed;
    }

    IEnumerator AreaMagicRushCombo()
    {
        Debug.Log(">>> COMBO: AREA -> MAGIC -> RUSH <<<");

        // Tạo vùng nguy hiểm
        if (areaDenialPrefab != null)
        {
            Vector3 areaPos = GetPredictedPlayerPosition();
            if (NavMesh.SamplePosition(areaPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Instantiate(areaDenialPrefab, hit.position, Quaternion.identity);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // Bắn phép
        agent.isStopped = true;
        animator.SetTrigger("Skill");
        yield return new WaitForSeconds(0.5f);

        // Lao vào
        agent.isStopped = false;
        agent.speed = rushSpeed;
        animator.SetFloat("Speed", rushSpeed);

        float rushTimer = 0f;
        while (Vector3.Distance(transform.position, player.position) > 2.5f && rushTimer < 2f)
        {
            agent.SetDestination(player.position);
            rushTimer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;
        animator.SetTrigger("HeavyAttack");
        yield return new WaitForSeconds(1f);

        agent.speed = moveSpeed;
    }

    IEnumerator TripleMagicBurst()
    {
        Debug.Log(">>> COMBO: TRIPLE MAGIC BURST <<<");

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Bắn 3 đợt phép liên tiếp
        for (int i = 0; i < 3; i++)
        {
            animator.SetTrigger("Skill");
            yield return new WaitForSeconds(0.4f);
            // CastMagic sẽ được gọi bởi Animation Event
        }

        yield return new WaitForSeconds(0.5f);
        agent.speed = moveSpeed;
    }

    IEnumerator AreaDenialAttack()
    {
        Debug.Log(">>> AREA DENIAL ATTACK <<<");
        isBusy = true;
        currentStamina -= magicStaminaCost;

        agent.isStopped = true;
        animator.SetTrigger("Skill");

        // Tạo nhiều vùng nguy hiểm xung quanh player
        for (int i = 0; i < 3; i++)
        {
            Vector3 areaPos = GetPredictedPlayerPosition() + Random.insideUnitSphere * 4f;
            areaPos.y = transform.position.y;

            if (NavMesh.SamplePosition(areaPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Instantiate(areaDenialPrefab, hit.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.5f);
        isBusy = false;
    }

    // --- [TÍNH NĂNG MỚI] COMBO MAGIC + RUSH + HEAVY ATTACK (CẢI THIỆN) ---
    IEnumerator MagicRushCombo()
    {
        Debug.Log(">>> BOSS KÍCH HOẠT COMBO HỦY DIỆT! <<<");
        isBusy = true;
        currentStamina -= comboStaminaCost;

        // Nếu agent hoặc player không còn hợp lệ thì hủy combo để tránh lỗi
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh || player == null)
        {
            isBusy = false;
            yield break;
        }

        // GIAI ĐOẠN 1: BẮN PHÉP với dự đoán vị trí
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetFloat("Speed", 0);
        animator.SetTrigger("Skill");

        yield return new WaitForSeconds(0.5f);

        // GIAI ĐOẠN 2: LAO TỚI với dự đoán
        Debug.Log(">>> LAO TỚI! <<<");
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh || player == null)
        {
            isBusy = false;
            yield break;
        }
        agent.isStopped = false;
        agent.speed = rushSpeed;
        animator.SetFloat("Speed", rushSpeed);

        float rushTimer = 0f;
        float maxRushTime = 3f;
        Vector3 targetPos = GetPredictedPlayerPosition();

        while (Vector3.Distance(transform.position, targetPos) > 2.5f && rushTimer < maxRushTime)
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh || player == null)
            {
                isBusy = false;
                yield break;
            }
            // Cập nhật target nếu player di chuyển
            if (rushTimer % 0.3f < Time.deltaTime)
            {
                targetPos = GetPredictedPlayerPosition();
            }
            agent.SetDestination(targetPos);
            rushTimer += Time.deltaTime;
            yield return null;
        }

        // GIAI ĐOẠN 3: HEAVY ATTACK
        Debug.Log(">>> HEAVY ATTACK! <<<");
        if (agent != null && agent.isActiveAndEnabled)
        {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        }
        animator.SetFloat("Speed", 0);

        animator.SetTrigger("HeavyAttack");
        yield return new WaitForSeconds(1.0f);

        // GIAI ĐOẠN 4: NORMAL ATTACK
        Debug.Log(">>> NORMAL ATTACK! <<<");
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.8f);

        // KẾT THÚC COMBO
        isBusy = false;
        if (agent != null && agent.isActiveAndEnabled)
        {
        agent.speed = moveSpeed;
        }
    }

    // --- CÁC HÀM CŨ (GIỮ NGUYÊN) ---

    void ChasePlayer(bool isRushing)
    {
        if (!agent.isOnNavMesh || !agent.isActiveAndEnabled) return;
        agent.isStopped = false;
        agent.speed = isRushing ? rushSpeed : moveSpeed;

        // Sử dụng vị trí dự đoán khi đuổi theo
        Vector3 targetPos = (isRushing && usePrediction) ? GetPredictedPlayerPosition() : player.position;
        agent.SetDestination(targetPos);

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
        SummonMinionsInternal(minionPrefabs, minionCount, 5f, buff: isEnraged);
    }

    void SummonPhase3Minions()
    {
        List<GameObject> prefabsToUse = (phase3MinionPrefabs != null && phase3MinionPrefabs.Count > 0)
            ? phase3MinionPrefabs
            : minionPrefabs;

        int countToUse = phase3MinionCount > 0 ? phase3MinionCount : minionCount;
        float radiusToUse = Mathf.Max(0.1f, phase3SummonRadius);

        SummonMinionsInternal(prefabsToUse, countToUse, radiusToUse, buff: phase3BuffMinions);
    }

    void SummonMinionsInternal(List<GameObject> prefabs, int count, float radius, bool buff)
    {
        if (prefabs == null || prefabs.Count == 0) return;
        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Count)];

            Vector3 randomPoint = transform.position + Random.insideUnitSphere * radius;
            randomPoint.y = transform.position.y;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                // 1. Tạo Zombie con
                GameObject minion = Instantiate(selectedPrefab, hit.position, Quaternion.identity);

                // 2. Bắt lao vào Player ngay (nếu có script ZombieAI)
                var zombieScript = minion.GetComponent<ZombieAI>();
                if (zombieScript != null) zombieScript.aggressiveMode = true;

                // 3. Buff (tuỳ chọn)
                if (buff) BuffMinionDamage(minion);
            }
        }
    }

    public void CastMagic()
    {
        if (magicPrefab != null && castPoint != null && player != null)
        {
            // Sử dụng vị trí dự đoán để tính hướng bắn chính xác hơn
            Vector3 targetPos = usePrediction ? GetPredictedPlayerPosition() : player.position;

            // Tạo vector hướng về target để làm hướng chính
            Vector3 directionToTarget = (targetPos - castPoint.position).normalized;
            directionToTarget.y = 0;

            // Xoay boss về hướng target trước khi bắn (tùy chọn)
            if (directionToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToTarget);
            }

            float startAngle = magicCount > 1 ? -magicSpread / 2f : 0f;
            float angleStep = magicCount > 1 ? magicSpread / (magicCount - 1) : 0f;

            for (int i = 0; i < magicCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Quaternion spawnRotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);

                GameObject orb = Instantiate(magicPrefab, castPoint.position, spawnRotation);
                BossMagicOrb script = orb.GetComponent<BossMagicOrb>();
                if (script != null)
                {
                    script.Setup(player, magicDamage);
                }
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

        rushSpeed += 5f;
        comboCooldown = 0.2f;
        magicCooldown *= 0.7f; // Hồi skill nhanh hơn
        staminaRegenRate *= 1.5f; // Hồi stamina nhanh hơn
        
        // Phase 2: Giảm counter window để counter ít hơn
        counterWindow *= phase2CounterWindowMultiplier;

        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies) { if (enemy != this.gameObject) BuffMinionDamage(enemy); }
    }

    void EnterDesperationMode()
    {
        isDesperate = true;
        animator.SetBool("IsDesperate", true);
        if (meshRenderer != null && desperationMaterial != null) meshRenderer.material = desperationMaterial;

        // Phase 3: Tất cả đều tăng cường
        rushSpeed += 8f;
        moveSpeed += 2f;
        comboCooldown = 0.15f;
        magicCooldown *= 0.5f;
        staminaRegenRate *= 2f;
        attackSpeedMultiplier = 2f;

        // Tăng sát thương
        normalDamage *= 1.5f;
        heavyDamage *= 1.5f;
        magicDamage *= 1.5f;

        // Gọi thêm đệ
        if (!hasSummoned) PerformSummon();

        // Phase 3: auto summon theo chu kỳ
        if (phase3AutoSummon)
        {
            if (phase3SummonOnEnter)
            {
                SummonPhase3Minions();
            }
            nextPhase3SummonTime = Time.time + Mathf.Max(0.1f, phase3SummonInterval);
        }

        Debug.Log(">>> BOSS ĐÃ VÀO TRẠNG THÁI TUYỆT VỌNG! <<<");
    }

    void OnDisable() { CancelInvoke(); StopAllCoroutines(); }

    void CheckPhaseAndSummon()
    {
        float healthPercent = bossHealth.currentHealth / bossHealth.maxHealth;

        // Phase 3: Tuyệt vọng (15% máu)
        if (healthPercent <= desperationThreshold && !isDesperate)
        {
            EnterDesperationMode();
        }
        // Phase 2: Hóa điên (50% máu)
        else if (healthPercent <= 0.5f && !isEnraged)
        {
            EnterEnrageMode();
        }

        // Triệu hồi đệ
        if (healthPercent <= summonThreshold && !hasSummoned)
        {
            PerformSummon();
        }

        // Phase 3: triệu hồi định kỳ (không phụ thuộc hasSummoned one-shot)
        if (isDesperate && phase3AutoSummon && Time.time >= nextPhase3SummonTime)
        {
            SummonPhase3Minions();
            nextPhase3SummonTime = Time.time + Mathf.Max(0.1f, phase3SummonInterval);
        }
    }

    public void DealDamageArea() { CheckAndDealDamage(attackRange, normalDamage, 300f); }
    public void DealHeavyDamage() { CheckAndDealDamage(attackRange + 1f, heavyDamage, 1000f); }

    void CheckAndDealDamage(float radius, float dmgAmount, float pushForce)
    {
        Vector3 hitCenter = transform.position + transform.forward * meleeHitForwardOffset + Vector3.up * meleeHitUpOffset;
        Collider[] hitPlayers = Physics.OverlapSphere(hitCenter, radius);
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
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > viewRange) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPos - origin).normalized;
        float distance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstacleLayer))
        {
            // Kiểm tra xem có phải player hoặc là child của player không
            Transform hitParent = hit.transform;
            while (hitParent != null)
            {
                if (hitParent == player) return true;
                hitParent = hitParent.parent;
            }
            return false;
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
        // Ưu tiên TargetRegistry
        if (TargetRegistry.Instance != null)
        {
            Transform closest = TargetRegistry.GetClosestTarget(transform.position);
            if (closest != null) { player = closest; return; }
        }

        // Fallback: tìm tất cả tag "Player" → chọn gần nhất
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
