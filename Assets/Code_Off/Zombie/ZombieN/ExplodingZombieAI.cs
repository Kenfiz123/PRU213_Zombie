using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ExplodingZombieAI : MonoBehaviour
{
    private enum State { Chase, Fuse, Exploded }

    [Header("--- CÀI ĐẶT CHUNG ---")]
    public Transform player;
    [Tooltip("Nếu tích: Zombie lao thẳng vào Player ngay khi spawn")]
    public bool aggressiveMode = false;

    [Header("--- TỐC ĐỘ ---")]
    public float moveSpeed = 4.5f;
    [Tooltip("Tốc độ tối đa khi gần target (tăng tốc dần)")]
    public float rushSpeed = 7f;
    [Tooltip("Khoảng cách bắt đầu tăng tốc")]
    public float rushDistance = 8f;

    [Header("--- KHOẢNG CÁCH NỔ ---")]
    [Tooltip("Khoảng cách kích hoạt ngòi nổ")]
    public float fuseDistance = 2f;
    [Tooltip("Thời gian ngòi nổ (giây)")]
    public float fuseTime = 1.2f;

    [Header("--- THÔNG SỐ NỔ ---")]
    public float explosionRadius = 5f;
    [Tooltip("Damage tối đa (ngay tâm nổ)")]
    public float maxDamage = 60f;
    [Tooltip("Damage tối thiểu (rìa vụ nổ)")]
    public float minDamage = 15f;
    [Tooltip("Gây damage cho zombie khác trong tầm nổ")]
    public bool friendlyFire = true;
    public GameObject explosionVFX;
    public AudioClip fuseSound;   // Tiếng xì khói
    public AudioClip explosionSound;

    [Header("--- CẢNH BÁO TRƯỚC KHI NỔ ---")]
    [Tooltip("Phồng to lên trước khi nổ")]
    public float swellScale = 1.4f;
    [Tooltip("Nhấp nháy đỏ")]
    public Color warningColor = Color.red;

    // Private
    private NavMeshAgent agent;
    private Animator animator;
    private State currentState = State.Chase;
    private bool hasExploded = false;
    private float findTargetTimer = 0f;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private Renderer[] renderers;
    private Color[] originalColors;
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.maxDistance = 20f;

        originalScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();

        // Lưu màu gốc
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_BaseColor"))
                originalColors[i] = renderers[i].material.GetColor("_BaseColor");
            else if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.GetColor("_Color");
            else
                originalColors[i] = Color.white;
        }

        propBlock = new MaterialPropertyBlock();

        FindClosestTarget();

        agent.speed = moveSpeed;
        agent.stoppingDistance = fuseDistance * 0.8f;
        agent.angularSpeed = 300f;
        agent.acceleration = 12f;

        if (aggressiveMode && player != null)
            agent.SetDestination(player.position);
    }

    void Update()
    {
        if (hasExploded || currentState == State.Exploded) return;

        // Tìm target gần nhất mỗi 1 giây
        findTargetTimer += Time.deltaTime;
        if (findTargetTimer >= 1f)
        {
            findTargetTimer = 0f;
            FindClosestTarget();
        }

        if (player == null) return;

        if (currentState == State.Chase)
            HandleChase();
    }

    void HandleChase()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // === TỐC ĐỘ TĂNG DẦN KHI GẦN ===
        if (distance < rushDistance)
        {
            // Lerp từ moveSpeed → rushSpeed khi càng gần
            float t = 1f - Mathf.Clamp01(distance / rushDistance);
            agent.speed = Mathf.Lerp(moveSpeed, rushSpeed, t);
        }
        else
        {
            agent.speed = moveSpeed;
        }

        agent.SetDestination(player.position);
        animator.SetFloat("Speed", agent.velocity.magnitude);

        // === KÍCH HOẠT NGÒI NỔ ===
        if (distance <= fuseDistance)
        {
            currentState = State.Fuse;
            StartCoroutine(FuseAndExplode());
        }
    }

    IEnumerator FuseAndExplode()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Trigger animation nổ
        if (animator != null)
            animator.SetTrigger("Explode");

        // Tiếng xì khói
        if (fuseSound != null)
            audioSource.PlayOneShot(fuseSound);

        // === HIỆU ỨNG CẢNH BÁO: phồng to + nhấp nháy đỏ ===
        float elapsed = 0f;
        float blinkRate = 4f; // Nhấp nháy 4 lần/giây, tăng dần

        while (elapsed < fuseTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fuseTime; // 0→1

            // Phồng to dần
            float scale = Mathf.Lerp(1f, swellScale, progress);
            transform.localScale = originalScale * scale;

            // Nhấp nháy nhanh dần (4→12 lần/giây)
            blinkRate = Mathf.Lerp(4f, 12f, progress);
            bool isRed = Mathf.Sin(elapsed * blinkRate * Mathf.PI * 2f) > 0f;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = isRed ? warningColor : originalColors[i];
                if (renderers[i].material.HasProperty("_BaseColor"))
                    renderers[i].material.SetColor("_BaseColor", c);
                else if (renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.SetColor("_Color", c);
            }

            yield return null;
        }

        Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        currentState = State.Exploded;

        // GỠ TAG ENEMY để WaveManager không đếm xác
        gameObject.tag = "Untagged";

        // 1. VFX nổ (tự hủy sau 3s)
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // 2. Tiếng nổ
        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);

        // 3. Gây sát thương theo khoảng cách
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        System.Collections.Generic.HashSet<GameObject> damaged =
            new System.Collections.Generic.HashSet<GameObject>();

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;
            GameObject root = hit.gameObject;
            if (root == gameObject) continue; // Không tự gây damage cho mình
            if (damaged.Contains(root)) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            // Damage giảm dần theo khoảng cách (gần = max, xa = min)
            float t = Mathf.Clamp01(dist / explosionRadius);
            float dmg = Mathf.Lerp(maxDamage, minDamage, t);

            // --- Player ---
            PlayerHealth ph = root.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(dmg);
                damaged.Add(root);

                // Đẩy lùi
                Rigidbody rb = root.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 force = (hit.transform.position - transform.position).normalized * 600f + Vector3.up * 250f;
                    rb.AddForce(force, ForceMode.Impulse);
                }
                continue;
            }

            // --- Ally ---
            AllyHealth ah = root.GetComponent<AllyHealth>();
            if (ah != null)
            {
                ah.TakeDamage(dmg);
                damaged.Add(root);
                continue;
            }

            // --- Zombie khác (friendly fire) ---
            if (friendlyFire && root.CompareTag("Enemy"))
            {
                ZombieHealth zh = root.GetComponent<ZombieHealth>();
                if (zh != null)
                {
                    zh.TakeDamage(dmg);
                    damaged.Add(root);
                }
            }
        }

        // 4. Camera shake
        CameraShake.ShakeExplosion();

        // 5. Ẩn model + hủy
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = false;
        }
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (agent != null) agent.enabled = false;

        Destroy(gameObject, 0.1f);
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

    // Vẽ gizmo debug
    void OnDrawGizmosSelected()
    {
        // Vòng nổ
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Khoảng cách kích hoạt ngòi
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fuseDistance);

        // Khoảng cách bắt đầu tăng tốc
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rushDistance);
    }
}
