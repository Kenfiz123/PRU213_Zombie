using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Thanh máu Boss hiện ở trên cùng màn hình.
/// Tự tạo UI bằng code. Tự tìm Boss khi spawn.
/// Gắn vào Empty GameObject.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance { get; private set; }

    [Header("═══ CÀI ĐẶT ═══")]
    public string bossName = "BOSS";
    public Color barColor = new Color(0.8f, 0.1f, 0.1f);
    public Color barBgColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    public Color damageFlashColor = new Color(1f, 0.3f, 0.1f);

    private Canvas canvas;
    private GameObject panelRoot;
    private Image healthFill;
    private Image damageFill; // thanh damage trễ
    private TMP_Text nameText;
    private TMP_Text hpText;

    private ZombieHealth bossHealth;
    private float lastHealthPercent = 1f;
    private float damageFillTarget = 1f;
    private bool isActive = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        BuildUI();
        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!isActive || bossHealth == null)
        {
            // Boss chết hoặc bị destroy
            if (isActive && bossHealth == null)
                Hide();
            return;
        }

        float percent = Mathf.Clamp01(bossHealth.currentHealth / bossHealth.maxHealth);

        // Dùng scale X thay vì fillAmount (đáng tin hơn)
        Vector3 hpScale = healthFill.rectTransform.localScale;
        hpScale.x = percent;
        healthFill.rectTransform.localScale = hpScale;

        // Thanh damage trễ (giảm chậm theo)
        if (percent < damageFillTarget)
            damageFillTarget = percent;
        damageFillTarget = Mathf.MoveTowards(damageFillTarget, percent, Time.deltaTime * 0.4f);
        Vector3 dmgScale = damageFill.rectTransform.localScale;
        dmgScale.x = damageFillTarget;
        damageFill.rectTransform.localScale = dmgScale;

        // Flash khi mất máu
        if (percent < lastHealthPercent - 0.01f)
            healthFill.color = damageFlashColor;
        else
            healthFill.color = Color.Lerp(healthFill.color, barColor, Time.deltaTime * 5f);

        lastHealthPercent = percent;

        // Cập nhật text
        hpText.text = $"{Mathf.CeilToInt(bossHealth.currentHealth)} / {Mathf.CeilToInt(bossHealth.maxHealth)}";
    }

    /// <summary>
    /// Gọi khi Boss spawn — truyền ZombieHealth của boss vào.
    /// </summary>
    public void Show(ZombieHealth boss, string name = null)
    {
        bossHealth = boss;
        if (!string.IsNullOrEmpty(name))
            bossName = name;

        nameText.text = bossName;
        healthFill.fillAmount = 1f;
        damageFill.fillAmount = 1f;
        lastHealthPercent = 1f;
        damageFillTarget = 1f;
        healthFill.color = barColor;

        hpText.text = $"{Mathf.CeilToInt(boss.maxHealth)} / {Mathf.CeilToInt(boss.maxHealth)}";

        panelRoot.SetActive(true);
        isActive = true;
    }

    public void Hide()
    {
        isActive = false;
        bossHealth = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void BuildUI()
    {
        // Tìm Canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("BossHPCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Panel gốc — thanh ngang ở trên cùng
        panelRoot = new GameObject("BossHealthBar", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.92f);
        panelRect.anchorMax = new Vector2(0.8f, 0.97f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRoot.GetComponent<Image>().color = barBgColor;

        // Outline
        Outline outline = panelRoot.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.1f, 0.1f, 0.7f);
        outline.effectDistance = new Vector2(2, 2);

        // Tên Boss — phía trên thanh máu
        GameObject nameObj = new GameObject("BossName", typeof(RectTransform));
        nameObj.transform.SetParent(panelRoot.transform, false);
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = bossName;
        nameText.fontSize = 22;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(1f, 0.3f, 0.2f);
        nameText.alignment = TextAlignmentOptions.Center;
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.anchoredPosition = new Vector2(0, 20);
        nameRect.sizeDelta = new Vector2(0, 30);

        // Damage fill (nền đỏ cam, giảm chậm)
        GameObject dmgObj = new GameObject("DamageFill", typeof(RectTransform), typeof(Image));
        dmgObj.transform.SetParent(panelRoot.transform, false);
        damageFill = dmgObj.GetComponent<Image>();
        damageFill.color = new Color(0.6f, 0.15f, 0.05f, 0.7f);
        damageFill.raycastTarget = false;
        RectTransform dmgRect = dmgObj.GetComponent<RectTransform>();
        dmgRect.anchorMin = new Vector2(0.01f, 0.1f);
        dmgRect.anchorMax = new Vector2(0.99f, 0.9f);
        dmgRect.pivot = new Vector2(0f, 0.5f); // Pivot trái để scale từ trái
        dmgRect.offsetMin = Vector2.zero;
        dmgRect.offsetMax = Vector2.zero;

        // Health fill (thanh máu chính)
        GameObject fillObj = new GameObject("HealthFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(panelRoot.transform, false);
        healthFill = fillObj.GetComponent<Image>();
        healthFill.color = barColor;
        healthFill.raycastTarget = false;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.01f, 0.1f);
        fillRect.anchorMax = new Vector2(0.99f, 0.9f);
        fillRect.pivot = new Vector2(0f, 0.5f); // Pivot trái để scale từ trái
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // HP text — trên thanh máu
        GameObject hpObj = new GameObject("HPText", typeof(RectTransform));
        hpObj.transform.SetParent(panelRoot.transform, false);
        hpText = hpObj.AddComponent<TextMeshProUGUI>();
        hpText.text = "";
        hpText.fontSize = 16;
        hpText.fontStyle = FontStyles.Bold;
        hpText.color = Color.white;
        hpText.alignment = TextAlignmentOptions.Center;
        RectTransform hpRect = hpObj.GetComponent<RectTransform>();
        hpRect.anchorMin = Vector2.zero;
        hpRect.anchorMax = Vector2.one;
        hpRect.offsetMin = Vector2.zero;
        hpRect.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
