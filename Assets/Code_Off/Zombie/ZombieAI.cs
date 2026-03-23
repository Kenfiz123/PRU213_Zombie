using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    private enum ZombieState { Patrol, Chase, Attack }

    [Header("--- CÀI ĐẶT CHUNG ---")]
    [SerializeField] private string playerTag = "Player";

    // [MỚI] Tích vào cái này thì Zombie sẽ biết vị trí Player ngay lập tức
    [Tooltip("Nếu tích: Zombie sẽ luôn biết vị trí Player và đuổi theo ngay khi sinh ra.")]
    public bool aggressiveMode = false;

    [Header("--- TỐC ĐỘ ---")]
    public float patrolSpeed = 1.0f;
    public float chaseSpeed = 3.5f;

    [Header("--- GIÁC QUAN ---")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("--- TẤN CÔNG ---")]
    [SerializeField] private float attackDistance = 1.5f;
    public float attackCooldown = 2.0f;
    public float damage = 10f;

    private ZombieState currentState;
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    private float patrolTimer;
    private float lastAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // aggressiveMode được set bởi WaveManager sau Instantiate
        // Không ép về false ở đây nữa
    }

    private float retargetTimer = 0f;
    private float retargetInterval = 2f;

    private void Start()
    {
        FindClosestTarget();

        agent.stoppingDistance = attackDistance - 0.2f;
        agent.updateRotation = true;

        if (aggressiveMode)
        {
            TransitionToState(ZombieState.Chase);
        }
        else
        {
            TransitionToState(ZombieState.Patrol);
        }
    }

    private void Update()
    {
        // Tìm lại target gần nhất mỗi vài giây
        retargetTimer += Time.deltaTime;
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            FindClosestTarget();
        }

        if (player == null) return;
        UpdateAnimation();

        switch (currentState)
        {
            case ZombieState.Patrol: HandlePatrol(); break;
            case ZombieState.Chase: HandleChase(); break;
            case ZombieState.Attack: HandleAttack(); break;
        }
    }

    void HandlePatrol()
    {
        if (!agent.isOnNavMesh) return;
        if (IsPlayerInSight()) { TransitionToState(ZombieState.Chase); return; }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime)
            {
                SetNewPatrolDestination();
                patrolTimer = 0f;
            }
        }
    }

    void HandleChase()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Đủ gần -> Đánh
        if (distanceToPlayer <= attackDistance)
        {
            TransitionToState(ZombieState.Attack);
            return;
        }

        // [LOGIC MỚI]
        // Nếu KHÔNG hung hăng (aggressiveMode = false) thì mới cho phép bỏ cuộc
        if (!aggressiveMode && distanceToPlayer > viewDistance * 1.5f)
        {
            TransitionToState(ZombieState.Patrol);
            return;
        }

        // Nếu aggressiveMode = true -> Luôn luôn đuổi theo dù Player chạy xa đến đâu
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void HandleAttack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > attackDistance) { TransitionToState(ZombieState.Chase); return; }

        agent.isStopped = true;
        RotateTowardsPlayer();

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            animator.SetTrigger("Attack");
            lastAttackTime = Time.time;
        }
    }

    void TransitionToState(ZombieState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case ZombieState.Patrol:
                agent.speed = patrolSpeed;
                agent.isStopped = false;
                SetNewPatrolDestination();
                break;
            case ZombieState.Chase:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                break;
            case ZombieState.Attack:
                agent.isStopped = true;
                break;
        }
    }

    /// <summary>
    /// Tìm target gần nhất - dùng TargetRegistry (cached) thay vì FindGameObjectsWithTag
    /// </summary>
    void FindClosestTarget()
    {
        // Ưu tiên dùng TargetRegistry (nhanh hơn nhiều)
        if (TargetRegistry.Instance != null)
        {
            Transform closest = TargetRegistry.GetClosestTarget(transform.position);
            if (closest != null) player = closest;
            return;
        }

        // Fallback nếu chưa có TargetRegistry
        GameObject[] targets = GameObject.FindGameObjectsWithTag(playerTag);
        float closestDist = Mathf.Infinity;
        Transform closest2 = null;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;
            float dist = (transform.position - targets[i].transform.position).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest2 = targets[i].transform;
            }
        }

        if (closest2 != null)
            player = closest2;
    }

    bool IsPlayerInSight()
    {
        if (player == null) return false;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > viewDistance) return false;
        Vector3 dir = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dir) > viewAngle / 2) return false;
        if (Physics.Raycast(transform.position + Vector3.up, dir, out RaycastHit hit, viewDistance, obstacleMask)) return false;
        return true;
    }

    void SetNewPatrolDestination()
    {
        Vector3 rnd = Random.insideUnitSphere * patrolRadius;
        rnd += transform.position;
        if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas)) agent.SetDestination(hit.position);
    }

    void UpdateAnimation() { animator.SetFloat("Speed", agent.velocity.magnitude); }

    void RotateTowardsPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized; dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    public void ZombieHitEvent()
    {
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackDistance + 1.0f)
        {
            // Thử gây damage cho Player
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                return;
            }

            // Thử gây damage cho Ally
            AllyHealth ah = player.GetComponent<AllyHealth>();
            if (ah != null)
            {
                ah.TakeDamage(damage);
            }
        }
    }
}