using UnityEngine;

public class HazardZone : MonoBehaviour
{
    [Header("--- CẤU HÌNH ---")]
    [Tooltip("Sát thương gây ra mỗi lần (mỗi nhịp)")]
    public float damageAmount = 15f;

    [Tooltip("Thời gian tồn tại của vùng này trước khi biến mất (giây)")]
    public float duration = 10f;

    [Tooltip("Tần suất gây sát thương (giây/lần). Ví dụ: 0.5 nghĩa là 1 giây trừ máu 2 lần.")]
    public float tickRate = 0.5f;

    private float nextDamageTime = 0f;

    void Start()
    {
        // Tự động hủy object này sau khoảng thời gian duration
        Destroy(gameObject, duration);
    }

    // Hàm này được Unity gọi liên tục khi có vật thể ĐANG ĐỨNG bên trong Collider
    void OnTriggerStay(Collider other)
    {
        // Kiểm tra xem đã đến lúc gây sát thương tiếp theo chưa
        if (Time.time >= nextDamageTime)
        {
            // Chỉ tác động lên Player
            if (other.CompareTag("Player"))
            {
                PlayerHealth pHealth = other.GetComponent<PlayerHealth>();

                // Nếu tìm thấy máu của Player thì trừ máu
                if (pHealth != null)
                {
                    pHealth.TakeDamage(damageAmount);

                    // Cập nhật thời gian cho lần gây damage tiếp theo
                    nextDamageTime = Time.time + tickRate;

                    Debug.Log($"🔥 Player bị bỏng! Trừ {damageAmount} máu.");
                }
            }
        }
    }
}