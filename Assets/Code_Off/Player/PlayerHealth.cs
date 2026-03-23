using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("=== CÀI ĐẶT MÁU ===")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    // Cho script khác đọc máu hiện tại (read-only)
    public float CurrentHealth => currentHealth;

    [Header("=== GIÁP (ARMOR) ===")]
    [Tooltip("Script PlayerArmor trên cùng nhân vật (nếu có).")]
    [SerializeField] private PlayerArmor playerArmor;

    [Header("=== GIAO DIỆN (UI) ===")]
    public Slider healthSlider;       // Kéo thanh máu vào đây
    public Image damageImage;         // Kéo màn hình đỏ vào đây
    public GameObject gameOverPanel;  // <--- ĐÃ THÊM LẠI: Kéo GameOverGroup vào đây

    [Header("=== ÂM THANH ===")]
    [Tooltip("Nhạc Game Over (kéo AudioClip vào đây)")]
    public AudioClip gameOverMusic;
    [Range(0f, 1f)]
    public float gameOverVolume = 0.8f;

    [Header("=== HIỆU ỨNG ===")]
    public float flashSpeed = 5f;
    public Color flashColor = new Color(1f, 0f, 0f, 0.5f);

    private bool damaged = false;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Tự tìm PlayerArmor nếu chưa gán
        if (playerArmor == null)
        {
            playerArmor = GetComponent<PlayerArmor>();
        }

        // Tắt bảng Game Over lúc đầu game
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Xử lý hiệu ứng chớp đỏ
        if (damageImage != null)
        {
            if (damaged)
            {
                damageImage.color = flashColor;
            }
            else
            {
                damageImage.color = Color.Lerp(damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
            }
        }
        damaged = false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        damaged = true;
        float finalDamage = amount;

        // Nếu có giáp: cho giáp hấp thụ trước, sau đó máu chỉ trừ phần còn lại
        if (playerArmor != null)
        {
            finalDamage = playerArmor.AbsorbDamage(amount);
        }

        currentHealth -= finalDamage;

        if (healthSlider != null) healthSlider.value = currentHealth;

        // Debug.Log("Bị cắn! Máu còn: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    // Thêm hàm này vào trong PlayerHealth.cs
    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;
        currentHealth += amount;

        // Không được hồi vượt quá Max Health
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        if (healthSlider != null) healthSlider.value = currentHealth;
    }
    void Die()
    {
        isDead = true;
        Debug.Log("GAME OVER!");

        // 1. Hiện bảng Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // 2. Hiện con trỏ chuột để bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Tắt nhạc nền
        WaveManager.StopBGM();

        // 4. Phát nhạc Game Over
        if (gameOverMusic != null)
        {
            GameObject musicObj = new GameObject("GameOverMusic");
            AudioSource src = musicObj.AddComponent<AudioSource>();
            src.clip = gameOverMusic;
            src.volume = gameOverVolume;
            src.loop = false;
            src.Play();
        }

        // 5. Dừng thời gian game
        Time.timeScale = 0;

        // 4. Vô hiệu hóa súng (để không bắn được nữa)
        PlayerShooting shooting = GetComponentInChildren<PlayerShooting>();
        if (shooting != null) shooting.enabled = false;
    }
}