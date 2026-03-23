using UnityEngine;
using UnityEngine.UI; // Cần thiết để chỉnh thanh Slider

public class PlayerStamina : MonoBehaviour
{
    [Header("--- CÀI ĐẶT CHỈ SỐ ---")]
    public float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    public float staminaRegenRate = 15f;    // Tốc độ hồi phục mỗi giây
    public float regenDelay = 2.0f;         // Thời gian chờ sau khi dùng stamina mới bắt đầu hồi

    [Header("--- UI ---")]
    public Slider staminaSlider;            // Kéo thanh Slider vào đây
    [Tooltip("Tốc độ mượt của thanh stamina (chỉ ảnh hưởng UI).")]
    [SerializeField] private float uiLerpSpeed = 10f;
    [Tooltip("Nếu bật: log khi không đủ stamina (khuyến nghị tắt khi release).")]
    [SerializeField] private bool logWhenEmpty = false;

    private float lastActionTime = 0f;      // Thời điểm cuối cùng dùng thể lực
    private bool uiDirty = true;

    void Start()
    {
        currentStamina = Mathf.Clamp(currentStamina <= 0f ? maxStamina : currentStamina, 0f, maxStamina);

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Update()
    {
        // Regen chỉ khi cần (đỡ chạy vô ích mỗi frame khi đã full stamina)
        if (currentStamina < maxStamina && Time.time - lastActionTime > regenDelay)
        {
            RegenerateStamina();
        }

        UpdateUI();
    }

    void RegenerateStamina()
    {
        float before = currentStamina;
        currentStamina = Mathf.Clamp(currentStamina + staminaRegenRate * Time.deltaTime, 0f, maxStamina);
        if (!Mathf.Approximately(before, currentStamina)) uiDirty = true;
    }

    void UpdateUI()
    {
        if (staminaSlider == null) return;

        // Chỉ chạy Lerp khi value đang khác target (giảm work khi đứng yên)
        float target = currentStamina;
        if (uiDirty || !Mathf.Approximately(staminaSlider.value, target))
        {
            float speed = Mathf.Max(0.01f, uiLerpSpeed);
            staminaSlider.value = Mathf.Lerp(staminaSlider.value, target, Time.deltaTime * speed);

            // Khi gần tới target thì coi như xong để khỏi lerp mãi
            if (Mathf.Abs(staminaSlider.value - target) < 0.01f)
            {
                staminaSlider.value = target;
                uiDirty = false;
            }
        }
    }

    // --- HÀM CHO CÁC SCRIPT KHÁC GỌI ---

    // Dùng cho hành động tức thời (Chém, Lướt, Nhảy)
    // Trả về TRUE nếu đủ thể lực, FALSE nếu không đủ
    public bool TryUseStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (currentStamina >= amount)
        {
            currentStamina = Mathf.Max(0f, currentStamina - amount);
            lastActionTime = Time.time; // Reset thời gian chờ hồi phục
            uiDirty = true;
            return true;
        }
        else
        {
            if (logWhenEmpty) Debug.Log("⚠️ Không đủ thể lực!");
            return false;
        }
    }

    // Dùng cho hành động liên tục (Chạy bộ - Sprint)
    public bool UseStaminaContinuous(float amountPerSecond)
    {
        if (amountPerSecond <= 0f) return true;
        if (currentStamina > 0f)
        {
            float before = currentStamina;
            currentStamina = Mathf.Max(0f, currentStamina - amountPerSecond * Time.deltaTime);
            lastActionTime = Time.time;
            if (!Mathf.Approximately(before, currentStamina)) uiDirty = true;
            return true;
        }
        return false;
    }

    // Cho UI/Script khác đọc (read-only)
    public float CurrentStamina => currentStamina;
}