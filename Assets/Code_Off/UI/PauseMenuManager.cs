using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; } = false;

    [Header("--- THAM CHIẾU (để trống = tự tạo) ---")]
    public Canvas pauseCanvas;

    // === NỘI BỘ ===
    private GameObject panel;
    private bool wasCreated = false;

    void Awake()
    {
        IsPaused = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    void Pause()
    {
        // Không pause khi đã game over hoặc đã thắng
        if (Time.timeScale == 0f) return;

        IsPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panel == null)
            BuildUI();

        panel.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (panel != null)
            panel.SetActive(false);

        // Chặn bắn khi đóng menu
        PlayerShooting.BlockInputBriefly();
    }

    public void RestartGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        DifficultyManager.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game!");
        Application.Quit();
    }

    // ═══════════════════════════════════════════════════════════════
    // TẠO UI BẰNG CODE
    // ═══════════════════════════════════════════════════════════════
    void BuildUI()
    {
        // Canvas
        if (pauseCanvas == null)
        {
            GameObject canvasObj = new GameObject("PauseCanvas");
            canvasObj.transform.SetParent(transform);
            pauseCanvas = canvasObj.AddComponent<Canvas>();
            pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.sortingOrder = 200;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Panel nền tối
        panel = CreatePanel(pauseCanvas.transform);

        // Tiêu đề
        CreateText(panel.transform, "TẠM DỪNG", 72, Color.white,
            new Vector2(0, 250), new Vector2(600, 100), FontStyles.Bold);

        // Tên độ khó hiện tại
        string diffName = DifficultyManager.GetName();
        Color diffColor = DifficultyManager.GetColor();
        CreateText(panel.transform, $"Độ khó: {diffName}", 32, diffColor,
            new Vector2(0, 180), new Vector2(500, 50), FontStyles.Normal);

        // Nút TIẾP TỤC
        CreateButton(panel.transform, "TIẾP TỤC", new Color(0.2f, 0.7f, 0.3f),
            new Vector2(0, 80), new Vector2(400, 70), Resume);

        // Nút CHƠI LẠI
        CreateButton(panel.transform, "CHƠI LẠI", new Color(0.2f, 0.5f, 0.8f),
            new Vector2(0, -10), new Vector2(400, 70), RestartGame);

        // Nút VỀ MENU
        CreateButton(panel.transform, "VỀ MENU", new Color(0.8f, 0.6f, 0.1f),
            new Vector2(0, -100), new Vector2(400, 70), BackToMenu);

        // Nút THOÁT GAME
        CreateButton(panel.transform, "THOÁT GAME", new Color(0.8f, 0.15f, 0.15f),
            new Vector2(0, -190), new Vector2(400, 70), QuitGame);

        wasCreated = true;
    }

    // ─── HELPER TẠO UI ───

    GameObject CreatePanel(Transform parent)
    {
        GameObject obj = new GameObject("PausePanel");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.85f);

        return obj;
    }

    TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Color color,
        Vector2 pos, Vector2 size, FontStyles style)
    {
        GameObject obj = new GameObject("Text_" + text);
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

        return tmp;
    }

    void CreateButton(Transform parent, string label, Color bgColor,
        Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject("Btn_" + label);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        // Text trên nút
        CreateText(obj.transform, label, 36, Color.white,
            Vector2.zero, size, FontStyles.Bold);
    }
}
