using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Aegis.GrenadeSystem.HiEx;

/// <summary>
/// Hệ thống nâng cấp vũ khí. Tự tạo UI đẹp bằng code.
/// Gắn vào Empty GameObject (KHÔNG gắn trên UpgradePanel).
/// Ấn B để mở/đóng.
/// </summary>
public class WeaponUpgradeManager : MonoBehaviour
{
    public static WeaponUpgradeManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════════
    // ĐIỂM
    // ═══════════════════════════════════════════════════════════════
    [Header("═══ ĐIỂM ═══")]
    public int currentPoints = 0;
    public int pointsPerKill = 10;
    public int pointsPerSpecialKill = 20;
    public int pointsPerBossKill = 100;

    // ═══════════════════════════════════════════════════════════════
    // UPGRADE CONFIG
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class UpgradeConfig
    {
        public string upgradeName;
        public int maxLevel = 5;
        [HideInInspector] public int currentLevel = 0;
        public int baseCost = 50;
        public int costPerLevel = 30;
        public float valuePerLevel = 10f;

        public int GetCost() => baseCost + currentLevel * costPerLevel;
        public bool IsMaxed() => currentLevel >= maxLevel;
    }

    [Header("═══ NÂNG CẤP PISTOL ═══")]
    public UpgradeConfig pistolDamage = new UpgradeConfig { upgradeName = "Sát thương", baseCost = 40, costPerLevel = 20, valuePerLevel = 15f };
    public UpgradeConfig pistolFireRate = new UpgradeConfig { upgradeName = "Tốc độ bắn", baseCost = 50, costPerLevel = 25, valuePerLevel = 10f };
    public UpgradeConfig pistolMagazine = new UpgradeConfig { upgradeName = "Băng đạn", baseCost = 30, costPerLevel = 15, valuePerLevel = 20f };
    public UpgradeConfig pistolReload = new UpgradeConfig { upgradeName = "Nạp đạn", baseCost = 40, costPerLevel = 20, valuePerLevel = 15f };

    [Header("═══ NÂNG CẤP AK ═══")]
    public UpgradeConfig akDamage = new UpgradeConfig { upgradeName = "Sát thương", baseCost = 50, costPerLevel = 30, valuePerLevel = 12f };
    public UpgradeConfig akFireRate = new UpgradeConfig { upgradeName = "Tốc độ bắn", baseCost = 60, costPerLevel = 30, valuePerLevel = 8f };
    public UpgradeConfig akMagazine = new UpgradeConfig { upgradeName = "Băng đạn", baseCost = 40, costPerLevel = 20, valuePerLevel = 15f };
    public UpgradeConfig akReload = new UpgradeConfig { upgradeName = "Nạp đạn", baseCost = 50, costPerLevel = 25, valuePerLevel = 12f };

    [Header("═══ NÂNG CẤP DAO ═══")]
    public UpgradeConfig knifeDamage = new UpgradeConfig { upgradeName = "Sát thương", baseCost = 30, costPerLevel = 20, valuePerLevel = 20f };
    public UpgradeConfig knifeSpeed = new UpgradeConfig { upgradeName = "Tốc độ chém", baseCost = 40, costPerLevel = 25, valuePerLevel = 10f };

    [Header("═══ NÂNG CẤP NHÂN VẬT ═══")]
    public UpgradeConfig upgradeMaxHP = new UpgradeConfig { upgradeName = "Máu tối đa", maxLevel = 5, baseCost = 40, costPerLevel = 25, valuePerLevel = 20f };
    public UpgradeConfig upgradeMaxArmor = new UpgradeConfig { upgradeName = "Giáp tối đa", maxLevel = 5, baseCost = 40, costPerLevel = 25, valuePerLevel = 20f };
    public UpgradeConfig upgradeMaxStamina = new UpgradeConfig { upgradeName = "Stamina tối đa", maxLevel = 5, baseCost = 35, costPerLevel = 20, valuePerLevel = 15f };
    public UpgradeConfig upgradeStaminaRegen = new UpgradeConfig { upgradeName = "Hồi Stamina", maxLevel = 5, baseCost = 30, costPerLevel = 20, valuePerLevel = 20f };

    // ═══════════════════════════════════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════════════════════════════════
    [Header("═══ VŨ KHÍ ═══")]
    public PlayerShooting pistolShooting;
    public PlayerShooting akShooting;
    public PlayerKnifeController knifeController;

    [Header("═══ NHÂN VẬT ═══")]
    public PlayerHealth playerHealth;
    public PlayerArmor playerArmor;
    public PlayerStamina playerStamina;

    [Header("═══ LỰU ĐẠN ═══")]
    [Tooltip("Kéo GrenadeSystem trên Player vào đây")]
    public GrenadeSystem grenadeSystem;
    [Tooltip("Giá mỗi quả lựu đạn")]
    public int grenadePrice = 30;

    [Header("═══ CANVAS ═══")]
    [Tooltip("Kéo Canvas chính vào đây")]
    public Canvas mainCanvas;

    [Header("═══ ÂM THANH ═══")]
    public AudioClip upgradeSound;
    public AudioClip errorSound;

    // Giá trị gốc
    private float pistolBaseDmg, pistolBaseRate, pistolBaseReload;
    private int pistolBaseMag;
    private float akBaseDmg, akBaseRate, akBaseReload;
    private int akBaseMag;
    private float knifeBaseFast, knifeBaseHeavy, knifeBaseFastRate, knifeBaseHeavyRate;
    private float baseMaxHP, baseMaxArmor, baseMaxStamina, baseStaminaRegen;

    private bool isOpen = false;
    private float lastToggleTime = -1f;

    // UI được tạo tự động
    private GameObject panelRoot;
    private TextMeshProUGUI pointsText;
    private List<UpgradeSlotUI> allSlots = new List<UpgradeSlotUI>();
    private Button grenadeButton;
    private TextMeshProUGUI grenadeButtonText;

    private class UpgradeSlotUI
    {
        public Button button;
        public TextMeshProUGUI text;
        public Image[] levelBlocks;
        public Color accentColor;
        public UpgradeConfig config;
        public System.Action applyCallback;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Lưu giá trị gốc
        if (pistolShooting != null)
        {
            pistolBaseDmg = pistolShooting.damage;
            pistolBaseRate = pistolShooting.fireRate;
            pistolBaseMag = pistolShooting.maxAmmo;
            pistolBaseReload = pistolShooting.reloadTime;
        }
        if (akShooting != null)
        {
            akBaseDmg = akShooting.damage;
            akBaseRate = akShooting.fireRate;
            akBaseMag = akShooting.maxAmmo;
            akBaseReload = akShooting.reloadTime;
        }
        if (knifeController != null)
        {
            knifeBaseFast = knifeController.fastDamage;
            knifeBaseHeavy = knifeController.heavyDamage;
            knifeBaseFastRate = knifeController.fastRate;
            knifeBaseHeavyRate = knifeController.heavyRate;
        }
        if (playerHealth != null) baseMaxHP = playerHealth.maxHealth;
        if (playerArmor != null) baseMaxArmor = playerArmor.maxArmor;
        if (playerStamina != null)
        {
            baseMaxStamina = playerStamina.maxStamina;
            baseStaminaRegen = playerStamina.staminaRegenRate;
        }

        BuildUI();
        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (Time.unscaledTime - lastToggleTime < 0.3f) return;
            lastToggleTime = Time.unscaledTime;
            TogglePanel();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.unscaledTime - lastToggleTime < 0.3f) return;
            lastToggleTime = Time.unscaledTime;
            TogglePanel();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // TẠO UI TỰ ĐỘNG
    // ═══════════════════════════════════════════════════════════════
    void BuildUI()
    {
        if (mainCanvas == null)
            mainCanvas = FindObjectOfType<Canvas>();

        // === Panel gốc (full màn hình, nền tối) ===
        panelRoot = CreatePanel("UpgradePanel_Auto", mainCanvas.transform);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);

        // === Container chính (giới hạn kích thước) ===
        GameObject container = CreatePanel("Container", panelRoot.transform);
        RectTransform contRect = container.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.05f, 0.08f);
        contRect.anchorMax = new Vector2(0.95f, 0.92f);
        contRect.offsetMin = Vector2.zero;
        contRect.offsetMax = Vector2.zero;
        container.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

        // Thêm viền
        Outline outline = container.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        outline.effectDistance = new Vector2(2, 2);

        // === Tiêu đề ===
        CreateText("NÂNG CẤP VŨ KHÍ", container.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -15),
            36, new Color(1f, 0.85f, 0.2f), FontStyles.Bold, TextAlignmentOptions.Center);

        // === Điểm ===
        pointsText = CreateText("Điểm: 0", container.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60),
            24, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);

        // === 4 CỘT ===
        // Cột 1: PISTOL
        BuildWeaponColumn(container.transform, "PISTOL", 0.01f, 0.24f,
            new Color(0.2f, 0.5f, 0.8f, 1f),
            new UpgradeConfig[] { pistolDamage, pistolFireRate, pistolMagazine, pistolReload },
            new string[] { "Sát Thương", "Tốc Độ Bắn", "Băng Đạn", "Nạp Đạn" },
            ApplyPistolUpgrades);

        // Cột 2: AK-47
        BuildWeaponColumn(container.transform, "AK-47", 0.26f, 0.49f,
            new Color(0.8f, 0.4f, 0.2f, 1f),
            new UpgradeConfig[] { akDamage, akFireRate, akMagazine, akReload },
            new string[] { "Sát Thương", "Tốc Độ Bắn", "Băng Đạn", "Nạp Đạn" },
            ApplyAKUpgrades);

        // Cột 3: DAO
        BuildWeaponColumn(container.transform, "DAO", 0.51f, 0.74f,
            new Color(0.6f, 0.2f, 0.2f, 1f),
            new UpgradeConfig[] { knifeDamage, knifeSpeed },
            new string[] { "Sát Thương", "Tốc Độ Chém" },
            ApplyKnifeUpgrades);

        // Cột 4: NHÂN VẬT
        BuildWeaponColumn(container.transform, "NHÂN VẬT", 0.76f, 0.99f,
            new Color(0.2f, 0.7f, 0.3f, 1f),
            new UpgradeConfig[] { upgradeMaxHP, upgradeMaxArmor, upgradeMaxStamina, upgradeStaminaRegen },
            new string[] { "Máu Tối Đa", "Giáp Tối Đa", "Stamina", "Hồi Stamina" },
            ApplyPlayerUpgrades);

        // === Nút MUA LỰU ĐẠN ===
        if (grenadeSystem != null)
        {
            GameObject grenBtn = CreateButton($"MUA LỰU ĐẠN (${grenadePrice})", container.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60),
                250, 40, new Color(0.7f, 0.4f, 0.1f, 1f));
            grenadeButton = grenBtn.GetComponent<Button>();
            grenadeButton.onClick.AddListener(BuyGrenade);

            // Lấy text con
            grenadeButtonText = grenBtn.GetComponentInChildren<TextMeshProUGUI>();
        }

        // === Nút ĐÓNG [B] ===
        GameObject closeBtn = CreateButton("ĐÓNG [B]", container.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 15),
            160, 40, new Color(0.6f, 0.15f, 0.15f, 1f));
        closeBtn.GetComponent<Button>().onClick.AddListener(() => TogglePanel());
    }

    void BuildWeaponColumn(Transform parent, string title, float xMin, float xMax,
        Color accentColor, UpgradeConfig[] configs, string[] displayNames, System.Action applyCallback)
    {
        // Nền cột
        GameObject col = CreatePanel(title + "_Col", parent);
        RectTransform colRect = col.GetComponent<RectTransform>();
        colRect.anchorMin = new Vector2(xMin, 0.08f);
        colRect.anchorMax = new Vector2(xMax, 0.82f);
        colRect.offsetMin = Vector2.zero;
        colRect.offsetMax = Vector2.zero;
        col.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.9f);

        // Viền cột
        Outline colOutline = col.AddComponent<Outline>();
        colOutline.effectColor = accentColor * 0.6f;
        colOutline.effectDistance = new Vector2(1, 1);

        // Tiêu đề cột
        CreateText(title, col.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10),
            22, accentColor, FontStyles.Bold, TextAlignmentOptions.Center);

        // Thanh phân cách
        GameObject sep = CreatePanel("Sep", col.transform);
        RectTransform sepRect = sep.GetComponent<RectTransform>();
        sepRect.anchorMin = new Vector2(0.1f, 1f);
        sepRect.anchorMax = new Vector2(0.9f, 1f);
        sepRect.anchoredPosition = new Vector2(0, -38);
        sepRect.sizeDelta = new Vector2(0, 2);
        sep.GetComponent<Image>().color = accentColor * 0.5f;

        // Các nút upgrade
        float startY = 0.82f;
        float spacing = configs.Length <= 2 ? 0.28f : 0.18f;

        for (int i = 0; i < configs.Length; i++)
        {
            float yCenter = startY - i * spacing;
            string name = (i < displayNames.Length) ? displayNames[i] : configs[i].upgradeName;
            BuildUpgradeSlot(col.transform, configs[i], name, applyCallback, accentColor, yCenter);
        }
    }

    void BuildUpgradeSlot(Transform parent, UpgradeConfig config, string displayName,
        System.Action applyCallback, Color accentColor, float yAnchor)
    {
        // Nền slot
        GameObject slot = CreatePanel(displayName + "_Slot", parent);
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.05f, yAnchor - 0.07f);
        slotRect.anchorMax = new Vector2(0.95f, yAnchor + 0.07f);
        slotRect.offsetMin = Vector2.zero;
        slotRect.offsetMax = Vector2.zero;
        slot.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.95f);

        // Tên upgrade
        TextMeshProUGUI nameText = CreateText(displayName, slot.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
            15, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
        RectTransform nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.55f);
        nameRect.anchorMax = new Vector2(0.7f, 0.95f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // 5 ô level (thay vì thanh bar liên tục)
        GameObject barContainer = CreatePanel("LevelBars", slot.transform);
        barContainer.GetComponent<Image>().color = Color.clear;
        RectTransform barContRect = barContainer.GetComponent<RectTransform>();
        barContRect.anchorMin = new Vector2(0.05f, 0.15f);
        barContRect.anchorMax = new Vector2(0.65f, 0.5f);
        barContRect.offsetMin = Vector2.zero;
        barContRect.offsetMax = Vector2.zero;

        // Tạo 5 ô vuông level
        Image[] levelBlocks = new Image[config.maxLevel];
        for (int b = 0; b < config.maxLevel; b++)
        {
            GameObject block = CreatePanel("Lv" + b, barContainer.transform);
            RectTransform blockRect = block.GetComponent<RectTransform>();
            float bMin = (float)b / config.maxLevel + 0.01f;
            float bMax = (float)(b + 1) / config.maxLevel - 0.01f;
            blockRect.anchorMin = new Vector2(bMin, 0.1f);
            blockRect.anchorMax = new Vector2(bMax, 0.9f);
            blockRect.offsetMin = Vector2.zero;
            blockRect.offsetMax = Vector2.zero;
            block.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f); // Xám nhạt khi chưa nâng
            levelBlocks[b] = block.GetComponent<Image>();
        }

        // Info text (giá)
        TextMeshProUGUI infoText = CreateText("$0", slot.transform,
            new Vector2(0.65f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero,
            14, new Color(0.9f, 0.9f, 0.9f), FontStyles.Normal, TextAlignmentOptions.Right);
        RectTransform infoRect = infoText.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.7f, 0.05f);
        infoRect.anchorMax = new Vector2(0.95f, 0.95f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;

        // Nút upgrade (phủ toàn bộ slot)
        Button btn = slot.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(1, 1, 1, 0);
        colors.highlightedColor = new Color(1, 1, 1, 0.1f);
        colors.pressedColor = new Color(1, 1, 1, 0.2f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = colors;

        // Lưu reference
        UpgradeSlotUI slotUI = new UpgradeSlotUI
        {
            button = btn,
            text = infoText,
            levelBlocks = levelBlocks,
            accentColor = accentColor,
            config = config,
            applyCallback = applyCallback
        };
        allSlots.Add(slotUI);

        btn.onClick.AddListener(() => TryUpgrade(slotUI));
    }

    // ═══════════════════════════════════════════════════════════════
    // UI HELPERS
    // ═══════════════════════════════════════════════════════════════
    GameObject CreatePanel(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        return go;
    }

    TextMeshProUGUI CreateText(string content, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        int fontSize, Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject("Text_" + content, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(400, fontSize + 10);

        return tmp;
    }

    GameObject CreateButton(string label, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        float width, float height, Color bgColor)
    {
        GameObject go = CreatePanel("Btn_" + label, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(width, height);
        go.GetComponent<Image>().color = bgColor;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = bgColor * 1.3f;
        cb.pressedColor = bgColor * 0.7f;
        btn.colors = cb;

        TextMeshProUGUI txt = CreateText(label, go.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            16, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return go;
    }

    // ═══════════════════════════════════════════════════════════════
    // MỞ/ĐÓNG
    // ═══════════════════════════════════════════════════════════════
    public void TogglePanel()
    {
        if (isOpen)
        {
            isOpen = false;
            if (panelRoot != null) panelRoot.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            PlayerShooting.BlockInputBriefly();
        }
        else
        {
            isOpen = true;
            if (panelRoot != null) panelRoot.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshUI();
        }
    }

    public void OpenPanel() { if (!isOpen) TogglePanel(); }
    public void ClosePanel() { if (isOpen) TogglePanel(); }

    // ═══════════════════════════════════════════════════════════════
    // ĐIỂM
    // ═══════════════════════════════════════════════════════════════
    public void AddPoints(int amount)
    {
        currentPoints += amount;
        if (isOpen) RefreshUI();
    }

    public void OnNormalKill() => AddPoints(Mathf.RoundToInt(pointsPerKill * DifficultyManager.PointsMul));
    public void OnSpecialKill() => AddPoints(Mathf.RoundToInt(pointsPerSpecialKill * DifficultyManager.PointsMul));
    public void OnBossKill() => AddPoints(Mathf.RoundToInt(pointsPerBossKill * DifficultyManager.PointsMul));

    // ═══════════════════════════════════════════════════════════════
    // NÂNG CẤP
    // ═══════════════════════════════════════════════════════════════
    void TryUpgrade(UpgradeSlotUI slot)
    {
        if (slot.config.IsMaxed())
        {
            PlaySound(errorSound);
            return;
        }

        int cost = slot.config.GetCost();
        if (currentPoints < cost)
        {
            PlaySound(errorSound);
            return;
        }

        currentPoints -= cost;
        slot.config.currentLevel++;
        PlaySound(upgradeSound);

        slot.applyCallback?.Invoke();
        RefreshUI();
    }

    // ═══════════════════════════════════════════════════════════════
    // ÁP DỤNG
    // ═══════════════════════════════════════════════════════════════
    void ApplyPistolUpgrades()
    {
        if (pistolShooting == null) return;
        pistolShooting.damage = pistolBaseDmg * (1f + pistolDamage.currentLevel * pistolDamage.valuePerLevel / 100f);
        pistolShooting.fireRate = pistolBaseRate * (1f + pistolFireRate.currentLevel * pistolFireRate.valuePerLevel / 100f);
        pistolShooting.maxAmmo = Mathf.RoundToInt(pistolBaseMag * (1f + pistolMagazine.currentLevel * pistolMagazine.valuePerLevel / 100f));
        pistolShooting.reloadTime = pistolBaseReload / (1f + pistolReload.currentLevel * pistolReload.valuePerLevel / 100f);
    }

    void ApplyAKUpgrades()
    {
        if (akShooting == null) return;
        akShooting.damage = akBaseDmg * (1f + akDamage.currentLevel * akDamage.valuePerLevel / 100f);
        akShooting.fireRate = akBaseRate * (1f + akFireRate.currentLevel * akFireRate.valuePerLevel / 100f);
        akShooting.maxAmmo = Mathf.RoundToInt(akBaseMag * (1f + akMagazine.currentLevel * akMagazine.valuePerLevel / 100f));
        akShooting.reloadTime = akBaseReload / (1f + akReload.currentLevel * akReload.valuePerLevel / 100f);
    }

    void ApplyKnifeUpgrades()
    {
        if (knifeController == null) return;
        float dmgMul = 1f + knifeDamage.currentLevel * knifeDamage.valuePerLevel / 100f;
        knifeController.fastDamage = knifeBaseFast * dmgMul;
        knifeController.heavyDamage = knifeBaseHeavy * dmgMul;
        float spdMul = 1f + knifeSpeed.currentLevel * knifeSpeed.valuePerLevel / 100f;
        knifeController.fastRate = knifeBaseFastRate / spdMul;
        knifeController.heavyRate = knifeBaseHeavyRate / spdMul;
    }

    void ApplyPlayerUpgrades()
    {
        // Máu tối đa
        if (playerHealth != null)
        {
            float oldMax = playerHealth.maxHealth;
            playerHealth.maxHealth = baseMaxHP * (1f + upgradeMaxHP.currentLevel * upgradeMaxHP.valuePerLevel / 100f);
            // Hồi thêm phần máu tăng → full máu luôn
            float diff = playerHealth.maxHealth - oldMax;
            if (diff > 0) playerHealth.Heal(diff);
            // Cập nhật slider: maxValue VÀ value
            if (playerHealth.healthSlider != null)
            {
                playerHealth.healthSlider.maxValue = playerHealth.maxHealth;
                playerHealth.healthSlider.value = playerHealth.CurrentHealth;
            }
        }

        // Giáp tối đa
        if (playerArmor != null)
        {
            float oldMax = playerArmor.maxArmor;
            playerArmor.maxArmor = baseMaxArmor * (1f + upgradeMaxArmor.currentLevel * upgradeMaxArmor.valuePerLevel / 100f);
            float diff = playerArmor.maxArmor - oldMax;
            if (diff > 0) playerArmor.AddArmor(diff);
            if (playerArmor.armorSlider != null)
            {
                playerArmor.armorSlider.maxValue = playerArmor.maxArmor;
                playerArmor.armorSlider.value = playerArmor.CurrentArmor;
            }
        }

        // Stamina tối đa + hồi
        if (playerStamina != null)
        {
            playerStamina.maxStamina = baseMaxStamina * (1f + upgradeMaxStamina.currentLevel * upgradeMaxStamina.valuePerLevel / 100f);
            playerStamina.staminaRegenRate = baseStaminaRegen * (1f + upgradeStaminaRegen.currentLevel * upgradeStaminaRegen.valuePerLevel / 100f);
            if (playerStamina.staminaSlider != null)
            {
                playerStamina.staminaSlider.maxValue = playerStamina.maxStamina;
                playerStamina.staminaSlider.value = playerStamina.CurrentStamina;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CẬP NHẬT UI
    // ═══════════════════════════════════════════════════════════════
    void RefreshUI()
    {
        if (pointsText != null)
            pointsText.text = $"ĐIỂM: {currentPoints}";

        foreach (var slot in allSlots)
        {
            if (slot.config.IsMaxed())
            {
                slot.text.text = "MAX";
                slot.text.color = new Color(1f, 0.85f, 0.2f);
                slot.button.interactable = false;
            }
            else
            {
                int cost = slot.config.GetCost();
                bool canAfford = currentPoints >= cost;
                slot.text.text = canAfford ? $"${cost}" : $"<color=#666>${cost}</color>";
                slot.text.color = canAfford ? new Color(0.4f, 1f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
                slot.button.interactable = canAfford;
            }

            // Cập nhật level blocks
            if (slot.levelBlocks != null)
            {
                for (int i = 0; i < slot.levelBlocks.Length; i++)
                {
                    if (slot.levelBlocks[i] == null) continue;
                    if (i < slot.config.currentLevel)
                        slot.levelBlocks[i].color = slot.accentColor; // Đã nâng cấp
                    else
                        slot.levelBlocks[i].color = new Color(0.25f, 0.25f, 0.3f, 1f); // Chưa nâng
                }
            }
        }

        RefreshGrenadeButton();
    }

    // ═══════════════════════════════════════════════════════════════
    // MUA LỰU ĐẠN
    // ═══════════════════════════════════════════════════════════════
    void BuyGrenade()
    {
        if (grenadeSystem == null) return;
        if (currentPoints < grenadePrice)
        {
            PlaySound(errorSound);
            return;
        }

        currentPoints -= grenadePrice;
        grenadeSystem.PickupGrenade();
        PlaySound(upgradeSound);
        RefreshUI();
    }

    void RefreshGrenadeButton()
    {
        if (grenadeButton == null || grenadeButtonText == null) return;

        bool canAfford = currentPoints >= grenadePrice;
        grenadeButton.interactable = canAfford;

        if (canAfford)
            grenadeButtonText.text = $"MUA LỰU ĐẠN (${grenadePrice})";
        else
            grenadeButtonText.text = $"<color=#666>MUA LỰU ĐẠN (${grenadePrice})</color>";
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
    }
}
