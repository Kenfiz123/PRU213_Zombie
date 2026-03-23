using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [Header("--- KHO ĐỒ (Chỉnh số lượng ở đây) ---")]
    public int medkitCount = 2;
    public int armorPlateCount = 3;
    public int pistolAmmoStock = 50; // Đạn dự trữ
    public int akAmmoStock = 120;    // Đạn dự trữ

    [Header("--- THAM CHIẾU ---")]
    public PlayerHealth playerHealth;
    public PlayerArmor playerArmor;
    // public WeaponManager weaponManager; (Dành cho việc nạp đạn - sẽ làm sau)

    void Start()
    {
        // Tự tìm script nếu quên kéo
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerArmor == null) playerArmor = GetComponent<PlayerArmor>();
    }

    // --- HÀM DÙNG ĐỒ (Được gọi từ UI) ---

    public void UseMedkit()
    {
        if (medkitCount > 0)
        {
            // Chỉ dùng nếu máu chưa đầy
            if (playerHealth.CurrentHealth < playerHealth.maxHealth)
            {
                playerHealth.Heal(50); // Hồi 50 máu
                medkitCount--; // Trừ 1 cái
                Debug.Log("❤️ Đã dùng Medkit. Còn lại: " + medkitCount);
            }
            else
            {
                Debug.Log("⚠️ Máu đang đầy!");
            }
        }
        else
        {
            Debug.Log("❌ Hết Medkit rồi!");
        }
    }

    public void UseArmorPlate()
    {
        if (armorPlateCount > 0)
        {
            // Chỉ dùng nếu giáp chưa đầy
            if (playerArmor.CurrentArmor < playerArmor.maxArmor)
            {
                playerArmor.AddArmor(50); // Hồi 50 giáp
                armorPlateCount--;
                Debug.Log("🛡️ Đã dùng Giáp. Còn lại: " + armorPlateCount);
            }
            else
            {
                Debug.Log("⚠️ Giáp đang đầy!");
            }
        }
        else
        {
            Debug.Log("❌ Hết Giáp rồi!");
        }
    }

    // Hàm nhận đồ (Dùng cho việc nhặt đồ sau này)
    public void AddItem(string itemType, int amount)
    {
        switch (itemType)
        {
            case "Medkit": medkitCount += amount; break;
            case "Armor": armorPlateCount += amount; break;
            case "PistolAmmo": pistolAmmoStock += amount; break;
            case "AKAmmo": akAmmoStock += amount; break;
        }
    }

    // --- HÀM CHO SÚNG "XIN" ĐẠN TỪ BALO ---
    // weaponAmmoType: "PistolAmmo", "AKAmmo", ...
    // requestedAmount: số viên muốn lấy
    // Trả về: số viên thực tế lấy được (có thể < requestedAmount nếu balo không đủ)
    public int RequestAmmo(string weaponAmmoType, int requestedAmount)
    {
        if (requestedAmount <= 0) return 0;

        int available = 0;

        switch (weaponAmmoType)
        {
            case "PistolAmmo":
                available = pistolAmmoStock;
                break;
            case "AKAmmo":
                available = akAmmoStock;
                break;
            default:
                Debug.LogWarning($"PlayerInventory.RequestAmmo: ammo type không hỗ trợ: {weaponAmmoType}");
                return 0;
        }

        if (available <= 0) return 0;

        int taken = Mathf.Min(requestedAmount, available);

        // Trừ khỏi kho tương ứng
        switch (weaponAmmoType)
        {
            case "PistolAmmo":
                pistolAmmoStock -= taken;
                break;
            case "AKAmmo":
                akAmmoStock -= taken;
                break;
        }

        return taken;
    }
}