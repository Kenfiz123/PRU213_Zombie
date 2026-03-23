using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tối ưu zombie theo khoảng cách từ camera.
/// - Gần: Full AI + Animator + NavMesh update thường xuyên
/// - Trung bình: Giảm Animator quality, NavMesh update thưa hơn
/// - Xa: Tắt Animator, NavMesh update rất thưa
/// Gắn lên PREFAB zombie (cạnh ZombieAI).
/// </summary>
public class ZombieLOD : MonoBehaviour
{
    [Header("--- KHOẢNG CÁCH LOD ---")]
    [Tooltip("Dưới khoảng cách này = Full quality")]
    public float nearDistance = 20f;
    [Tooltip("Dưới khoảng cách này = Medium quality")]
    public float mediumDistance = 40f;
    // Xa hơn mediumDistance = Low quality

    private Animator animator;
    private NavMeshAgent agent;
    private Transform cam;
    private float checkTimer;
    private int currentLOD = -1; // -1 = chưa set

    // Stagger: mỗi zombie check ở frame khác nhau để không check cùng lúc
    private float checkInterval;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (Camera.main != null)
            cam = Camera.main.transform;

        // Stagger interval: 0.3-0.7s ngẫu nhiên để zombie không check cùng frame
        checkInterval = Random.Range(0.3f, 0.7f);
        checkTimer = Random.Range(0f, checkInterval);
    }

    void Update()
    {
        if (cam == null) return;

        checkTimer += Time.deltaTime;
        if (checkTimer < checkInterval) return;
        checkTimer = 0f;

        float distSqr = (transform.position - cam.position).sqrMagnitude;

        if (distSqr < nearDistance * nearDistance)
        {
            SetLOD(0); // NEAR - Full quality
        }
        else if (distSqr < mediumDistance * mediumDistance)
        {
            SetLOD(1); // MEDIUM
        }
        else
        {
            SetLOD(2); // FAR
        }
    }

    void SetLOD(int lod)
    {
        if (lod == currentLOD) return;
        currentLOD = lod;

        switch (lod)
        {
            case 0: // NEAR - Full quality
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.updateRotation = true;
                }
                break;

            case 1: // MEDIUM - Giảm quality
                if (animator != null)
                {
                    animator.enabled = true;
                    // Chỉ animate khi renderer visible
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                }
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.updateRotation = true;
                }
                break;

            case 2: // FAR - Tối thiểu
                if (animator != null)
                {
                    // Tắt hoàn toàn animation khi không nhìn thấy
                    animator.cullingMode = AnimatorCullingMode.CullCompletely;
                }
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.updateRotation = false;
                }
                break;
        }
    }

    void OnDisable()
    {
        currentLOD = -1;
    }
}
