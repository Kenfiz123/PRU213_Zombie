using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Last Stand — Khi HP = 0, ngã xuống chỉ bắn được pistol (damage giảm).
/// Giết đủ số zombie → hồi sinh. Chỉ được 1 lần mỗi game.
/// Tất cả thông số tự động theo độ khó.
/// </summary>
public class LastStandManager : MonoBehaviour
{
    public static LastStandManager Instance { get; private set; }
    public static bool IsInLastStand { get; private set; } = false;

    [Header("--- THAM CHIẾU ---")]
    public PlayerHealth playerHealth;
    public WeaponSwitching weaponSwitching;
    public PlayerMovement playerMovement;

    [Header("--- PISTOL INDEX ---")]
    [Tooltip("Index của pistol trong WeaponSwitching (thường là 0 hoặc 1)")]
    public int pistolWeaponIndex = 1;

    [Header("--- ÂM THANH ---")]
    public AudioClip lastStandSound;
    public AudioClip reviveSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    // === NỘI BỘ ===
    private bool hasUsed = false; // Chỉ dùng 1 lần
    private int killCount = 0;
    private int killsRequired;
    private float pistolDamageMul;
    private float reviveHealthPercent;
    private float lastStandDuration;

    // UI
    private GameObject uiPanel;
    private TextMeshProUGUI killText;
    private TextMeshProUGUI timerText;
    private Image vignetteImage;
    private float timeRemaining;

    // Cache
    private float originalMoveSpeed;
    private AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        IsInLastStand = false;
    }

    void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        if (weaponSwitching == null)
            weaponSwitching = GetComponentInChildren<WeaponSwitching>();
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        ApplyDifficultySettings();
    }

    void ApplyDifficultySettings()
    {
        switch (DifficultyManager.Current)
        {
            case DifficultyManager.Difficulty.Easy:
                killsRequired = 3;
                pistolDamageMul = 0.8f;
                reviveHealthPercent = 0.5f;   // Hồi 50% HP
                lastStandDuration = 20f;
                break;

            case DifficultyManager.Difficulty.Normal:
                killsRequired = 5;
                pistolDamageMul = 0.6f;
                reviveHealthPercent = 0.3f;   // Hồi 30% HP
                lastStandDuration = 15f;
                break;

            case DifficultyManager.Difficulty.Hard:
                killsRequired = 8;
                pistolDamageMul = 0.4f;
                reviveHealthPercent = 0.2f;   // Hồi 20% HP
                lastStandDuration = 12f;
                break;

            case DifficultyManager.Difficulty.Asian:
                killsRequired = 15;
                pistolDamageMul = 0.2f;
                reviveHealthPercent = 0.1f;   // Hồi 10% HP
                lastStandDuration = 8f;
                break;
        }
    }

    void Update()
    {
        if (!IsInLastStand) return;

        // Đếm ngược thời gian
        timeRemaining -= Time.deltaTime;
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";

        // Nhấp nháy viền đỏ
        if (vignetteImage != null)
        {
            float alpha = 0.3f + Mathf.PingPong(Time.time * 2f, 0.3f);
            vignetteImage.color = new Color(1f, 0f, 0f, alpha);
        }

        // Hết thời gian → chết thật
        if (timeRemaining <= 0f)
        {
            FailLastStand();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // KÍCH HOẠT LAST STAND (gọi từ PlayerHealth.Die())
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Thử kích hoạt Last Stand. Return true nếu thành công.</summary>
    public bool TryActivate()
    {
        if (hasUsed) return false;

        hasUsed = true;
        IsInLastStand = true;
        killCount = 0;
        timeRemaining = lastStandDuration;

        // Âm thanh
        if (lastStandSound != null)
            audioSource.PlayOneShot(lastStandSound, soundVolume);

        // Ép chuyển sang pistol ngay lập tức
        if (weaponSwitching != null)
        {
            weaponSwitching.selectedWeapon = pistolWeaponIndex;
            weaponSwitching.SelectWeapon();
        }

        // Tạo UI
        BuildUI();

        // Camera shake mạnh
        CameraShake.Shake(0.6f, 0.5f);

        Debug.Log($"[LastStand] Kích hoạt! Cần giết {killsRequired} zombie trong {lastStandDuration}s");

        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // GHI NHẬN KILL (gọi từ ZombieHealth.Die())
    // ═══════════════════════════════════════════════════════════════

    public void OnZombieKilled()
    {
        if (!IsInLastStand) return;

        killCount++;
        if (killText != null)
            killText.text = killCount + " / " + killsRequired;

        // Camera shake nhẹ feedback
        CameraShake.Shake(0.1f, 0.05f);

        if (killCount >= killsRequired)
        {
            Revive();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // HỒI SINH
    // ═══════════════════════════════════════════════════════════════

    void Revive()
    {
        IsInLastStand = false;

        // Hồi máu theo %
        float healAmount = playerHealth.maxHealth * reviveHealthPercent;
        playerHealth.ForceRevive(healAmount);

        // Âm thanh
        if (reviveSound != null)
            audioSource.PlayOneShot(reviveSound, soundVolume);

        // Xóa UI
        if (uiPanel != null)
            Destroy(uiPanel);

        // Camera shake
        CameraShake.Shake(0.3f, 0.2f);

        Debug.Log($"[LastStand] HỒI SINH! Máu: {healAmount}");
    }

    // ═══════════════════════════════════════════════════════════════
    // THẤT BẠI → CHẾT THẬT
    // ═══════════════════════════════════════════════════════════════

    void FailLastStand()
    {
        IsInLastStand = false;

        if (uiPanel != null)
            Destroy(uiPanel);

        // Gọi chết thật
        playerHealth.ForceDie();

        Debug.Log("[LastStand] Thất bại! GAME OVER!");
    }

    // ═══════════════════════════════════════════════════════════════
    // DAMAGE MULTIPLIER (PlayerShooting sẽ đọc giá trị này)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Hệ số damage pistol trong Last Stand</summary>
    public float GetDamageMultiplier()
    {
        return IsInLastStand ? pistolDamageMul : 1f;
    }

    // ═══════════════════════════════════════════════════════════════
    // UI
    // ═══════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("LastStandCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        uiPanel = canvasObj;

        // Viền đỏ nhấp nháy (fullscreen)
        GameObject vigObj = new GameObject("Vignette");
        vigObj.transform.SetParent(canvasObj.transform, false);
        RectTransform vigRt = vigObj.AddComponent<RectTransform>();
        vigRt.anchorMin = Vector2.zero;
        vigRt.anchorMax = Vector2.one;
        vigRt.sizeDelta = Vector2.zero;
        vignetteImage = vigObj.AddComponent<Image>();
        vignetteImage.color = new Color(1f, 0f, 0f, 0.3f);
        vignetteImage.raycastTarget = false;

        // Title "LAST STAND"
        CreateText(canvasObj.transform, "LAST STAND", 80, Color.red,
            new Vector2(0, 350), new Vector2(800, 100), FontStyles.Bold);

        // Kill counter
        killText = CreateText(canvasObj.transform, "0 / " + killsRequired, 60, Color.white,
            new Vector2(0, 270), new Vector2(400, 80), FontStyles.Bold);

        // Subtitle
        CreateText(canvasObj.transform, "GIẾT ZOMBIE ĐỂ HỒI SINH!", 36, Color.yellow,
            new Vector2(0, 210), new Vector2(800, 50), FontStyles.Normal);

        // Timer
        timerText = CreateText(canvasObj.transform, Mathf.CeilToInt(lastStandDuration) + "s", 48,
            new Color(1f, 0.5f, 0f), new Vector2(0, 150), new Vector2(200, 60), FontStyles.Bold);
    }

    TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Color color,
        Vector2 pos, Vector2 size, FontStyles style)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        return tmp;
    }
}
