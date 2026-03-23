using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiện icon dash + cooldown overlay.
/// Gắn vào Canvas, kéo Image overlay (filled) vào.
/// </summary>
public class DashCooldownUI : MonoBehaviour
{
    [Header("--- THAM CHIẾU ---")]
    [Tooltip("Kéo Player vào đây (có PlayerMovement).")]
    public PlayerMovement playerMovement;

    [Tooltip("Image overlay dạng Filled (fillMethod = Radial360). Sáng khi sẵn sàng, tối khi cooldown.")]
    public Image cooldownOverlay;

    [Tooltip("Text hiện số giây còn lại (tùy chọn).")]
    public Text cooldownText;

    [Header("--- MÀU ---")]
    public Color readyColor = new Color(1f, 1f, 1f, 0f);       // Trong suốt khi sẵn sàng
    public Color cooldownColor = new Color(0f, 0f, 0f, 0.6f);  // Tối khi đang cooldown

    void Update()
    {
        if (playerMovement == null || cooldownOverlay == null) return;

        float remaining = playerMovement.DashCooldownRemaining;
        bool ready = playerMovement.IsDashReady;

        if (ready)
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.color = readyColor;
            if (cooldownText != null) cooldownText.text = "DASH";
        }
        else
        {
            // Fill giảm dần từ 1 → 0 khi cooldown hết
            float total = 3f; // dashCooldown mặc định, có thể lấy từ script
            cooldownOverlay.fillAmount = remaining / total;
            cooldownOverlay.color = cooldownColor;
            if (cooldownText != null) cooldownText.text = remaining.ToString("F1") + "s";
        }
    }
}
