using UnityEngine;
using UnityEngine.UI;

public class PlayerArmor : MonoBehaviour
{
    [Header("--- CHỈ SỐ GIÁP ---")]
    public float maxArmor = 100f;
    [SerializeField] private float currentArmor;

    [Header("--- CẤU HÌNH HẤP THỤ ---")]
    [Tooltip("Tỉ lệ sát thương Giáp gánh chịu (0.8 = 80%)")]
    public float absorptionPercent = 0.8f;

    [Header("--- UI ---")]
    public Slider armorSlider;
    [Tooltip("Tốc độ mượt của thanh giáp (chỉ ảnh hưởng UI).")]
    [SerializeField] private float uiLerpSpeed = 10f;

    private bool uiDirty = true;

    void Start()
    {
        // Khởi tạo giáp (nếu chưa set thì full giáp)
        currentArmor = Mathf.Clamp(currentArmor <= 0f ? maxArmor : currentArmor, 0f, maxArmor);
        absorptionPercent = Mathf.Clamp01(absorptionPercent);
        UpdateUI();
    }

    void Update()
    {
        if (armorSlider == null) return;

        if (uiDirty || !Mathf.Approximately(armorSlider.value, currentArmor))
        {
            float speed = Mathf.Max(0.01f, uiLerpSpeed);
            armorSlider.value = Mathf.Lerp(armorSlider.value, currentArmor, Time.deltaTime * speed);

            if (Mathf.Abs(armorSlider.value - currentArmor) < 0.01f)
            {
                armorSlider.value = currentArmor;
                uiDirty = false;
            }
        }
    }

    // --- HÀM TÍNH TOÁN SÁT THƯƠNG KIỂU MỚI (80/20) ---
    public float AbsorbDamage(float totalDamage)
    {
        // 1. Nếu không có giáp, Máu chịu tất cả
        if (currentArmor <= 0f) return totalDamage;

        // 2. Tính toán lượng damage lý thuyết chia ra
        float damageToArmor = totalDamage * absorptionPercent; // 80 damage
        float damageToHealth = totalDamage * (1f - absorptionPercent); // 20 damage

        // 3. Kiểm tra xem Giáp hiện tại có đủ để gánh 80 damage kia không
        if (currentArmor >= damageToArmor)
        {
            // TRƯỜNG HỢP 1: Giáp đủ dày
            currentArmor = Mathf.Max(0f, currentArmor - damageToArmor);
            uiDirty = true;
            // Trả về 20 damage để trừ vào máu
            return damageToHealth;
        }
        else
        {
            // TRƯỜNG HỢP 2: Giáp còn ít (Vỡ giáp)
            // Ví dụ: Cần trừ 80 giáp, nhưng chỉ còn 10 giáp

            float damageAbsorbed = currentArmor; // Giáp chỉ đỡ được 10
            float overflowDamage = damageToArmor - damageAbsorbed; // 70 damage bị tràn

            currentArmor = 0f; // Giáp về 0
            uiDirty = true;

            // Máu sẽ chịu: 20 damage gốc + 70 damage tràn = 90 damage
            return damageToHealth + overflowDamage;
        }
    }

    public void AddArmor(float amount)
    {
        if (amount <= 0f) return;
        currentArmor = Mathf.Clamp(currentArmor + amount, 0f, maxArmor);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (armorSlider == null) return;

        armorSlider.maxValue = maxArmor;
        armorSlider.value = currentArmor;
        uiDirty = false;
    }

    // Cho script khác đọc giáp hiện tại (read-only)
    public float CurrentArmor => currentArmor;
}