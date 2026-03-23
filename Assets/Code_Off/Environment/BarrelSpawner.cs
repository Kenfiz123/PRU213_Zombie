using UnityEngine;
using UnityEngine.AI;

public class BarrelSpawner : MonoBehaviour
{
    [Header("=== BARREL SPAWNER ===")]
    public GameObject barrelPrefab;
    public int barrelCount = 10;

    [Header("=== VÙNG SPAWN ===")]
    [Tooltip("Kích thước vùng spawn (X, Z). Barrel spawn ngẫu nhiên trong vùng này")]
    public Vector2 spawnArea = new Vector2(80f, 80f);
    [Tooltip("Khoảng cách tối thiểu giữa 2 barrel")]
    public float minDistance = 8f;
    [Tooltip("Khoảng cách tối thiểu từ Player spawn (tránh nổ ngay đầu game)")]
    public float minDistFromPlayer = 15f;

    void Start()
    {
        if (barrelPrefab == null)
        {
            Debug.LogWarning("[BarrelSpawner] Chưa gắn barrelPrefab!");
            return;
        }

        SpawnBarrels();
    }

    void SpawnBarrels()
    {
        Vector3 center = transform.position;
        Vector3 playerPos = Vector3.zero;

        // Tìm vị trí Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerPos = player.transform.position;

        // Lưu các vị trí đã spawn
        Vector3[] spawnedPositions = new Vector3[barrelCount];
        int spawned = 0;
        int maxAttempts = barrelCount * 20;
        int attempts = 0;

        while (spawned < barrelCount && attempts < maxAttempts)
        {
            attempts++;

            // Random vị trí trong vùng spawn
            float x = center.x + Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f);
            float z = center.z + Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f);

            // Raycast xuống tìm mặt đất
            Vector3 rayOrigin = new Vector3(x, center.y + 50f, z);
            RaycastHit hit;
            if (!Physics.Raycast(rayOrigin, Vector3.down, out hit, 100f))
                continue;

            // Nâng lên trên mặt đất để barrel không chìm
            Vector3 spawnPos = hit.point + Vector3.up * 0.5f;

            // Check khoảng cách từ Player
            if (Vector3.Distance(spawnPos, playerPos) < minDistFromPlayer)
                continue;

            // Check khoảng cách từ barrel khác
            bool tooClose = false;
            for (int i = 0; i < spawned; i++)
            {
                if (Vector3.Distance(spawnPos, spawnedPositions[i]) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Spawn barrel
            GameObject barrel = Instantiate(barrelPrefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            barrel.name = $"Barrel_{spawned + 1}";
            spawnedPositions[spawned] = spawnPos;
            spawned++;
        }

        Debug.Log($"[BarrelSpawner] Đã spawn {spawned}/{barrelCount} barrel");
    }

    // Hiện vùng spawn trong Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Vector3 size = new Vector3(spawnArea.x, 1f, spawnArea.y);
        Gizmos.DrawWireCube(transform.position, size);
    }
}
