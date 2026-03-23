using UnityEngine;

public class MagicZone : MonoBehaviour
{
    [Header("--- CẤU HÌNH MƯA ---")]
    public GameObject magicPrefab; // Kéo Prefab cục phép (BƯỚC 1) vào đây
    public float radius = 5f;      // Bán kính vùng mưa (phải khớp với vòng tròn đỏ)
    public float spawnHeight = 10f; // Độ cao thả rơi (mét)
    public float spawnRate = 0.2f; // Tốc độ rơi: 0.2 giây rớt 1 cục
    public float duration = 5f;    // Thời gian tồn tại của vùng phép

    private float nextSpawnTime = 0f;

    void Start()
    {
        // Tự hủy cái vòng tròn đỏ sau khi hết thời gian
        Destroy(gameObject, duration);
    }

    void Update()
    {
        // Logic sinh sản liên tục
        if (Time.time >= nextSpawnTime)
        {
            SpawnMagic();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnMagic()
    {
        if (magicPrefab == null) return;

        // 1. Chọn một điểm ngẫu nhiên trong hình tròn (X, Z)
        Vector2 randomPoint = Random.insideUnitCircle * radius;

        // 2. Tính toán vị trí thả (Cộng thêm vào vị trí tâm của MagicZone)
        Vector3 spawnPos = transform.position;
        spawnPos.x += randomPoint.x;
        spawnPos.z += randomPoint.y; // Lưu ý: Random.insideUnitCircle trả về x,y nên ta gán y vào z
        spawnPos.y += spawnHeight;   // Đưa lên cao

        // 3. Tạo cục phép
        Instantiate(magicPrefab, spawnPos, Quaternion.identity);
    }

    // Vẽ vòng tròn trong Scene để dễ căn chỉnh (Debug)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}