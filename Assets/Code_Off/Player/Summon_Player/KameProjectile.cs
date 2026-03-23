using UnityEngine;

/// <summary>
/// Script cho viên đạn KameKameHa - Bay thẳng một đường, nhắm vào vị trí zombie tại thời điểm bắn
/// LƯU Ý: Kame bắn thẳng, KHÔNG tự động theo dõi mục tiêu. 
/// Chỉ trúng zombie nếu zombie đang ở trên đường đi của Kame.
/// </summary>
public class KameProjectile : MonoBehaviour
{
    [Header("--- CẤU HÌNH ---")]
    [Tooltip("Tốc độ bay của Kame (m/s)")]
    public float speed = 15f;
    
    [Tooltip("Sát thương gây ra khi trúng địch")]
    public float damage = 100f;
    
    [Tooltip("Thời gian tồn tại tối đa (giây). Nếu quá thời gian này mà chưa trúng gì thì tự hủy")]
    public float maxLifetime = 5f;
    
    [Header("--- LAYER & TAG ---")]
    [Tooltip("Layer của địch (Zombie)")]
    public LayerMask enemyLayer;
    
    [Tooltip("Layer của vật cản (Tường, Đất...)")]
    public LayerMask obstacleLayer;

    [Header("--- KIỂM TRA VA CHẠM ---")]
    [Tooltip("Bán kính kiểm tra va chạm (m). Dùng để phát hiện zombie trên đường đi")]
    public float hitRadius = 0.5f;
    
    [Tooltip("Khoảng cách kiểm tra phía trước (m). Kiểm tra zombie trong phạm vi này")]
    public float checkDistance = 2f;

    private Vector3 moveDirection; // Hướng bay (chỉ tính X, Z - ngang mặt đất)
    private bool hasHit = false; // Đã trúng địch chưa (để tránh trúng nhiều lần)
    private Vector3 initialTargetPosition; // Vị trí mục tiêu ban đầu (để debug)

    /// <summary>
    /// Khởi tạo hướng bay của Kame (chỉ tính X, Z, bỏ qua Y)
    /// QUAN TRỌNG: Kame sẽ bắn thẳng theo hướng này, KHÔNG tự động theo dõi mục tiêu.
    /// Chỉ trúng zombie nếu zombie đang ở trên đường đi của Kame.
    /// </summary>
    /// <param name="targetPosition">Vị trí mục tiêu TẠI THỜI ĐIỂM BẮN (chỉ dùng X, Z để tính hướng)</param>
    public void Initialize(Vector3 targetPosition)
    {
        // Lưu vị trí mục tiêu ban đầu (để debug)
        initialTargetPosition = targetPosition;
        
        // Tính hướng chỉ dựa trên X và Z (bỏ qua chênh lệch độ cao Y)
        Vector3 myPos = transform.position;
        Vector3 targetPos = targetPosition;
        
        // Đặt Y của cả 2 điểm bằng nhau (ngang mặt đất)
        myPos.y = 0f;
        targetPos.y = 0f;
        
        // Tính hướng và normalize
        moveDirection = (targetPos - myPos).normalized;
        
        // Quay projectile về hướng bay (chỉ quay theo trục Y - ngang mặt đất)
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
        else
        {
            Debug.LogWarning("KameProjectile: Không thể tính hướng bay! moveDirection = zero");
        }

        // Tự hủy sau maxLifetime giây nếu chưa trúng gì
        Destroy(gameObject, maxLifetime);
        
        Debug.Log($"☄️ KameProjectile khởi tạo: Hướng = {moveDirection}, Mục tiêu ban đầu = {targetPosition}");
    }

    void Update()
    {
        // Bay thẳng về phía trước (theo hướng đã tính - ngang mặt đất)
        // LƯU Ý: Kame bay thẳng, KHÔNG tự động theo dõi mục tiêu
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);

        // Kiểm tra liên tục xem có zombie nào trên đường đi không (phương pháp bổ sung)
        // Điều này đảm bảo trúng zombie ngay cả khi collider trigger không kích hoạt
        if (!hasHit)
        {
            CheckForEnemiesOnPath();
        }
    }

    /// <summary>
    /// Kiểm tra liên tục xem có zombie nào đang ở trên đường đi của Kame không
    /// Phương pháp này bổ sung cho OnTriggerEnter để đảm bảo trúng zombie trên đường đi
    /// </summary>
    void CheckForEnemiesOnPath()
    {
        // Tính vị trí phía trước để kiểm tra
        Vector3 checkPosition = transform.position + transform.forward * checkDistance;
        
        // Kiểm tra trong phạm vi hình cầu
        Collider[] hitColliders = Physics.OverlapSphere(checkPosition, hitRadius, enemyLayer);
        
        foreach (Collider col in hitColliders)
        {
            // Kiểm tra xem zombie có đang ở trên đường đi không (chỉ tính X, Z)
            Vector3 zombiePos = col.transform.position;
            Vector3 kamePos = transform.position;
            
            // Đặt Y = 0 để so sánh ngang mặt đất
            zombiePos.y = 0f;
            kamePos.y = 0f;
            
            // Tính vector từ Kame đến Zombie
            Vector3 toZombie = (zombiePos - kamePos).normalized;
            
            // Kiểm tra xem zombie có nằm trên đường đi không (dot product gần = 1)
            float dot = Vector3.Dot(transform.forward, toZombie);
            
            // Nếu zombie nằm trên đường đi (dot > 0.7 nghĩa là góc < 45 độ)
            if (dot > 0.7f)
            {
                // Gây damage
                ZombieHealth enemyHealth = col.GetComponent<ZombieHealth>();
                if (enemyHealth == null) enemyHealth = col.GetComponentInParent<ZombieHealth>();
                
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"☄️ KameKameHa trúng {col.name} trên đường đi! Gây {damage} damage.");
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Nếu đã trúng rồi thì bỏ qua (tránh trúng nhiều lần)
        if (hasHit) return;

        // 1. Nếu trúng địch (Zombie) -> Gây damage
        // LƯU Ý: Chỉ trúng nếu zombie đang ở trên đường đi của Kame
        if (IsInLayerMask(other.gameObject.layer, enemyLayer))
        {
            // Kiểm tra xem zombie có đang ở trên đường đi không (chỉ tính X, Z)
            Vector3 zombiePos = other.transform.position;
            Vector3 kamePos = transform.position;
            
            // Đặt Y = 0 để so sánh ngang mặt đất
            zombiePos.y = 0f;
            kamePos.y = 0f;
            
            // Tính vector từ Kame đến Zombie
            Vector3 toZombie = (zombiePos - kamePos).normalized;
            
            // Kiểm tra xem zombie có nằm trên đường đi không (dot product gần = 1)
            float dot = Vector3.Dot(transform.forward, toZombie);
            
            // Nếu zombie nằm trên đường đi (dot > 0.7 nghĩa là góc < 45 độ)
            if (dot > 0.7f)
            {
                ZombieHealth enemyHealth = other.GetComponent<ZombieHealth>();
                if (enemyHealth == null) enemyHealth = other.GetComponentInParent<ZombieHealth>();
                
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"☄️ KameKameHa trúng {other.name} trên đường đi! Gây {damage} damage.");
                    
                    // Hủy projectile sau khi trúng
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                // Zombie không nằm trên đường đi -> Bỏ qua (Kame bay xuyên qua)
                return;
            }
        }

        // 2. Nếu trúng vật cản (Tường, Đất...) -> Hủy đạn
        if (IsInLayerMask(other.gameObject.layer, obstacleLayer))
        {
            Debug.Log($"☄️ KameKameHa va chạm với {other.name}.");
            Destroy(gameObject);
            return;
        }

        // 3. Nếu trúng Player -> Bỏ qua (không làm gì, để đạn bay xuyên qua)
        if (other.CompareTag("Player"))
        {
            return;
        }

        // 4. Trúng bất cứ thứ gì khác -> Hủy đạn (an toàn)
        Destroy(gameObject);
    }

    /// <summary>
    /// Kiểm tra xem layer có nằm trong LayerMask không
    /// </summary>
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    // Vẽ đường bay trong Scene để debug (tùy chọn)
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Vẽ đường bay
            if (moveDirection != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, moveDirection * 20f);
            }
            
            // Vẽ vùng kiểm tra va chạm
            Gizmos.color = Color.yellow;
            Vector3 checkPos = transform.position + transform.forward * checkDistance;
            Gizmos.DrawWireSphere(checkPos, hitRadius);
            
            // Vẽ vị trí mục tiêu ban đầu (nếu có)
            if (initialTargetPosition != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(initialTargetPosition, 0.5f);
                Gizmos.DrawLine(transform.position, initialTargetPosition);
            }
        }
    }
}
