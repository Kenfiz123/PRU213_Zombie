using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Hiện chữ "HEADSHOT!" bay lên giữa màn hình khi bắn trúng đầu.
/// Gắn vào Canvas. Tự tạo UI bằng code.
/// </summary>
public class HeadshotUI : MonoBehaviour
{
    public static HeadshotUI Instance { get; private set; }

    [Header("--- CẤU HÌNH ---")]
    public float displayDuration = 0.8f;
    public float floatSpeed = 80f;
    public int fontSize = 42;
    public Color headshotColor = new Color(1f, 0.2f, 0.2f, 1f); // Đỏ

    private GameObject textObj;
    private TextMeshProUGUI tmpText;
    private CanvasGroup canvasGroup;
    private Coroutine activeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        CreateUI();
    }

    void CreateUI()
    {
        // Container
        textObj = new GameObject("HeadshotText");
        textObj.transform.SetParent(transform, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 50f); // Hơi trên giữa màn hình
        rt.sizeDelta = new Vector2(400, 60);

        // CanvasGroup để fade
        canvasGroup = textObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // TextMeshPro
        tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "HEADSHOT!";
        tmpText.fontSize = fontSize;
        tmpText.color = headshotColor;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.enableWordWrapping = false;

        // Outline
        tmpText.outlineWidth = 0.2f;
        tmpText.outlineColor = Color.black;

        textObj.SetActive(false);
    }

    /// <summary>
    /// Gọi khi headshot để hiện chữ bay lên
    /// </summary>
    public void ShowHeadshot()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(HeadshotAnimation());
    }

    /// <summary>
    /// Hiện chữ headshot kèm số damage
    /// </summary>
    public void ShowHeadshot(float damage)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        tmpText.text = $"HEADSHOT! ({damage:F0})";
        activeRoutine = StartCoroutine(HeadshotAnimation());
    }

    IEnumerator HeadshotAnimation()
    {
        textObj.SetActive(true);
        RectTransform rt = textObj.GetComponent<RectTransform>();
        Vector2 startPos = new Vector2(0, 50f);
        rt.anchoredPosition = startPos;
        canvasGroup.alpha = 1f;

        // Scale punch
        rt.localScale = Vector3.one * 1.5f;

        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / displayDuration;

            // Bay lên
            rt.anchoredPosition = startPos + Vector2.up * floatSpeed * t;

            // Scale từ 1.5 → 1.0
            float scale = Mathf.Lerp(1.5f, 1.0f, Mathf.Min(t * 3f, 1f));
            rt.localScale = Vector3.one * scale;

            // Fade out nửa sau
            if (t > 0.5f)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f);
            }

            yield return null;
        }

        canvasGroup.alpha = 0f;
        textObj.SetActive(false);
        activeRoutine = null;
    }
}
