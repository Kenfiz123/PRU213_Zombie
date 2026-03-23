using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiện hướng dẫn toàn bộ phím và gameplay. Ấn H để mở/đóng.
/// Gắn vào Empty GameObject. UI tự tạo bằng code.
/// </summary>
public class GameGuideManager : MonoBehaviour
{
    [Header("═══ CANVAS ═══")]
    public Canvas mainCanvas;

    private GameObject panelRoot;
    private bool isOpen = false;
    private float lastToggleTime = -1f;
    private int currentPage = 0;
    private TextMeshProUGUI contentText;
    private TextMeshProUGUI pageIndicator;
    private Button btnPrev, btnNext;

    private string[] pages;

    void Start()
    {
        BuildPages();
        BuildUI();
        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
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

        // Mũi tên trái phải chuyển trang
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                ChangePage(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                ChangePage(1);
        }
    }

    void BuildPages()
    {
        pages = new string[]
        {
            // === TRANG 1: ĐIỀU KHIỂN CƠ BẢN ===
            "<color=#FFD700><size=28>ĐIỀU KHIỂN CƠ BẢN</size></color>\n\n" +
            "<color=#4FC3F7>DI CHUYỂN</color>\n" +
            "  <color=#FFF>W A S D</color>  —  Đi lên / trái / xuống / phải\n" +
            "  <color=#FFF>Chuột</color>      —  Nhìn xung quanh\n" +
            "  <color=#FFF>Space</color>      —  Nhảy\n" +
            "  <color=#FFF>Shift</color>       —  Chạy nhanh (tốn Stamina)\n" +
            "  <color=#FFF>C + WASD</color> —  Dash/Lướt (cooldown 3s)\n\n" +
            "<color=#4FC3F7>TƯƠNG TÁC</color>\n" +
            "  <color=#FFF>E</color>  —  Mở rương / Nhặt vật phẩm / Dùng Skill Card\n" +
            "  <color=#FFF>R</color>  —  Nạp đạn\n" +
            "  <color=#FFF>F</color>  —  Bật/Tắt đèn pin\n" +
            "  <color=#FFF>Tab</color>  —  Mở kho đồ (Inventory)\n",

            // === TRANG 2: CHIẾN ĐẤU ===
            "<color=#FFD700><size=28>CHIẾN ĐẤU</size></color>\n\n" +
            "<color=#FF5252>SÚNG</color>\n" +
            "  <color=#FFF>Chuột trái</color>       —  Bắn\n" +
            "  <color=#FFF>Scroll chuột</color>  —  Đổi vũ khí (Pistol / AK)\n" +
            "  <color=#FFF>R</color>                    —  Nạp đạn\n" +
            "  <color=#FFEB3B>Headshot</color>: Bắn trúng đầu → <color=#FFF>x2 damage!</color>\n\n" +
            "<color=#FF5252>LỰU ĐẠN</color>\n" +
            "  <color=#FFF>G</color>  —  Ném lựu đạn (mua bằng điểm nâng cấp)\n" +
            "  Nổ sau 3s, gây damage theo khoảng cách\n\n" +
            "<color=#FF5252>DAO</color>\n" +
            "  <color=#FFF>Chuột trái</color>   —  Chém nhanh (20 dmg)\n" +
            "  <color=#FFF>Chuột phải</color>  —  Chém mạnh (50 dmg)\n\n" +
            "<color=#FF5252>SKILL CARD</color>\n" +
            "  <color=#FFF>1  2  3</color>  —  Chọn ô skill (Hotbar dưới màn hình)\n" +
            "  <color=#FFF>E</color>         —  Kích hoạt skill đã chọn\n",

            // === TRANG 3: HỆ THỐNG ===
            "<color=#FFD700><size=28>HỆ THỐNG GAME</size></color>\n\n" +
            "<color=#4FC3F7>PHÍM TẮT</color>\n" +
            "  <color=#FFF>B</color>  —  Mở bảng Nâng Cấp Vũ Khí\n" +
            "  <color=#FFF>G</color>  —  Ném lựu đạn\n" +
            "  <color=#FFF>H</color>  —  Mở/Đóng hướng dẫn này\n" +
            "  <color=#FFF>Tab</color>  —  Mở kho đồ\n" +
            "  <color=#FFF>Esc</color>  —  Đóng panel đang mở\n\n" +
            "<color=#4FC3F7>NÂNG CẤP (ấn B)</color>\n" +
            "  Giết zombie → kiếm điểm\n" +
            "  Zombie thường: <color=#4CAF50>10 điểm</color>\n" +
            "  Zombie đặc biệt: <color=#FF9800>20 điểm</color>\n" +
            "  Boss: <color=#F44336>100 điểm</color>\n" +
            "  Nâng cấp: DMG, tốc bắn, băng đạn, nạp đạn,\n" +
            "  máu, giáp, stamina, <color=#FFEB3B>mua lựu đạn</color>\n",

            // === TRANG 4: GAMEPLAY ===
            "<color=#FFD700><size=28>GAMEPLAY</size></color>\n\n" +
            "<color=#FF9800>CẤU TRÚC GAME</color>\n" +
            "  Game có <color=#FFF>3 Phase</color>, mỗi phase nhiều wave zombie\n" +
            "  Zombie mạnh dần qua mỗi phase\n" +
            "  Hết 3 phase → <color=#F44336>BOSS xuất hiện!</color>\n" +
            "  <color=#FF5252>Mặt trăng máu</color> xuất hiện khi Boss spawn\n" +
            "  Nhạc nền thay đổi khi Boss xuất hiện\n" +
            "  Giết Boss → <color=#4CAF50>CHIẾN THẮNG!</color>\n\n" +
            "<color=#FF9800>RƯƠNG & VẬT PHẨM</color>\n" +
            "  Rương spawn ngẫu nhiên trên map\n" +
            "  Mở rương (E) → nhận 1 trong:\n" +
            "    • Medkit / Armor Plate\n" +
            "    • Đạn Pistol / Đạn AK\n" +
            "    • Skill Card (Heal / Armor / Summon)\n",

            // === TRANG 5: KỸ NĂNG & MẸO ===
            "<color=#FFD700><size=28>KỸ NĂNG & MẸO</size></color>\n\n" +
            "<color=#AB47BC>5 SKILL CARD</color>\n" +
            "  <color=#4CAF50>Heal</color>         —  Hồi full máu\n" +
            "  <color=#42A5F5>Armor</color>       —  Hồi full giáp\n" +
            "  <color=#FF7043>Summon</color>     —  Triệu hồi đồng minh\n" +
            "  <color=#CE93D8>MagicZone</color> —  Mưa phép gây sát thương vùng\n" +
            "  <color=#FFD54F>PowerBuff</color>  —  x2 DMG + reload nhanh 10s\n\n" +
            "<color=#AB47BC>MẸO CHƠI</color>\n" +
            "  • Dùng Dash (C) để né đòn zombie nổ\n" +
            "  • Bật đèn pin (F) khi trời tối\n" +
            "  • Nhắm đầu zombie → Headshot x2 damage\n" +
            "  • Lựu đạn hiệu quả với nhóm zombie đông\n" +
            "  • Nâng cấp sớm để dễ thở phase sau\n" +
            "  • Giáp hấp thụ 80% sát thương\n" +
            "  • Ưu tiên nâng DMG súng + Máu trước\n",
        };
    }

    void BuildUI()
    {
        if (mainCanvas == null)
            mainCanvas = FindObjectOfType<Canvas>();

        // === Panel gốc ===
        panelRoot = CreatePanel("GuidePanel", mainCanvas.transform);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);

        // === Container ===
        GameObject container = CreatePanel("Container", panelRoot.transform);
        RectTransform contRect = container.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.1f, 0.06f);
        contRect.anchorMax = new Vector2(0.9f, 0.94f);
        contRect.offsetMin = Vector2.zero;
        contRect.offsetMax = Vector2.zero;
        container.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.13f, 0.98f);

        Outline outline = container.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.7f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        // === Tiêu đề ===
        CreateText("HƯỚNG DẪN CHƠI", container.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20),
            new Vector2(500, 45), 32, new Color(0.3f, 0.85f, 1f), FontStyles.Bold,
            TextAlignmentOptions.Center);

        // === Phím tắt nhỏ ===
        CreateText("[H] Đóng  |  [← →] Chuyển trang", container.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -55),
            new Vector2(500, 25), 14, new Color(0.5f, 0.5f, 0.5f), FontStyles.Italic,
            TextAlignmentOptions.Center);

        // === Nội dung chính ===
        GameObject contentArea = CreatePanel("ContentArea", container.transform);
        contentArea.GetComponent<Image>().color = new Color(0.07f, 0.07f, 0.09f, 0.9f);
        RectTransform caRect = contentArea.GetComponent<RectTransform>();
        caRect.anchorMin = new Vector2(0.03f, 0.1f);
        caRect.anchorMax = new Vector2(0.97f, 0.87f);
        caRect.offsetMin = Vector2.zero;
        caRect.offsetMax = Vector2.zero;

        contentText = CreateText("", contentArea.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, 22, Color.white, FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        RectTransform ctRect = contentText.GetComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0.04f, 0.02f);
        ctRect.anchorMax = new Vector2(0.96f, 0.98f);
        ctRect.offsetMin = Vector2.zero;
        ctRect.offsetMax = Vector2.zero;
        contentText.lineSpacing = 8f;
        contentText.richText = true;
        contentText.enableAutoSizing = true;
        contentText.fontSizeMin = 16;
        contentText.fontSizeMax = 28;

        // === Nút PREV ===
        GameObject prevObj = CreateButton("◄", container.transform,
            new Vector2(0.15f, 0f), new Vector2(0.15f, 0f), new Vector2(0, 25),
            100, 36, new Color(0.25f, 0.25f, 0.3f));
        btnPrev = prevObj.GetComponent<Button>();
        btnPrev.onClick.AddListener(() => ChangePage(-1));

        // === Page indicator ===
        pageIndicator = CreateText("1 / 5", container.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 25),
            new Vector2(150, 36), 18, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);

        // === Nút NEXT ===
        GameObject nextObj = CreateButton("►", container.transform,
            new Vector2(0.85f, 0f), new Vector2(0.85f, 0f), new Vector2(0, 25),
            100, 36, new Color(0.25f, 0.25f, 0.3f));
        btnNext = nextObj.GetComponent<Button>();
        btnNext.onClick.AddListener(() => ChangePage(1));

        // === Nút ĐÓNG ===
        GameObject closeObj = CreateButton("ĐÓNG [H]", container.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 70),
            140, 36, new Color(0.6f, 0.15f, 0.15f));
        closeObj.GetComponent<Button>().onClick.AddListener(() => TogglePanel());

        ShowPage(0);
    }

    void ChangePage(int dir)
    {
        currentPage = Mathf.Clamp(currentPage + dir, 0, pages.Length - 1);
        ShowPage(currentPage);
    }

    void ShowPage(int page)
    {
        currentPage = page;
        if (contentText != null)
            contentText.text = pages[page];
        if (pageIndicator != null)
            pageIndicator.text = $"{page + 1} / {pages.Length}";
        if (btnPrev != null)
            btnPrev.interactable = page > 0;
        if (btnNext != null)
            btnNext.interactable = page < pages.Length - 1;
    }

    public void TogglePanel()
    {
        if (isOpen)
        {
            isOpen = false;
            panelRoot.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            PlayerShooting.BlockInputBriefly();
        }
        else
        {
            isOpen = true;
            panelRoot.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowPage(currentPage);
        }
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
        cb.highlightedColor = bgColor * 1.4f;
        cb.pressedColor = bgColor * 0.7f;
        cb.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        btn.colors = cb;

        TextMeshProUGUI txt = CreateText(label, go.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, 16, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
        RectTransform txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return go;
    }
}
