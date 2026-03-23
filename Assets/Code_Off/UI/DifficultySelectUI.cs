using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Màn hình chọn độ khó hiện khi bắt đầu game.
/// Pause game cho đến khi người chơi chọn.
/// Gắn vào Empty GameObject.
/// </summary>
public class DifficultySelectUI : MonoBehaviour
{
    [Header("═══ CANVAS ═══")]
    public Canvas mainCanvas;

    [Header("═══ ẢNH CHẾ GIỄU (Chế độ Dễ) ═══")]
    [Tooltip("Ảnh hiện khi chọn chế độ Dễ — 'Bạn sợ à?'")]
    public Sprite easyMockImage;

    private GameObject panelRoot;
    private GameObject mockPanel;
    private bool hasSelected = false;

    void Start()
    {
        // Nếu đã chọn rồi (restart scene) → không hiện lại
        if (DifficultyManager.HasSelected)
        {
            Destroy(gameObject);
            return;
        }

        // Nếu có MainMenuUI → đợi menu đóng (MainMenuUI sẽ bật lại script này)
        if (FindObjectOfType<MainMenuUI>() != null)
        {
            enabled = false;
            return;
        }

        ShowDifficultySelect();
    }

    void OnEnable()
    {
        // Được bật lại bởi MainMenuUI sau khi menu đóng
        if (panelRoot == null && !hasSelected && !DifficultyManager.HasSelected)
        {
            ShowDifficultySelect();
        }
    }

    void ShowDifficultySelect()
    {
        BuildUI();

        // Pause game
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Giữ cursor mở cho đến khi chọn xong (phòng script khác lock cursor)
        if (!hasSelected)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
    }

    void SelectDifficulty(DifficultyManager.Difficulty diff)
    {
        if (hasSelected) return;

        // Chế độ Dễ → hiện popup chế giễu
        if (diff == DifficultyManager.Difficulty.Easy)
        {
            ShowMockPopup();
            return;
        }

        ConfirmSelection(diff);
    }

    void ConfirmSelection(DifficultyManager.Difficulty diff)
    {
        hasSelected = true;

        DifficultyManager.SetDifficulty(diff);

        // Resume game
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerShooting.BlockInputBriefly();

        if (mockPanel != null) Destroy(mockPanel);
        Destroy(panelRoot);
        Destroy(gameObject);
    }

    void ShowMockPopup()
    {
        if (mockPanel != null) return;

        // Ẩn panel chọn độ khó
        panelRoot.SetActive(false);

        // ═══ POPUP NỀN ═══
        mockPanel = new GameObject("MockPanel", typeof(RectTransform), typeof(Image));
        mockPanel.transform.SetParent(mainCanvas.transform, false);
        mockPanel.transform.SetAsLastSibling();
        RectTransform mockRect = mockPanel.GetComponent<RectTransform>();
        mockRect.anchorMin = Vector2.zero;
        mockRect.anchorMax = Vector2.one;
        mockRect.offsetMin = Vector2.zero;
        mockRect.offsetMax = Vector2.zero;
        mockPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.95f);

        // ═══ CONTAINER ═══
        GameObject container = CreatePanel("MockContainer", mockPanel.transform);
        RectTransform contRect = container.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.2f, 0.08f);
        contRect.anchorMax = new Vector2(0.8f, 0.92f);
        contRect.offsetMin = Vector2.zero;
        contRect.offsetMax = Vector2.zero;
        container.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.98f);
        Outline outline = container.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.85f, 0.4f, 0.6f);
        outline.effectDistance = new Vector2(2, 2);

        // ═══ ẢNH ═══
        GameObject imgObj = new GameObject("MockImage", typeof(RectTransform), typeof(Image));
        imgObj.transform.SetParent(container.transform, false);
        Image img = imgObj.GetComponent<Image>();
        RectTransform imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.15f, 0.25f);
        imgRect.anchorMax = new Vector2(0.85f, 0.95f);
        imgRect.offsetMin = Vector2.zero;
        imgRect.offsetMax = Vector2.zero;
        img.preserveAspect = true;
        img.raycastTarget = false;

        if (easyMockImage != null)
        {
            img.sprite = easyMockImage;
            img.color = Color.white;
        }
        else
        {
            // Fallback: text thay ảnh
            img.color = new Color(0.15f, 0.15f, 0.15f);
            CreateText("BẠN SỢ À?", imgObj.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(400, 80), 48, new Color(1f, 0.85f, 0.2f), FontStyles.Bold,
                TextAlignmentOptions.Center);
        }

        // ═══ 2 NÚT ═══
        // Nút "Ừ, tôi sợ" → chơi Easy
        GameObject btnYes = CreatePanel("Btn_Yes", container.transform);
        RectTransform yesRect = btnYes.GetComponent<RectTransform>();
        yesRect.anchorMin = new Vector2(0.08f, 0.05f);
        yesRect.anchorMax = new Vector2(0.48f, 0.18f);
        yesRect.offsetMin = Vector2.zero;
        yesRect.offsetMax = Vector2.zero;
        btnYes.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.25f);

        Button yesBtn = btnYes.AddComponent<Button>();
        ColorBlock ycb = yesBtn.colors;
        ycb.highlightedColor = new Color(0.3f, 0.7f, 0.35f);
        ycb.pressedColor = new Color(0.15f, 0.35f, 0.18f);
        yesBtn.colors = ycb;
        yesBtn.onClick.AddListener(() => ConfirmSelection(DifficultyManager.Difficulty.Easy));

        CreateText("Ừ, tôi sợ", btnYes.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, 24, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);

        // Nút "Không" → quay lại chọn
        GameObject btnNo = CreatePanel("Btn_No", container.transform);
        RectTransform noRect = btnNo.GetComponent<RectTransform>();
        noRect.anchorMin = new Vector2(0.52f, 0.05f);
        noRect.anchorMax = new Vector2(0.92f, 0.18f);
        noRect.offsetMin = Vector2.zero;
        noRect.offsetMax = Vector2.zero;
        btnNo.GetComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f);

        Button noBtn = btnNo.AddComponent<Button>();
        ColorBlock ncb = noBtn.colors;
        ncb.highlightedColor = new Color(0.85f, 0.2f, 0.2f);
        ncb.pressedColor = new Color(0.4f, 0.1f, 0.1f);
        noBtn.colors = ncb;
        noBtn.onClick.AddListener(() => CloseMockPopup());

        CreateText("Không, tôi không sợ", btnNo.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, 22, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
    }

    void CloseMockPopup()
    {
        if (mockPanel != null)
        {
            Destroy(mockPanel);
            mockPanel = null;
        }
        panelRoot.SetActive(true);
    }

    void BuildUI()
    {
        if (mainCanvas == null)
            mainCanvas = FindObjectOfType<Canvas>();

        // Đảm bảo Canvas có đủ components để nhận click
        if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
            mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ═══ PANEL NỀN ═══
        panelRoot = new GameObject("DifficultyPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(mainCanvas.transform, false);
        // Đặt lên trên cùng
        panelRoot.transform.SetAsLastSibling();
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.92f);

        // ═══ CONTAINER ═══
        GameObject container = CreatePanel("Container", panelRoot.transform);
        RectTransform contRect = container.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.15f, 0.1f);
        contRect.anchorMax = new Vector2(0.85f, 0.9f);
        contRect.offsetMin = Vector2.zero;
        contRect.offsetMax = Vector2.zero;
        container.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        Outline outline = container.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.6f, 1f, 0.6f);
        outline.effectDistance = new Vector2(2, 2);

        // ═══ TIÊU ĐỀ ═══
        CreateText("CHỌN ĐỘ KHÓ", container.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40),
            new Vector2(700, 60), 44, new Color(0.4f, 0.8f, 1f), FontStyles.Bold,
            TextAlignmentOptions.Center);

        CreateText("Độ khó ảnh hưởng đến sức mạnh zombie, điểm thưởng và tài nguyên",
            container.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -85),
            new Vector2(800, 35), 20, new Color(0.55f, 0.55f, 0.55f), FontStyles.Italic,
            TextAlignmentOptions.Center);

        // ═══ 4 CỘT: EASY / NORMAL / HARD / ASIAN ═══
        float colWidth = 0.22f;
        float gap = 0.013f;
        float totalWidth = colWidth * 4 + gap * 3;
        float startX = 0.5f - totalWidth / 2f;

        // --- DỄ ---
        CreateDifficultyCard(container.transform, startX, colWidth,
            "DỄ", new Color(0.3f, 0.85f, 0.4f),
            "Zombie yếu hơn\nNhiều rương hơn\nĐiểm x1.5",
            "Zombie HP  <color=#4CAF50>-30%</color>\n" +
            "Zombie DMG <color=#4CAF50>-40%</color>\n" +
            "Số zombie  <color=#4CAF50>-30%</color>\n" +
            "Player DMG <color=#4CAF50>+30%</color>\n" +
            "Boss HP    <color=#4CAF50>-40%</color>",
            DifficultyManager.Difficulty.Easy);

        // --- BÌNH THƯỜNG ---
        CreateDifficultyCard(container.transform, startX + (colWidth + gap), colWidth,
            "BÌNH THƯỜNG", new Color(1f, 0.85f, 0.2f),
            "Trải nghiệm chuẩn\nCân bằng",
            "Zombie HP  <color=#FFF>x1.0</color>\n" +
            "Zombie DMG <color=#FFF>x1.0</color>\n" +
            "Số zombie  <color=#FFF>x1.0</color>\n" +
            "Player DMG <color=#FFF>x1.0</color>\n" +
            "Boss HP    <color=#FFF>x1.0</color>",
            DifficultyManager.Difficulty.Normal);

        // --- KHÓ ---
        CreateDifficultyCard(container.transform, startX + (colWidth + gap) * 2, colWidth,
            "KHÓ", new Color(1f, 0.2f, 0.15f),
            "Zombie mạnh hơn\nÍt rương hơn\nĐiểm x2",
            "Zombie HP  <color=#F44336>+50%</color>\n" +
            "Zombie DMG <color=#F44336>+50%</color>\n" +
            "Số zombie  <color=#F44336>+40%</color>\n" +
            "Player DMG <color=#F44336>-20%</color>\n" +
            "Boss HP    <color=#F44336>+80%</color>",
            DifficultyManager.Difficulty.Hard);

        // --- ASIAN ---
        CreateDifficultyCard(container.transform, startX + (colWidth + gap) * 3, colWidth,
            "ASIAN", new Color(0.8f, 0.05f, 0.9f),
            "Chỉ dành cho\nnhững người\nkhông sợ chết",
            "Zombie HP  <color=#E040FB>x10</color>\n" +
            "Zombie DMG <color=#E040FB>x10</color>\n" +
            "Zombie SPD <color=#E040FB>x3</color>\n" +
            "Số zombie  <color=#E040FB>x5</color>\n" +
            "Player DMG <color=#E040FB>-70%</color>\n" +
            "Boss HP    <color=#E040FB>x10</color>\n" +
            "<color=#FFD700>Điểm       x10</color>",
            DifficultyManager.Difficulty.Asian);
    }

    void CreateDifficultyCard(Transform parent, float xStart, float width,
        string title, Color titleColor, string desc, string stats,
        DifficultyManager.Difficulty diff)
    {
        // Card background
        GameObject card = CreatePanel("Card_" + title, parent);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(xStart, 0.06f);
        cardRect.anchorMax = new Vector2(xStart + width, 0.82f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        card.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(titleColor.r, titleColor.g, titleColor.b, 0.5f);
        cardOutline.effectDistance = new Vector2(2, 2);

        // Title
        CreateText(title, card.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25),
            new Vector2(300, 50), 34, titleColor, FontStyles.Bold,
            TextAlignmentOptions.Center);

        // Description
        CreateText(desc, card.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -75),
            new Vector2(280, 80), 18, new Color(0.75f, 0.75f, 0.75f), FontStyles.Normal,
            TextAlignmentOptions.Center);

        // Stats
        TextMeshProUGUI statsTmp = CreateText(stats, card.transform,
            new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.58f), Vector2.zero,
            Vector2.zero, 18, Color.white, FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        statsTmp.enableAutoSizing = true;
        statsTmp.fontSizeMin = 14;
        statsTmp.fontSizeMax = 22;

        // Button
        GameObject btnObj = CreatePanel("Btn_" + title, card.transform);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.08f, 0.03f);
        btnRect.anchorMax = new Vector2(0.92f, 0.16f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Color btnColor = new Color(titleColor.r * 0.6f, titleColor.g * 0.6f, titleColor.b * 0.6f);
        btnObj.GetComponent<Image>().color = btnColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = titleColor;
        cb.pressedColor = btnColor * 0.6f;
        btn.colors = cb;
        btn.onClick.AddListener(() => SelectDifficulty(diff));

        CreateText("CHỌN", btnObj.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, 24, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
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
        Vector2 sizeDelta, int fontSize, Color color, FontStyles style,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject("Txt", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.richText = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        return tmp;
    }
}
