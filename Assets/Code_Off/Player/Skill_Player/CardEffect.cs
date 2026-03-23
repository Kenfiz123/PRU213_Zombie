using UnityEngine;

// abstract nghĩa là script này chỉ làm mẫu, không dùng trực tiếp
public abstract class CardEffect : MonoBehaviour
{
    [Header("--- HIỆU ỨNG CHUNG ---")]
    public GameObject pickupEffect; // Hiệu ứng nổ khi ăn

    // Hàm này sẽ được các script con viết lại (override)
    public abstract void Activate(GameObject player);

    // Hàm dùng chung để xóa thẻ
    protected void DestroyCard()
    {
        if (pickupEffect != null) Instantiate(pickupEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}