using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class AllyController : MonoBehaviour
{
    // ===================== ENUMS =====================
    public enum AllyState { Follow, Combat, Retreat, Support, Dead }

    // ===================== STATE =====================
    [Header("--- TRẠNG THÁI ---")]
    [SerializeField] private AllyState currentState = AllyState.Follow;
    public AllyState CurrentState => currentState;

    // ===================== THAM CHIẾU =====================
    [Header("--- THAM CHIẾU ---")]
    public Transform player;
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer;

    // ===================== DI CHUYỂN =====================
    [Header("--- DI CHUYỂN ---")]
    public float followStopDistance = 3f;
    public float followFarDistance = 15f; // Quá xa thì chạy nhanh về
    [Tooltip("Tốc độ đi bộ bình thường")]
    public float walkSpeed = 4f;
    [Tooltip("Tốc độ chạy nhanh (rush/retreat)")]
    public float runSpeed = 8f;

    // ===================== TACTICAL MOVEMENT =====================
    [Header("--- DI CHUYỂN CHIẾN THUẬT ---")]
    [Tooltip("Bán kính strafe quanh target khi combat")]
    public float strafeRadius = 4f;
    [Tooltip("Tốc độ strafe (góc/giây)")]
    public float strafeAngularSpeed = 60f;
    [Tooltip("Khoảng cách dodge khi né đòn")]
    public float dodgeDistance = 2.5f;
    [Tooltip("Khoảng cách tối ưu để dùng combo cận chiến")]
    public float engageRange = 4f;
    [Tooltip("Khoảng cách tối ưu để bắn Kame")]
    public float disengageRange = 12f;

    private float strafeAngle = 0f;
    private int strafeDirection = 1; // 1 = phải, -1 = trái

    // ===================== QUÉT MỤC TIÊU =====================
    [Header("--- QUÉT MỤC TIÊU ---")]
    public float scanRange = 15f;
    [Tooltip("Bán kính coi là bị vây (để chuyển Retreat)")]
    public float surroundCheckRadius = 5f;
    [Tooltip("Số enemy tối thiểu để coi là bị vây")]
    public int surroundThreshold = 3;

    // ===================== SÁT THƯƠNG =====================
    [Header("--- SÁT THƯƠNG ---")]
    public float damageNormal = 20f;
    public float damageHeavy = 40f;
    public float damageKame = 100f;
    [Tooltip("Bội số damage hiện tại (Phase system sẽ thay đổi giá trị này)")]
    public float damageMultiplier = 1f;

    // ===================== COOLDOWN =====================
    [Header("--- COOLDOWN ---")]
    public float comboCooldown = 3.0f;
    public float kameCooldown = 4.0f;
    [Tooltip("Cooldown giữa các lần dodge")]
    public float dodgeCooldown = 2f;
    [Tooltip("Cooldown giữa các lần heal Player")]
    public float healCooldown = 10f;
    [Tooltip("Cooldown giữa các lần taunt")]
    public float tauntCooldown = 15f;

    private float nextAttackTime = 0f;
    private float nextDodgeTime = 0f;
    private float nextHealTime = 0f;
    private float nextTauntTime = 0f;

    // ===================== STAMINA =====================
    [Header("--- STAMINA ---")]
    public float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    public float staminaRegenRate = 12f;
    public float staminaRegenDelay = 1.5f;
    public float kameStaminaCost = 40f;
    public float comboStaminaCost = 20f;
    public float healStaminaCost = 50f;
    public float tripleKameStaminaCost = 90f;

    private float lastStaminaUseTime = -999f;

    // ===================== HỖ TRỢ PLAYER =====================
    [Header("--- HỖ TRỢ PLAYER ---")]
    [Tooltip("Lượng máu heal cho Player mỗi lần")]
    public float healAmount = 30f;
    [Tooltip("Khoảng cách tối đa để heal Player")]
    public float healRange = 5f;
    [Tooltip("Player HP dưới % này thì Ally sẽ heal (0.4 = 40%)")]
    public float playerHealThreshold = 0.4f;
    [Tooltip("Player HP dưới % này thì Ally sẽ taunt kéo aggro (0.25 = 25%)")]
    public float playerTauntThreshold = 0.25f;
    [Tooltip("Khoảng cách không có enemy mới chuyển sang Support")]
    public float safeSupportRadius = 8f;

    private PlayerHealth playerHealth;
    private PlayerArmor playerArmor;

    // ===================== DỰ ĐOÁN VỊ TRÍ =====================
    [Header("--- DỰ ĐOÁN VỊ TRÍ ---")]
    public bool usePrediction = true;
    public float predictionTime = 0.5f;

    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private float lastTrackingUpdate = 0f;
    private float trackingUpdateRate = 0.1f;

    // ===================== PHASE =====================
    [Header("--- PHASE ---")]
    [Tooltip("Ngưỡng HP để vào Phase 2 (Empowered). 0.6 = 60%")]
    public float phase2Threshold = 0.6f;
    [Tooltip("Ngưỡng HP để vào Phase 3 (Desperate). 0.3 = 30%")]
    public float phase3Threshold = 0.3f;

    private int currentPhase = 1;

    // ===================== DODGE / COUNTER =====================
    [Header("--- DODGE / COUNTER ---")]
    [Tooltip("Xác suất dodge khi bị đánh (0.3 = 30%)")]
    [Range(0f, 1f)] public float dodgeChance = 0.3f;
    [Tooltip("Bội số damage counter sau khi dodge")]
    public float counterDamageMultiplier = 1.5f;

    private bool canCounter = false;
    private float counterWindow = 1.0f;
    private float lastDodgeTime = -999f;

    // ===================== KAMEKAMEHA =====================
    [Header("--- KAMEKAMEHA ---")]
    public GameObject kameBeamPrefab;
    public Transform kameFirePoint;
    public float kameBeamDuration = 2f;

    private GameObject currentKameBeam;

    // ===================== BIẾN NỘI BỘ =====================
    private NavMeshAgent agent;
    private Animator anim;
    private AllyHealth allyHealth;
    private Transform currentTarget;
    private Transform lastTarget;
    private bool isBusy = false;
    private bool isDead = false;

    // Cache các parameter tồn tại trong Animator (tránh warning khi set param không có)
    private HashSet<string> animParams = new HashSet<string>();

    // Cooldown Kame (dùng float thay vì HashSet per-zombie để tránh loop vĩnh viễn)
    private float nextKameTime = 0f;

    // ========================================================
    //                       KHỞI TẠO
    // ========================================================

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        allyHealth = GetComponent<AllyHealth>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerHealth = p.GetComponent<PlayerHealth>();
                playerArmor = p.GetComponent<PlayerArmor>();
            }
        }
        else
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerArmor = player.GetComponent<PlayerArmor>();
        }

        agent.stoppingDistance = followStopDistance;
        agent.speed = walkSpeed;
        currentStamina = maxStamina;

        // Cache tên tất cả parameter có trong Animator
        if (anim != null)
        {
            foreach (var p in anim.parameters)
                animParams.Add(p.name);
        }

        TransitionToState(AllyState.Follow);
    }

    // ===================== ANIMATOR SAFE HELPERS =====================
    void AnimTrigger(string name)
    {
        if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null && animParams.Contains(name))
            anim.SetTrigger(name);
    }

    void AnimBool(string name, bool value)
    {
        if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null && animParams.Contains(name))
            anim.SetBool(name, value);
    }

    void AnimFloat(string name, float value)
    {
        if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null && animParams.Contains(name))
            anim.SetFloat(name, value);
    }

    // ========================================================
    //                     VÒNG LẶP CHÍNH
    // ========================================================

    void Update()
    {
        if (isDead || player == null) return;

        UpdateStamina();
        UpdateTargetTracking();

        // Nếu đang bận (combo/dodge) thì chỉ xoay mặt
        if (isBusy)
        {
            if (currentTarget != null)
                RotateTowards(currentTarget.position, 15f);
            return;
        }

        switch (currentState)
        {
            case AllyState.Follow: HandleFollow(); break;
            case AllyState.Combat: HandleCombat(); break;
            case AllyState.Retreat: HandleRetreat(); break;
            case AllyState.Support: HandleSupport(); break;
        }

        UpdateAnimation();
        EvaluateStateTransition();
    }

    // ========================================================
    //                    CHUYỂN TRẠNG THÁI
    // ========================================================

    void TransitionToState(AllyState newState)
    {
        // Rời state cũ
        switch (currentState)
        {
            case AllyState.Retreat:
                AnimBool("IsRetreating", false);
                break;
        }

        currentState = newState;

        // Vào state mới
        switch (newState)
        {
            case AllyState.Follow:
                agent.speed = walkSpeed;
                agent.stoppingDistance = followStopDistance;
                if (agent.isOnNavMesh) agent.isStopped = false;
                break;

            case AllyState.Combat:
                agent.stoppingDistance = engageRange - 0.5f;
                if (agent.isOnNavMesh) agent.isStopped = false;
                strafeAngle = Random.Range(0f, 360f);
                strafeDirection = Random.value > 0.5f ? 1 : -1;
                break;

            case AllyState.Retreat:
                agent.speed = runSpeed;
                if (agent.isOnNavMesh) agent.isStopped = false;
                AnimBool("IsRetreating", true);
                break;

            case AllyState.Support:
                agent.speed = runSpeed;
                agent.stoppingDistance = 2f;
                if (agent.isOnNavMesh) agent.isStopped = false;
                break;

            case AllyState.Dead:
                isDead = true;
                if (agent.isOnNavMesh) agent.isStopped = true;
                AnimTrigger("Die");
                StopAllCoroutines();
                CancelInvoke();
                break;
        }
    }

    void EvaluateStateTransition()
    {
        if (isDead) return;

        float allyHpPercent = allyHealth != null
            ? allyHealth.currentHealth / allyHealth.maxHealth
            : 1f;
        float playerHpPercent = (playerHealth != null)
            ? playerHealth.CurrentHealth / playerHealth.maxHealth
            : 1f;
        bool hasEnemyNearby = HasEnemyInRange(scanRange);
        bool hasEnemyClose = HasEnemyInRange(safeSupportRadius);
        bool isSurrounded = CountEnemiesInRange(surroundCheckRadius) >= surroundThreshold;

        switch (currentState)
        {
            case AllyState.Follow:
                if (hasEnemyNearby)
                {
                    TransitionToState(AllyState.Combat);
                }
                else if (playerHpPercent < playerHealThreshold && !hasEnemyClose
                         && currentStamina >= healStaminaCost && Time.time >= nextHealTime)
                {
                    TransitionToState(AllyState.Support);
                }
                break;

            case AllyState.Combat:
                if (allyHpPercent <= phase3Threshold || isSurrounded)
                {
                    TransitionToState(AllyState.Retreat);
                }
                else if (!hasEnemyNearby)
                {
                    currentTarget = null;
                    TransitionToState(AllyState.Follow);
                }
                break;

            case AllyState.Retreat:
                // Đã về gần Player và HP hồi đủ -> quay lại
                float distToPlayer = Vector3.Distance(transform.position, player.position);
                if (distToPlayer < followStopDistance + 2f && allyHpPercent > 0.5f)
                {
                    TransitionToState(hasEnemyNearby ? AllyState.Combat : AllyState.Follow);
                }
                // Bị dồn vào góc (không thể retreat) -> chiến đấu
                else if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    if (hasEnemyNearby) TransitionToState(AllyState.Combat);
                }
                break;

            case AllyState.Support:
                if (hasEnemyClose)
                {
                    TransitionToState(AllyState.Combat);
                }
                else if (playerHpPercent >= playerHealThreshold)
                {
                    TransitionToState(AllyState.Follow);
                }
                break;
        }
    }

    // ========================================================
    //                   STATE HANDLERS
    // ========================================================

    // -------- FOLLOW --------
    void HandleFollow()
    {
        if (!agent.isOnNavMesh) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Quá xa -> chạy nhanh lại
        agent.speed = distToPlayer > followFarDistance ? runSpeed : walkSpeed;

        if (distToPlayer > followStopDistance)
        {
            agent.SetDestination(player.position);
        }

        // Vẫn quét enemy khi đi theo
        if (currentTarget == null) FindBestTarget();
    }

    // -------- COMBAT --------
    void HandleCombat()
    {
        if (!agent.isOnNavMesh) return;

        // Tìm target nếu chưa có hoặc target đã chết
        if (currentTarget == null || !ValidateTarget(currentTarget))
        {
            currentTarget = null;
            FindBestTarget();
            if (currentTarget == null) return; // Không có ai -> EvaluateState sẽ chuyển Follow
        }

        float distToTarget = Vector3.Distance(transform.position, currentTarget.position);

        // ===== KAME: Bắn theo cooldown (không phải per-zombie để tránh loop vĩnh viễn) =====
        if (Time.time >= nextKameTime && currentStamina >= kameStaminaCost)
        {
            // DỪNG HẲN di chuyển, quay mặt về đúng target
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            RotateTowards(currentTarget.position, 25f);

            lastTarget = currentTarget;
            StartCoroutine(FireKameImmediate());
            return; // Không làm gì khác trong frame này
        }

        // ---- CHIẾN THUẬT DI CHUYỂN (sau Kame - ưu tiên tiến vào đánh cận chiến) ----
        if (distToTarget > engageRange)
        {
            // Chưa đủ gần → chạy thẳng về phía zombie
            agent.isStopped = false;
            agent.speed = distToTarget > disengageRange ? runSpeed : walkSpeed;
            agent.SetDestination(currentTarget.position);
        }
        else if (Time.time < nextAttackTime)
        {
            // Đã trong tầm cận chiến nhưng cooldown chưa hết → strafe né đòn
            PerformStrafe();
        }
        else
        {
            // Trong tầm + cooldown hết → dừng lại, nhìn target, chờ attack code
            agent.isStopped = true;
            RotateTowards(currentTarget.position, 15f);
        }

        // ---- TẤN CÔNG CẬN CHIẾN ----
        if (Time.time >= nextAttackTime)
        {
            // Counter attack sau dodge (ưu tiên cao nhất)
            if (canCounter && distToTarget <= engageRange + 1f)
            {
                canCounter = false;
                StartCoroutine(CounterAttack());
                return;
            }

            // Combo cận chiến
            if (distToTarget <= engageRange + 1f && currentStamina >= comboStaminaCost)
            {
                int pattern = ChooseComboPattern(distToTarget);
                StartCoroutine(ExecuteComboPattern(pattern));
            }
            else if (distToTarget > engageRange + 1f)
            {
                // Quá xa cho melee → chạy lại gần
                agent.speed = runSpeed;
                agent.SetDestination(currentTarget.position);
            }
        }
    }

    // -------- RETREAT --------
    void HandleRetreat()
    {
        if (!agent.isOnNavMesh) return;

        // Chạy về phía Player
        Vector3 retreatTarget = player.position;

        // Nếu có enemy gần, chạy theo hướng đối diện enemy
        if (currentTarget != null)
        {
            Vector3 awayFromEnemy = (transform.position - currentTarget.position).normalized;
            Vector3 toPlayer = (player.position - transform.position).normalized;
            // Kết hợp hướng trốn enemy + hướng về Player
            retreatTarget = transform.position + (awayFromEnemy + toPlayer).normalized * 5f;
        }

        if (NavMesh.SamplePosition(retreatTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(player.position);
        }

        agent.speed = runSpeed;

        // Phase 3 đặc biệt: Nếu Player cũng sắp chết -> Kamikaze
        float playerHpPercent = playerHealth != null
            ? playerHealth.CurrentHealth / playerHealth.maxHealth : 1f;
        float allyHpPercent = allyHealth != null
            ? allyHealth.currentHealth / allyHealth.maxHealth : 1f;

        if (playerHpPercent < 0.2f && allyHpPercent < 0.15f && currentStamina >= kameStaminaCost)
        {
            // Kamikaze: bắn Kame x3 damage rồi chết
            FindBestTarget();
            if (currentTarget != null)
            {
                StartCoroutine(KamikazeKame());
            }
        }
    }

    // -------- SUPPORT --------
    void HandleSupport()
    {
        if (!agent.isOnNavMesh) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Chạy đến gần Player
        if (distToPlayer > healRange)
        {
            agent.SetDestination(player.position);
            return;
        }

        // Đủ gần -> Heal
        if (Time.time >= nextHealTime && currentStamina >= healStaminaCost)
        {
            agent.isStopped = true;
            RotateTowards(player.position, 15f);

            AnimTrigger("Heal");

            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount);
                Debug.Log($"💚 Ally heal Player +{healAmount} HP!");
            }

            UseStamina(healStaminaCost);
            nextHealTime = Time.time + healCooldown;

            // Heal xong -> quay lại Follow
            Invoke("ResumeAfterSupport", 1.5f);
        }
    }

    void ResumeAfterSupport()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        if (currentState == AllyState.Support)
            TransitionToState(AllyState.Follow);
    }

    // ========================================================
    //               TAUNT (KÉO AGGRO)
    // ========================================================

    public void TryTaunt()
    {
        if (Time.time < nextTauntTime) return;
        if (isDead || isBusy) return;

        float playerHpPercent = playerHealth != null
            ? playerHealth.CurrentHealth / playerHealth.maxHealth : 1f;

        if (playerHpPercent > playerTauntThreshold) return;

        Debug.Log("📢 Ally TAUNT! Kéo aggro zombie về phía mình!");
        AnimTrigger("Taunt");
        nextTauntTime = Time.time + tauntCooldown;

        // Tìm tất cả zombie gần Player và đổi target về Ally
        Collider[] nearbyEnemies = Physics.OverlapSphere(player.position, scanRange, enemyLayer);
        foreach (Collider enemy in nearbyEnemies)
        {
            // Thử đổi target của ZombieAI
            ZombieAI zombieAI = enemy.GetComponent<ZombieAI>();
            if (zombieAI != null)
            {
                zombieAI.aggressiveMode = true;
                // ZombieAI không có public target field nên ta dựa vào aggressive mode
                // Zombie sẽ tự tìm player gần nhất -> vì ally gần hơn nên sẽ đuổi ally
            }
        }
    }

    // ========================================================
    //         KAME - BẮN NGAY KHI ĐỔI MỤC TIÊU (1 LẦN/ZOMBIE)
    // ========================================================

    IEnumerator FireKameImmediate()
    {
        string targetName = currentTarget != null ? currentTarget.name : "???";
        Debug.Log($"☄️ KAME! Bắn {targetName} → cooldown {kameCooldown}s trước lần tiếp theo");
        isBusy = true;
        UseStamina(kameStaminaCost);
        // Đặt cooldown Kame ngay khi bắt đầu bắn
        nextKameTime = Time.time + kameCooldown;
        // Sau Kame chờ ngắn rồi cho đánh combo ngay
        nextAttackTime = Time.time + 0.5f;

        // Dừng lại, quay mặt về target
        if (agent.isOnNavMesh) agent.isStopped = true;
        if (currentTarget != null)
            RotateTowards(currentTarget.position, 25f);

        // Trigger animation KameKame (Animation Event sẽ gọi Event_KameHa)
        AnimTrigger("DoKame");

        // Chờ animation KameKame chạy xong (chỉnh thời gian cho khớp animation của bạn)
        yield return new WaitForSeconds(2.0f);

        // Xong → tiếp tục chiến đấu
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        isBusy = false;
    }

    // ========================================================
    //          COMBO PATTERNS (CHỈ CẬN CHIẾN, KHÔNG CÓ KAME)
    // ========================================================

    int ChooseComboPattern(float distToTarget)
    {
        // Phase 2+: Combo mạnh hơn
        if (currentPhase >= 2)
        {
            // 30% cơ hội combo mạnh (Rush → Heavy → Normal → Heavy)
            if (Random.value < 0.3f) return 2;
        }

        // Chọn random giữa các combo cận chiến
        // Pattern 0: Rush → Normal → Heavy
        // Pattern 1: Normal → Heavy (tại chỗ)
        // Pattern 2: Rush → Heavy → Normal → Heavy (Phase 2+, aggressive)
        // Pattern 3: Rush → Combo → Dodge back
        int[] patterns = { 0, 1, 3 };
        return patterns[Random.Range(0, patterns.Length)];
    }

    IEnumerator ExecuteComboPattern(int pattern)
    {
        isBusy = true;

        switch (pattern)
        {
            case 0: yield return StartCoroutine(Combo_KameRushHeavy()); break;
            case 1: yield return StartCoroutine(Combo_MeleeKameFinish()); break;
            case 2: yield return StartCoroutine(Combo_TripleKameBurst()); break;
            case 3: yield return StartCoroutine(Combo_RushComboDodge()); break;
        }

        isBusy = false;
    }

    // Pattern 0: Rush → Normal → Heavy (lao vào đánh)
    IEnumerator Combo_KameRushHeavy()
    {
        Debug.Log("⚡ COMBO: Rush → Normal → Heavy!");
        UseStamina(comboStaminaCost);
        nextAttackTime = Time.time + comboCooldown;

        // Bước 1: Rush tới target
        if (currentTarget == null || !agent.isOnNavMesh) { isBusy = false; yield break; }
        agent.isStopped = false;
        agent.speed = runSpeed;

        float rushTimer = 0f;
        while (currentTarget != null
               && Vector3.Distance(transform.position, currentTarget.position) > engageRange - 0.5f
               && rushTimer < 2f)
        {
            Vector3 targetPos = usePrediction ? GetPredictedTargetPosition() : currentTarget.position;
            agent.SetDestination(targetPos);
            rushTimer += Time.deltaTime;
            yield return null;
        }

        // Bước 2: Normal Attack
        if (currentTarget == null) { isBusy = false; yield break; }
        if (agent.isOnNavMesh) agent.isStopped = true;
        RotateTowards(currentTarget.position, 20f);
        AnimTrigger("DoCombo");
        yield return new WaitForSeconds(0.5f);
        ApplyDamage(damageNormal * damageMultiplier);

        // Bước 3: Heavy Attack
        yield return new WaitForSeconds(0.6f);
        ApplyDamage(damageHeavy * damageMultiplier);
        yield return new WaitForSeconds(0.5f);

        if (agent.isOnNavMesh) agent.isStopped = false;
        agent.speed = walkSpeed;
    }

    // Pattern 1: Normal → Heavy (tại chỗ, không rush)
    IEnumerator Combo_MeleeKameFinish()
    {
        Debug.Log("⚡ COMBO: Normal → Heavy!");
        UseStamina(comboStaminaCost);
        nextAttackTime = Time.time + comboCooldown;

        if (agent.isOnNavMesh) agent.isStopped = true;
        RotateTowards(currentTarget != null ? currentTarget.position : transform.position + transform.forward, 20f);

        // N_Attack → Heavy_Attack
        AnimTrigger("DoCombo");
        yield return new WaitForSeconds(0.4f);
        ApplyDamage(damageNormal * damageMultiplier);
        yield return new WaitForSeconds(0.6f);
        ApplyDamage(damageHeavy * damageMultiplier);

        yield return new WaitForSeconds(0.5f);
        if (agent.isOnNavMesh) agent.isStopped = false;
    }

    // Pattern 2: Rush → Heavy → Normal → Heavy (Phase 2+, combo mạnh liên tục)
    IEnumerator Combo_TripleKameBurst()
    {
        Debug.Log("⚡ COMBO: Rush → Heavy → Normal → Heavy (Aggressive)!");
        UseStamina(comboStaminaCost * 1.5f);
        nextAttackTime = Time.time + comboCooldown * 1.2f;

        // Bước 1: Rush
        if (currentTarget == null || !agent.isOnNavMesh) { isBusy = false; yield break; }
        agent.isStopped = false;
        agent.speed = runSpeed;

        float rushTimer = 0f;
        while (currentTarget != null
               && Vector3.Distance(transform.position, currentTarget.position) > engageRange - 0.5f
               && rushTimer < 1.5f)
        {
            agent.SetDestination(currentTarget.position);
            rushTimer += Time.deltaTime;
            yield return null;
        }

        if (agent.isOnNavMesh) agent.isStopped = true;

        // Bước 2: Heavy Attack
        RotateTowards(currentTarget != null ? currentTarget.position : transform.forward, 25f);
        AnimTrigger("DoCombo");
        yield return new WaitForSeconds(0.4f);
        ApplyDamage(damageHeavy * damageMultiplier);

        // Bước 3: Normal Attack
        yield return new WaitForSeconds(0.5f);
        ApplyDamage(damageNormal * damageMultiplier);

        // Bước 4: Heavy Attack lần 2
        yield return new WaitForSeconds(0.5f);
        ApplyDamage(damageHeavy * damageMultiplier);

        yield return new WaitForSeconds(0.4f);
        if (agent.isOnNavMesh) agent.isStopped = false;
        agent.speed = walkSpeed;
    }

    // Pattern 3: Rush -> Combo -> Dodge back
    IEnumerator Combo_RushComboDodge()
    {
        Debug.Log("⚡ COMBO: Rush → Combo → Dodge!");
        UseStamina(comboStaminaCost);
        nextAttackTime = Time.time + comboCooldown;

        // Bước 1: Rush tới target
        if (currentTarget == null || !agent.isOnNavMesh) { isBusy = false; yield break; }
        agent.speed = runSpeed;
        agent.isStopped = false;

        float rushTimer = 0f;
        while (currentTarget != null
               && Vector3.Distance(transform.position, currentTarget.position) > engageRange - 0.5f
               && rushTimer < 1.5f)
        {
            agent.SetDestination(currentTarget.position);
            rushTimer += Time.deltaTime;
            yield return null;
        }

        // Bước 2: Combo
        if (agent.isOnNavMesh) agent.isStopped = true;
        AnimTrigger("DoCombo");
        yield return new WaitForSeconds(0.4f);
        ApplyDamage(damageNormal * damageMultiplier);
        yield return new WaitForSeconds(0.6f);
        ApplyDamage(damageHeavy * damageMultiplier);

        // Bước 3: Dodge lùi về sau
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(PerformDodge(true));

        agent.speed = walkSpeed;
    }

    // Kamikaze: Kame x3 rồi tự hủy
    IEnumerator KamikazeKame()
    {
        Debug.Log("💀 KAMIKAZE KAME! x3 DAMAGE!");
        isBusy = true;

        if (agent.isOnNavMesh) agent.isStopped = true;

        float originalMultiplier = damageMultiplier;
        damageMultiplier = 3f;

        AnimTrigger("DoKame");
        yield return new WaitForSeconds(0.5f);
        // Event_KameHa gọi từ Animation Event

        damageMultiplier = originalMultiplier;

        yield return new WaitForSeconds(1f);

        // Tự hủy
        if (allyHealth != null) allyHealth.TakeDamage(9999f);
    }

    // ========================================================
    //                  COUNTER ATTACK
    // ========================================================

    IEnumerator CounterAttack()
    {
        Debug.Log("⚡ COUNTER ATTACK!");
        isBusy = true;
        canCounter = false;

        if (agent.isOnNavMesh) agent.isStopped = true;

        // Đánh counter với bonus damage
        float counterDmg = damageHeavy * damageMultiplier * counterDamageMultiplier;

        AnimTrigger("DoCombo");
        yield return new WaitForSeconds(0.3f);

        if (currentTarget != null)
        {
            ZombieHealth enemyHealth = currentTarget.GetComponent<ZombieHealth>();
            if (enemyHealth == null) enemyHealth = currentTarget.GetComponentInParent<ZombieHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(counterDmg);
                Debug.Log($"💥 Counter! Gây {counterDmg} damage!");
            }
        }

        yield return new WaitForSeconds(0.5f);
        nextAttackTime = Time.time + comboCooldown * 0.5f; // Counter hồi nhanh hơn
        if (agent.isOnNavMesh) agent.isStopped = false;
        isBusy = false;
    }

    // ========================================================
    //                TACTICAL MOVEMENT
    // ========================================================

    void PerformStrafe()
    {
        if (currentTarget == null || !agent.isOnNavMesh) return;

        // Tính vị trí strafe trên vòng cung quanh target
        strafeAngle += strafeAngularSpeed * strafeDirection * Time.deltaTime;

        Vector3 offset = new Vector3(
            Mathf.Sin(strafeAngle * Mathf.Deg2Rad) * strafeRadius,
            0f,
            Mathf.Cos(strafeAngle * Mathf.Deg2Rad) * strafeRadius
        );

        Vector3 strafePos = currentTarget.position + offset;

        if (NavMesh.SamplePosition(strafePos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.speed = walkSpeed;
            agent.SetDestination(hit.position);
        }

        // Luôn nhìn về target
        RotateTowards(currentTarget.position, 10f);

        // Đổi hướng strafe ngẫu nhiên
        if (Random.value < 0.005f) // ~0.5% mỗi frame
        {
            strafeDirection *= -1;
        }
    }

    IEnumerator PerformDodge(bool forceBackward = false)
    {
        if (!agent.isOnNavMesh) yield break;

        AnimTrigger("Dodge");

        Vector3 dodgeDir;
        if (forceBackward && currentTarget != null)
        {
            // Lùi về sau (đối diện target)
            dodgeDir = (transform.position - currentTarget.position).normalized;
        }
        else
        {
            // Dodge sang bên
            dodgeDir = Random.value > 0.5f ? transform.right : -transform.right;
        }

        Vector3 dodgeTarget = transform.position + dodgeDir * dodgeDistance;

        if (NavMesh.SamplePosition(dodgeTarget, out NavMeshHit hit, dodgeDistance, NavMesh.AllAreas))
        {
            agent.speed = runSpeed * 1.5f;
            agent.SetDestination(hit.position);
        }

        nextDodgeTime = Time.time + dodgeCooldown;
        yield return new WaitForSeconds(0.4f);

        agent.speed = walkSpeed;
    }

    // ========================================================
    //     CALLBACK TỪ AllyHealth (KHI BỊ ĐÁNH)
    // ========================================================

    public void OnDamageReceived(float damage)
    {
        if (isDead || isBusy) return;

        // Trigger flinch animation
        AnimTrigger("TakeDamage");

        // Thử dodge
        if (Time.time >= nextDodgeTime && Random.value < dodgeChance)
        {
            StartCoroutine(PerformDodge());
            canCounter = true;
            lastDodgeTime = Time.time;

            // Counter window: có thể counter trong 1 giây sau dodge
            Invoke("ExpireCounter", counterWindow);
        }

        // Nếu Player HP thấp -> taunt
        TryTaunt();
    }

    void ExpireCounter()
    {
        canCounter = false;
    }

    // Khi AllyHealth phát hiện chuyển phase
    public void OnPhaseChange(int newPhase)
    {
        if (newPhase == currentPhase) return;
        currentPhase = newPhase;

        switch (newPhase)
        {
            case 2: // Empowered
                damageMultiplier = 1.5f;
                staminaRegenRate *= 1.5f;
                comboCooldown *= 0.7f;
                AnimBool("IsEmpowered", true);
                Debug.Log("🔥 Ally EMPOWERED! Damage x1.5!");
                break;

            case 3: // Desperate
                // Ưu tiên retreat
                if (currentState == AllyState.Combat)
                    TransitionToState(AllyState.Retreat);
                Debug.Log("💀 Ally DESPERATE! Retreating...");
                break;
        }
    }

    // Khi chết
    public void OnDeath()
    {
        TransitionToState(AllyState.Dead);
    }

    // ========================================================
    //               TARGET FINDING & PRIORITY
    // ========================================================

    // Scan enemy trong phạm vi - dùng enemyLayer trước, fallback sang component check nếu không thấy
    Collider[] ScanEnemies(float range)
    {
        Collider[] result = Physics.OverlapSphere(transform.position, range, enemyLayer);
        if (result.Length > 0) return result;

        // Fallback: quét tất cả và lọc theo component zombie
        Collider[] all = Physics.OverlapSphere(transform.position, range);
        var list = new System.Collections.Generic.List<Collider>();
        foreach (var col in all)
        {
            if (col.gameObject == gameObject) continue; // skip self
            if (col.GetComponent<ZombieAI>() != null
             || col.GetComponent<ZombieHealth>() != null
             || col.GetComponent<RangedZombieAI>() != null
             || col.GetComponent<BossAI>() != null
             || col.GetComponentInParent<ZombieHealth>() != null)
                list.Add(col);
        }
        return list.ToArray();
    }

    bool IsEnemyCollider(Collider col)
    {
        if (col.gameObject == gameObject) return false;
        return ((enemyLayer.value & (1 << col.gameObject.layer)) != 0)
            || col.GetComponent<ZombieAI>() != null
            || col.GetComponent<ZombieHealth>() != null
            || col.GetComponent<RangedZombieAI>() != null
            || col.GetComponent<BossAI>() != null
            || col.GetComponentInParent<ZombieHealth>() != null;
    }

    void FindBestTarget()
    {
        Collider[] enemies = ScanEnemies(scanRange);
        if (enemies.Length == 0) { currentTarget = null; return; }

        Transform bestTarget = null;
        int bestPriority = -1;
        float bestDistance = Mathf.Infinity;

        foreach (Collider enemy in enemies)
        {
            if (!CheckLineOfSight(enemy.transform)) continue;

            // Kiểm tra còn sống
            ZombieHealth zh = enemy.GetComponent<ZombieHealth>();
            if (zh == null) zh = enemy.GetComponentInParent<ZombieHealth>();
            if (zh != null && zh.currentHealth <= 0) continue;

            int priority = GetTargetPriority(enemy);
            float dist = Vector3.Distance(transform.position, enemy.transform.position);

            // Ưu tiên cao hơn hoặc cùng priority nhưng gần hơn
            if (priority > bestPriority || (priority == bestPriority && dist < bestDistance))
            {
                bestPriority = priority;
                bestDistance = dist;
                bestTarget = enemy.transform;
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            lastTargetPosition = bestTarget.position;
        }
    }

    int GetTargetPriority(Collider enemy)
    {
        // Boss: cao nhất
        if (enemy.CompareTag("Boss")) return 100;
        if (enemy.GetComponent<BossAI>() != null || enemy.GetComponent<U_BossAI>() != null) return 100;

        // Zombie tự nổ: rất nguy hiểm
        if (enemy.GetComponent<ExplodingZombieAI>() != null) return 80;

        // Zombie bắn xa: nguy hiểm
        if (enemy.GetComponent<RangedZombieAI>() != null) return 60;

        // Zombie thường
        return 10;
    }

    bool ValidateTarget(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > scanRange * 1.5f) return false;

        ZombieHealth zh = target.GetComponent<ZombieHealth>();
        if (zh == null) zh = target.GetComponentInParent<ZombieHealth>();
        if (zh != null && zh.currentHealth <= 0) return false;

        return true;
    }

    bool CheckLineOfSight(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);
        if (Physics.Raycast(transform.position + Vector3.up, direction, distance, obstacleLayer))
        {
            return false;
        }
        return true;
    }

    // ========================================================
    //                   PREDICTION
    // ========================================================

    void UpdateTargetTracking()
    {
        if (!usePrediction || currentTarget == null) return;
        if (Time.time - lastTrackingUpdate < trackingUpdateRate) return;

        targetVelocity = (currentTarget.position - lastTargetPosition) / trackingUpdateRate;
        lastTargetPosition = currentTarget.position;
        lastTrackingUpdate = Time.time;
    }

    Vector3 GetPredictedTargetPosition()
    {
        if (!usePrediction || currentTarget == null)
            return currentTarget != null ? currentTarget.position : transform.position;
        return currentTarget.position + (targetVelocity * predictionTime);
    }

    // ========================================================
    //                    STAMINA
    // ========================================================

    void UpdateStamina()
    {
        if (currentStamina >= maxStamina) return;
        if (Time.time - lastStaminaUseTime < staminaRegenDelay) return;

        currentStamina = Mathf.Clamp(currentStamina + staminaRegenRate * Time.deltaTime, 0f, maxStamina);
    }

    void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        lastStaminaUseTime = Time.time;
    }

    // ========================================================
    //                  DAMAGE HELPERS
    // ========================================================

    void ApplyDamage(float dmgAmount)
    {
        if (currentTarget == null) return;

        ZombieHealth enemyHealth = currentTarget.GetComponent<ZombieHealth>();
        if (enemyHealth == null) enemyHealth = currentTarget.GetComponentInParent<ZombieHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(dmgAmount);
        }
    }

    // ========================================================
    //              ANIMATION EVENT: KAME
    // ========================================================

    public void Event_KameHa()
    {
        if (currentTarget == null) return;

        Vector3 spawnPosition = transform.position + transform.forward * 0.5f + Vector3.up * 1.2f;
        if (kameFirePoint != null) spawnPosition = kameFirePoint.position;

        // Tính hướng bắn (chỉ X, Z)
        Vector3 myPos = spawnPosition; myPos.y = 0f;
        Vector3 targetPos = usePrediction ? GetPredictedTargetPosition() : currentTarget.position;
        targetPos.y = 0f;

        Vector3 shootDirection = (targetPos - myPos).normalized;
        if (shootDirection == Vector3.zero) return;

        Quaternion spawnRotation = Quaternion.LookRotation(shootDirection);

        if (kameBeamPrefab != null)
        {
            if (currentKameBeam != null) Destroy(currentKameBeam);

            currentKameBeam = Instantiate(kameBeamPrefab, spawnPosition, spawnRotation);

            KameProjectile kameProjectile = currentKameBeam.GetComponent<KameProjectile>();
            if (kameProjectile != null)
            {
                kameProjectile.damage = damageKame * damageMultiplier;
                kameProjectile.enemyLayer = enemyLayer;
                kameProjectile.obstacleLayer = obstacleLayer;
                kameProjectile.Initialize(currentTarget.position);
            }
            else
            {
                if (kameBeamDuration > 0f) Destroy(currentKameBeam, kameBeamDuration);
            }
        }
    }

    // ========================================================
    //                     UTILITY
    // ========================================================

    void RotateTowards(Vector3 position, float speed)
    {
        Vector3 dir = (position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        bool isMoving = agent.velocity.magnitude > 0.1f;
        AnimBool("IsWalk", isMoving);
        AnimFloat("Speed", agent.velocity.magnitude);
    }

    bool HasEnemyInRange(float range)
    {
        return ScanEnemies(range).Length > 0;
    }

    int CountEnemiesInRange(float range)
    {
        return ScanEnemies(range).Length;
    }

    void OnDisable()
    {
        CancelInvoke();
        StopAllCoroutines();
    }

    // ========================================================
    //                     GIZMOS (DEBUG)
    // ========================================================

    void OnDrawGizmosSelected()
    {
        // Vòng quét enemy
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scanRange);

        // Vòng engage (cận chiến)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageRange);

        // Vòng disengage (Kame)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, disengageRange);

        // Vòng bị vây
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, surroundCheckRadius);

        // Vẽ đường tới target
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up, currentTarget.position + Vector3.up);
        }
    }
}
