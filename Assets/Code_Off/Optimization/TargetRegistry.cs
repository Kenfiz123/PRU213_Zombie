using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Registry trung tâm cho tất cả target (Player, Ally).
/// Zombie tra cứu ở đây thay vì gọi FindGameObjectsWithTag mỗi zombie.
/// Tự động: các script khác KHÔNG cần gọi gì, chỉ cần gắn lên 1 Empty GameObject.
/// </summary>
public class TargetRegistry : MonoBehaviour
{
    public static TargetRegistry Instance { get; private set; }

    // Danh sách target còn sống (Player + Ally)
    private static List<Transform> targets = new List<Transform>();

    // Cache cho WaveManager: đếm enemy
    private static List<GameObject> enemies = new List<GameObject>();
    private static int enemyCount = 0;
    private static float lastEnemyCheckTime = 0f;

    [Header("--- CẤU HÌNH ---")]
    [Tooltip("Tần suất cập nhật danh sách target (giây)")]
    public float targetUpdateInterval = 0.5f;
    [Tooltip("Tần suất đếm enemy cho WaveManager (giây)")]
    public float enemyCheckInterval = 1f;

    private float targetTimer = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        targets.Clear();
        enemies.Clear();
        enemyCount = 0;
    }

    void Update()
    {
        targetTimer += Time.deltaTime;
        if (targetTimer >= targetUpdateInterval)
        {
            targetTimer = 0f;
            RefreshTargets();
        }

        // Đếm enemy cho WaveManager
        if (Time.time - lastEnemyCheckTime >= enemyCheckInterval)
        {
            lastEnemyCheckTime = Time.time;
            RefreshEnemies();
        }
    }

    void RefreshTargets()
    {
        targets.Clear();
        GameObject[] found = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && found[i].activeInHierarchy)
                targets.Add(found[i].transform);
        }
    }

    void RefreshEnemies()
    {
        enemies.Clear();
        GameObject[] found = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && found[i].activeInHierarchy)
                enemies.Add(found[i]);
        }
        enemyCount = enemies.Count;
    }

    // ═══════════════════════════════════════════════════════════════
    // API CHO ZOMBIE GỌI
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tìm target gần nhất. Dùng thay cho FindGameObjectsWithTag trong zombie.
    /// </summary>
    public static Transform GetClosestTarget(Vector3 from)
    {
        Transform best = null;
        float bestDist = Mathf.Infinity;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] == null) { targets.RemoveAt(i); continue; }

            float d = (targets[i].position - from).sqrMagnitude; // sqrMagnitude nhanh hơn Distance
            if (d < bestDist) { bestDist = d; best = targets[i]; }
        }
        return best;
    }

    /// <summary>Số enemy hiện tại (cached, không gọi Find mỗi frame).</summary>
    public static int EnemyCount => enemyCount;

    /// <summary>Danh sách enemy (cached).</summary>
    public static List<GameObject> Enemies => enemies;
}
