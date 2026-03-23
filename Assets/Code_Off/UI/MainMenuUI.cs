using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu chính hiện khi bắt đầu game.
/// Gắn vào Empty GameObject (cùng chỗ với DifficultySelectUI).
/// Script này chạy TRƯỚC DifficultySelectUI (dùng Script Execution Order hoặc đặt priority thấp).
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("═══ CANVAS ═══")]
    public Canvas mainCanvas;

    [Header("═══ GAME TITLE ═══")]
    public string gameTitle = "ZOMBIE SHOOT";
    public string gameSubtitle = "Survive or Die Trying";

    private GameObject panelRoot;
    private bool menuClosed = false;

    public static bool MenuDone { get; private set; } = false;

    void Awake()
    {
        // Reset khi scene load lại
        MenuDone = false;
    }

    void Start()
    {
        // Nếu đã chọn độ khó rồi (restart scene) → bỏ qua menu, chơi lại luôn
        if (DifficultyManager.HasSelected)
        {
            Destroy(gameObject);
            return;
        }

        // Tắt DifficultySelectUI cho đến khi menu đóng
        DifficultySelectUI diffUI = FindObjectOfType<DifficultySelectUI>();
        if (diffUI != null)
            diffUI.enabled = false;

        BuildUI();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!menuClosed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
    }

    void OnPlay()
    {
        menuClosed = true;
        MenuDone = true;

        if (panelRoot != null) Destroy(panelRoot);

        // Bật DifficultySelectUI
        DifficultySelectUI diffUI = FindObjectOfType<DifficultySelectUI>(true);
        if (diffUI != null)
        {
            diffUI.enabled = true;
            diffUI.gameObject.SetActive(true);
        }
        else
        {
            // Không có DifficultySelectUI → bắt đầu luôn
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            PlayerShooting.BlockInputBriefly();
        }

        Destroy(gameObject);
    }

    private GameObject guidePopup;

    void OnGuide()
    {
        if (guidePopup != null)
        {
            Destroy(guidePopup);
            guidePopup = null;
            return;
        }

        // Tạo popup hướng dẫn nhanh
        guidePopup = new GameObject("GuidePopup", typeof(RectTransform), typeof(Image));
        guidePopup.transform.SetParent(panelRoot.transform, false);
        guidePopup.transform.SetAsLastSibling();
        RectTransform popRect = guidePopup.GetComponent<RectTransform>();
        popRect.anchorMin = new Vector2(0.1f, 0.05f);
        popRect.anchorMax = new Vector2(0.9f, 0.95f);
        popRect.offsetMin = Vector2.zero;
        popRect.offsetMax = Vector2.zero;
        guidePopup.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        Outline ol = guidePopup.AddComponent<Outline>();
        ol.effectColor = new Color(0.9f, 0.15f, 0.1f, 0.5f);
        ol.effectDistance = new Vector2(2, 2);

        // Title
        CreateText("HƯỚNG DẪN CHƠI", guidePopup.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30),
            new Vector2(500, 50), 36, new Color(0.9f, 0.15f, 0.1f), FontStyles.Bold,
            TextAlignmentOptions.Center);

        // Content
        string guide =
            "<color=#FFD700>DI CHUYỂN</color>\n" +
            "  W A S D  —  Di chuyển      |  Shift  —  Chạy nhanh\n" +
            "  Space  —  Nhảy                |  C + WASD  —  Lộn tránh\n\n" +
            "<color=#FFD700>CHIẾN ĐẤU</color>\n" +
            "  Chuột trái  —  Bắn            |  R  —  Nạp đạn\n" +
            "  Chuột phải  —  Đánh dao    |  G  —  Ném lựu đạn\n" +
            "  Scroll / 1,2  —  Đổi súng   |  F  —  Đèn pin\n\n" +
            "<color=#FFD700>HỆ THỐNG</color>\n" +
            "  Tab  —  Mở túi đồ            |  B  —  Nâng cấp vũ khí\n" +
            "  H  —  Hướng dẫn (in-game) |  E  —  Nhặt đồ / Mở rương\n" +
            "  1,2,3  —  Dùng Skill Card\n\n" +
            "<color=#FFD700>MẸO CHƠI</color>\n" +
            "  - Bắn trúng đầu (Headshot) gây <color=#FF5555>x2 sát thương</color>\n" +
            "  - Giết zombie nhận <color=#4CAF50>điểm nâng cấp</color> → Ấn B mua upgrade\n" +
            "  - Mỗi wave kết thúc sẽ spawn rương chứa đồ\n" +
            "  - Boss xuất hiện ở wave cuối mỗi phase\n" +
            "  - Lựu đạn cũng gây damage lên Player — cẩn thận!";

        TextMeshProUGUI contentTmp = CreateText(guide, guidePopup.transform,
            new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.88f), Vector2.zero,
            Vector2.zero, 22, Color.white, FontStyles.Normal,
            TextAlignmentOptions.TopLeft);
        contentTmp.enableAutoSizing = true;
        contentTmp.fontSizeMin = 16;
        contentTmp.fontSizeMax = 24;

        // Nút Đóng
        GameObject btnClose = new GameObject("BtnClose", typeof(RectTransform), typeof(Image));
        btnClose.transform.SetParent(guidePopup.transform, false);
        RectTransform closeRect = btnClose.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.35f, 0.02f);
        closeRect.anchorMax = new Vector2(0.65f, 0.1f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        btnClose.GetComponent<Image>().color = new Color(0.8f, 0.15f, 0.1f);

        Button closeBtn = btnClose.AddComponent<Button>();
        ColorBlock ccb = closeBtn.colors;
        ccb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        ccb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        closeBtn.colors = ccb;
        closeBtn.onClick.AddListener(() => { Destroy(guidePopup); guidePopup = null; });

        CreateText("ĐÓNG", btnClose.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, 26, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
    }

    void OnQuit()
    {
        Debug.Log("Quit Game!");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void BuildUI()
    {
        if (mainCanvas == null)
            mainCanvas = FindObjectOfType<Canvas>();

        if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
            mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ═══ FULLSCREEN PANEL ═══
        panelRoot = new GameObject("MainMenuPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(mainCanvas.transform, false);
        panelRoot.transform.SetAsLastSibling();
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.97f);

        // ═══ VIGNETTE OVERLAY (aesthetic) ═══
        GameObject vignette = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
        vignette.transform.SetParent(panelRoot.transform, false);
        RectTransform vigRect = vignette.GetComponent<RectTransform>();
        vigRect.anchorMin = Vector2.zero;
        vigRect.anchorMax = Vector2.one;
        vigRect.offsetMin = Vector2.zero;
        vigRect.offsetMax = Vector2.zero;
        Image vigImg = vignette.GetComponent<Image>();
        vigImg.color = new Color(0.05f, 0, 0, 0.3f);
        vigImg.raycastTarget = false;

        // ═══ TITLE ═══
        TextMeshProUGUI titleTmp = CreateText(gameTitle, panelRoot.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120),
            new Vector2(900, 120), 82, new Color(0.9f, 0.15f, 0.1f), FontStyles.Bold,
            TextAlignmentOptions.Center);

        // Glow effect cho title
        Outline titleOutline = titleTmp.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(1f, 0.1f, 0.05f, 0.4f);
        titleOutline.effectDistance = new Vector2(3, 3);

        // Subtitle
        CreateText(gameSubtitle, panelRoot.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -200),
            new Vector2(700, 45), 24, new Color(0.6f, 0.6f, 0.6f), FontStyles.Italic,
            TextAlignmentOptions.Center);

        // ═══ SEPARATOR LINE ═══
        GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(panelRoot.transform, false);
        RectTransform lineRect = line.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.35f, 0.58f);
        lineRect.anchorMax = new Vector2(0.65f, 0.58f);
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = new Vector2(0, 2);
        line.GetComponent<Image>().color = new Color(0.9f, 0.15f, 0.1f, 0.5f);

        // ═══ BUTTONS ═══
        float btnY = 0.48f;
        float btnH = 0.07f;
        float btnGap = 0.02f;

        // CHƠI
        CreateMenuButton("CHƠI GAME", panelRoot.transform,
            btnY, btnH,
            new Color(0.8f, 0.15f, 0.1f), new Color(1f, 0.25f, 0.15f),
            36, OnPlay);

        // HƯỚNG DẪN
        CreateMenuButton("HƯỚNG DẪN", panelRoot.transform,
            btnY - btnH - btnGap, btnH,
            new Color(0.15f, 0.15f, 0.2f), new Color(0.25f, 0.25f, 0.35f),
            28, OnGuide);

        // THOÁT
        CreateMenuButton("THOÁT", panelRoot.transform,
            btnY - (btnH + btnGap) * 2, btnH,
            new Color(0.15f, 0.15f, 0.2f), new Color(0.25f, 0.25f, 0.35f),
            28, OnQuit);

        // ═══ BOTTOM INFO ═══
        CreateText("WASD - Di chuyển  |  Mouse - Nhắm bắn  |  H - Hướng dẫn", panelRoot.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60),
            new Vector2(800, 30), 16, new Color(0.4f, 0.4f, 0.4f), FontStyles.Normal,
            TextAlignmentOptions.Center);

        CreateText("v1.0  |  Unity 6  |  HDRP", panelRoot.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30),
            new Vector2(400, 25), 14, new Color(0.3f, 0.3f, 0.3f), FontStyles.Normal,
            TextAlignmentOptions.Center);
    }

    void CreateMenuButton(string text, Transform parent, float yPos, float height,
        Color normalColor, Color hoverColor, int fontSize,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + text, typeof(RectTransform), typeof(Image));
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, yPos);
        btnRect.anchorMax = new Vector2(0.65f, yPos + height);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = normalColor;

        // Border
        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(hoverColor.r, hoverColor.g, hoverColor.b, 0.5f);
        btnOutline.effectDistance = new Vector2(1, 1);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(hoverColor.r / normalColor.r,
            hoverColor.g / Mathf.Max(normalColor.g, 0.01f),
            hoverColor.b / Mathf.Max(normalColor.b, 0.01f));
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        cb.selectedColor = Color.white;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        // Text
        CreateText(text, btnObj.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, fontSize, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
    }

    // ═══════════════════════════════════════════════════════════════
    // UI HELPERS
    // ═══════════════════════════════════════════════════════════════
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
