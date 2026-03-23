using UnityEngine;
using UnityEngine.UI;

public class AllyHealth : MonoBehaviour
{
    [Header("--- CHỈ SỐ MÁU ---")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("--- UI ---")]
    public Slider healthSlider;

    [Header("--- PHASE SYSTEM ---")]
    [Tooltip("Ngưỡng HP để vào Phase 2 (Empowered). 0.6 = 60%")]
    public float phase2Threshold = 0.6f;
    [Tooltip("Ngưỡng HP để vào Phase 3 (Desperate). 0.3 = 30%")]
    public float phase3Threshold = 0.3f;

    [Header("--- DEATH ---")]
    [Tooltip("Thời gian chờ death animation trước khi Destroy")]
    public float deathDelay = 2f;

    private int currentPhase = 1;
    private bool isDead = false;
    private Animator anim;
    private AllyController allyController;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        allyController = GetComponent<AllyController>();
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        currentHealth -= amount;
        UpdateUI();

        // Thông báo cho AllyController biết đã bị đánh
        if (allyController != null)
        {
            allyController.OnDamageReceived(amount);
        }

        // Kiểm tra chuyển Phase
        CheckPhaseTransition();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        UpdateUI();

        // Kiểm tra lại phase (nếu heal vượt ngưỡng có thể quay lại phase thấp hơn)
        CheckPhaseTransition();
    }

    void CheckPhaseTransition()
    {
        float hpPercent = currentHealth / maxHealth;
        int newPhase;

        if (hpPercent <= phase3Threshold)
            newPhase = 3;
        else if (hpPercent <= phase2Threshold)
            newPhase = 2;
        else
            newPhase = 1;

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;

            // Thông báo AllyController chuyển phase
            if (allyController != null)
            {
                allyController.OnPhaseChange(newPhase);
            }

            Debug.Log($"⚡ Ally Phase {newPhase}! HP: {hpPercent * 100:F0}%");
        }
    }

    void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("💀 Ally đã hy sinh!");

        // Thông báo AllyController
        if (allyController != null)
        {
            allyController.OnDeath();
        }

        // Death animation
        if (anim != null) anim.SetTrigger("Die");

        // Tắt NavMesh
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // Tắt collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Xóa sau delay (chờ death animation)
        Destroy(gameObject, deathDelay);
    }

    // Cho script khác đọc
    public bool IsDead => isDead;
    public int CurrentPhase => currentPhase;
}
