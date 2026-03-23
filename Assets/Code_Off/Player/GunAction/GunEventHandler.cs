using UnityEngine;

public class GunEventHandler : MonoBehaviour
{
    [Header("Cài đặt vỏ đạn")]
    [SerializeField] private GameObject casingPrefab; // Prefab vỏ đạn
    [SerializeField] private Transform casingExitPoint; // Vị trí vỏ đạn bay ra
    [SerializeField] private float ejectForce = 2.0f; // Lực bay ra

    // Tên hàm này PHẢI GIỐNG Y HỆT tên trong Animation Event (CasingRelease)
    public void CasingRelease()
    {
        if (casingPrefab != null && casingExitPoint != null)
        {
            // 1. Tạo vỏ đạn tại lỗ thoát
            GameObject casing;
            if (ObjectPool.Instance != null)
                casing = ObjectPool.Instance.Get(casingPrefab, casingExitPoint.position, casingExitPoint.rotation);
            else
                casing = Instantiate(casingPrefab, casingExitPoint.position, casingExitPoint.rotation);

            // 2. Thêm lực đẩy để vỏ đạn bay ra ngoài
            Rigidbody rb = casing.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Vector3 direction = (casingExitPoint.right * 1.5f + casingExitPoint.up).normalized;
                rb.AddForce(direction * ejectForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 10f);
            }

            // 3. Trả vỏ đạn về pool sau 2 giây
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.ReturnToPool(casing, 2.0f);
            else
                Destroy(casing, 2.0f);
        }
    }
}